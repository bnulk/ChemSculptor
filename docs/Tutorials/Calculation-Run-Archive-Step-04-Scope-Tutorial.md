# 全过程计算档案包教程 04：统一成果包范围契约

## 1. 本步骤解决什么问题

科学点成果包和全过程计算档案包保存的内容不同：

```text
科学点成果包
  以 ResultId 为边界
  保存最终科学结论和科学点集合

全过程计算档案包
  以 RootWorkflowId 为边界
  保存全部阶段和全部作业尝试
  同时完整保留科学点集合
```

如果两类包各自定义状态、目录和完整性规则，后续 API、客户端和归档代码
就会产生两套难以互操作的边界。第 4 步因此新增统一的
`ReportPackageScope`，但保留两类包不同的强制性规则。

本步骤只扩展契约和验证器：

```text
不读取 ScientificData
不读取作业目录
不复制文件
不生成 SHA-256
不组装压缩包
不修改 API 和 WinForms
```

## 2. 核心类型

新增文件：

```text
src/ChemSculptor.Reporting/ReportPackageScope.cs
```

范围类别：

```csharp
public enum ReportPackageScopeKind
{
    ScientificPointArtifact,
    CalculationRunArchive
}
```

计算结论状态：

```csharp
public enum ReportCalculationConclusionStatus
{
    Succeeded,
    PartiallySucceeded,
    Failed,
    Canceled,
    InProgress,
    Unknown
}
```

这两个枚举有意分开：

```text
ReportPackageScopeKind
  决定这个包是哪一种包

ReportCalculationConclusionStatus
  描述计算本身成功、失败还是仍在运行

ReportPackageStatus
  描述成果包是否完成组装

ReportEvidenceCompleteness
  描述生成报告所需证据是否完整
```

不能用一个 `Complete` 同时表达计算成功、证据完整和文件组装完成。

## 3. 创建两类范围

科学点成果包：

```csharp
ReportPackageScope scope =
    ReportPackageScope.CreateScientificPointArtifact(resultId);
```

生成的关键值：

```text
Kind = ScientificPointArtifact
ResultId = resultId
IncludeSciencePointSet = true
IncludeExecutionEvidence = false
IncludeExcludedArtifacts = false
```

全过程计算档案包：

```csharp
ReportPackageScope scope =
    ReportPackageScope.CreateCalculationRunArchive(
        rootWorkflowId,
        resultId);
```

生成的关键值：

```text
Kind = CalculationRunArchive
RootWorkflowId = rootWorkflowId
ResultId = resultId
IncludeSciencePointSet = true
IncludeExecutionEvidence = true
IncludeExcludedArtifacts = true
```

全过程档案的 `ResultId` 可以为空，因为后续允许按根工作流审计一个失败流程；
但只要能确定科学成果，就应写入并参与一致性校验。

## 4. 固定目录语义

默认目录：

```text
science
process
```

规则：

```text
目录相对于成果包根目录
禁止绝对路径
禁止反斜杠路径
禁止目录穿越
禁止空片段、尾部斜杠和重复斜杠
science 与 process 必须不同
```

这一步只校验逻辑目录，不创建真实目录。后续组装器负责把已校验的目录
映射到临时包目录，并再次检查最终真实路径没有越界。

## 5. 两类包的强制规则

科学点成果包必须满足：

```text
ResultId 非空
包含科学点集合
不包含全过程执行证据
```

如果调用方需要执行证据，必须创建 `CalculationRunArchive`，不能在科学点包中
偷偷加入 `process/`。

全过程计算档案包必须满足：

```text
RootWorkflowId 非空
包含科学点集合
包含全部阶段和作业尝试证据
包含排除项清单
science 与 process 目录安全且不同
```

上述约束由 `ReportPackageScopeValidator.Validate` 检查。失败时返回稳定问题码，
例如：

```text
report.scope.missing
report.scope.result_id.missing
report.scope.root_workflow_id.missing
report.scope.science_point_set.not_included
report.scope.process_evidence.included
report.scope.execution_evidence.not_included
report.scope.excluded_artifacts.not_included
report.scope.directories.not_distinct
report.scope.science_directory.invalid
report.scope.process_directory.invalid
```

## 6. 计算失败与档案完整

这两个事实必须同时成立：

```text
计算失败 + 全部证据已保存 = 档案可以 Complete
计算仍在运行 + 只生成临时快照 = 不能标记档案 Complete
```

验证器允许：

```csharp
scope.CalculationConclusion =
    ReportCalculationConclusionStatus.Failed;

ReportPackageScopeValidator.Validate(
    scope,
    ReportPackageStatus.Complete);
```

验证器拒绝：

```csharp
scope.CalculationConclusion =
    ReportCalculationConclusionStatus.InProgress;

ReportPackageScopeValidator.Validate(
    scope,
    ReportPackageStatus.Complete);
```

同样，计算结论为 `Unknown` 时不能把档案标记为 `Complete`，因为规范要求
完整档案只针对已经结束并能够判定状态的根工作流。

## 7. 与四个主契约的关系

范围已接入：

```text
ReportPackageRequest.Scope
ReportEvidenceSnapshot.Scope
ReportDocument.Scope
ReportPackageManifest.Scope
```

四个对象都保留同一范围副本，使后续每一层都能独立校验边界，而不需要
回溯读取请求对象。`Scope` 暂时可空，用于兼容第 0 步已经写入的旧 JSON。

新代码不得依赖这个兼容空值：

```text
新生成的科学点成果包必须显式设置 Scope
新生成的全过程档案包必须显式设置 Scope
后续快照构建器发现缺失 Scope 时应生成问题，而不是自行猜测
```

## 8. 外部标识一致性

验证器允许同时传入外层 `resultId` 和 `rootWorkflowId`：

```csharp
ReportPackageScopeValidator.Validate(
    scope,
    packageStatus,
    resultId,
    rootWorkflowId);
```

外层标识与范围标识不一致时返回：

```text
report.scope.result_id.mismatch
report.scope.root_workflow_id.mismatch
```

这一步只做字符串规范化后的比较，不查询仓储。标识是否真实存在，由后续
证据快照构建器负责验证。

## 9. 测试覆盖

`ReportContractTests` 新增以下场景：

```text
两类范围 JSON 往返
科学点包拒绝执行证据
档案包必须包含科学点、执行证据和排除项
运行中档案不能是 Complete
未知结论档案不能是 Complete
计算失败但证据完整时档案可以是 Complete
外层标识与范围标识冲突
绝对路径、反斜杠、目录穿越和重复斜杠被拒绝
science 与 process 不能是同一个目录
```

验收命令：

```powershell
dotnet test tests\ChemSculptor.Core.Tests\ChemSculptor.Core.Tests.csproj `
  -c Release --no-restore `
  --filter FullyQualifiedName~ReportContractTests
```

## 10. 本步骤不做什么

```text
不自动从作业推导 RootWorkflowId
不自动从科学结果推导 ResultId
不读取 ledger.json 或 manifest.json
不枚举作业目录
不判断科学点来源是否悬空
不生成 exclusion 清单
不计算文件摘要
不渲染报告
不创建 zip
```

下一步应基于已经完成的作业清单与科学关联审计，构建全过程证据快照，
明确阶段、作业尝试和科学点之间的因果关系。
