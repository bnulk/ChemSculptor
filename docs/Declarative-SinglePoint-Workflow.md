# 声明式单点计算工作流

> 适用版本：`v0.25.0` 之后
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
input-generation
  ↓
submit
  ↓
wait
  ↓
extract
  ↓
validate
  ↓
plan
```

对应技能：

```text
calculation.prepare-input
calculation.submit
calculation.wait
calculation.extract-result
calculation.workflow-validation
calculation.workflow-processing-plan
```

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
submit.Inputs["inputResult"] = "input-generation"
```

含义：

```text
把 input-generation 节点的输出
作为 submit 技能的 inputResult 输入
```

另一个示例：

```text
input-generation.Inputs["request"] = "$input.request"
```

含义：

```text
把工作流初始输入 request
作为技能输入 request
```

---

## 5. 每个节点做什么

### input-generation

解析坐标、生成输入文件、复制到 run、构建执行上下文。

输出：

```text
CalculationInputGenerationResult
```

### submit

把输入提交到 `IComputeBackend`。

输出：

```text
CalculationSubmissionSkillResult
```

### wait

轮询后端状态直到：

```text
Completed
Failed
Canceled
```

输出：

```text
CalculationWaitSkillResult
```

### extract

调用 Gaussian Adapter 解析输出并翻译为通用结果。

输出：

```text
CalculationResultExtractionResult
```

### validate

调用通用验证服务，合并全部适用验证器。

输出：

```text
CalculationWorkflowValidationSkillResult
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

input-generation = Passed
submit = Passed
wait = Passed
extract = Passed
validate = Passed
plan = Passed
```

---

## 8. 为什么这比直接代码编排更适合以后

以后增加异常诊断时，可以在 `validate` 后面加入条件分支：

```text
validate
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
