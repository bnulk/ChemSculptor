# ChemSculptor“单点计算”代码全流程说明

> 适用版本：v0.17.0 之后
> 当前阶段目标：客户端发送原始文本，服务器识别“单点计算”，生成 Gaussian 输入并启动本机 g16
> 当前未实现：解析输出、读取能量、查询计算状态、远程执行

本文按真实代码顺序讲解一次“单点计算”从客户端到服务器的全过程。建议对照代码阅读。

---

## 1. 一句话概括

```text
客户端发送原始文本和坐标
   ↓
服务器会话层保存消息并识别意图
   ↓
服务器根据意图调用单点计算执行器
   ↓
解析坐标、创建工作区、生成 Gaussian 输入文件
   ↓
构建 g16 命令并提交本机执行后端
   ↓
返回作业标识、状态和文件路径
   ↓
客户端显示结果
```

---

## 2. 各项目职责

| 项目 | 职责 |
|---|---|
| `ChemSculptor.WinForms` | 客户端界面，只发送原始文本和坐标 |
| `ChemSculptor.Api` | HTTP 路由与适配，不包含业务编排 |
| `ChemSculptor.Agent` | 智能体编排：意图到计算执行 |
| `ChemSculptor.Conversation` | 会话、消息、意图和回复 |
| `ChemSculptor.Compute` | 计算模型、默认方案、任务解释 |
| `ChemSculptor.Compute.Gaussian` | Gaussian 输入生成、命令和运行上下文 |
| `ChemSculptor.Compute.Local` | 本机进程启动、状态跟踪和日志保存 |
| `ChemSculptor.InputProcessor` | 坐标解析与规范几何转换 |

依赖方向：

```text
WinForms → HTTP → Api
Api → Agent
Agent → Conversation → Compute → InputProcessor
Agent → Compute.Gaussian
Agent → Compute.Local
```

客户端不引用任何 ChemSculptor 服务器端项目。

---

## 3. 关键数据模型

### 客户端请求

`AgentMessageRequestDto`：

```text
SessionId
Text
CoordinateText
```

### 服务器请求

`AgentMessageRequest`：

```text
SessionId
Text
CoordinateText
```

### 会话解释结果

`ConversationReply`：

```text
ReplyMessage
ReplyType
RequiresUserAction
Intent
Questions
JobId
Artifacts
Diagnostics
```

`ConversationIntent`：

```text
TaskType
WorkflowId
IsSupported
Confidence
Diagnostics
```

### 计算执行结果

`SinglePointExecutionResult`：

```text
Succeeded
Error
Diagnostics
JobId
Status
InputFilePath
Message
```

### 智能体请求与结果

`AgentRequest`：

```text
SessionId
Text
CoordinateText
Charge
Multiplicity
```

`AgentResult`：

```text
IsSupported
Error
TaskType
JobId
Status
InputFilePath
Message
Diagnostics
```

---

## 4. 第 1 步：客户端发送原始文本

位置：`src/ChemSculptor.WinForms/MainForm.cs`

用户操作：

```text
1. 选择一个坐标 txt 文件
2. 在输入框输入：单点计算
3. 点击“发送”
```

客户端入口：

```csharp
private async Task SendTextAsync()
{
    string text = _inputBox.Text;

    if (string.IsNullOrWhiteSpace(text))
    {
        AppendMessage("error", "请先在输入框写下你的目标。");
        return;
    }

    _inputBox.Clear();
    AppendMessage("user", text);
    await SendAgentMessageAsync(text);
}
```

关键点：

```text
客户端不 Trim 文本
客户端不识别“单点计算”
客户端只把原始文本交给服务器
```

---

## 5. 第 2 步：客户端读取坐标并发送 HTTP 请求

位置：`MainForm.SendAgentMessageAsync`

### 读取坐标

```csharp
string coordinateText = await File.ReadAllTextAsync(filePath);
```

### 组装请求

