# ChemSculptor 项目改动说明

> 维护约定：**每次代码改动后更新本文档**，记录版本、改动目的、改动内容和教程式说明。
> 版本规则：`MAJOR.MINOR.PATCH`。新增功能 +1 MINOR；修复问题 +1 PATCH；架构性大改动 +1 MAJOR。

---

## v0.20.2（2026-09-26）：新增 Skill 集合与工作流组织教程

### 版本

- 当前版本：`0.20.2`
- 日期：2026-09-26
- 版本类型：文档

### 改动目的

新增一份偏设计思想的教程，说明：

```text
Skill 集合与工作流的区别
Skill、Catalog、Workflow、Agent、Gate 的职责
静态工作流与动态工作流
DAG、依赖和并行
数据契约和版本管理
SCF 异常的修正子工作流
TADF 多阶段工作流
父子作业和审计
当前实现与未来目标的边界
```

### 改动内容

新增：

```text
docs/Skill-Collections-and-Workflow-Tutorial.md
```

并在 `README.md` 中增加教程链接。

本次不修改运行代码。

### 验证

- 文档链接检查
- 与当前 WorkflowDefinition、WorkflowNode 和 Skill 结构对照

---

## v0.20.1（2026-09-26）：新增 Skill 集合学习教程

### 版本

- 当前版本：`0.20.1`
- 日期：2026-09-26
- 版本类型：文档

### 改动目的

新增一份面向项目维护者的教程，集中解释：

```text
Skill、Adapter、Parser、Translator、Catalog 的区别
为什么 Agent 不应直接依赖具体程序
当前 Skill 项目的目录和依赖方向
一次正常单点计算的 Skill 调用顺序
JsonSkill 和 SkillJsonInvoker 的工作方式
怎样新增一个 Skill
怎样新增 ORCA 程序族
异常处理框架当前做到什么程度
```

### 改动内容

新增：

```text
docs/ChemSculptor-Skill-Collection-Tutorial.md
```

并在 `README.md` 中增加教程链接。

本次不修改运行代码。

### 验证

- 文档链接检查
- 与当前 v0.20.0 代码结构对照

---

## v0.20.0（2026-09-26）：按显式 Skill 集合重组正常计算链路

### 版本

- 当前版本：`0.20.0`
- 日期：2026-09-26
- 版本类型：架构调整（Skill 目录与调用边界）

### 改动目的

把正常单点计算从“Agent 直接调用 Gaussian 技术类”改为“Agent 只通过
`ISkillRegistry` 和通用模型调用 Skill”。

阅读代码时先看到能力：

```text
GaussianInputGenerationSkill
GaussianSinglePointResultExtractionSkill
CalculationResultValidationSkill
```

需要深入时再看实现：

```text
GaussianInputWriter
GaussianOutputParser
GaussianResultTranslator
Gaussian16ProgramAdapter
```

### 新增项目

```text
src/ChemSculptor.Skills.Common
src/ChemSculptor.Skills.Gaussian
src/ChemSculptor.Skills.Orca
```

### 正常计算链路

```text
SinglePointCalculationExecutor
  → GaussianInputGenerationSkill
  → IComputeBackend
  → CalculationJobMonitor
  → GaussianSinglePointResultExtractionSkill
  → CalculationResultValidationSkill
  → CalculationProcessingPlanner
```

Agent 不再直接引用 `ChemSculptor.Compute.Gaussian` 或
`ChemSculptor.Compute.Local`。

### 技能目录

Common：

```text
calculation.result-validation
```

Gaussian：

```text
gaussian.input-generation
gaussian.single-point-result-extraction
gaussian.failure-diagnosis
gaussian.failure-correction-proposal
```

ORCA：

```text
目录和注册框架已建立，当前没有已实现技能。
```

### 异常处理框架

当前不执行异常处理，只保留：

```text
GaussianFailureDiagnosisSkill
GaussianFailureCorrectionProposalSkill
通用 CalculationFailure
通用 CalculationProcessingPlan
GaussianProcessingPlan
```

其中异常诊断技能当前报告“框架尚未实现”，避免在未完成逻辑时被误用。

### 通用技能调用

新增：

```text
ISkillInvoker
SkillJsonInvoker
JsonSkill<TRequest, TResult>
```

Agent 使用通用请求和结果类型调用技能；Gaussian 技能在内部把它们转换为
Gaussian 专用请求、解析结果和程序上下文。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：23/23 通过
- 实际计算：状态 `Parsed`
- 实际能量：`-76.3801013836 Hartree`
- `/skills/` 能列出：
  - `gaussian.input-generation`
  - `gaussian.single-point-result-extraction`
  - `calculation.result-validation`
  - `gaussian.failure-diagnosis`
  - `gaussian.failure-correction-proposal`

---

## v0.19.0（2026-09-26）：计算结果与处理方案的双向翻译框架

### 版本

- 当前版本：`0.19.0`
- 日期：2026-09-26
- 版本类型：架构调整（程序专用模型与通用模型解耦）

### 改动目的

避免智能体直接理解 Gaussian 的关键词和输出格式。现在由 Gaussian 模块负责
两端的翻译：

```text
Gaussian 文本输出
  → GaussianOutputParser
  → GaussianOutput
  → GaussianResultTranslator
  → CalculationResult
  → 通用 CalculationProcessingPlanner
  → CalculationProcessingPlan
  → GaussianProcessingPlanTranslator
  → GaussianProcessingPlan
  → ProgramProcessingPlan
```

Agent 只处理 `CalculationResult`、`CalculationProcessingPlan` 和
`ProgramProcessingPlan`，不包含 Gaussian 路线、Link 名称或输出格式规则。

### 改动内容

新增 Gaussian 专用数据结构：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutput.cs
src/ChemSculptor.Compute.Gaussian/GaussianProcessingPlan.cs
```

新增翻译器：

```text
src/ChemSculptor.Compute.Gaussian/GaussianResultTranslator.cs
src/ChemSculptor.Compute.Gaussian/GaussianProcessingPlanTranslator.cs
```

新增通用处理方案：

```text
src/ChemSculptor.Compute/CalculationProcessingModels.cs
src/ChemSculptor.Agent/ICalculationProcessingPlanner.cs
src/ChemSculptor.Agent/RuleBasedCalculationProcessingPlanner.cs
```

通用模型包括：

```text
CalculationProcessingOutcome
CalculationProcessingActionType
CalculationProcessingAction
CalculationProcessingPlan
ProgramProcessingAction
ProgramProcessingPlan
CalculationFailureKind
```

处理结果保存为三个文件：

```text
results/result.json
results/processing-plan.json
results/program-processing-plan.json
```

### 教程式说明

#### 一、为什么要分成两层

如果 Agent 直接读取：

```text
Normal termination of Gaussian 16
SCF Done: E(RCAM-B3LYP) = ...
```

那么以后增加 ORCA 时，Agent 就必须继续增加 ORCA 专用判断，逐渐变成“所有
计算程序的大杂烩”。

现在职责变成：

```text
Agent
  只知道“正常完成、缺少能量、需要重试、需要用户决定”

Gaussian 模块
  知道这些通用状态怎样对应 Gaussian 的输入和关键词
