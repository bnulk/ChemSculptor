# 全过程档案第 6 步教程：产物发现

## 1. 这一步解决什么问题

第 5 步已经能说明“作业为什么存在、属于哪个阶段”。第 6 步继续回答：

```text
这个作业预期产生什么文件
实际存在什么文件
文件扮演什么角色
文件是否必要
哪些文件被排除，为什么被排除
哪些路径或文件无法安全读取
当前证据范围是否完整
```

这一阶段的输出叫“产物发现快照”，不是最终档案包。

## 2. 输入和输出

唯一范围标识：

```text
RootWorkflowId
```

主要输入：

```text
workflow-runs/<RootWorkflowId>/ledger.json
完整作业清单
各作业目录中的 manifest.json
实际作业目录
程序适配器声明的候选文件规则
通用路径和排除策略
```

持久化输出：

```text
workflow-runs/<RootWorkflowId>/artifacts-discovery.json
```

调用入口：

```csharp
ICalculationWorkflowArtifactDiscoveryRepository repository;

CalculationWorkflowArtifactDiscoverySnapshot snapshot =
    await repository.DiscoverAsync(rootWorkflowId);
```

读取最近一次快照：

```csharp
CalculationWorkflowArtifactDiscoverySnapshot? snapshot =
    await repository.GetAsync(rootWorkflowId);
```

## 3. 为什么不能直接生成最终文件清单

文件“存在”与文件“已经进入档案包”是两个不同事实。

```text
发现阶段
  Present

最终清单阶段
  Included
```

第 6 步只产生 `Present`。第 7 步才读取文件、计算 SHA-256、校验长度，
并决定它能否成为最终清单项。组装器真正复制成功后，才使用
`Included`。

这样可以避免：

```text
发现后文件被删除，但 Manifest 已经谎称包含
文件在发现后被改写，摘要与实际不一致
路径安全检查通过，但复制阶段再次遇到链接穿越
可选文件被误当成必要证据
必要条件被排除后仍把档案标成完整
```

## 4. 作业范围

发现仓储调用完整作业清单，不传入界面筛选条件。因此以下作业都会进入
快照：

```text
Completed
Parsed
Validated
Failed
Canceled
Created
Running
恢复作业
NTO 派生作业
稳定性检查和修正作业
```

计算失败不等于档案不完整。只要失败输入、已有输出、诊断和状态仍然
存在，发现快照依然可以完整。

## 5. 快照结构

`CalculationWorkflowArtifactDiscoverySnapshot` 包含四部分：

```text
Jobs
Files
Issues
Summary
```

### 5.1 Jobs

每个作业尝试至少记录：

```text
StageId
StageOrder
JobId
AttemptNumber
State
TaskType
Program
ManifestStatus
JobDirectoryExists
RunDirectoryExists
FileCount
PresentCount
MissingCount
ExcludedCount
UnreadableCount
SecurityRejectedCount
```

### 5.2 Files

每个文件记录至少包含：

```text
ArtifactId
StageId
JobId
AttemptNumber
LogicalPath
Role
Requirement
Status
SourceKind
MediaType
Length
CanUseForRestart
ExclusionCategory
Message
CanRegenerate
```

### 5.3 Issues

问题由稳定代码、严重级别、阶段、作业和逻辑路径组成。路径不会使用
服务器绝对路径。

### 5.4 Summary

汇总包含文件数量、存在数量、缺失数量、排除数量、不可读数量、安全
拒绝数量、总字节数、错误数和 `IsComplete`。

排除项按类别汇总：

```text
Category
FileCount
TotalBytes
Reason
CanRegenerate
AffectsReproduction
```

## 6. 文件角色

通用角色位于 `CalculationWorkflowArtifactRole`：

```text
JobManifest
SourceInput
GeometryInput
ProgramInput
PrimaryOutput
ConsoleOutput
RestartState
NormalizedResult
ValidationEvidence
ParameterAudit
ProcessingPlan
PostProcessOutput
ScientificData
Narrative
Report
Manifest
Checksum
Other
```

角色由通用固定规则和程序适配器规则共同决定。无法识别的持久化文件
进入 `Other`，不能因为没有专门规则就丢弃。

## 7. 必要性

`CalculationWorkflowArtifactRequirement` 只有两种：

```text
Required
Optional
```

重要性来自规则，而不是文件是否恰好存在：

```text
Gaussian 作业在 Created 状态
  程序输入尚不要求存在

Gaussian 作业进入 Running 后
  程序输入是必要证据

Gaussian 作业进入 Completed / Parsed / Validated 后
  主要输出也是必要证据

检查点通配规则
  默认只是可选候选
```

