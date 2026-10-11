# 氧气示例：从客户端提交到最终科学摘要

> 架构提示：本文涉及早期工作流节点名称。最新的单点计算 Skill 分层见
> [SinglePoint-Skill-Architecture.md](../SinglePoint-Skill-Architecture.md)。

本文面向第一次接触 ChemSculptor 的读者。目标是用一个真实的氧气例子，说明
下面这条完整链路：

```text
客户端坐标和需求提交
  → 单点计算工作流
  → 波函数稳定性检查
  → 矫正计算
  → 科学数据仓储
  → 客户端最终摘要
```

## 1. 先理解系统中的五个核心概念

### 1.1 客户端

客户端是 WinForms 程序。它只负责：

```text
选择坐标文本
输入自然语言需求
通过 HTTP 提交
显示服务器返回的信息
下载计算文件
```

客户端不执行量子化学计算，也不读取科学数据文件。

### 1.2 服务端

服务端是 `ChemSculptor.Api`。它负责：

```text
接收 HTTP 请求
调用智能体与会话层
启动计算工作流
查询作业状态
返回客户端摘要
```

### 1.3 工作流

工作流是声明式节点序列。氧气单点任务的主链路是：

```text
input-generation
  → submit
  → wait
  → extract
  → validate
  → stability-check
  → stability-correction-plan
  → recovery-job
  → recovery-execution
  → recovery-stability-check
  → plan
  → science-data-record
```

每个节点调用一个 Skill。工作流本身不包含具体程序语法。

### 1.4 计算点

计算点不是单纯的坐标。一个计算点同时包含：

```text
几何结构
化学组分
电子态
计算方法、基组和程序
能量等性质
验证记录
来源和矫正血缘
```

氧气原始单重态和修正三重态是两个不同的点。

### 1.5 科学数据仓储

科学数据仓储保存已经整理好的科学成果。以后文字报告和客户端科学摘要都从这里
读取，而不是重新解析计算程序日志。

## 2. 本次示例输入

### 2.1 用户需求

```text
单点计算
```

### 2.2 氧气坐标

```text
O 0.000000 0.000000 0.000000
O 0.000000 0.000000 1.207000
```

### 2.3 默认计算方案

服务端当前默认使用：

```text
程序：Gaussian 16
方法：CAM-B3LYP
基组：6-31G*
任务：单点能
SCF 最大迭代：200
```

默认自旋多重度由电子数规则先给出，然后由稳定性检查纠错。

## 3. 第一阶段：客户端提交

WinForms 把用户原始文本和坐标文件内容发送到：

```text
POST /agent/messages
```

请求体：

```json
{
  "sessionId": "oxygen-e2e-client",
  "text": "单点计算",
  "coordinateText": "O 0.000000 0.000000 0.000000\nO 0.000000 0.000000 1.207000"
}
```

客户端不做以下事情：

```text
不解析“单点计算”
不推断电荷
不推断自旋多重度
不生成 Gaussian 输入
不决定是否需要稳定性检查
```

这些工作全部在服务端完成。

服务端返回根作业标识，例如：

```text
job-6680bea68de4467cb97536eb1c181242
```

根作业标识同时作为当前工作流标识。

## 4. 第二阶段：会话层解释任务

`AgentService` 调用的会话层首先识别用户文本：

```text
“单点计算”
  → 任务类型：SinglePoint
  → 电子态目标：GroundState
```

如果是：

```text
“三重态单点计算”
```

则解释为：

```text
任务类型：SinglePoint
电子态目标：TargetSpinState
目标多重度：3
```

当前阶段只实现了单点计算。优化、频率、TD-DFT 和 ONIOM 等任务尚未进入
这条主链。

## 5. 第三阶段：生成计算输入

工作流节点 `input-generation` 调用：

```text
calculation.prepare-input
```

它完成：

```text
解析坐标文本
计算电子数
应用默认计算方案
建立规范几何结构
调用当前程序适配器写出输入文件
准备运行目录、输出目录和结果目录
```

氧气原始输入会被写为类似：

```text
%chk=job-....chk
%mem=4GB
%nprocshared=4

#p CAM-B3LYP/6-31G* SP scfcyc=200

ChemSculptor single point calculation

0 1
O ...
O ...
```

这里的 `0 1` 是第一次默认尝试：总电荷 0，自旋多重度 1。

## 6. 第四阶段：提交和等待计算

`submit` 节点调用通用提交 Skill。它通过 `IComputeBackend` 提交作业，而具体
命令由当前程序适配器提供。

`wait` 节点轮询任务状态，直到：

```text
Completed
Failed
Canceled
```