```csharp
AgentMessageRequestDto request = new AgentMessageRequestDto();
request.SessionId = _activeSession.Id;
request.Text = text;
request.CoordinateText = coordinateText;
```

客户端不决定电荷和多重度。它们由服务器端默认方案提供，后续通过服务器端交互确认。

### 发送请求

```csharp
HttpResponseMessage response =
    await _http.PostAsync(Endpoint("/agent/messages"), content);
```

实际请求：

```text
POST http://127.0.0.1:5178/agent/messages
Content-Type: application/json
```

请求体示例：

```json
{
  "sessionId": "session-1",
  "text": "单点计算",
  "coordinateText": "O 0.000000 0.000000 0.117300\nH ..."
}
```

---

## 6. 第 3 步：服务器路由匹配

位置：`src/ChemSculptor.Api/Endpoints/AgentEndpoints.cs`

启动时登记：

```csharp
RouteGroupBuilder agent = EndpointRouteBuilderExtensions.MapGroup(app, "/agent");
EndpointRouteBuilderExtensions.MapPost(agent, "/messages", SubmitMessageAsync);
```

对应路由：

```text
POST /agent/messages → SubmitMessageAsync
```

`Program.cs` 在启动时调用：

```csharp
AgentEndpoints.MapAgentEndpoints(app);
```

---

## 7. 第 4 步：Api 交给 Agent，Agent 再交给会话层

位置：

```text
Api/Endpoints/AgentEndpoints.cs
Agent/AgentService.cs
```

Api 端点只做 HTTP 到 AgentRequest 的转换：

```csharp
AgentRequest agentRequest = new AgentRequest();
agentRequest.SessionId = request.SessionId;
agentRequest.Text = request.Text;
agentRequest.CoordinateText = request.CoordinateText;

AgentResult result = await agentService.HandleMessageAsync(agentRequest, cancellationToken);
```

随后 `AgentService` 调用会话层：

```csharp
ConversationRequest conversationRequest = new ConversationRequest();
conversationRequest.SessionId = request.SessionId;
conversationRequest.Text = request.Text;

ConversationReply conversationReply =
    await _conversationService.HandleMessageAsync(conversationRequest, cancellationToken);
```

此时 Api 只负责 HTTP；原始文本、坐标和参数都放在 `AgentRequest` 中，由 Agent 决定何时使用。

---

## 8. 第 5 步：会话服务保存消息并解释意图

位置：`src/ChemSculptor.Conversation/ConversationService.cs`

### 创建或读取会话

```csharp
ConversationSession? session = await _repository.GetSessionAsync(sessionId, cancellationToken);

if (session == null)
{
    session = new ConversationSession();
    session.SessionId = sessionId;
    session.Title = "新会话";
    session.WorkflowId = "workflow-" + Guid.NewGuid().ToString("N");
    await _repository.SaveSessionAsync(session, cancellationToken);
}
```

一个会话对应一个工作流标识。

### 保存用户消息

```csharp
ConversationMessage userMessage = new ConversationMessage();
userMessage.SessionId = sessionId;
userMessage.Role = ConversationMessageRole.User;
userMessage.Text = request.Text;
await _repository.AppendMessageAsync(userMessage, cancellationToken);
```

### 调用任务解释器

```csharp
InterpretedTask interpretedTask =
    await _taskInterpreter.InterpretAsync(request.Text, cancellationToken);
```

### 生成结构化意图

```csharp
ConversationIntent intent = new ConversationIntent();
intent.TaskType = interpretedTask.TaskType;
intent.WorkflowId = interpretedTask.WorkflowId;
intent.IsSupported = interpretedTask.IsSupported;
intent.Confidence = interpretedTask.Confidence;
```

### 生成回复并保存

当前支持单点计算时：

```text
已识别任务类型：单点计算。
```

不支持时：

```text
当前阶段只支持单点计算。
```

会话层把用户消息和智能体回复都写入会话历史。

---

## 9. 第 6 步：规则解释器识别“单点计算”

