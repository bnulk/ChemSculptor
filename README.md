# ChemSculptor 极简内核框架（C#）

这是《ChemSculptor × CC-WOS 针对性架构设计》的 C# 落地骨架。当前只提供框架：
领域契约、极简内核、Minimal API 宿主和一个可跑的示例工作流，不含任何化学逻辑。

> 面向初学者的完整讲解教程见 [docs/ChemSculptor-Tutorial.md](docs/ChemSculptor-Tutorial.md)。

## 项目结构

```text
ChemSculptor.slnx
src/
  ChemSculptor.Domain/  领域契约：工作流模型、容器协议、规则/验证/记忆接口
  ChemSculptor.Core/    极简内核：事件总线、容器注册、DAG 引擎、状态机、仓储
  ChemSculptor.Api/     Minimal API 宿主：工作流、容器、日志、审批端点
tests/
  ChemSculptor.Core.Tests/  内核依赖序、验证门、状态机测试
```

## 与原设计对应关系

| 原设计概念 | C# 落点 |
|---|---|
| 极简内核 | `ChemSculptor.Core.WorkflowEngine` + `WorkflowStateRules` |
| 消息总线 | `IEventBus` / `InMemoryEventBus` |
| 技能容器统一契约 | `ISkillContainer` / `IContainerRegistry` |
| 声明式工作流模板 | `WorkflowDefinition` / `WorkflowNode` |
| 规则引擎 | `IRuleEngine`（当前 `AllowAllRuleEngine` 占位） |
| 验证门 | `IValidationGate`（当前 `PassThroughValidationGate` 占位） |
| 案例记忆 | `ICaseMemory`（当前 `InMemoryCaseMemory` 占位） |
| LLM 网关 | `ILlmGateway`（仅接口，待接入） |
| 工作台/审批/回放 | `/workflows/{id}/intervene`、`/approvals/{id}`、`/tasks/{id}/log` 端点 |

## 运行

```bash
dotnet run --project src/ChemSculptor.Api
```

启动时会自动注册 `echo` 示例容器，并载入
`src/ChemSculptor.Api/workflows/tadf-mechanism.json` 示例工作流。

```bash
# 查看已提交的工作流
curl http://localhost:5000/workflows

# 执行示例工作流
curl -X POST http://localhost:5000/workflows/tadf_mechanism_diagnosis/run

# 查看事件日志（回放/溯源）
curl http://localhost:5000/tasks/tadf_mechanism_diagnosis/log

# 提交自定义工作流
curl -X POST http://localhost:5000/workflows -H "Content-Type: application/json" -d @workflow.json
```

## 后续填充点

1. `IRuleEngine`：把设计文档中的“条件—动作”规则落库并实现校验。
2. `IValidationGate`：按节点接入结构/优化/激发态/SOC/MECP 各阶段验证。
3. `ISkillContainer`：新增 `structure_builder`、`gaussian_opt`、`orca_tddfit`、
   `nto_analysis`、`soc_calc`、`mecp_search` 等容器实现。
4. `ILlmGateway`：接入 LLM 的“自然语言 → 工作流草案”建议，草案仍走规则校验。
5. 持久化：把 `IWorkflowRepository` 从内存实现换成 PostgreSQL/SQLite。