```

#### 二、正常结果的流程

```text
Gaussian 输出正常结束并含有能量
  → GaussianOutput.NormalTermination = true
  → CalculationResult.FailureKind = None
  → CalculationProcessingPlan.Outcome = Completed
  → 没有处理动作
```

#### 三、异常结果的流程

例如输出没有最终能量：

```text
GaussianOutput.Energy = null
  → CalculationResult.FailureKind = EnergyMissing
  → 通用方案产生 RetryAsIs
  → Gaussian 模块翻译为 RerunSameInput
```

例如 Gaussian 报错结束：

```text
GaussianOutput.ErrorTermination = true
  → CalculationResult.FailureKind = ProgramError
  → 通用方案产生 ReviewOutput
  → Gaussian 模块翻译为 InspectOutput
```

当前阶段只生成方案并保存，不自动执行重试或输入修改。

#### 四、文件含义

```text
result.json                    通用计算结果
processing-plan.json           智能体可以理解的通用处理方案
program-processing-plan.json   翻译后的程序专用处理方案
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：23/23 通过
- 实际 Gaussian 计算：状态 `Parsed`
- 实际能量：`-76.3801013836 Hartree`
- 通用处理方案：`outcome = Completed`
- Gaussian 专用处理方案生成成功

---

## v0.18.0（2026-09-26）：Gaussian 输出解析与规范化结果

### 版本

- 当前版本：`0.18.0`
- 日期：2026-09-26
- 版本类型：新增功能（第五阶段，Gaussian 输出解析）

### 改动目的

让单点计算不再只停留在“启动 g16”和取得输出文件，而是继续完成：

```text
等待计算进程结束
读取 output.log
判断 Normal termination
提取最终 SCF 能量
保存 result.json
提供状态和结果查询接口
```

### 改动内容

新增 Gaussian 输出解析器：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutputParser.cs
```

解析内容：

- 识别 `Normal termination of Gaussian 16`
- 识别 `Error termination`
- 提取最后一次 `SCF Done: E(...) = ...`
- 支持 Gaussian 的 `D` 指数格式
- 生成结构化诊断信息

新增文件仓储：

```text
src/ChemSculptor.Compute/FileCalculationRepository.cs
```

保存位置：

```text
jobs/<jobId>/manifest.json
jobs/<jobId>/results/result.json
```

新增后台作业监控器：

```text
src/ChemSculptor.Agent/CalculationJobMonitor.cs
```

工作方式：

```text
轮询 IComputeBackend.GetStatusAsync
  → 状态进入 Completed / Failed / Canceled
  → 读取 output.log
  → 调用 IQuantumProgramAdapter.ParseOutputAsync
  → 保存 result.json
  → 更新 manifest.json 中的作业状态
```

新增查询服务：

```text
ICalculationQueryService
CalculationQueryService
```

新增 API：

```text
GET /calculations/{jobId}/status
GET /calculations/{jobId}/result
```

### 教程式说明

#### 一、为什么解析要放在后台

Gaussian 可能运行数秒、数分钟甚至数小时。提交 API 不能一直等待进程结束，
否则 HTTP 请求会长时间占用连接。

现在的流程是：

```text
提交 API 立即返回 Running
后台监控器继续等待
Gaussian 结束后自动解析并保存结果
客户端稍后查询状态或结果
```

#### 二、怎样判断计算成功

不能只依赖进程退出码。解析器还会检查：

```text
Normal termination of Gaussian 16
```

并提取：

```text
SCF Done: E(RCAM-B3LYP) = -76.3801014 A.U.
```

只有同时满足以下条件，作业才进入 `Parsed`：

```text
进程状态为 Completed
输出中存在 Normal termination
成功提取到最终能量
```

#### 三、如何查询结果

查询状态：

```powershell
Invoke-RestMethod `
    -Uri "http://127.0.0.1:5093/calculations/<jobId>/status" `
    -Method Get
```

查询规范化结果：

```powershell
Invoke-RestMethod `
    -Uri "http://127.0.0.1:5093/calculations/<jobId>/result" `
    -Method Get
```

结果示例：

```json
{
  "energy": -76.3801013836,
  "energyUnit": "Hartree",
  "normalTermination": true,
  "program": "Gaussian 16",
  "method": "CAM-B3LYP",
  "basis": "6-31G*",
  "charge": 0,
  "multiplicity": 1
}
```

#### 四、当前边界

已经具备：

```text
输出解析
能量提取
作业状态保存
规范化结果保存
状态查询 API
结果查询 API
```

尚未具备：

```text
科学结果验证门
自动查错与纠错
WinForms 自动轮询最终能量
远程 HPC 后端
作业队列
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：19/19 通过
- 端到端实测：水的 CAM-B3LYP/6-31G* 单点计算状态进入 `Parsed`
- 实测能量：`-76.3801013836 Hartree`
- 实测结果文件：`jobs/<jobId>/results/result.json`

---

## v0.17.1（2026-09-26）：输入文件复制到运行目录后执行

### 版本

- 当前版本：`0.17.1`
- 日期：2026-09-26
- 版本类型：问题修复 / 工作区布局调整

### 改动目的

让计算程序始终在自己的 `run` 目录中执行，使 Gaussian 生成的检查点文件、
临时文件和输出文件自然落在同一个目录，避免结果分散到 `input` 目录。

### 改动内容

- `SinglePointCalculationExecutor` 生成原始输入后，把 `.gjf` 复制到 `run` 目录。
- `CalculationJob.InputFilePath` 指向 `run` 目录中的实际执行输入。
- `Gaussian16ProgramAdapter` 构建上下文时使用 `run` 目录中的输入文件。
- Gaussian `%chk` 改为相对文件名，例如 `job-xxxx.chk`。
- API 返回的 `InputFilePath` 仍指向 `input` 目录中的原始输入文件。

目录变化：

```text
jobs/<jobId>/input/
  molecule.xyz
  <jobId>.gjf

jobs/<jobId>/run/
  <jobId>.gjf
  <jobId>.chk
  output.log
  stdout.log
  stderr.log
```

### 教程式说明

原来的 `g16` 参数直接指向：

```text
<作业目录>\input\<jobId>.gjf
```

这样 `%chk` 的绝对路径也会指向 `input` 目录。现在执行器先把输入复制到：

```text
<作业目录>\run\<jobId>.gjf
```

然后 `g16` 以 `run` 作为工作目录，并接收 `run` 中的输入文件。Gaussian 输入
中的检查点设置为：

```text
%chk=<jobId>.chk
```

相对路径以工作目录为基准，因此检查点自然写入 `run`。

### 验证

