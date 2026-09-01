# ChemSculptor C# 框架教程：从基金申请书到可运行的代码

> 目标读者：刚开始接触“多层架构”“解决方案/项目”“接口与依赖注入”这类概念的业余开发者。
> 前提：你懂一点 C# 基础，知道类、方法、`async/await` 大致是什么；不知道也不怕，遇到不懂的词可以跳到文末的术语表。

这份教程配合仓库里的真实代码讲解。建议你一边读一边打开下面的文件对照：

- `ChemSculptor.slnx`：解决方案文件
- `src/ChemSculptor.Domain`：领域契约
- `src/ChemSculptor.Core`：极简内核
- `src/ChemSculptor.Api`：Web API 宿主
- `tests/ChemSculptor.Core.Tests`：内核测试

---

## 1. 先回忆：基金申请书要解决的三个痛点

在《ChemSculptor × CC-WOS 针对性架构设计》里，ChemSculptor 是为了解决三个核心问题：

1. **LLM 专业性不足**：大语言模型可以聊化学，但不能让它直接改输入文件、直接提交计算任务。
2. **系统耦合严重**：Gaussian、ORCA、MECP 搜索、动力学分析如果互相直接调用，项目会变成一团乱麻。
3. **自动化黑箱**：计算任务失败后，用户不知道为什么失败、系统改了什么、结果能不能信。

所以设计文档给出了一个总原则：

> **极简内核只做调度、通信、状态、日志、容器生命周期，不做任何化学判断。**

这份 C# 框架就是这句话的代码版。

---

## 2. 一句话看懂整个架构

ChemSculptor 的工作方式可以压缩成一句话：

> 用户提交一份“工作流定义”（声明式 DAG）→ 内核按依赖顺序调度技能容器执行 → 每个结果先过验证门 → 通过才继续，失败就停下来记录 → 整个过程发布事件日志并写入案例记忆。

“技能容器”就是未来装 Gaussian、ORCA、NTO 分析等能力的插槽。现在只有一个演示用的 `echo` 容器，但它把整条链路跑通了。

---

## 3. 先弄懂三个基础概念：解决方案、项目、csproj

这是初学者最容易困惑的地方，先讲清楚。

### 3.1 `.csproj` = 一个“可独立编译的模块”

每个 `.csproj` 文件描述一个项目（project）。项目编译后会生成一个 `.dll` 程序集。

打开 `src/ChemSculptor.Core/ChemSculptor.Core.csproj`，内容大概是：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\ChemSculptor.Domain\ChemSculptor.Domain.csproj" />
  </ItemGroup>
</Project>
```

关键点：

- `TargetFramework`：目标 .NET 版本。
- `ProjectReference`：**这个项目依赖另一个项目**。Core 依赖 Domain。
- `Sdk="Microsoft.NET.Sdk"`：普通类库；API 项目用的是 `Microsoft.NET.Sdk.Web`，因为它是 Web 应用。

### 3.2 `.slnx` = 把多个项目放在一个“篮子”里

`ChemSculptor.slnx` 是解决方案文件，它本身不写代码，只负责告诉你“这个仓库由哪几个项目组成”。你在命令行执行：

```bash
dotnet build ChemSculptor.slnx
```

就是一次性构建解决方案里的全部项目。

### 3.3 为什么拆成多个项目，而不是一个？

用一个生活类比：

- 一个项目 = 一个“部门”，有自己的职责和门禁。
- 项目引用 = “部门 A 可以找部门 B 办事，但不能随便翻部门 C 的抽屉”。
- 编译器 = 门禁保安。你在 Domain 里写了 `using ChemSculptor.Core;`，编译会直接报错。

这样做的收益：

1. **依赖方向清晰**：Domain 谁都不依赖，Core 依赖 Domain，Api 依赖 Core 和 Domain，Tests 依赖 Core 和 Domain。
2. **改动隔离**：只改 Core 时，Domain 不用重新编译。
3. **便于测试**：测试项目可以直接引用 Core，不需要启动整个 Web 服务。
4. **便于复用**：以后加 CLI、加 Blazor 工作台，都引用同一个 Core，不用复制代码。

你以前“一个程序一个项目”的做法没有错，只是当系统有内核、容器、验证、记忆、工作台这些明确边界时，拆开更划算。

---

## 4. 四个项目逐个讲

### 4.1 `ChemSculptor.Domain`：只有“契约”，没有“实现”

Domain（领域层）里放的是**接口和数据结构**，不放具体逻辑。它是整个架构的“共同语言”。

#### 工作流定义

打开 `src/ChemSculptor.Domain/WorkflowDefinition.cs`：

```csharp
public sealed record WorkflowDefinition
{
    public required string Id { get; init; }
    public required string Version { get; init; }
    public required string Goal { get; init; }
    public IReadOnlyList<WorkflowNode> Nodes { get; init; } = [];
}

