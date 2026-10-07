# ChemSculptor WinForms 客户端 Models 说明

> 适用版本：`v0.27.1` 之后
> 客户端定位：只负责用户输入、HTTP 通信、界面状态、结果显示和文件下载

---

## 1. 为什么客户端需要自己的 Models

WinForms 客户端不引用服务器项目：

```text
ChemSculptor.Domain
ChemSculptor.Core
ChemSculptor.Agent
ChemSculptor.Compute
ChemSculptor.Compute.Gaussian
```

客户端只通过 HTTP 和服务器通信，因此需要一套自己的数据外形：

```text
客户端发送什么 JSON
客户端接收什么 JSON
界面需要保存什么状态
```

这些类型放在：

```text
src/ChemSculptor.WinForms/Models/
```

它们不是服务器的业务模型，而是客户端自己的通信模型和界面状态模型。

---

## 2. Models 不是单独的项目

`Models` 只是 `ChemSculptor.WinForms` 项目中的目录和命名空间。

它不会生成单独的 DLL，也不会形成新的服务器组件。

目录结构：

```text
src/ChemSculptor.WinForms/Models/
├── Ui/
│   └── UiModels.cs
├── Requests/
│   └── RequestModels.cs
└── Responses/
    └── ResponseModels.cs
```

对应命名空间：

```text
ChemSculptor.WinForms.Models.Ui
ChemSculptor.WinForms.Models.Requests
ChemSculptor.WinForms.Models.Responses
```

三个命名空间分别表示三个方向：

```text
Ui
  WinForms 控件和客户端状态之间使用

Requests
  WinForms 向服务器发送数据

Responses
  服务器向 WinForms 返回数据
```

---

## 3. Ui：界面状态模型

位置：

```text
src/ChemSculptor.WinForms/Models/Ui/UiModels.cs
```

命名空间：

```text
ChemSculptor.WinForms.Models.Ui
```

这里存放只服务于界面窗口的对象。

### 3.1 ChatMessage

表示对话区中的一条消息：

```text
Role
  消息角色，例如 user、system、error、hint

Text
  消息正文

Timestamp
  消息时间
```

它只用于显示，不发送给服务器。

### 3.2 ChatSession

表示左侧会话列表中的一个会话：

```text
Id
  客户端会话标识

Title
  会话标题

Messages
  当前会话中的消息列表
```

它负责把多条 `ChatMessage` 组织成一个会话。

### 3.3 CalculationJobItem

表示客户端当前正在跟踪的计算作业：

```text
JobId
  服务器返回的作业标识

State
  客户端看到的作业状态

IsFinished
  客户端是否已经停止轮询

ResultText
  已经格式化、准备显示的结果文本
```

`CalculationJobItem` 是界面状态，不是服务器的 `CalculationJob`。

服务器拥有真正的计算作业数据，客户端只保存显示和轮询所需的最少状态。

---

## 4. Requests：请求协议模型

位置：

```text
src/ChemSculptor.WinForms/Models/Requests/RequestModels.cs
```

命名空间：

```text
ChemSculptor.WinForms.Models.Requests
```

这里的类型表示：

```text
客户端准备发送给服务器的请求体
```

当前类型：

```text
AgentMessageRequestDto
```

主要字段：

```text
SessionId
  当前客户端会话标识

Text
  用户输入的自然语言原文

CoordinateText
  用户选择的坐标文件内容
```

处理自然语言和坐标含义的责任都在服务器端。

客户端不在这里生成：

```text
charge
multiplicity
method
basis
program
任务类型
工作流节点
```

因此 `Requests` 目录只负责“原样传递用户提供的信息”。

---

## 5. Responses：响应协议模型

位置：

```text
src/ChemSculptor.WinForms/Models/Responses/ResponseModels.cs
```

命名空间：

```text
ChemSculptor.WinForms.Models.Responses
```

这里的类型表示：

```text
客户端从服务器接收的 JSON 数据外形
```

当前类型分为几组。

### 5.1 消息接受响应

```text
AgentMessageResultDto
```

服务器接收用户消息后，返回：

```text
任务类型
作业标识
当前状态
输入文件路径
输出文件路径
面向用户的说明
诊断信息
```

客户端最关心的是：

```text
JobId
Status
Message
```

有了 `JobId`，客户端才能继续轮询计算状态。

### 5.2 作业状态响应

```text
CalculationStatusDto
```

客户端定时调用：

```text
GET /calculations/{jobId}/status
```

并读取：

```text
JobId
State
StartedAt
CompletedAt
InputFilePath
OutputFilePath
Diagnostics
```

### 5.3 计算结果响应

```text
CalculationResultDto
```

包含：

```text
能量
能量单位
程序
方法
基组
电荷
自旋多重度
是否正常终结
失败类别
诊断信息
```

### 5.4 验证响应

```text
CalculationValidationDto
CalculationValidationCheckDto
CalculationDiagnosticDto
```

