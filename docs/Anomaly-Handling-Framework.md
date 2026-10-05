# ChemSculptor 异常处理框架

> 状态：已建立通用项目骨架，尚未接入主工作流
> 适用方向：执行异常、资源异常、数值算法异常、科学合理性异常  
> 目标：以小型、清晰、可扩展的方式逐步加入异常诊断与修正子工作流

---

## 1. 定位

异常处理不是主计算流程中的一段普通代码，也不是 WinForms 客户端的职责。

它应作为主计算流程验证失败后启动的独立子工作流：

```text
主计算工作流
  ├── 准备输入
  ├── 提交计算
  ├── 等待结束
  ├── 提取结果
  ├── 验证结果
  │
  ├── 验证通过 → 正常结束
  └── 验证不通过 → 异常处理子工作流
```

客户端只负责展示：

```text
发生了什么异常
智能体准备如何处理
是否需要用户确认
重算是否开始
重算结果如何
```

客户端不负责诊断、修正、审批决策或执行重算。

---

## 2. 首要原则

异常处理框架遵循以下原则：

```text
执行异常与科学异常分开
症状与根因分开
诊断与修正分开
修正与审批分开
原作业与派生作业分开
置信度与授权分开
通用修正意图与具体程序参数分开
所有自动动作都必须有预算、通知和记录
```

其中最重要的一条是：

```text
置信度决定智能体有多相信自己。
风险和授权决定系统有没有资格直接执行。
```

---

## 3. 一级分类

异常体系先分成两条独立路线。

```text
ExecutionAnomaly
  执行异常

ScientificAnomaly
  科学异常
```

判断口径：

```text
执行异常
  计算任务没有正常运行完成，或者运行条件不成立

科学异常
  计算程序运行和解析完成后，结果在数值算法或科学上不可接受
```

### 3.1 执行异常

建议分类：

```text
ProcessFailure
  进程失败、异常退出、取消

ResourceFailure
  内存、磁盘、CPU、GPU、队列资源不足

EnvironmentFailure
  可执行文件、许可证、环境变量或运行目录不可用

FileOrTransportFailure
  输入复制、输出获取、网络传输或文件完整性失败

InfrastructureFailure
  节点故障、SSH 失败、调度器失败
```

执行异常通常不改变科学含义。优先处理方式：

```text
修复运行条件
  ↓
使用原科学输入重新计算
  ↓
重新验证
```

### 3.2 科学异常

建议分类：

```text
NumericalAlgorithmAnomaly
  数值迭代、收敛、稳定性问题

PhysicalPlausibilityAnomaly
  物理量不合理

ChemicalPlausibilityAnomaly
  化学结构、电子态或反应路径不合理

CrossResultConsistencyAnomaly
  多个结果之间不一致

ValidationRequirementAnomaly
  必要验证项没有通过
```

科学异常可能需要改变计算含义，因此必须经过更严格的诊断和审批。

---

## 4. 两条处理路线

### 4.1 执行异常路线

```text
收集进程和资源证据
  ↓
识别执行异常
  ↓
判断是否可自动恢复
  ↓
修复运行条件
  ↓
创建执行重试
  ↓
使用原科学输入重算
  ↓
验证结果
```

执行异常先处理，因为：

```text
程序没有可靠运行完成时
科学诊断缺少可靠证据
```

### 4.2 科学异常路线

```text
收集数值和科学证据
  ↓
识别科学异常
  ↓
形成诊断报告
  ↓
生成通用修正意图
  ↓
翻译为具体程序修正
  ↓
进行风险与审批判断
  ↓
创建派生计算作业
  ↓
比较新旧结果
```

### 4.3 跨类情况

两类异常可以互相成为证据，但不能混成一个分类。

例如：

```text
内存不足
  → 程序异常退出
  → 缺少正常终结标志
```

主分类是：

```text
ResourceFailure
```

而不是：

```text
NumericalAlgorithmAnomaly
```

反过来：