public sealed record WorkflowNode
{
    public required string Id { get; init; }
    public required string Container { get; init; }
    public IReadOnlyList<string> DependsOn { get; init; } = [];
    public string? Gate { get; init; }
}
```

对照示例工作流 `src/ChemSculptor.Api/workflows/tadf-mechanism.json`：

```json
{
  "id": "tadf_mechanism_diagnosis",
  "version": "1.0.0",
  "goal": "判断超分子体系是否为 TADF 并定位主要发光通道",
  "nodes": [
    { "id": "structure", "container": "echo", "dependsOn": [] },
    { "id": "s0_opt", "container": "echo", "dependsOn": [ "structure" ] },
    { "id": "soc", "container": "echo", "dependsOn": [ "nto" ], "gate": "validate_soc_quality" }
  ]
}
```

`dependsOn` 是依赖声明：`soc` 必须在 `nto` 之后执行。`Gate` 表示这个节点的输出要过一个验证门。

#### 状态

`src/ChemSculptor.Domain/WorkflowState.cs` 里定义了两个枚举：

- `WorkflowState`：整个工作流的状态（`Draft`、`Ready`、`Running`、`Passed`、`Failed`、`Recovering`、`AwaitingApproval`……）
- `TaskState`：单个节点的状态（`Pending`、`Running`、`Passed`、`Failed`……）

这两个枚举正好对应设计文档第 3.3 节的状态机。

#### 接口就是“契约”

打开 `src/ChemSculptor.Domain/SkillAbstractions.cs` 和 `DomainServices.cs`，你会看到一批接口：

```csharp
public interface ISkillContainer
{
    string Name { get; }
    string Version { get; }
    IReadOnlyList<string> Capabilities { get; }
    Task<TaskResult> ExecuteAsync(TaskRequest request, CancellationToken cancellationToken = default);
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}
```

接口只规定“能力长什么样”，不规定“能力怎么实现”。内核调用 `ExecuteAsync` 时，根本不需要知道背后是 Gaussian 还是 ORCA。这正是“技能容器化、统一契约”的设计目标。

其他重要接口：

| 接口 | 对应设计文档概念 | 当前实现 |
|---|---|---|
| `ISkillContainer` | 技能容器统一契约 | `EchoSkillContainer` |
| `IContainerRegistry` | 容器注册/发现 | `ContainerRegistry` |
| `IEventBus` | 消息总线 | `InMemoryEventBus` |
| `IWorkflowRepository` | 状态/日志持久化 | `InMemoryWorkflowRepository` |
| `IRuleEngine` | 规则引擎 | `AllowAllRuleEngine`（占位） |
| `IValidationGate` | 验证门 | `PassThroughValidationGate`（占位） |
| `ICaseMemory` | 案例记忆 | `InMemoryCaseMemory`（占位） |
| `ILlmGateway` | LLM 网关 | 只有接口，尚未实现 |

### 4.2 `ChemSculptor.Core`：极简内核

Core 是设计文档里“极简内核”的落点。它只负责五件事：任务调度、消息通信、状态管理、日志记录、容器生命周期。

#### 事件总线 `InMemoryEventBus.cs`

```csharp
public sealed class InMemoryEventBus : IEventBus
{
    public async Task PublishAsync(WorkflowEvent @event, CancellationToken cancellationToken = default)
    {
        // 把事件发给所有订阅者
        await Task.WhenAll(handlers.Select(handler => handler(@event, cancellationToken)));
    }

    public IDisposable Subscribe(Func<WorkflowEvent, CancellationToken, Task> handler) { ... }
}
```

类比：事件总线就是一个“工作群”。内核往群里发一条 `task.completed`，谁关心谁自己订阅，互不干扰。以后工作台订阅这些事件，就能实时画 DAG 状态图。

#### 状态机 `WorkflowStateRules.cs`

```csharp
public static bool CanTransition(WorkflowState from, WorkflowState to) =>
    Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