- `dotnet build ChemSculptor.slnx --no-restore --nologo`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx --no-build --nologo`：16/16 通过
- 端到端实测：`run` 中生成 `.gjf`、`.chk`、`output.log`、`stdout.log` 和 `stderr.log`
- 端到端实测：`input` 中只保留原始 `.gjf` 和 `molecule.xyz`

---

## v0.17.0（2026-09-26）：本机执行后端与 Gaussian 单点计算启动

### 版本

- 当前版本：`0.17.0`
- 日期：2026-09-26
- 版本类型：新增功能（第四阶段，本机执行后端）

### 改动目的

让单点计算不再停留在“生成 Gaussian 输入文件”，而是可以继续调用本机
`g16`，在后台启动计算，并保存标准输出、标准错误和程序输出文件。

本阶段仍然不实现作业队列。用户提交后立即启动本机进程，后续再替换为远程
HPC 或集群后端。

### 改动内容

新增项目：

```text
src/ChemSculptor.Compute.Local
```

新增类型：

```text
LocalProcessBackendOptions
LocalProcessState
LocalProcessBackend
```

`LocalProcessBackend` 实现 `IComputeBackend`：

- 使用 `ProcessStartInfo` 和 `ArgumentList` 启动本机程序。
- 使用 `UseShellExecute = false`，不依赖命令行字符串拼接。
- 同时捕获标准输出和标准错误。
- 进程退出后写入 `stdout.log` 和 `stderr.log`。
- 退出码为 0 时标记 `Completed`，否则标记 `Failed`。
- 支持取消并终止进程树。

新增 Gaussian 16 适配器：

```text
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapterOptions.cs
```

适配器负责：

- 判断计算方案是否为 Gaussian 16。
- 根据通用计算方案生成 `.gjf`。
- 构建 `g16 输入文件 输出文件` 的命令行参数。
- 从 `PATH` 或现有 `GAUSS_EXEDIR` 解析 Gaussian 可执行文件目录。
- 把 `GAUSS_EXEDIR` 显式传给子进程。

计算公式模型调整：

- `CalculationExecutionContext.Arguments`：程序启动参数。
- `CalculationExecutionContext.EnvironmentVariables`：子进程环境变量。
- `CalculationJob.RunDirectory`：本次作业的运行目录。
- `ICalculationWorkspace.GetJobOutputPath`：取得程序输出文件路径。

单点执行链路调整：

```text
SinglePointCalculationExecutor
  → 解析坐标
  → 创建作业工作区
  → Gaussian16ProgramAdapter 生成输入文件
  → Gaussian16ProgramAdapter 构建执行上下文
  → IComputeBackend.SubmitAsync
  → LocalProcessBackend 启动 g16
```

接口调整：

- `SinglePointExecutionResult`、`AgentResult` 和 API 响应新增输出文件路径。
- WinForms 在收到服务器响应后显示输出文件路径。

### 教程式说明

#### 一、为什么不能把 g16 直接写进 Agent

Agent 负责编排，不应该知道 Gaussian 的输入格式、命令行参数和环境变量。
因此本次把具体程序细节放在 `ChemSculptor.Compute.Gaussian`：

```text
Agent
  只知道 IQuantumProgramAdapter 和 IComputeBackend

Gaussian16ProgramAdapter
  知道 Gaussian 16 要怎样生成输入、怎样启动

LocalProcessBackend
  知道怎样在本机启动一个进程并跟踪它