对于本机后端，Gaussian 在 `run` 目录中执行。计算结束后会生成：

```text
output.log
*.chk
*.fchk
*.gjf
```

## 7. 第五阶段：结果提取和验证

通用结果提取节点调用当前程序适配器解析输出：

```text
g16 输出
  → 程序专用解析结果
  → 通用 CalculationResult
```

通用结果包含：

```text
能量
能量单位
程序
方法
基组
电荷
多重度
正常终结标志
失败类别
输出文件路径
```

验证节点检查：

```text
作业标识
程序、方法和基组
电荷和多重度
正常终结
能量是否存在
能量是否有限
输出文件是否存在
```

本次氧气原始单点结果的关键数据是：

```text
程序正常结束：True
多重度：1
能量：-150.210765371 Hartree
```

## 8. 第六阶段：波函数稳定性检查

工作流节点：

```text
stability-check
```

使用通用 Skill：

```text
anomaly.check-wavefunction-stability
```

再由程序适配器选择 Gaussian 专用实现。

对于已经有输出文件的情况，检查器解析稳定性输出。对于本次氧气任务，检查器
需要从原始 `.chk` 创建辅助稳定性作业：

```text
复制原始 .chk
  → 保留原方法、基组和坐标来源
  → 增加 guess=read geom=check stable
  → 运行稳定性检查作业
  → 解析输出
```

本次检查结果：

```text
稳定性状态：Finding
不稳定类型：RHF-to-UHF
说明：初始单重态波函数不稳定
```

稳定性检查不是简单的“程序是否正常结束”。程序正常结束只是必要条件。

## 9. 第七阶段：生成矫正方案

节点：

```text
stability-correction-plan
```

读取稳定性矩阵的全部本征值，找到最低能量方向。

本次氧气结果中，最低本征值对应的电子态是：

```text
Triplet
自旋多重度：3
```

因为目标任务是默认基态，规划器生成：

```text
修正意图：spin-multiplicity.change
原多重度：1
目标多重度：3
RequiresApproval：false
```

这里不是直接修改原始作业。系统保留原始点，并准备创建一个派生点。

## 10. 第八阶段：创建和运行矫正作业

节点：

```text
recovery-job
  → recovery-execution
```

创建派生作业时：

```text
复制原始输入文件
替换 %chk
保留方法和基组
保留 scfcyc=200
把电荷/多重度行修改为 0 3
保存父子血缘
```

派生作业执行完成后得到：

```text
多重度：3
能量：-150.274273534 Hartree
程序正常结束：True
```

## 11. 第九阶段：派生稳定性复检

节点：

```text
recovery-stability-check
```

它再次使用通用稳定性检查 Skill，但输入来自派生作业：

```text
恢复作业执行结果
  → 派生作业
  → 派生 .chk
  → 稳定性复检
```

本次复检结果：

```text
The wavefunction is stable under the perturbations considered.
状态：Passed
```

此时计算层面的结论是：

```text
原始单重态点：被取代
派生三重态点：被接受
矫正次数：1
```

## 12. 第十阶段：提交科学数据仓储

工作流末尾节点：

```text
science-data-record
```

它调用：

```text
science.record-calculation-result
```

提取项目把计算和异常处理结果翻译为：

```text
ScientificResult
  CalculationPointSet
    original point
    corrected point
  CalculationPointRelation
  ScientificObservable
```

本次氧气科学数据中的两个点：

```text
原始点
  Id：point-job-6680bea68de4467cb97536eb1c181242
  Status：Superseded
  Multiplicity：1
  Stability：Finding
  Energy：-150.210765371 Hartree

修正点
  Id：point-job-<recovery-job-id>
  Status：Accepted
  Multiplicity：3
  Stability：Passed
  Energy：-150.274273534 Hartree
  CorrectionCount：1
```

点关系：

```text
原始点
  → DerivedFrom
  → 修正点
```

最终物理量：

```text
名称：单点能量
值：-150.274273534
单位：Hartree
来源：修正后的 Accepted Point
```

科学数据文件位置：

```text
<scientific-data-root>/
  scientific-result-<rootJobId>.json
```

典型内容结构：

```text
ScientificResult
  PointSet
    points
    relations
  Observables
  Metadata
    rootWorkflowId
    correctionCount
    acceptedPointId
    correctionPlanId
    anomalyRecordId
```

## 13. 第十一阶段：生成并发送客户端摘要

科学数据仓储不直接把原始数据发送给客户端。摘要项目负责把它转换为显示模型：

```text
ScientificData
  → ClientScientificSummary
```

客户端摘要包含：

```text
ResultId
Title
Status
Summary
Sections
Metadata
```

