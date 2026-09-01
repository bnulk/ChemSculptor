# ChemSculptor 项目改动说明

> 维护约定：**每次代码改动后更新本文档**，记录版本、改动目的、改动内容和教程式说明。
> 版本规则：`MAJOR.MINOR.PATCH`。新增功能 +1 MINOR；修复问题 +1 PATCH；架构性大改动 +1 MAJOR。

---

## v0.2.0（2026-09-02）：新增 WinForms 单机版

### 版本

- 当前版本：`0.2.0`
- 日期：2026-09-02
- 版本类型：新增功能（MINOR 升级）

### 改动目的

开发阶段只有我一个人使用，先把 ChemSculptor 做成**单机版**，用熟悉的 WinForms 做界面；同时保留将来升级成“服务器版 + 独立客户端”的可能。

### 改动内容

新增解决方案项目 `ChemSculptor.WinForms`：

```text
src/ChemSculptor.WinForms/
├── ChemSculptor.WinForms.csproj   # WinForms 项目，引用 Core + Domain
├── Program.cs                     # 入口：手动组装内存版内核
├── MainForm.cs                    # 最小界面：载入示例 / 刷新 / 执行所选
└── Services/
    ├── IChemSculptorService.cs    # 界面层依赖的服务接口（切换缝）
    └── LocalChemSculptorService.cs # 本地实现：直接调用 WorkflowEngine
```

核心设计决定：

1. WinForms 只引用 `ChemSculptor.Core` 和 `ChemSculptor.Domain`，**不引用 Api**，也不走 HTTP。
2. 界面层只认识 `IChemSculptorService`，不认识具体内核，为将来“界面独立 + 调 API”留好接缝。
3. 示例工作流 JSON 通过 csproj 链接复制到 WinForms 输出目录，和 Api 共用同一个 `tadf-mechanism.json`。
4. MainForm 订阅事件总线，把 `task.started`、`task.completed` 等事件实时显示到日志框。

### 教程式说明

#### 为什么加一个 WinForms 项目

之前框架只能通过 API（curl/浏览器）操作。单机开发时更自然的方式是：打开一个窗口，点按钮执行。WinForms 项目就是这扇窗口，而且它直接调用内存版内核，不需要启动 Web 服务。

#### 程序如何启动

`Program.cs` 手工组装“本地版依赖”：

```csharp
var eventBus = new InMemoryEventBus();
var repository = new InMemoryWorkflowRepository();
var registry = new ContainerRegistry();
registry.RegisterAsync(new EchoSkillContainer()).GetAwaiter().GetResult();

var engine = new WorkflowEngine(
    registry,
    eventBus,
    repository,
    new AllowAllRuleEngine(),
    new PassThroughValidationGate(),
    new InMemoryCaseMemory());
```

这和 Api 版用的组件完全一样，只是没有 DI 容器和 HTTP，全部在内存里直接 new。

#### IChemSculptorService 为什么重要

界面不直接碰 `WorkflowEngine`，而是碰一个接口：

```csharp
public interface IChemSculptorService
{
    Task<IReadOnlyList<WorkflowRun>> ListWorkflowsAsync(...);
    Task<WorkflowRun> SubmitAsync(WorkflowDefinition definition, ...);
    Task<WorkflowRun> RunAsync(string workflowId, ...);
    Task<IReadOnlyList<WorkflowEvent>> GetLogAsync(string workflowId, ...);
    event Action<WorkflowEvent>? EventReceived;
}
```

现在提供 `LocalChemSculptorService`（本地实现）；将来写服务器版时，新增 `HttpChemSculptorService` 调用 Api 即可，MainForm 不用改。

#### MainForm 的三个按钮

- **载入示例**：读取 `tadf-mechanism.json`，反序列化成 `WorkflowDefinition`，调用 `SubmitAsync`
- **刷新**：调用 `ListWorkflowsAsync`，把工作流和状态显示在左侧列表
- **执行所选**：取列表选中项，调用 `RunAsync`

右侧日志框显示事件流。事件从内核经事件总线发出，由 `LocalChemSculptorService` 转成界面事件，MainForm 再追加到文本框。

#### 如何运行

Visual Studio：

1. 打开 `ChemSculptor.slnx`
2. 右键 `ChemSculptor.WinForms` → “设为启动项目”
3. 按 F5

命令行：

```powershell
dotnet run --project src/ChemSculptor.WinForms
```

#### 将来如何切换成服务器版

```text
现在：MainForm → IChemSculptorService → LocalChemSculptorService → Core
以后：MainForm → IChemSculptorService → HttpChemSculptorService → Api → Core
```

界面项目以后可以整体移出解决方案，逻辑不变。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 原 Domain / Core / Api / Tests 均未修改，向后兼容

### 运行环境

- Windows + .NET 10 SDK
- 需要 Windows 桌面支持（WinForms）

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