位置：`src/ChemSculptor.Compute/TaskInterpretation.cs`

当前规则关键词：

```text
单点
single point
sp
```

只要文本包含其中之一，就解释为：

```text
TaskType   = SinglePoint
WorkflowId = single_point
IsSupported = true
Confidence  = 1.0
```

当前是规则解释，不涉及 LLM。

以后可以替换为：

```text
规则解释 + LLM 兜底 + 规则校验
```

---

## 10. 第 7 步：Agent 根据意图决定是否执行

位置：`src/ChemSculptor.Agent/AgentService.cs`

```csharp
if (!conversationReply.Intent.IsSupported)
{
    // 返回“当前阶段无法执行该请求”
}

if (conversationReply.Intent.TaskType != CalculationTaskType.SinglePoint)
{
    // 返回“该任务类型尚未实现”
}
```

只有意图为单点计算时，才调用：

```csharp
SinglePointExecutionResult executionResult = await executor.ExecuteAsync(
    request.CoordinateText,
    cancellationToken);
```

这一步体现了边界：

```text
Conversation 负责理解
Agent 负责编排
Compute/InputProcessor 负责执行
Api 只负责 HTTP
```

---

## 11. 第 8 步：单点计算执行器

位置：`src/ChemSculptor.Agent/SinglePointCalculationExecutor.cs`

执行顺序：

```text
1. 检查坐标文本
2. 解析坐标
3. 检查是否有原子
4. 生成 jobId
5. 创建作业工作区
6. 保存 molecule.xyz
7. 转换为 CanonicalGeometry
8. 创建默认单点方案
9. 使用服务器默认电荷和多重度
10. 生成 Gaussian 输入文件
```

### 生成 jobId

```csharp
string jobId = "job-" + Guid.NewGuid().ToString("N");
```

### 创建工作区

```csharp
await _workspace.EnsureJobWorkspaceAsync(jobId, cancellationToken);
```

### 保存坐标副本

```csharp
string coordinatePath = Path.Combine(
    _workspace.GetInputDirectory(jobId),
    CalculationWorkspacePaths.JobCoordinatesFileName);
await File.WriteAllTextAsync(coordinatePath, coordinateText, cancellationToken);
```

得到：

```text
input/molecule.xyz
```

---

## 12. 第 9 步：解析坐标

位置：`src/ChemSculptor.InputProcessor/GeometryTextParser.cs`

解析器支持：

```text
标准 XYZ（原子数行 + 名称行 + 坐标行）
简化输入（只有 元素 x y z 行）
```

对水分子输入，返回：

```text
MolecularGeometry
  SourceName = 未命名分子
  Formula    = H2O
  Atoms      = O、H、H
  Diagnostics = 空
```

如果解析不到原子，执行器返回失败，不会生成输入文件。

---

## 13. 第 10 步：转换为规范几何

位置：`src/ChemSculptor.InputProcessor/GeometryIntake/CanonicalGeometryMapper.cs`

转换规则：

```text
GeometryAtom → CanonicalAtom
原子序号从 1 开始
元素与坐标原样复制
单位标记为 Angstrom
记录来源 jobId
```

坐标数值不做任何数学变换。

---

## 14. 第 11 步：创建默认单点方案

位置：`src/ChemSculptor.Compute/CalculationDefaults.cs`

默认值：

```text
Program      = Gaussian 16
TaskType     = SinglePoint
Method       = CAM-B3LYP
Basis        = 6-31G*
Charge       = 0
Multiplicity = 1
ProcessorCount = 4
```

执行器直接使用默认方案中的电荷和多重度。当前阶段客户端不传这两个值，
后续要修改时由服务器端参数交互和审批完成。

---

## 15. 第 12 步：生成 Gaussian 输入文件

位置：`src/ChemSculptor.Compute.Gaussian/GaussianInputWriter.cs`

生成内容：