```

以后增加 ORCA 时，可以新增 ORCA 适配器；以后连接远程 HPC 时，可以新增远程
执行后端。Agent 的主流程不需要复制一份。

#### 二、一次完整调用发生了什么

客户端发送“单点计算”和坐标后：

1. `AgentService` 从会话层得到单点计算意图。
2. `SinglePointCalculationExecutor` 解析坐标，并创建 `job-...` 作业目录。
3. `CalculationDefaults` 提供 CAM-B3LYP、6-31G*、电荷 0、多重度 1、4 核等默认值。
4. `Gaussian16ProgramAdapter` 生成：

```text
<作业目录>\input\<jobId>.gjf
```

5. 适配器构建执行上下文：

```text
可执行文件：g16
参数：<jobId>.gjf <output.log>
运行目录：<作业目录>\run
环境变量：GAUSS_EXEDIR=<Gaussian 可执行文件目录>
```

6. `LocalProcessBackend` 启动进程并立即返回作业编号。
7. 后台监控任务等待 Gaussian 结束，然后保存：

```text
stdout.log
stderr.log
output.log
```

#### 三、为什么要传递 GAUSS_EXEDIR

只把 `g16.exe` 放进 `PATH` 并不一定足够。第一次端到端实测时，Gaussian
启动了，但报告：

```text
No executable for file l1.exe.
Search path GAUSS_EXEDIR is ""
```

原因是当前进程没有继承 Gaussian 启动脚本中的 `GAUSS_EXEDIR`。现在适配器会：

1. 如果系统已有 `GAUSS_EXEDIR`，直接沿用。
2. 如果没有，就从 `PATH` 找到 `g16.exe` 并取得它所在目录。
3. 把这个目录作为 `GAUSS_EXEDIR` 传给本机子进程。

这一步只属于 Gaussian 适配器，不污染通用本机后端。

#### 四、如何运行和检查

启动 API：

```powershell
dotnet run --project src/ChemSculptor.Api --urls http://127.0.0.1:5091
```

提交一个水的单点计算：

```powershell
$body = @{
    coordinateText = "O 0.000000 0.000000 0.117300`nH 0.000000 0.757200 -0.469200`nH 0.000000 -0.757200 -0.469200"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://127.0.0.1:5091/calculations/single-point" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

响应会包含：

```text
jobId
status = Running
inputFilePath
outputFilePath
```

Gaussian 结束后，在输出文件中搜索：

```text
Normal termination of Gaussian 16
```

#### 五、当前阶段的能力边界

已经具备：

```text
本机后台启动 g16
保存输入、输出和错误日志
记录运行状态
返回输入和输出文件位置
```

尚未具备：

```text
作业队列
并行数量控制
计算任务状态 API
取消计算 API
Gaussian 输出自动解析
远程 HPC 执行后端
```

这些能力保留在后续阶段，不改变当前接口方向。

### 验证

- `dotnet build ChemSculptor.slnx --no-restore --nologo`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx --no-build --nologo`：16/16 通过
- 本机端到端实测：水的 CAM-B3LYP/6-31G* 单点计算正常结束
- 实测关键结果：`HF=-76.3801014`
- 实测输出：`Normal termination of Gaussian 16`
- 实测 `stderr.log` 为空

---

## v0.16.0（2026-09-25）：客户端不再生成化学参数

### 版本

- 当前版本：`0.16.0`
- 日期：2026-09-25
- 版本类型：职责边界调整（客户端只发送文本和坐标）

### 改动目的

贯彻“客户端只负责传送，不处理客户信息”的原则：

```text
客户端不决定电荷
客户端不决定自旋多重度
客户端不选择计算方法
客户端只发送原始文本和坐标文本
```

所有化学与计算相关参数由服务器端默认方案、规则和后续交互决定。

### 改动内容

WinForms：

- `AgentMessageRequestDto` 移除 `Charge` 和 `Multiplicity`。
- `SinglePointCalculationRequestDto` 移除 `Charge` 和 `Multiplicity`。
- `SendAgentMessageAsync` 不再写入这两个字段。

Api：

- `AgentMessageRequest` 移除 `Charge` 和 `Multiplicity`。
- `SinglePointCalculationRequest` 移除 `Charge` 和 `Multiplicity`。
- `AgentEndpoints` 与 `CalculationEndpoints` 不再映射这两个字段。

Agent：

- `AgentRequest` 移除 `Charge` 和 `Multiplicity`。
- `AgentSinglePointRequest` 移除 `Charge` 和 `Multiplicity`。
- `AgentService` 调用执行器时只传坐标。
- `SinglePointCalculationExecutor.ExecuteAsync` 改为只接收坐标文本。

Compute：

- 电荷和多重度继续由 `CalculationDefaults` 在服务器端提供：

```text
Charge       = 0
Multiplicity = 1
```

### 当前行为

```text
客户端
  → SessionId + Text + CoordinateText
  → Agent
  → Conversation 解释意图
  → 单点执行器使用服务器默认方案
  → 生成 Gaussian 输入文件
```

用户以后要修改电荷或多重度时，必须通过服务器端参数交互、风险检查和人工确认完成。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：15/15 通过

---

## v0.15.0（2026-09-25）：拆分独立智能体编排层

### 版本

- 当前版本：`0.15.0`
- 日期：2026-09-25
- 版本类型：架构拆分（Api 变薄，编排独立）

### 改动目的

把“HTTP 路由”和“智能体编排”分开，避免 Api 随着任务类型增加而变重：

```text
Api：只做 HTTP 适配
Agent：负责意图到计算执行的编排
Conversation：负责会话、消息、意图和回复
Compute：负责计算模型与执行
```

### 改动内容

新增项目：

```text
src/ChemSculptor.Agent
```

新增类型：

```text
AgentRequest
AgentSinglePointRequest
AgentResult
IAgentService
AgentService
AgentServiceRegistration
```

迁移内容：

```text
SinglePointCalculationExecutor
  从 ChemSculptor.Api 迁移到 ChemSculptor.Agent
```

Api 调整：

- `AgentEndpoints` 只把 HTTP 请求转换为 `AgentRequest` 并调用 `IAgentService`。
- `CalculationEndpoints` 只把 HTTP 请求转换为 `AgentSinglePointRequest` 并调用 `IAgentService`。
- Api 不再直接引用 Conversation、Compute、Compute.Gaussian。
- Api 通过 `AgentServiceRegistration.AddAgentServices` 注册智能体服务。

依赖关系：

```text
Api → Agent
Agent → Conversation → Compute → InputProcessor
Agent → Compute.Gaussian
```

### 处理流程

```text
WinForms 原始文本
   ↓ HTTP
Api /agent/messages
   ↓
IAgentService.HandleMessageAsync
   ↓
ConversationService
   ↓
RuleBasedTaskInterpreter
   ↓
AgentService 根据意图调用 SinglePointCalculationExecutor
   ↓
生成 Gaussian 输入文件
   ↓
Api 返回 HTTP 响应
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：15/15 通过

---

## v0.14.0（2026-09-23）：独立会话层与原始消息处理

### 版本

- 当前版本：`0.14.0`
- 日期：2026-09-23
- 版本类型：新增会话层（不引入 LLM）

### 改动目的

按“客户端只发送原始文本，服务器负责解释”的原则，把会话、消息、意图和回复从 Api 中独立出来，形成 `ChemSculptor.Conversation`。

当前阶段仍只识别单点计算，但处理形式已经改为：

```text
客户端原始文本
   ↓
Conversation 解释意图
   ↓
服务器根据意图调用计算执行器
```

### 改动内容

新增项目：

```text
src/ChemSculptor.Conversation
```

新增模型：

```text
ConversationSession
ConversationMessage
ConversationRequest
ConversationIntent
ConversationQuestion
ConversationReply
```

新增接口与实现：

```text
IConversationRepository
IConversationService
InMemoryConversationRepository
ConversationService
```

Api 调整：

- `AgentEndpoints` 改为先调用 `IConversationService`。
- Conversation 返回结构化 `ConversationReply` 和 `ConversationIntent`。
- 只有在意图为单点计算时，才调用 `SinglePointCalculationExecutor`。
- `Program.cs` 注册会话服务和内存会话仓储。

WinForms 调整：

- `AgentMessageRequestDto` 增加 `SessionId`。
- 发送消息时携带当前会话标识。
- 客户端仍然不解释用户文本。

测试新增：

```text
ConversationServiceTests
  ├── “单点计算”被识别为单点任务
  ├── 未知任务被拒绝
  └── 用户消息和智能体回复写入会话历史
```

### 处理流程

```text
WinForms 发送原始文本
   ↓
POST /agent/messages
   ↓
ConversationService.HandleMessageAsync
   ├── 创建或读取会话
   ├── 保存用户消息
   ├── 调用 ITaskInterpreter
   ├── 生成 ConversationIntent
   └── 保存智能体回复
   ↓
AgentEndpoints 检查 Intent
   ├── 单点计算 → SinglePointCalculationExecutor
   └── 其他     → 返回暂不支持
   ↓
返回 AgentMessageResponse
```

### 设计边界

```text
Conversation：会话、消息、意图、回复
Compute：计算模型、默认方案、任务解释
Compute.Gaussian：Gaussian 输入生成
Api：HTTP 路由与编排
WinForms：界面与 HTTP 客户端
```

Conversation 不生成 Gaussian 输入文件，也不直接处理 HTTP。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：13/13 通过

---

## v0.13.0（2026-09-22）：交互窗口“单点计算”命令

### 版本

- 当前版本：`0.13.0`
- 日期：2026-09-22
- 版本类型：客户端交互命令（不启动计算程序）

### 改动目的

不增加按钮，改为通过交互窗口命令触发单点计算调试流程：

```text
用户输入：单点计算
点击：发送
```

客户端读取当前选择的坐标文件，调用服务器已有端点，并显示输入文件生成结果。

### 改动内容

WinForms：

- 新增命令常量 `单点计算`。
- `SendTextAsync` 识别该命令后调用 `TriggerSinglePointAsync`。
- 新增 `TriggerSinglePointAsync`：
  - 读取当前选择的 txt 坐标文件
  - 组装 `SinglePointCalculationRequestDto`
  - 调用 `POST /calculations/single-point`
  - 显示作业标识、状态和输入文件路径

服务器：

- 复用已有 `/calculations/single-point` 端点。
- 当前只生成 Gaussian 输入文件，不启动 g16。

### 使用方式

```text
1. 启动 Api 和 WinForms
2. 在 WinForms 顶部选择 samples/water.xyz.txt
3. 在底部输入框输入：单点计算
4. 点击“发送”
```

对话区会显示：

```text
单点计算已触发：job-xxxx，状态 InputGenerated。
输入文件：<工作区路径>\input\job-xxxx.gjf
Gaussian 输入文件已生成，尚未启动计算程序。
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：10/10 通过
- 服务器端输入生成链路已完成人工验证

---

## v0.12.1（2026-09-22）：移除 WinForms 临时单点计算调试按钮

### 版本

- 当前版本：`0.12.1`
- 日期：2026-09-22
- 版本类型：界面调整（行为保持不变）

### 改动目的

为后续通过交互窗口命令触发单点计算做准备，先移除 WinForms 中的临时“单点计算（调试）”按钮，恢复原有布局。

### 改动内容

- 移除 `MainForm` 中的 `_triggerSinglePointButton` 字段、初始化和事件订阅。
- 移除按钮对应的 `OnTriggerSinglePointClick` 与 `TriggerSinglePointAsync` 方法。
- 移除不再使用的 `System.Text.Json` 引用。
- 保留以下服务器端和客户端模型：
  - `POST /calculations/single-point`
  - `SinglePointCalculationRequest`
  - `SinglePointCalculationResponse`
  - WinForms 中的单点请求与响应 DTO

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- WinForms 布局恢复为：
  - 发送
  - 发送坐标
  - 提交任务

---

## v0.12.0（2026-09-22）：WinForms 触发单点计算调试链路

### 版本

- 当前版本：`0.12.0`
- 日期：2026-09-22
- 版本类型：调试链路（不启动计算程序）

### 改动目的

让 WinForms 客户端可以触发一次单点计算调试请求，服务器完成：

```text
接收坐标
解析坐标
创建工作区
生成 Gaussian 输入文件
返回作业标识与文件路径
```

当前不启动 Gaussian，也不解析输出，仅用于打通客户端到输入文件生成的链路。

### 改动内容

InputProcessor：

- 新增 `CanonicalGeometryMapper`，把旧解析结果转换为规范几何模型。

Api：

- 新增 `/calculations/single-point` 端点。
- 新增单点计算请求、响应和错误响应契约。
- Api 引用 `ChemSculptor.Compute` 与 `ChemSculptor.Compute.Gaussian`。
- `Program.cs` 注册工作区与 Gaussian 输入生成器，并将依赖注入调用改为显式静态调用。

WinForms：

- 新增“单点计算（调试）”按钮。
- 新增单点计算请求与响应客户端模型。
- 按钮触发时发送坐标文本、默认电荷 0 和默认多重度 1。
- 服务器返回后，在对话区显示作业标识、状态和生成的输入文件路径。

工作区：

- `%ProgramData%\ChemSculptor` 作为首选根目录。
- 如果 ProgramData 不可写，回退到当前用户 LocalAppData。
- 如果 LocalAppData 也不可写，回退到系统临时目录，保证受限环境可调试。

### 调试调用链

```text
WinForms“单点计算（调试）”按钮
  → POST /calculations/single-point
  → GeometryTextParser 解析坐标
  → CanonicalGeometryMapper 转换几何
  → WorkspaceManager 创建作业目录
  → GaussianInputWriter 生成 .gjf
  → 返回 InputGenerated 与输入文件路径
```

### 当前限制

```text
不启动 g16
不解析计算输出
不保存长期计算作业记录
内存暂用 4GB 调试默认值，后续根据电子数计算
电荷和多重度暂用 0 和 1，后续接入交互确认
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：10/10 通过
- 端到端调用 `/calculations/single-point`：
  - 返回状态 `InputGenerated`
  - Gaussian 输入文件成功生成
  - 文件包含 `%nprocshared=4`
  - 文件包含 `#p CAM-B3LYP/6-31G* SP`

---

## v0.11.0（2026-09-20）：Gaussian 输入文件生成第三阶段

### 版本

- 当前版本：`0.11.0`
- 日期：2026-09-20
- 版本类型：新增输入生成（不执行计算程序）

### 改动目的

把默认单点计算方案和规范几何转换为 Gaussian 输入文件，为后续本机执行和输出解析打基础。

当前阶段只生成输入文件：

```text
不启动 Gaussian
不解析输出
不写入结果
```

### 改动内容

新增项目：

```text
src/ChemSculptor.Compute.Gaussian
```

新增类型：

```text
GaussianInputOptions
  内存、核数、检查点路径和标题

GaussianInputWriter
  生成 Gaussian 单点输入文件
```

默认输入内容：

```text
%chk=<与输入文件同名的 .chk>
%mem=<调用方提供的内存设置>
%nprocshared=4

#p CAM-B3LYP/6-31G* SP

标题

0 1
O x y z
H x y z
...
```

### 设计说明

输入生成与程序执行分离：

```text
CalculationSpec
  提供任务类型、方法、基组、电荷和多重度

CanonicalGeometry
  提供原子坐标

GaussianInputOptions
  提供内存、核数、检查点路径和标题

GaussianInputWriter
  只负责生成输入文件
```

当前只支持：

```text
CalculationTaskType.SinglePoint
```

其他任务类型返回 `NotSupportedException`。

### 验证

新增测试：

```text
GaussianInputWriterTests
  ├── 默认 CAM-B3LYP/6-31G* 单点输入内容正确
  └── 非单点任务被拒绝
```

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：10/10 通过
- 未启动任何计算程序

---

## v0.10.0（2026-09-20）：计算工作区管理第二阶段

### 版本

- 当前版本：`0.10.0`
- 日期：2026-09-20
- 版本类型：新增工作区管理（不执行计算）

### 改动目的

实现计算工作区的目录规则与创建逻辑，为后续保存几何资产、生成输入文件、运行计算和保存结果提供统一文件结构。

默认工作区根目录：

```text
%ProgramData%\ChemSculptor
```

### 改动内容

`ChemSculptor.Compute` 新增：

```text
CalculationWorkspaceOptions
  工作区根目录与各子目录名称配置

CalculationWorkspacePaths
  几何文件、作业清单、坐标、输出、结果等固定文件名

WorkspaceManager
  实现 ICalculationWorkspace
  负责路径生成、目录创建和标识校验
```

`ICalculationWorkspace` 新增：

```text
EnsureGeometryWorkspaceAsync
EnsureJobWorkspaceAsync
```

新增测试：

```text
WorkspaceManagerTests
  ├── 路径生成符合约定
  ├── 创建工作区时目录实际存在
  └── 非法标识被拒绝
```

### 目录结构

```text
%ProgramData%\ChemSculptor/
  geometries/
    <geometryId>/
      original.txt
      canonical.json
      validation.json

  jobs/
    <jobId>/
      manifest.json
      input/
        molecule.xyz
      run/
        output.log
      results/
        result.json
        summary.txt
        validation.json
```

### 安全约束

- 目录名只允许字母、数字、连字符和下划线
- 禁止路径分隔符和 `..`
- 路径由 ID 生成，不使用用户提供的文件名
- 工作区根目录可配置，不写死在代码中

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：8/8 通过
- 未调用任何计算程序

---

## v0.9.0（2026-09-20）：计算模型与接口第一阶段

### 版本

- 当前版本：`0.9.0`
- 日期：2026-09-20
- 版本类型：新增计算框架（仅模型与接口，不执行计算）

### 改动目的

建立计算任务的数据结构和扩展点，为后续本机 Gaussian 单点计算、远程集群和 ORCA 适配预留统一基础。

当前阶段只定义：

```text
计算任务类型
计算方案
计算参数
计算作业
计算结果
执行上下文
风险与审批模型
```

不实现具体程序执行、输入文件生成和输出解析。

### 改动内容

新增项目：

```text
src/ChemSculptor.Compute
```

新增模型：

```text
CalculationTaskType
CalculationSpec
CalculationParameter
CalculationRequest
CalculationTask
CalculationJob
CalculationResult
CalculationExecutionContext
CalculationValidationReport
RiskAssessment
CalculationQuestion
CalculationPlan
ApprovalDecision
```

新增扩展接口：

```text
IQuantumProgramAdapter
IComputeBackend
ICalculationQueue
ICalculationScheduler
ICalculationWorkspace
ICalculationRepository
ICalculationParameterValidator
IScientificRiskEvaluator
IApprovalService
ICalculationPlanner
ISinglePointCalculationService
```

新增默认方案：

```text
CalculationDefaults
  默认程序：Gaussian 16
  默认任务：SinglePoint
  默认方法：CAM-B3LYP
  默认基组：6-31G*
  默认电荷：0
  默认多重度：1
  默认核数：4
```

### 设计说明

计算模型保持程序无关：

```text
模型和接口中不绑定具体程序
程序名称使用字符串保存
只有默认方案中写入默认程序 Gaussian 16
Gaussian / ORCA 的差异由后续程序适配器实现
```

计算与执行分离：

```text
CalculationSpec
  描述“怎么算”

CalculationJob
  描述“算哪一次”

IQuantumProgramAdapter
  负责输入生成、命令构建、输出解析

IComputeBackend
  负责本机或远程执行

ICalculationScheduler
  负责并发控制，当前只预留接口
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 代码中除默认程序 `Gaussian 16` 外，不包含具体计算程序绑定

---

## v0.8.0（2026-09-15）：技能命名统一去除 Container

### 版本

- 当前版本：`0.8.0`
- 日期：2026-09-15
- 版本类型：架构命名重构（接口路径与字段名同步调整）

### 改动目的

为避免“技能容器”“DI 容器”“Docker 容器”三个概念混淆，统一技能相关命名，不再使用 `Container`：

```text
IContainerRegistry → ISkillRegistry
ContainerRegistry  → SkillRegistry
ISkillContainer    → ISkill
EchoSkillContainer → EchoSkill
ContainerDescriptor → SkillDescriptor
WorkflowNode.Container → WorkflowNode.Skill
TaskRequest.ContainerId → TaskRequest.SkillId
```

### 改动内容

Domain：

- `ISkillContainer` 改为 `ISkill`
- `IContainerRegistry` 改为 `ISkillRegistry`
- `ContainerDescriptor` 改为 `SkillDescriptor`
- `WorkflowNode.Container` 改为 `WorkflowNode.Skill`
- `TaskRequest.ContainerId` 改为 `TaskRequest.SkillId`

Core：

- `ContainerRegistry.cs` 改为 `SkillRegistry.cs`
- `EchoSkillContainer.cs` 改为 `EchoSkill.cs`
- `WorkflowEngine` 改为通过 `ISkillRegistry` 解析并调用 `ISkill`

Api：

- `RegisterContainerRequest` 改为 `RegisterSkillRequest`
- `ContainerEndpoints` 改为 `SkillEndpoints`
- 路由 `/containers` 改为 `/skills`
- 路由 `/containers/register` 改为 `/skills/register`
- 示例工作流 JSON 字段 `container` 改为 `skill`

Tests：

- 测试实现 `RecordingContainer` 改为 `RecordingSkill`
- 所有技能接口与注册表引用同步更新

文档：

- `README.md` 和教程中的技能命名同步更新
- `docs/Coding-Conventions.md` 增加技能命名约定：技能相关命名禁止使用 `Container`

### 教程式说明

命名职责现在非常明确：

```text
DI 容器（DI Container）
  负责创建对象和管理生命周期

SkillRegistry
  负责登记和查找技能

ISkill / EchoSkill
  负责执行具体科学能力

Docker 容器
  以后作为技能的运行环境
```

接口路径变化：

```text
旧：GET  /containers
新：GET  /skills

旧：POST /containers/register
新：POST /skills/register
```

工作流定义变化：

```json
旧：{ "id": "structure", "container": "echo" }
新：{ "id": "structure", "skill": "echo" }
```

### 验证

- `dotnet build ChemSculptor.slnx`：构建通过
- `dotnet test ChemSculptor.slnx`：测试通过
- 技能相关命名扫描不再出现 `Container`（WinForms 自带 `SplitContainer` 除外）

---

## v0.7.0（2026-09-13）：几何接收与解析框架骨架

### 版本

- 当前版本：`0.7.0`
- 日期：2026-09-13
- 版本类型：新增框架（不包含真实解析逻辑）

### 改动目的

为复杂几何输入（例如 ONIOM 分层模型）建立可扩展的处理框架，把以下四层职责分开：

```text
原始文件层 → 解析器层 → 规范模型层 → 验证层
```

当前只搭建接口、模型和流程骨架，不实现任何具体格式解析，也不改动现有 `/geometries` 的简单 XYZ 行为。

### 改动内容

在 `ChemSculptor.InputProcessor` 下新增 `GeometryIntake` 框架：

```text
GeometryIntakeModels.cs
  原始文件、规范几何、原子、片段、层、链接原子、约束、诊断

GeometryParserFramework.cs
  解析器接口、解析结果、解析器注册表
  XYZ / Gaussian / ORCA / ONIOM 解析器骨架

GeometryValidationFramework.cs
  验证器接口、验证报告、骨架验证器

GeometryAssetFramework.cs
  几何资产、资产仓储接口、内存实现

GeometryIntakeService.cs
  串联“提交 → 选择解析器 → 解析 → 验证 → 保存资产”
```

### 教程式说明

#### 处理流程

```text
RawGeometrySubmission（原始提交）
   ↓
GeometryParserRegistry（选择解析器）
   ↓
IGeometryParser（格式解析器，当前为骨架）
   ↓
CanonicalGeometry（规范几何模型）
   ↓
IGeometryValidator（验证器，当前为骨架）
   ↓
GeometryAsset（原始文件 + 解析结果 + 验证报告）
   ↓
InMemoryGeometryAssetRepository（保存资产）
```

#### 分层设计要点

- 接收层只保存原始文件，不解释格式
- 每种格式对应一个解析器，不再把不同格式的判断堆进同一个方法
- 解析器统一输出 `CanonicalGeometry`
- ONIOM 的层、片段、链接原子、约束属于规范模型，不属于计算任务参数
- 验证器独立于解析器，便于以后增加坐标、分层、链接原子等检查

#### 当前状态

```text
已建立：接口、模型、注册表、资产仓储、接收服务
未实现：具体格式解析、具体验证规则
未接入：Api 端点仍使用原有简单 XYZ 解析流程
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 现有 `/geometries` 行为和已有客户端功能未改变

---

## v0.6.1（2026-09-12）：补充标准中文 C# 注释并最终确定编码约定

### 版本

- 当前版本：`0.6.1`
- 日期：2026-09-12
- 版本类型：代码注释与规范补充（不改变运行行为）

### 改动目的

为现有代码补充标准中文 C# 注释，并把以下原则正式确立为项目约定：

1. 禁止顶层语句
2. 去除可选语法糖
3. `async / await` 不属于要避免的语法糖，必须保留
4. 不使用扩展方法调用写法，改为显式静态调用
5. 注释统一使用标准 C# 注释风格与中文说明

### 改动内容

为以下项目补充 XML 注释与关键逻辑注释：

```text
ChemSculptor.Domain
ChemSculptor.Core
ChemSculptor.InputProcessor
ChemSculptor.Api
ChemSculptor.WinForms
ChemSculptor.Core.Tests
```

注释规范：

- 公共类型、接口、方法和属性使用 `/// <summary>` XML 注释
- 关键内部逻辑使用 `//` 中文行注释
- 注释说明职责、输入输出、边界条件与平台必需机制的原因

同步更新：

```text
docs/Coding-Conventions.md
  增加注释规范章节
  明确 async/await 例外
  明确扩展方法必须显式静态调用
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过

---

## v0.6.0（2026-09-10）：全项目去除可选语法糖

### 版本

- 当前版本：`0.6.0`
- 日期：2026-09-10
- 版本类型：代码风格与架构重构（行为保持一致）

### 改动目的

按 `docs/Coding-Conventions.md` 的最终口径重构已有代码：

- 保留 `async / await`
- 不使用扩展方法调用写法
- 去除其他可选语法糖，采用传统、显式写法

### 改动内容

Domain：

- `record`、`required`、`init` 改为普通类与可读写属性
- 集合表达式、对象初始化器改为显式构造与逐项赋值

Core：

- LINQ、Lambda、集合表达式、对象初始化器、表达式体成员改为 `for` / `foreach`、命名方法与显式赋值
- `WorkflowStateRules` 改为显式构建转换表
- `WorkflowEngine` 改为显式循环调度，不使用 LINQ

InputProcessor：

- 几何与请求模型改为普通类
- LINQ、集合表达式、范围切片改为循环、显式集合与 `Substring`

Api：

- 所有端点改为显式静态调用：
  `EndpointRouteBuilderExtensions.MapGet / MapPost / MapGroup`
- 端点处理改为命名静态方法，不再使用 Lambda
- 匿名响应类型改为显式响应类
- `Program.cs` 保持传统 `Main`，依赖查询不再使用扩展方法
- `POST /client/jobs` 输入从 multipart 表单改为 `text/plain` 原始文本，避免表单与防伪依赖

WinForms：

- 保持三区对话界面
- 事件 Lambda 改为命名事件方法
- LINQ、switch 表达式、字符串插值、范围切片、`??=`、对象初始化器与集合表达式全部改为传统写法
- 提交任务改为直接发送 `text/plain` 文本，与 Api 新输入方式一致

Tests：

- 测试数据构造改为显式对象与集合
- `Assert.All` 的 Lambda 改为 `foreach`

### 教程式说明

这次改动不改变系统架构，只改变代码表达方式。核心映射如下：

```text
record              → class + 可读写属性
对象初始化器         → new + 逐项属性赋值
集合表达式 []        → new List<T>() / new T[] { }
LINQ                → for / foreach
Lambda              → 命名方法
扩展方法 app.MapGet  → EndpointRouteBuilderExtensions.MapGet(app, ...)
匿名类型 new { }     → 显式响应类
字符串插值 $"..."    → 字符串拼接
三元 ? :             → if / else
?? / ??=            → if 判断
switch 表达式        → if / else
范围切片 [..n]       → Substring
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 运行时回归：
  - 根端点返回 13 个接口
  - `/geometries` 接收水分子坐标，返回 `H2O`、3 个原子
  - `/client/jobs` 接收纯文本任务，任务最终为 `Passed` 且结果可读取

---

## v0.5.1（2026-09-10）：Api 顶层语句改为传统 Main 写法

### 版本

- 当前版本：`0.5.1`
- 日期：2026-09-10
- 版本类型：代码风格重构（行为不变）

### 改动目的

按用户学习习惯，把 `ChemSculptor.Api` 的顶层语句改写为传统 `public static async Task Main` 形式，便于对照学习，不改变任何运行行为。

### 改动内容

- 重写 `src/ChemSculptor.Api/Program.cs`：
  - 新增 `namespace ChemSculptor.Api` 与 `public static class Program`
  - 原顶层启动语句全部移入 `Main(string[] args)`
  - 服务注册、示例工作流载入、端点挂载、`app.Run()` 顺序保持不变

### 验证

- `dotnet build src/ChemSculptor.Api/ChemSculptor.Api.csproj`：0 警告 0 错误

---

## v0.5.0（2026-09-07）：WinForms 对话式界面框架

### 版本

- 当前版本：`0.5.0`
- 日期：2026-09-07
- 版本类型：界面重构（仅 WinForms，不涉及 Api/Core/Domain）

### 改动目的

把 WinForms 界面从“工具按钮面板”升级为 Codex 风格的对话式外壳：左侧会话列表 + 中间对话流 + 底部输入区，为以后“自然语言 → 服务器理解 → 人工确认”的人机协同流程预留界面形态。

### 改动内容

仅修改 `ChemSculptor.WinForms`：

- 重写 `MainForm.cs`：新增左侧会话列表、中间消息流、底部输入与操作区。
- 新增本地会话模型：`ChatSession`、`ChatMessage`（仅内存，会话切换不丢失当前运行状态）。
- 保留并接入现有能力：
  - 选择 txt
  - 发送坐标到 `POST /geometries`
  - 提交任务到 `POST /client/jobs`
  - 轮询任务状态与结果
  - 保存最近一份结果 txt
- 自然语言输入当前只记录为会话消息，并提示“理解功能后续接入”，不假装服务器已支持对话。

### 教程式说明

#### 界面结构

```text
左侧：会话列表（新建会话 / 切换会话）
中间：对话流（用户消息、系统消息、错误与提示）
底部：自然语言输入 + 发送
顶部：服务地址、选择 txt、保存结果
```

#### 当前可用的三条真实链路

```text
1. 输入文本 → 记录为用户消息（暂不发送服务器）
2. 选择坐标 txt → 发送坐标 → /geometries → 系统消息显示分子式
3. 选择任务 txt → 提交任务 → /client/jobs → 轮询状态 → 结果消息
```

### 验证

- `dotnet build src/ChemSculptor.WinForms/ChemSculptor.WinForms.csproj`：0 警告 0 错误
- Api/Core/Domain/Tests 未改动

---

## v0.4.0（2026-09-06）：服务器端接收分子坐标（阶段 A 第一切片）

### 版本

- 当前版本：`0.4.0`
- 日期：2026-09-06
- 版本类型：新增功能（MINOR）

### 改动目的

按照阶段 A 推进“坐标进、结论出”的最小闭环，先打通第一环：客户端把分子坐标文本发送到服务器，服务器完成接收、解析并返回结构化确认。

### 改动内容

`ChemSculptor.InputProcessor` 新增几何文本解析能力：

- `GeometryModels.cs`：`GeometryAtom`、`MolecularGeometry`。
- `GeometryTextParser.cs`：`IGeometryTextParser` 接口和 XYZ 文本解析实现，支持标准 XYZ 文本（原子数行、注释行、`元素 x y z` 坐标行），输出分子式和原子列表，并对非坐标行/数量不一致给出诊断。

`ChemSculptor.Api` 新增坐标接收端点：

- 新增 `Endpoints/GeometryEndpoints.cs`：`POST /geometries`，接收 `text/plain` 坐标文本，解析后返回分子式、原子数、原子坐标和诊断。
- `Program.cs` 注册 `IGeometryTextParser`，挂载端点，并把 `POST /geometries` 加入根端点列表。

`ChemSculptor.WinForms` 增加坐标发送入口：

- 新增“发送坐标”按钮，复用当前选择的 txt。
- 新增 `GeometryAtomDto`、`GeometrySubmitResult` 本地模型。
- 发送成功后显示服务器返回的分子式、原子数和原子列表。

新增示例文件：

```text
samples/water.xyz.txt
```

### 教程式说明

#### 数据流

```text
WinForms 选择 water.xyz.txt
  → 点击“发送坐标”
  → POST /geometries（text/plain 原始坐标文本）
  → GeometryTextParser 解析 XYZ
  → 返回 JSON：{ formula, atomCount, atoms }
```

#### XYZ 文本格式

```text
3
water molecule
O 0.000000 0.000000 0.117300
H 0.000000 0.757200 -0.469200
H 0.000000 -0.757200 -0.469200
```

第 1 行是原子数，第 2 行是名称，之后每行是 `元素 x y z`。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 端到端实测：向 `POST /geometries` 发送 `samples/water.xyz.txt`，服务器返回 `H2O`、3 个原子及完整坐标，诊断为空。

---

## v0.3.0（2026-09-03）：WinForms 与 ChemSculptor 服务器端独立

### 版本

- 当前版本：`0.3.0`
- 日期：2026-09-03
- 版本类型：架构调整 + 新增功能（MINOR）

### 改动目的

按用户的界面约束调整架构：

1. WinForms 中禁止出现任何 `using ChemSculptor`，界面不再引用内核或领域模型。
2. WinForms 只负责：接收客户的 txt 输入 → 发送给 ChemSculptor → 被动轮询反馈 → 接收结束信息和结果 txt。
3. 单机版和以后的服务器版保持同构：单机时 ChemSculptor 本体运行在本地 Api；部署到服务器后，WinForms 只改服务地址即可。

### 改动内容

`ChemSculptor.WinForms` 重写为纯 HTTP 客户端：

- 移除对 `ChemSculptor.Core`、`ChemSculptor.Domain` 的项目引用。
- 删除 `Services/IChemSculptorService.cs` 和 `Services/LocalChemSculptorService.cs`。
- 新增本地 DTO：`Models/ClientJobSummary.cs`。
- `MainForm` 通过 `HttpClient` 完成：选择 txt → 上传到 `/client/jobs` → 定时轮询 `/client/jobs/{id}/status` → 结果就绪后读取 `/client/jobs/{id}/result`，并可保存为 txt。
- 界面层不再使用任何 ChemSculptor 命名空间。

新增 `ChemSculptor.InputProcessor`（客户输入解析工程）：

- `IClientInputParser`：输入解析器接口，未来可扩展 JSON/二进制解析器。
- `TextClientInputParser`：当前文本解析实现。
- `ProcessedClientRequest`：解析结果，包含工作流 Id、目标描述和原始文本。
- 文本请求格式 v1：支持 `workflow:` 和 `goal:` 行；未指定工作流时默认 `tadf_mechanism_diagnosis`。

`ChemSculptor.Api` 增加客户端作业能力：

- 新增 `Client/ClientJob.cs`、`Client/ClientJobService.cs`。
- 新增 `Endpoints/ClientJobEndpoints.cs`。
- `Program.cs` 注册 `TextClientInputParser` 和 `ClientJobService`，并挂载客户端作业端点。
- `ChemSculptor.Api.csproj` 引用 `ChemSculptor.InputProcessor`。
- 作业在 Api 进程内后台执行（单用户阶段不建队列，按用户决定预留位置）。

### 教程式说明

#### 现在的整体形态

```text
WinForms 客户端（纯界面，零 ChemSculptor 依赖）
   │ 选择用户 txt → HTTP 上传
   │ 定时轮询状态
   │ 下载结果 txt
   ▼
ChemSculptor.Api（本地运行 = 单机版；以后部署到服务器 = 服务器版）
   ├── InputProcessor：把客户输入文件解析成内核可用请求
   └── WorkflowEngine + 技能容器
```

#### 一次完整数据流

```text
用户选择 txt
  → WinForms POST /client/jobs（multipart 上传原始文本）
  → Api 保存任务，后台执行
  → InputProcessor 解析 workflow:/goal: 行
  → Api 选择工作流模板并调用 WorkflowEngine
  → WinForms 定时轮询 GET /client/jobs/{id}/status
  → 状态变为 Passed/Failed 且 HasResult=true
  → WinForms GET /client/jobs/{id}/result，得到结果 txt
```

#### 文本请求格式示例（v1，占位）

```text
workflow: tadf_mechanism_diagnosis
goal: 判断超分子体系是否为 TADF 并定位主要发光通道
```

#### 新增端点

| 方法 | 路径 | 作用 |
|---|---|---|
| `POST` | `/client/jobs` | 接收客户 txt，创建客户端任务 |
| `GET` | `/client/jobs/{id}/status` | 轮询任务状态 |
| `GET` | `/client/jobs/{id}/result` | 下载结果 txt |

#### 如何运行

先启动 Api：

```powershell
dotnet run --project src/ChemSculptor.Api --urls http://127.0.0.1:5080
```

再运行 WinForms：

```powershell
dotnet run --project src/ChemSculptor.WinForms
```

WinForms 顶部的服务地址默认是 `http://127.0.0.1:5080`；以后服务器版只需改成远程地址。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 客户端/服务器独立模式代码已按当前代码跑通（用户环境确认）；本文档更新仅涉及记录同步，不改动代码。

---

## v0.2.0（2026-09-02）：WinForms 单机版（历史过渡，已从当前代码中移除）

> 本节是历史记录。v0.2.0 描述的 WinForms 直连内核形态在当前仓库已不存在，相关文件已删除；当前客户端形态以 v0.3.0 为准。

### 版本

- 当前版本：`0.2.0`（历史）
- 日期：2026-09-02
- 版本类型：新增功能（MINOR）

### 当时做了什么

- 把 WinForms 加入解决方案，作为“单机版操作界面”。
- WinForms 曾直接引用 `ChemSculptor.Core` 与 `ChemSculptor.Domain`。
- 提供过 `IChemSculptorService` / `LocalChemSculptorService` 服务层，以及“载入示例 / 刷新 / 执行所选”按钮。
- `MainForm` 曾订阅事件总线，把 `task.started`、`task.completed` 等事件实时显示到日志框。

### 与当前代码的差异

当前 `ChemSculptor.WinForms` 已不含上述服务和按钮：

- csproj 不再引用 `ChemSculptor.Core` 与 `ChemSculptor.Domain`，没有任何项目引用。
- 不存在 `Services/` 文件夹，不存在 `IChemSculptorService`、`LocalChemSculptorService`。
- 不存在“载入示例 / 刷新 / 执行所选”等直连内核按钮。
- 界面只保留纯 HTTP 客户端功能：服务地址、选择 txt、提交任务、轮询状态、显示并保存结果。

v0.2.0 的代码形态已被 v0.3.0 取代，仅作为过程记录保留。

### 验证（当时）

- v0.2.0 时代构建与测试通过；其代码现已删除，不再作为运行基线。

---

## v0.1.0（历史）：框架骨架

首次建立的 C# 框架骨架：Domain 契约、Core 极简内核、Api 宿主、Tests 测试，以及示例工作流和教程文档。本次改动从 v0.1.0 开始登记版本号。

---

## 未来改动记录模板

以后每次改动后，在文档顶部追加以下格式：

```markdown
## vX.Y.Z（日期）：改动标题

### 版本
- 当前版本：X.Y.Z
- 日期：YYYY-MM-DD
- 版本类型：新增功能 / 修复 / 架构调整

### 改动目的
为什么做这次改动。

### 改动内容
改了哪些项目/文件，新增、修改、删除了什么。

### 教程式说明
这次改动在系统里扮演什么角色，和已有部分的关系，如何运行验证。

### 验证
- 构建结果
- 测试结果
- 其他验证方式
```