```text
进程正常结束
  → 缺少正常终结标志
  → 输出显示算法没有收敛
```

主分类应是：

```text
NumericalAlgorithmAnomaly
```

因此评估结果至少包含：

```text
PrimaryCategory
  主异常

ContributingCategories
  可能诱因

Evidence
  证据

RootCauseStatus
  已确认、可能、未知
```

---

## 5. 核心模型

第一阶段只保留以下核心对象：

```text
AnomalyEvidence
  一条异常证据

AnomalyFinding
  一条已经分类的异常

AnomalyAssessment
  一次作业的全部异常评估

DiagnosisReport
  对异常原因的诊断结论

CorrectionOption
  一个候选修正方案

CorrectionPlan
  最终采用的修正方案

ApprovalDecision
  自动批准、请求人工或禁止执行

RecoveryAttempt
  一次派生重算尝试

RecoveryOutcome
  一次重算的最终结果
```

关系：

```text
AnomalyEvidence
  → AnomalyFinding
  → AnomalyAssessment
  → DiagnosisReport
  → CorrectionOption
  → CorrectionPlan
  → ApprovalDecision
  → RecoveryAttempt
  → RecoveryOutcome
```

---

## 6. 通用修正意图

通用层只表达修正目的，不表达具体程序参数。

```text
RetryUnchanged
IncreaseResource
ImproveInitialGuess
StabilizeSolver
AdjustIterationLimit
ModifyNumericalAlgorithm
RegenerateInput
ModifyScientificSetting
RequestUserDecision
Abort
```

转换关系：

```text
通用修正意图
  ↓
具体程序适配器
  ↓
具体输入参数或运行设置
```

通用工作流中不能出现：

```text
具体程序名称
具体输入关键词
具体文件扩展名
具体错误代码
具体环境变量
```

这些内容只能出现在对应的程序模块和技能集合中。

---

## 7. 审批框架

审批由以下信息共同决定：

```text
风险等级
可逆性
是否改变科学含义
预计资源消耗
诊断置信度
修正成功率估计
证据质量
历史成功率
用户预授权范围
```

审批结果只有三种：

```text
AutoApprove
  自动批准

AskUser
  请求用户确认

Forbid
  禁止执行
```

默认策略：

| 情况 | 默认处理 |
|---|---|
| 低风险且可逆 | 自动执行，同时通知用户 |
| 中风险、有明确证据 | 满足条件可自动，否则询问 |
| 改变电荷、多重度、电子态、结构或计算方法 | 原则上人工确认 |
| 超出资源预算 | 请求用户确认 |
| 诊断证据不足 | 请求用户确认 |
| 无法安全修正 | 禁止自动执行并报告 |

自动批准必须记录：

```text
使用了哪个审批策略
哪些条件满足
为什么不需要人工审批
允许修改的最大范围
```

---

## 8. 重算与血缘

异常修正不能覆盖原始作业。

必须创建派生作业：

```text
原始作业
  ├── 原始输入
  ├── 原始输出
  ├── 原始结果
  └── 异常处理记录
        ↓
派生作业 1
  ├── 父作业标识
  ├── 根工作流标识
  ├── 修正方案
  ├── 修改后的输入
  ├── 新输出
  └── 新结果
```

至少保存：

```text
RootWorkflowId
ParentJobId
AttemptNumber
DiagnosisId
CorrectionPlanId
ApprovalDecisionId
ChangedParameters
Artifacts
Outcome
```

这样可以实现：

```text
解释为什么尝试修正
比较修正前后的结果
发现重复失败
从任意一次尝试恢复
以后构建案例记忆
```

---

## 9. 预算与停止条件

每条异常处理链路都必须有明确边界：

```text
最大尝试次数
最大计算时间
最大资源消耗
允许自动修正的类别
禁止重复同一修正
禁止形成 A → B → A 循环
超过预算后请求用户
```

可能的停止结果：

```text
Recovered
  已恢复

NeedsUserDecision
  需要用户决定

BudgetExceeded
  超出预算

NoSafeCorrection
  没有安全修正方案

Aborted
  停止处理
```