```

它维护了一张“允许转换表”。比如 `Ready → Running` 允许，`Draft → Passed` 不允许。这保证工作流不会乱跳状态，从机制上杜绝“没执行就通过了”。

#### 核心调度器 `WorkflowEngine.cs`

这是整个框架的心脏，流程是：

1. `SubmitAsync`：规则引擎校验工作流定义，通过后保存为 `Ready`，发布 `workflow.started` 事件。
2. `RunAsync`：把状态改成 `Running`，然后循环执行：
   - 找出“依赖都已完成”的节点（就绪节点）
   - 依次调用容器执行
   - 如果节点配置了 `Gate`，执行验证门
   - 失败立即把工作流置为 `Failed`
3. 全部通过后，状态置为 `Passed`，发布 `workflow.completed`，并把运行记录写入案例记忆。

代码里最关键的一段（节选）：

```csharp
var ready = pending
    .Where(id => nodes[id].DependsOn.All(completed.ContainsKey))
    .ToList();

foreach (var nodeId in ready)
{
    var result = await ExecuteNodeAsync(run, nodes[nodeId], completed, cancellationToken);
    completed[nodeId] = result;
    if (!result.Succeeded)
    {
        return await FailAsync(run, $"Node '{nodeId}' failed: {result.Diagnostics}", ...);
    }
}
```

这就是“只执行 DAG，不含化学逻辑”的内核。

#### 演示容器 `EchoSkillContainer.cs`

`echo` 容器不调用任何化学软件，只是把上游输出拼成字符串返回。它的意义是：证明“容器契约 → 调度 → 验证 → 日志 → 记忆”这条链路能跑通。

#### 占位服务 `FrameworkServices.cs`

```csharp
public sealed class AllowAllRuleEngine : IRuleEngine
{
    public Task<IReadOnlyList<string>> ValidateWorkflowAsync(...) =>
        Task.FromResult<IReadOnlyList<string>>([]);  // 什么都不拦
}
```

`AllowAllRuleEngine`、`PassThroughValidationGate`、`InMemoryCaseMemory` 都是“占位实现”：先让系统能跑，真正的规则、验证、知识库以后替换。

### 4.3 `ChemSculptor.Api`：把内核暴露成 HTTP 服务

`Program.cs` 做三件事：

1. **注册依赖**（依赖注入）：把 `IEventBus`、`IContainerRegistry`、`IRuleEngine` 等接口映射到实现类。
2. **注册示例容器并载入示例工作流**：启动时自动注册 `echo` 容器，读取 `tadf-mechanism.json`。
3. **映射端点**：调用 `MapWorkflowEndpoints()` 和 `MapContainerEndpoints()`。

端点一览：

| 方法 | 路径 | 作用 |
|---|---|---|
| `POST` | `/workflows` | 提交工作流定义 |
| `GET` | `/workflows` | 列出所有工作流 |
| `GET` | `/workflows/{id}` | 查询单个工作流 |
| `POST` | `/workflows/{id}/run` | 执行工作流 |
| `POST` | `/workflows/{id}/intervene` | 人工干预（当前为框架占位） |
| `GET` | `/tasks/{workflowId}/log` | 获取事件日志（回放/溯源） |
| `POST` | `/approvals/{id}` | 人工审批（当前为框架占位） |
| `GET` | `/containers` | 列出已注册容器 |
| `POST` | `/containers/register` | 注册容器 |

这些端点直接对应设计文档 3.4 节的“内核接口”。

### 4.4 `ChemSculptor.Core.Tests`：用测试证明框架可用

测试项目引用了 Core 和 Domain，不引用 Api，所以测试不启动 Web 服务，跑得又快又稳。

`WorkflowStateRulesTests.cs` 验证状态机规则：

```csharp
[Fact]
public void DraftCannotSkipToPassed()
{
    Assert.False(WorkflowStateRules.CanTransition(WorkflowState.Draft, WorkflowState.Passed));
}
```

`WorkflowEngineTests.cs` 验证两件事：

1. 节点按依赖顺序执行（`a → b → c`），最后 `Passed`。
2. 验证门拒绝时，工作流进入 `Failed`，结果里带失败原因。

> 提示：`tests/ChemSculptor.Core.Tests/UnitTest1.cs` 是项目模板自动生成的空测试，属于脚手架残留，实际框架代码里已经删除，不需要关注。

---

## 5. 一次工作流的完整旅程

假设你执行：

```bash
dotnet run --project src/ChemSculptor.Api
curl -X POST http://127.0.0.1:5080/workflows/tadf_mechanism_diagnosis/run
```

内部实际发生的事情：

```text
提交 JSON
  → SubmitAsync
      → IRuleEngine.ValidateWorkflowAsync        # 规则校验（当前放行）
      → 保存 WorkflowRun，状态 Ready
      → 发布 workflow.started
  → RunAsync
      → 状态 Running
      → 反复找“依赖已满足”的节点
          → 注册表解析容器 → 调用 ExecuteAsync
          → 节点配置了 Gate → IValidationGate.ValidateAsync
          → 发布 task.started / task.completed
      → 全部完成：状态 Passed，发布 workflow.completed
      → ICaseMemory.RecordAsync                  # 写入案例记忆