通配规则代表“程序可能生成这类文件”，不代表每个匹配文件都必须存在。

## 8. 发现状态

```text
Present
  文件存在，可以在第 7 步读取并计算摘要

ExcludedByPolicy
  文件存在，但属于 scratch、缓存、临时文件或零字节控制流

NotExpected
  按当前作业状态，该文件本来不应存在

Missing
  规则要求该文件，但它不存在

Unreadable
  文件存在，但元数据无法读取

SecurityRejected
  路径越界、重解析点或其它安全规则拒绝访问
```

必要条件出现 `Missing`、`Unreadable` 或 `SecurityRejected` 时，
发现结果不能标记为完整。

必要条件即使命中排除规则，也保持 `Required`，状态变为
`ExcludedByPolicy`，并产生 `artifacts.required.excluded` 问题。

## 9. 扫描范围

通用仓储只从作业目录内部建立候选：

```text
<job>/
  input/
  run/
  results/
```

同时读取作业根目录中的固定证据和 `manifest.json`。

不在内部递归扫描 scratch 目录。实际存在的未分类文件仍会形成记录，
通常进入 `Other` 或按通用规则分类。

## 10. 路径安全

`CalculationWorkflowArtifactPathPolicy` 负责：

```text
把候选路径归一化为绝对路径
确认路径仍位于所属作业目录内
拒绝目录穿越和外部绝对路径
逐级检查符号链接和 Windows 重解析点
拒绝跟随链接目标
只把作业内相对逻辑路径写入快照
```

安全检查失败不会退化成普通文件存在检查，而是产生显式问题状态。

## 11. 排除策略

`CalculationWorkflowArtifactDiscoveryPolicy` 识别：

```text
scratch
tmp / temp
cache / .cache / __pycache__
.git
*.tmp / *.temp / *.lock / *.lck / *.swp
以 ~ 结尾的交换文件
.DS_Store / Thumbs.db / desktop.ini
零字节 stdout.log / stderr.log / console.log
```

整目录排除使用汇总条目，记录目录中可安全测量的文件数和字节数。
不可访问内容不能伪造为零字节。

## 12. 顺序和确定性

快照按稳定规则排序：

```text
作业按阶段顺序、创建时间和 JobId
文件按逻辑路径和稳定标识
问题按问题码、阶段、作业和逻辑路径
排除汇总按类别和原因
```

同一冻结文件系统状态重新发现时，排序和稳定标识应可比较。
`CapturedAt` 是快照时间，不用于推断科学因果关系。

## 13. 服务重启

快照使用临时文件写入后原子替换：

```text
artifacts-discovery.json.tmp
  → artifacts-discovery.json
```

服务重启后可以通过新的仓储实例读取最近快照。重新发现会建立新的
时间截面，不能把旧快照自动当成当前文件状态。

## 14. 本步不做什么

```text
不复制任何文件
不计算 SHA-256
不生成最终 file manifest
不生成 checksums.sha256
不组装 archive 目录
不生成 zip
不修改科学点集合
不生成 Markdown 报告
不接入 API 和 WinForms
```

## 15. 测试映射

`CalculationWorkflowArtifactDiscoveryTests` 覆盖：

```text
DiscoversCompleteMultiStageArchive
  多阶段、失败和取消作业，检查点、未知文件和排除汇总，JSON 往返

ReportsMissingRequiredEvidence
  必要程序输入或输出缺失时 IsComplete = false

RejectsReparsePointWithoutFollowingIt
  拒绝重解析点，不读取链接目标

ReportsRunDirectoryOutsideWorkspaceContract
  运行目录越界产生稳定问题码

GaussianArtifactRequirementsFollowJobState
  Gaussian 输入和主要输出随作业状态改变必要性

RequiredEvidenceExcludedRemainsRequired
  必要条件被排除后不得降级
```

运行定向测试：

```powershell
dotnet test tests\ChemSculptor.Core.Tests\ChemSculptor.Core.Tests.csproj `
  -c Release --no-restore `
  --filter "FullyQualifiedName~CalculationWorkflowArtifactDiscoveryTests"
```

完整验证：

```powershell
dotnet build ChemSculptor.slnx -c Release -m:1 --no-restore
dotnet test tests\ChemSculptor.Core.Tests\ChemSculptor.Core.Tests.csproj `
  -c Release --no-restore
```

## 16. 下一步

第 7 步读取 `artifacts-discovery.json` 的 `Present` 项：

```text
再次验证路径安全
读取文件长度
计算 SHA-256
处理读取期间文件变化
生成最终文件清单
为 checksums.sha256 提供稳定输入
```

进入第 7 步时，不能把第 6 步的发现快照直接改写成“已归档”。