---

## 10. 通用工作流技能

通用工作流可以先规划为：

```text
anomaly.assess
anomaly.diagnose-execution
anomaly.recover-execution
anomaly.diagnose-scientific
anomaly.plan-correction
anomaly.authorize
anomaly.run-recovery
anomaly.evaluate-recovery
anomaly.record-case
```

这些技能只处理通用概念。

需要调用具体计算程序的检查必须遵循“通用 Skill + 专用 Skill”原则。

示例：

```text
通用 Skill
  anomaly.check-wavefunction-stability

Gaussian 专用 Skill
  gaussian.wavefunction-stability.check
```

通用工作流只引用：

```text
anomaly.check-wavefunction-stability
```

通用 Skill 根据计算程序选择专用 Skill，并对上层返回统一的
`AnomalyCheckResult`。

当前实现：

```text
通用 Skill
  ChemSculptor.Skills.Common.AnomalyWorkflow
  WavefunctionStabilityCheckSkill

Gaussian 专用 Skill
  ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability
  GaussianWavefunctionStabilityCheckSkill
```

通用 Skill 通过 `IAnomalyProviderRegistry` 查找：

```text
Descriptor.Code = wavefunction-stability
Descriptor.Program = Gaussian 16
Descriptor.ImplementationId = gaussian.wavefunction-stability.check
```

具体程序模块负责提供：

```text
具体输出中的异常识别
具体证据提取
通用诊断到具体诊断的映射
通用修正意图到具体参数的转换
具体结果的新旧比较
```

通用检查契约是 `IAnomalyCheck`。Gaussian 稳定性检查已经返回统一的
`AnomalyCheckResult`。通用
`anomaly.check-wavefunction-stability` Skill 已经可以通过注册表选择
`gaussian.wavefunction-stability.check` 专用 Skill。

---

## 11. 存储位置

原始作业目录保存异常处理记录：

```text
jobs/<jobId>/
  results/
    anomaly/
      <recordId>.json
  recovery/
    attempt-1/
```

每个异常处理记录的聚合内容包含：

```text
AnomalyAssessment
AnomalyCheckResult
DiagnosisReport
CorrectionPlan
ApprovalDecision
RecoveryAttempt
RecoveryOutcome
```

派生作业继续使用原有标准目录：

```text
jobs/<newJobId>/
  input/
  run/
  results/
  manifest.json
```

这样既保留原始作业，也保留每次修正尝试。

### 11.1 异常检查模型

单点计算完成后可以执行多项异常检查。每项检查都使用统一模型：

```text
AnomalyCheckDescriptor
  检查代码、名称、分类、版本和要求等级

AnomalyCheckResult
  执行状态、摘要、跳过原因、证据和发现
```

检查状态：

```text
NotRun
Passed
Finding
Inconclusive
Skipped
ExecutionFailed
Canceled
```

检查机制：

```text
OutputArtifact
  从主计算输出文件中提取证据

RuntimeSignal
  从进程、资源和运行环境信号中提取证据

AuxiliaryCalculation
  需要执行额外的辅助计算
```

其中 `Skipped` 用于：

```text
任务不要求该检查
当前计算模型不支持该检查
当前计算程序不支持该检查
缺少执行该检查所需的信息
```

跳过不是“没有设计这个节点”。所有单点计算流程都应安排波函数稳定性检查；
ONIOM 等不支持该检查的模型仍进入该节点，然后记录跳过状态和原因。

当前已定义通用检查代码：

```text
CommonAnomalyCheckCodes.WavefunctionStability
```

当前的 Gaussian 波函数稳定性链路：

```text
GaussianWavefunctionStabilityParser
  读取稳定性输出

GaussianWavefunctionStabilityInputWriter
  从原始 Gaussian 单点输入生成稳定性检查输入

WavefunctionStabilityResult
  通用稳定性结果

WavefunctionStabilityCheckSkill
  通用检查 Skill

GaussianWavefunctionStabilityCheckSkill
  Gaussian 专用检查 Skill

AnomalyCheckResult
  Passed、Finding、Skipped 或 Inconclusive

AnomalyFinding
  不稳定时包含 wavefunction-instability
```