本次氧气摘要的段落是：

```text
计算点：
  共 2 个计算点。

矫正统计：
  矫正次数：1。

单点能量：
  -150.274273534 Hartree
```

API 提供以下客户端接口：

```text
GET /scientific-results/{resultId}/client-summary
POST /scientific-results/{resultId}/client-summary/send/{clientId}
GET /clients/{clientId}/scientific-summaries

GET /calculations/{jobId}/client-summary
POST /calculations/{jobId}/client-summary/send/{clientId}
```

WinForms 使用按作业号发起的发送接口。作业处于终态后，客户端会反复请求摘要；
科学数据尚未生成时接口返回 404，客户端下一次轮询再试。

摘要成功后，客户端显示：

```text
科学摘要：单点计算
状态：Complete

计算点：
共 2 个计算点。

矫正统计：
矫正次数：1。

单点能量：
-150.274273534 Hartree
```

## 14. 完整数据流

```text
WinForms
  ↓ POST /agent/messages
ChemSculptor.Api
  ↓ Agent / Conversation
SinglePointWorkflowEngine
  ↓
Compute + Compute.Gaussian
  ↓
CalculationResult
  ↓
Anomaly stability check
  ↓
CorrectionPlan
  ↓
Recovery job
  ↓
Recovery stability check
  ↓
ScientificData.Extraction
  ↓
ScientificData repository
  ↓
ScientificSummary
  ↓
ChemSculptor.Api
  ↓
WinForms
```

## 15. 目录和源码阅读顺序

建议按下面顺序阅读：

```text
1. src/ChemSculptor.WinForms/MainForm.cs
2. src/ChemSculptor.Api/Endpoints/AgentEndpoints.cs
3. src/ChemSculptor.Agent/AgentService.cs
4. src/ChemSculptor.Conversation/ConversationService.cs
5. src/ChemSculptor.Agent/SinglePointWorkflowDefinitionFactory.cs
6. src/ChemSculptor.Core/WorkflowEngine.cs
7. src/ChemSculptor.Compute/CalculationModels.cs
8. src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
9. src/ChemSculptor.Skills.Gaussian/Anomaly/WavefunctionStability/
10. src/ChemSculptor.Anomaly/Planning/WavefunctionStabilityCorrectionPlanner.cs
11. src/ChemSculptor.ScientificData.Extraction/ScientificResultExtractor.cs
12. src/ChemSculptor.ScientificSummary/ClientSummaryService.cs
13. src/ChemSculptor.Api/Endpoints/ClientSummaryEndpoints.cs
14. src/ChemSculptor.WinForms/MainForm.cs
```

## 16. 如何运行

### 16.1 环境

```text
.NET 10 SDK
Gaussian 16
g16 可在 PATH 中运行
formchk 可在 PATH 中运行
```

### 16.2 构建

```powershell
dotnet restore ChemSculptor.slnx
dotnet build ChemSculptor.slnx -c Release
```

### 16.3 启动 API

```powershell
dotnet run --project src/ChemSculptor.Api -c Release
```

### 16.4 启动 WinForms

在 Visual Studio 中选择：

```text
ChemSculptor.WinForms
```

启动后选择氧气坐标 txt，在输入框写下：

```text
单点计算
```

然后点击“发送”。

## 17. 手工查看关键数据

计算作业：

```text
GET /calculations/{jobId}/status
GET /calculations/{jobId}/result
GET /calculations/{jobId}/validation
```

工作流节点结果：

```text
GET /workflows/{workflowId}
```

科学数据摘要：

```text
GET /calculations/{jobId}/client-summary
POST /calculations/{jobId}/client-summary/send/{clientId}
GET /clients/{clientId}/scientific-summaries
```

## 18. 当前实现边界

已经打通：

```text
氧气单点计算
波函数稳定性检查
自旋多重度矫正
矫正后稳定性复检
科学计算点提取
科学数据文件仓储
客户端摘要生成
客户端摘要发送和领取
```

尚未实现：

```text
真正的 WebSocket 推送
远程 HPC 队列
几何优化和其他任务类型
文章正文自动生成
多轮复杂异常恢复
客户端摘要模板的定制
```

## 19. 最重要的工程原则

```text
客户端只负责输入和显示
服务端负责解释、计算、验证和矫正
工作流负责调度
具体程序细节放在程序适配器
科学数据只保存科学事实
报告和客户端摘要只从科学数据读取
原始点、派生点和矫正血缘全部保留
```

理解了这条链路，就可以继续阅读单个项目的源码，并在对应层增加新的 Skill、
计算程序适配器、异常检测或科学成果类型。