```

查看日志：

```bash
curl http://127.0.0.1:5080/tasks/tadf_mechanism_diagnosis/log
```

你会看到一串事件：

```json
[
  { "type": "workflow.started",  "workflowId": "tadf_mechanism_diagnosis" },
  { "type": "task.started",      "workflowId": "tadf_mechanism_diagnosis", "nodeId": "structure" },
  { "type": "task.completed",    "workflowId": "tadf_mechanism_diagnosis", "nodeId": "structure" },
  { "type": "task.started",      "workflowId": "tadf_mechanism_diagnosis", "nodeId": "s0_opt" },
  ...
  { "type": "workflow.completed","workflowId": "tadf_mechanism_diagnosis" }
]
```

这就是设计文档说的“结果可回放率 100%”：每一次状态变化都有事件记录。

---

## 6. 亲手运行一遍

在仓库根目录依次执行：

```bash
dotnet build ChemSculptor.slnx
dotnet test ChemSculptor.slnx
dotnet run --project src/ChemSculptor.Api --urls http://127.0.0.1:5080
```

然后开另一个终端：

```bash
curl http://127.0.0.1:5080/
curl http://127.0.0.1:5080/workflows
curl -X POST http://127.0.0.1:5080/workflows/tadf_mechanism_diagnosis/run
curl http://127.0.0.1:5080/tasks/tadf_mechanism_diagnosis/log
```

预期结果：

- `GET /workflows` 返回一个 `tadf_mechanism_diagnosis` 记录，状态是 `Ready`。
- `POST /run` 之后状态变成 `Passed`，每个节点 `Passed`，`results` 里有每个节点的输出。
- `GET /tasks/.../log` 返回完整事件链。

> 小知识：JSON 里 `"state": 4` 是枚举的数字值，因为默认序列化枚举会输出数字。`WorkflowState` 中 `0=Draft, 1=Ready, 2=Running, 3=WaitingValidation, 4=Passed, 5=Failed`。以后想让接口直接显示 `"Passed"`，可以配置 `JsonStringEnumConverter`。

---

## 7. 手把手扩展：把 `echo` 换成真实技能

现在轮到你把框架变成自己的系统。下面以“结构构建容器”为例。

### 7.1 新建一个技能容器

在 `src/ChemSculptor.Core` 下新建 `StructureBuilderContainer.cs`：

```csharp
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class StructureBuilderContainer : ISkillContainer
{
    public string Name => "structure_builder";

    public string Version => "1.0.0";

    public IReadOnlyList<string> Capabilities { get; } = ["structure", "host-guest"];

    public Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        // 这里以后可以调用 PySCF、结构预处理、加氢、主客体组装等逻辑
        return Task.FromResult(new TaskResult
        {
            WorkflowId = request.WorkflowId,
            NodeId = request.NodeId,
            Succeeded = true,
            Output = "geometry=alpha_cd_vb10 charge=0 multiplicity=1"
        });
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
```

### 7.2 注册到 API

在 `Program.cs` 里加两行：

```csharp
builder.Services.AddSingleton<StructureBuilderContainer>();

// 启动注册容器时，追加：
await registry.RegisterAsync(app.Services.GetRequiredService<StructureBuilderContainer>());
```

### 7.3 改工作流 JSON

把 `tadf-mechanism.json` 的第一个节点改成：

```json
{ "id": "structure", "container": "structure_builder", "dependsOn": [] }
```

其余节点暂时还用 `echo`，这样你就能看到“真实容器 + 演示容器”混合执行。

### 7.4 把占位规则引擎换成真规则

新建 `ChemRuleEngine.cs`：

```csharp
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class ChemRuleEngine : IRuleEngine
{
    public Task<IReadOnlyList<string>> ValidateWorkflowAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        var problems = new List<string>();
        var ids = definition.Nodes.Select(n => n.Id).ToList();

        if (ids.Count != ids.Distinct().Count())
        {
            problems.Add("节点 id 不能重复");
        }

        foreach (var node in definition.Nodes)
        {
            foreach (var dep in node.DependsOn)
            {
                if (!ids.Contains(dep))
                {
                    problems.Add($"节点 {node.Id} 依赖了不存在的节点 {dep}");
                }
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(problems);
    }
}
```

然后在 `Program.cs` 里替换注册：

```csharp
// 原来是：builder.Services.AddSingleton<IRuleEngine, AllowAllRuleEngine>();
builder.Services.AddSingleton<IRuleEngine, ChemRuleEngine>();
```

这样 LLM 生成的“工作流草案”想进内核，必须先过这一关，正是“LLM 建议、规则把关”。

### 7.5 实现一个真正的验证门

`PassThroughValidationGate` 目前什么都不检查。你可以实现一个检查 SOC 数值的版本，只要 `Diagnostics` 或输出里出现异常值就返回 `Failed`。判断逻辑写在你自己的类里，内核不需要改一行。

---

## 8. 常见问题

**Q：为什么我不能把所有代码写在一个 Program.cs 里？**

可以，但会失去边界。内核一旦混入化学逻辑，就违背了“极简内核”原则，以后每加一个容器都要改动内核。拆开之后，加容器只新增一个类。

**Q：csproj 和 slnx 到底有什么区别？**

`.csproj` 定义一个项目（编译单元）；`.slnx` 把多个项目组织成一个解决方案，方便一次构建。

**Q：接口到底有什么用？我直接写类不行吗？**

接口让“使用者”和“实现者”解耦。内核只依赖 `ISkillContainer`，所以今天用 `echo`，明天换成 `GaussianContainer`，内核代码不用改。

**Q：为什么有 AllowAllRuleEngine 这种“什么都不干”的类？**

这是占位实现（stub）。先让整体架构跑通，再逐个填充真实逻辑。占位实现用 `Program.cs` 一行就能换成正式实现。

**Q：内存实现能直接上生产吗？**

不能。`InMemoryWorkflowRepository` 重启后数据丢失。生产环境要换成 PostgreSQL/SQLite 实现，接口不变，替换 DI 注册即可。

**Q：状态为什么返回数字而不是名称？**

枚举默认序列化为数字。想返回名称，可在 API 配置 `JsonStringEnumConverter`。

---

## 9. 术语表

| 术语 | 通俗解释 |
|---|---|
| 解决方案（Solution） | 装多个项目的篮子 |
| 项目（Project） | 一个可独立编译的模块，对应一个 csproj |
| 程序集（Assembly） | 编译后的 dll |
| 接口（Interface） | 只声明“能做什么”，不写“怎么做” |
| 契约（Contract） | 接口 + 数据结构的统称，双方共同遵守的约定 |
| 依赖注入（DI） | 在 `Program.cs` 里告诉系统“这个接口用哪个类实现” |
| 事件总线（Event Bus） | 发布/订阅事件的中间层，类似工作群 |
| DAG | 有向无环图，工作流节点和依赖组成的图 |
| 验证门（Gate） | 结果进入下游前必须通过的检查 |
| 占位实现（Stub） | 先让系统跑起来的空实现，之后再替换 |
| 仓储（Repository） | 负责数据保存和读取的抽象 |
| Minimal API | ASP.NET Core 用少量代码定义 HTTP 接口的写法 |

---

## 10. 下一步学习建议

1. 先跑通“亲手运行一遍”，再照着 7.1-7.3 加一个自己的容器。
2. 读一遍 `WorkflowEngine.cs`，把 `SubmitAsync` 和 `RunAsync` 的流程在纸上画出来。
3. 给 `IRuleEngine` 写真实规则，比如“节点 id 唯一”“依赖必须存在”“分子身份不允许被改变”。
4. 给 `IValidationGate` 写 SOC/MECP 的检查逻辑。
5. 把 `ILlmGateway` 接上真正的 LLM：自然语言 → 工作流草案 → 规则引擎校验 → 内核执行。
6. 最后把内存仓储换成 PostgreSQL，让结果能跨重启保存。

每完成一步，你的“可塑型科学智能体”就离基金申请书里描述的闭环更近一点。