Gaussian 专用检查 Skill 当前支持两种路径：

```text
已有稳定性输出
  → 直接解析

没有稳定性输出
  → 复制原始 .chk
  → 保留原方法和基组
  → 增加 guess=read geom=check stable
  → 创建派生稳定性检查作业
  → 等待结束
  → 解析稳定性结果
```

如果原始输入包含 ONIOM 等不支持的模型，检查 Skill 返回：

```text
Status = Skipped
SkippedReason = 当前 ONIOM 输入不支持波函数稳定性检查
```

通用稳定性检查 Skill 已经接入单点计算工作流：

```text
validate
  ↓
stability-check
  ↓
plan
```

检查输入不再重复写入坐标，而是通过 `geom=check` 从检查点读取几何。

检查结果会保存为 `AnomalyRecord`。发现不稳定时，结果包含：

```text
wavefunction-instability
```

### 11.2 异常记录聚合

`AnomalyRecord` 是异常处理存储的主要聚合根，包含：

```text
作业和工作流标识
总体状态
检查结果
诊断报告
修正计划
审批决定
恢复尝试
最终结果
创建和更新时间
```

文件仓储接口：

```text
IAnomalyRepository
```

当前实现：

```text
FileAnomalyRepository
```

每条记录写入：

```text
jobs/<jobId>/results/anomaly/<recordId>.json
```

---

## 12. 推荐模块位置

当前已经建立通用异常项目：

```text
ChemSculptor.Anomaly
  通用异常模型和接口
  异常提供器注册表

ChemSculptor.Skills.Common/AnomalyWorkflow
  通用异常处理技能

ChemSculptor.Compute.Gaussian/Anomaly
  Gaussian 异常识别和修正翻译

ChemSculptor.Skills.Gaussian/Anomaly
  Gaussian 专用诊断技能

ChemSculptor.Agent
  启动异常处理子工作流

ChemSculptor.WinForms
  显示异常说明和审批请求
```

`ChemSculptor.Anomaly` 不负责具体计算程序，也不直接负责工作流调度。
通用异常 Skill 先在通用技能项目中实现，规模足够大后再考虑独立为
`ChemSculptor.Skills.Anomaly`。

---

## 13. 增加一种新异常的方法

以后每增加一种异常，按以下顺序进行：

```text
1. 在具体程序模块中识别该异常
2. 提取为通用 AnomalyFinding
3. 编写或扩展诊断规则
4. 定义一个通用 CorrectionIntent
5. 在具体程序中翻译为参数或动作
6. 设定风险等级和审批策略
7. 创建派生重算
8. 比较修正前后的结果
9. 保存成功或失败案例
```

增加新异常不应该修改主工作流结构，也不应该让 WinForms 增加具体计算程序知识。

---

## 14. 推荐实施顺序

```text
第一阶段
  定义异常模型和存储结构

第二阶段
  执行异常检测，只报告不自动重算

第三阶段
  执行异常恢复，人工批准后重算

第四阶段
  科学异常检测，只诊断不修正

第五阶段
  科学异常修正方案，人工批准后重算

第六阶段
  低风险修正自动批准

第七阶段
  案例记忆、成功率统计和策略校准
```

早期保留人工审批不是倒退，而是积累可靠数据。等规则稳定后，再逐步扩大自动处理范围。

---

## 15. 最终固定规则

```text
执行异常和科学异常分开
症状和根因分开
诊断和修正分开
修正和审批分开
原作业和派生作业分开
置信度和授权分开
通用意图和具体参数分开
自动执行必须有预算、通知和完整记录
```

按照这套框架，每加入一类异常，只需要增加对应的诊断、修正意图、具体翻译和测试，不需要反复修改主计算工作流。