```text
%chk=<jobId>.chk
%mem=4GB
%nprocshared=4

#p CAM-B3LYP/6-31G* SP

ChemSculptor single point calculation

0 1
O 0.000000 0.000000 0.117300
H 0.000000 0.757200 -0.469200
H 0.000000 -0.757200 -0.469200

```

当前说明：

```text
%mem 使用调试占位值 4GB
后续阶段会根据电子数自动计算
%chk 与输入文件同名
当前只支持 SP
```

`GaussianInputWriter` 只负责写文件，不启动程序。

---

## 16. 第 13 步：启动本机 g16

位置：

```text
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
src/ChemSculptor.Compute.Local/LocalProcessBackend.cs
```

输入文件生成后，`Gaussian16ProgramAdapter` 构建执行上下文：

```text
ExecutablePath = g16
Arguments      = <run目录>\<jobId>.gjf <run目录>\output.log
RunDirectory   = <作业目录>\run
OutputFilePath = <作业目录>\run\output.log
```

执行器会先把 `input\<jobId>.gjf` 复制为 `run\<jobId>.gjf`。实际计算使用
`run` 中的副本，原始输入仍保留在 `input` 中。Gaussian 输入中的检查点使用
相对文件名 `%chk=<jobId>.chk`，因此 `.chk` 会写入当前工作目录 `run`。

如果系统没有配置 `GAUSS_EXEDIR`，适配器会从 `PATH` 中找到 `g16.exe`
所在目录，并把它传给子进程。否则 Gaussian 可能找不到 `l1.exe`。

`SinglePointCalculationExecutor` 把上下文交给 `IComputeBackend`。
当前注册的是 `LocalProcessBackend`，它使用 `ProcessStartInfo` 启动 `g16`，
并异步等待进程结束。

等价命令是：

```text
g16 <jobId>.gjf <output.log>
```

---

## 17. 第 14 步：服务器返回响应

执行器返回：

```text
Succeeded   = true
JobId       = job-...
Status      = Running
InputFilePath = .../job-....gjf
OutputFilePath = .../output.log
Message     = Gaussian 16 输入文件已生成，计算已在后台启动。
```

Agent 组合会话回复与执行结果：

```csharp
AgentMessageResponse response = new AgentMessageResponse();
response.TaskType = CalculationTaskType.SinglePoint.ToString();
response.JobId = executionResult.JobId;
response.Status = executionResult.Status;
response.InputFilePath = executionResult.InputFilePath;
response.OutputFilePath = executionResult.OutputFilePath;
response.Message = conversationReply.ReplyMessage + " " + executionResult.Message;
```

Api 再把 AgentResult 转换为 HTTP 200 + JSON。

---

## 18. 第 15 步：客户端显示结果

位置：`src/ChemSculptor.WinForms/MainForm.cs`

客户端反序列化为：

```csharp
AgentMessageResultDto? result =
    await response.Content.ReadFromJsonAsync<AgentMessageResultDto>();
```

然后追加对话消息：

```text
任务类型：SinglePoint，作业：job-...，状态：Running。
输入文件：C:\...\job-....gjf
输出文件：C:\...\output.log
已识别任务类型：单点计算。 Gaussian 16 输入文件已生成，计算已在后台启动。
```

---

## 19. 磁盘上的实际结果

工作区根目录选择顺序：

```text
1. %ProgramData%\ChemSculptor
2. %LOCALAPPDATA%\ChemSculptor
3. %TEMP%\ChemSculptor
```

目录结构：

```text
<工作区根目录>\jobs\<jobId>\
├── input\
│   ├── molecule.xyz
│   └── <jobId>.gjf
├── run\
│   ├── <jobId>.gjf
│   ├── <jobId>.chk
│   ├── output.log
│   ├── stdout.log
│   └── stderr.log
└── results\
```

Gaussian 执行时还会在工作目录中产生临时输入和检查点文件。正常结束后可以检查：

```text
output.log    Gaussian 主输出
<jobId>.gjf   实际执行使用的输入副本
<jobId>.chk   Gaussian 检查点文件
stdout.log    子进程标准输出
stderr.log    子进程标准错误
```

