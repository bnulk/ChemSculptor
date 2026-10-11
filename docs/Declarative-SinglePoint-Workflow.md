# 声明式单点计算工作流

> 适用版本：`v0.51.0` 之后
> 目标：理解单点计算如何从代码编排升级为声明式 DAG

---

## 1. 为什么需要声明式工作流

直接代码编排的问题：

```text
流程隐藏在方法中
增加节点需要修改 C# 顺序逻辑
节点依赖不容易单独查看
异常分支和人工审批难以插入
```

声明式工作流把流程写成定义：

```text
节点
技能
依赖关系
输入映射
```

执行引擎负责调度。

---

## 2. 当前 DAG

```text
single-point
  ↓
stability-check
  ↓
stability-correction-plan
  ↓
recovery-job
  ↓
recovery-execution
  ↓
recovery-stability-check
  ↓
plan
  ↓
science-data-record
```

对应技能：

```text
calculation.single-point
anomaly.check-wavefunction-stability
anomaly.plan-wavefunction-stability-correction
anomaly.create-recovery-job
anomaly.execute-recovery-job
calculation.workflow-validation
calculation.workflow-processing-plan
science.record-calculation-result
```

`calculation.single-point` 是复合 Skill，内部继续调用：

```text
calculation.prepare-input
  → calculation.submit
  → calculation.wait
  → calculation.extract-result
  → calculation.workflow-validation
```

原子 Skill 仍然保留并独立注册。

---

## 3. 初始输入

求值器提交：

```text
request
  序列化后的 CalculationInputGenerationRequest
```

工作流节点通过：

```text
$input.request
```

读取它。

---

## 4. 节点输入映射

`WorkflowNode.Inputs` 的键是技能输入名，值是来源。

示例：

```text
stability-check.Inputs["validation"] = "single-point"
```

含义：

```text
把 single-point 节点的输出
作为稳定性检查技能的 validation 输入
```

另一个示例：

```text
single-point.Inputs["request"] = "$input.request"
```

含义：

```text
把工作流初始输入 request
作为单点计算 Skill 的输入 request
```

---

## 5. 每个节点做什么

### single-point

调用输入准备、提交、等待、提取和验证原子 Skill，返回统一的
`SinglePointCalculationSkillResult`。

输出：

```text
SinglePointCalculationSkillResult
  Succeeded
  Passed
  Job
  Result
  Report
  Steps
```

### plan

根据验证后的作业和结果生成通用处理方案。

输出：

```text
CalculationWorkflowProcessingPlanSkillResult
```

---

## 6. 后台执行

Api 提交后立即返回：

```text
jobId
Running
```

工作流在后台运行。

客户端通过：

```text
GET /calculations/{jobId}/status
```

查询状态。

---

## 7. 工作流结果

可以查询：

```text
GET /workflows/{jobId}
```

查看：

```text
整体状态
每个节点状态
节点结果
```

正常单点计算实测：

```text
工作流状态：Passed

single-point = Passed
stability-check = Passed
stability-correction-plan = Passed
recovery-job = Passed
recovery-execution = Passed
recovery-stability-check = Passed
plan = Passed
science-data-record = Passed
```

---

## 8. 为什么这比直接代码编排更适合以后

以后增加真正的条件分支时，可以在 `single-point` 后面按验证结果分流：

```text
single-point
  ├── Passed → plan
  └── Failed → diagnose
                 → propose-correction
                 → approval
                 → retry
```

以后增加并行计算时，可以声明多个独立计算节点：

```text
┌→ calculation-A → extract-A ┐
├→ calculation-B → extract-B ┤
└→ calculation-C → extract-C ┘
              ↓
         compare-results
```

因此，声明式工作流是后续异常处理、并行调度和复杂科研任务的基础。