它们表示：

```text
验证是否通过
验证摘要
单项检查
期望值与实际值
诊断问题
```

### 5.5 成果包清单响应

```text
ScientificArtifactManifestDto
ScientificArtifactPointDto
ScientificArtifactFileDto
```

服务器决定有哪些科学点和文件，客户端只按清单下载。

文件描述包含：

```text
ArtifactId
Kind
DownloadFileName
Length
Sha256
IsAvailable
Error
DownloadPath
```

WinForms 不需要理解：

```text
为什么使用这个扩展名
这个文件怎样生成
这个文件怎样恢复计算
```

这些含义由服务器和具体程序适配器负责。

---

## 6. 一次完整的数据流

```text
用户选择坐标 txt
  ↓
WinForms 读取普通文本
  ↓
创建 AgentMessageRequestDto
  ↓
POST /agent/messages
  ↓
服务器解析 JSON
  ↓
返回 AgentMessageResultDto
  ↓
WinForms 创建 CalculationJobItem
  ↓
定时 GET /calculations/{jobId}/status
  ↓
接收 CalculationStatusDto
  ↓
结束后 GET result 和 validation
  ↓
接收 CalculationResultDto 和 CalculationValidationDto
  ↓
显示文本结果
  ↓
点击“保存”
  ↓
接收 ScientificArtifactManifestDto
  ↓
逐项下载 ScientificArtifactFileDto
  ↓
按科学点目录保存
```

可以概括为：

```text
Ui Models
  界面自己使用

Request Models
  WinForms → Api

Response Models
  Api → WinForms
```

---

## 7. Models 与服务器 Domain 的区别

```text
服务器 Domain
  服务器内部的共同语言
  描述任务、工作流、计算和验证

客户端 Models
  HTTP 通信外形
  客户端界面状态
```

例如：

```text
服务器：
  CalculationJob
  CalculationResult
  CalculationValidationReport

客户端：
  CalculationStatusDto
  CalculationResultDto
  CalculationValidationDto
```

名字相似不代表它们是同一类对象。

服务器可以拥有更多字段和更多行为，客户端只保留自己需要的字段。

这是一种有意的隔离，不是重复代码错误。

---

## 8. 为什么客户端不能直接引用服务器类型

如果 WinForms 直接引用：

```text
ChemSculptor.Compute.CalculationResult
```

会出现以下问题：

```text
服务器内部重构会影响客户端
客户端可能获得不该使用的业务能力
部署时必须携带服务器程序集
客户端和服务器生命周期被绑定
未来替换服务器时会牵动界面工程
```

现在的做法是：

```text
服务器内部可以改变
只要 HTTP JSON 契约保持稳定
客户端就不需要一起改变
```

---

## 9. 每个目录的固定边界

### Ui

允许：

```text
会话
消息
控件状态
轮询状态
格式化结果文本
```

禁止：

```text
发送给服务器的 DTO
化学计算逻辑
工作流逻辑
```

### Requests

允许：

```text
HTTP 请求字段
字段默认值
简单数据校验
```

禁止：

```text
生成计算方法
决定电荷或多重度
解析计算输出
```

### Responses

允许：

```text
HTTP 响应字段
JSON 反序列化目标
界面需要读取的结果
```

禁止：

```text
服务器业务方法
计算执行
具体程序规则
工作流调度
```

---

## 10. 以后新增类型时放在哪里

如果新增的是窗口状态：

```text
Models/Ui/
```

如果新增的是客户端发送给服务器的数据：

```text
Models/Requests/
```

如果新增的是服务器返回给客户端的数据：

```text
Models/Responses/
```

如果它既不是界面状态，也不是请求或响应：

```text
不要强行放进 Models
```

例如以下内容以后应放在自己的位置：

```text
本地配置
服务器地址
窗口尺寸
日志设置
用户偏好
```

它们更适合：

```text
Configuration
Settings
ClientState
```

---

## 11. 推荐阅读顺序

第一次阅读客户端模型时，建议按以下顺序：

1. `Models/Ui/UiModels.cs`
2. `Models/Requests/RequestModels.cs`
3. `Models/Responses/ResponseModels.cs`
4. `MainForm.SendAgentMessageAsync`
5. `MainForm.RefreshCalculationAsync`
6. `MainForm.SaveArtifactsAsync`

阅读过程可以概括为：

```text
界面现在保存什么
  ↓
客户端发送什么
  ↓
服务器返回什么
  ↓
这些数据如何驱动界面
```

---

## 12. 最终口径

WinForms 客户端模型只负责三件事：

```text
界面状态
向服务器请求的数据
服务器返回的数据
```

客户端模型不拥有：

```text
服务器内部类型
化学业务逻辑
具体计算程序
工作流执行逻辑
```

这条边界让 WinForms 可以保持简单，同时允许服务器端持续发展和重构。