当前 `results` 仍为空，因为还没有实现输出解析和规范化结果保存。

---

## 20. 当前没有做的事情

```text
没有解析能量
没有生成规范化结果
没有保存计算作业到数据库
没有 LLM
没有实时推送
没有风险审批
没有并行调度
没有计算状态查询 API
没有取消计算 API
```

当前会话仓储也是内存实现，服务重启后消息会丢失。

---

## 21. 调试方法

### 建议断点

```text
MainForm.SendTextAsync
MainForm.SendAgentMessageAsync
AgentEndpoints.SubmitMessageAsync
AgentService.HandleMessageAsync
AgentService.ExecuteSinglePointAsync
ConversationService.HandleMessageAsync
RuleBasedTaskInterpreter.InterpretAsync
SinglePointCalculationExecutor.ExecuteAsync
GeometryTextParser.ParseAsync
GaussianInputWriter.WriteAsync
Gaussian16ProgramAdapter.WriteInputAsync
Gaussian16ProgramAdapter.BuildExecutionContext
LocalProcessBackend.SubmitAsync
LocalProcessBackend.MonitorProcessAsync
```

### 直接测试服务器

```powershell
$coordinates = Get-Content -Raw "samples/water.xyz.txt"

$body = @{
    sessionId = "session-debug"
    text = "单点计算"
    coordinateText = $coordinates
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://127.0.0.1:5178/agent/messages" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body | ConvertTo-Json
```

### 查看生成文件

```powershell
Get-Content "<返回的 inputFilePath>"
```

---

## 22. 常见问题

| 现象 | 原因 |
|---|---|
| 400 当前阶段只支持单点计算 | 文本中没有“单点”等关键词 |
| 400 坐标文本不能为空 | 客户端没有选择坐标文件 |
| 响应中断 | 工作区或输入生成异常 |
| 找不到文件 | 工作区根目录被回退到其他位置 |
| 有 `.gjf` 没有 `output.log` | 检查 `stderr.log`、`g16` 路径和 `GAUSS_EXEDIR` |
| `GAUSS_EXEDIR is ""` | Gaussian 适配器没有从 `PATH` 找到 `g16.exe` |

---

## 23. 设计原则

```text
客户端只发送原文和附件
会话层负责理解和组织回复
执行层负责计算相关操作
高风险决策必须留给人工
输入文件生成与程序执行分离
所有关键步骤都要可追溯
```

---

## 24. 推荐阅读顺序

```text
1. WinForms/MainForm.cs
2. Api/Endpoints/AgentEndpoints.cs
3. Agent/AgentService.cs
4. Agent/SinglePointCalculationExecutor.cs
5. Conversation/ConversationService.cs
6. Compute/TaskInterpretation.cs
7. InputProcessor/GeometryTextParser.cs
8. InputProcessor/GeometryIntake/CanonicalGeometryMapper.cs
9. Compute/WorkspaceManager.cs
10. Compute/CalculationDefaults.cs
11. Compute.Gaussian/GaussianInputWriter.cs
12. Compute.Gaussian/Gaussian16ProgramAdapter.cs
13. Compute.Local/LocalProcessBackend.cs
14. tests/*Tests.cs
```

---

## 25. 下一步

当前链路已经能启动本机 Gaussian。下一步是：

```text
第五阶段：Gaussian 输出解析与结果验证
  读取 output.log
  判断 Normal termination
  提取最终能量
  保存规范化结果
```

之后是：

```text
计算状态查询、取消计算、远程 HPC 后端和作业队列
```

---

## 26. 一句话总结

> 当前“单点计算”链路是：客户端原样发送文本和坐标，会话层解释出单点计算意图，执行器解析坐标、创建工作区、按 CAM-B3LYP/6-31G* 默认方案生成 Gaussian 输入文件，再通过程序适配器和本机执行后端启动 `g16`；输出文件路径会返回客户端，但输出解析和能量提取尚未实现。
