# 全过程计算档案包教程 05：阶段与作业的因果关系

## 1. 本步要解决什么

完整档案不能只列出一些作业文件，还必须回答：

```text
这个作业属于哪个阶段
这个阶段在声明顺序中位于哪里
这个作业是由哪个作业派生出来的
这个作业是在修正哪个失败作业
哪些关系是账本明确记录的
哪些关系只是展示顺序
哪些关系断裂、冲突或形成循环
```

第 5 步引入独立的因果关系图，不把关系判断塞进报告层，也不根据
`CreatedAt` 或文件时间猜测“谁导致谁”。

## 2. 数据边界

构建因果关系图时只读取：

```text
workflow-runs/<RootWorkflowId>/ledger.json
jobs/<JobId>/manifest.json
```

不读取输入文件、输出文件、检查点、日志正文或科学点文件。因果图
只描述执行关系；具体文件发现属于后续第 6、7 步。

关键原则：

```text
不使用界面展示筛选后的作业列表
不把作业创建时间当作因果证据
不使用最终成功作业替代原始失败作业
失败、取消和被恢复作业同样进入图
```

## 3. 模型入口

文件：

```text
src/ChemSculptor.Compute/CalculationWorkflowCausalityModels.cs
```

顶层类型：

```text
CalculationWorkflowCausalityGraph
CalculationWorkflowCausalityNode
CalculationWorkflowCausalityEdge
CalculationWorkflowCausalityIssue
```

图对象包含：

```text
SchemaVersion
RootWorkflowId
CapturedAt
Nodes
Edges
Issues
```

## 4. 节点

节点只有两种：

```text
Stage
JobAttempt
```

阶段节点稳定标识：

```text
stage:<StageId>
```

作业节点稳定标识：

```text
job:<JobId>
```

阶段节点保存阶段状态和声明顺序；作业节点保存任务类型、程序、状态、
父作业、修正计划以及作业清单是否可用。

节点标识不能使用显示名称，否则界面改名会破坏历史关系。

## 5. 关系边

首批关系：

```text
StageContainsJob
StagePrecedesStage
JobDerivedFromJob
JobRecoveryOfJob
```

证据字段：

```text
StageJobReference
StageOrder
ParentJobId
AttemptNumber
CorrectionPlanId
```

### 5.1 StageContainsJob

来源：

```text
CalculationWorkflowStageEvidence.JobAttemptIds
```

阶段清单列出的作业才会得到包含边。若阶段引用了不存在的作业，产生
`causality.stage.job_missing`；若作业没有被任何阶段列出，产生
`causality.job.stage_missing`。

### 5.2 StagePrecedesStage

来源：

```text
CalculationWorkflowStageEvidence.Order
```

它表达“声明顺序”，不是科学上的充分因果。因此边的
`IsRequired` 固定为 `false`。

例如优化阶段声明在 TDDFT 阶段之前，不等于只要开始优化就必然完成
TDDFT。真正的科学依赖仍由高层 workflow 和后续证据快照表达。

### 5.3 JobDerivedFromJob

来源：

```text
CalculationWorkflowJobAttemptEvidence.ParentJobId
```

典型例子：

```text
频率作业来自最终优化作业
NTO 作业来自 TDDFT 源作业
```

`ParentJobId` 是派生关系的主要证据；`IsRequired` 为 `true`。

### 5.4 JobRecoveryOfJob

来源：

```text
ParentJobId
CorrectionPlanId
AttemptNumber
```

当作业存在修正计划或恢复尝试序号时，父子边表示恢复关系。若存在
`CorrectionPlanId`，边直接记录该证据；只有 `AttemptNumber > 0` 时，
记录 `AttemptNumber` 证据。

恢复关系仍然指向明确的父作业。只有修正计划而没有父作业会被报告为
关系断裂。

## 6. 构建流程

文件：

```text
src/ChemSculptor.Compute/FileCalculationWorkflowCausalityRepository.cs
```

入口：

```text
ICalculationWorkflowCausalityRepository.BuildAsync(RootWorkflowId)
```

执行顺序：

```text
1. 用完整查询读取根工作流作业清单
2. 建立阶段索引
3. 建立作业尝试索引
4. 生成阶段节点和作业节点
5. 生成阶段包含作业边
6. 生成阶段声明顺序边
7. 生成作业派生或恢复边
8. 检查父作业、根标识、自引用和循环
9. 稳定排序并写出 causality.json
```

完整查询明确保持筛选字段为空：

```text
StageIds = []
States = []
TaskTypes = []
Programs = []
ParentJobId = ""
RecoveryAttempt = null
IncludeUnindexedJobs = true
```

## 7. 冲突和断裂

主要问题码：

```text
causality.ledger.missing
causality.node.duplicate
causality.job.stage_missing
causality.stage.job_missing
causality.job.stage_conflict
causality.parent.missing
causality.parent.root_mismatch
causality.job.self_reference
causality.job.cycle
causality.edge.duplicate
causality.job.manifest_unavailable
causality.job.unindexed
causality.job.evidence_conflict
```

处理原则：

```text
缺失不等于忽略
冲突不能自动选一个“看起来正确”的值
循环关系必须显式记录
重复关系只保留一条边，但记录审计问题
作业清单不可用时仍保留账本节点，并标记证据受限
```

## 8. 持久化和确定性

输出路径：

```text
workflow-runs/<RootWorkflowId>/causality.json
```

写入方式：

```text
序列化到临时文件
原子替换 causality.json
异常时清理临时文件
```

排序规则：

```text
节点：节点类型、阶段顺序、NodeId
边：EdgeKind、EdgeId
问题：严重级别、问题码、JobId、NodeId
```

稳定标识和确定性排序使两次构建可以直接比较，也避免服务重启后结果
无故变化。

## 9. 测试

文件：

```text
tests/ChemSculptor.Core.Tests/CalculationWorkflowCausalityTests.cs
```

已覆盖：

```text
多阶段、多作业完整图
失败和取消作业保留
恢复作业使用 CorrectionPlanId 证据
NTO 作业使用 ParentJobId 派生证据
缺失父作业
父作业跨根工作流
自引用
互相引用形成循环
阶段引用不存在的作业
重复阶段引用不产生重复边
服务重启后读取
重复构建稳定排序
JSON 往返
完整查询不受展示筛选影响
```

## 10. 本步不做什么

本步不做：

```text
不读取计算输入和输出文件
不计算文件 SHA-256
不发现 scratch、检查点和后处理产物
不生成报告章节
不组装 zip 或成果包
不修改 API 和 WinForms
不自动把 causality.json 塞进科学点集合
```

下一步应进入全过程产物发现策略：定义每个阶段和作业尝试应收集哪些
文件，以及可访问、缺失、排除和失败状态。
