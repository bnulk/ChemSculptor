# Skill 集合与工作流组织教程

> 适用版本：`v0.20.0` 之后
> 本文重点：从设计思想理解 Skill 集合、工作流、Agent、验证门和异常恢复
> 当前状态：正常单点计算已按 Skill 集合组织；复杂工作流和异常执行仍是后续目标

---

## 1. 先建立一个最重要的区分

Skill 集合和工作流不是同一个概念。

```text
Skill 集合
  回答：系统“会做什么”

工作流
  回答：完成一个目标时“按什么顺序做”

Agent
  回答：面对当前目标“应该选择哪条工作流”
```

例如：

```text
Skill 集合中有：
  生成 Gaussian 输入
  运行计算
  提取单点能
  验证结果
  诊断 SCF 异常
  提出 SCF 修正方案
  执行修正后的计算

工作流规定：
  先生成输入
  再运行计算
  然后提取和验证
  异常时进入诊断与修正分支

Agent 决定：
  当前用户目标需要哪一种工作流
  是否需要向用户提问
  是否允许自动重试
```

Skill 是能力库，工作流是能力的组合方式。

---

## 2. 从日常语言理解四个角色

可以把这个系统类比成一个科学计算实验室。

### Skill 是实验人员掌握的独立能力

例如：

```text
会不会生成 Gaussian 输入
会不会运行高斯
会不会识别 SCF 不收敛
会不会提出收敛修正方案
会不会验证最终结果
```

### Skill Catalog 是能力清单

例如：

```text
Gaussian 技能清单：
  输入生成
  单点结果提取
  异常诊断
  修正方案

Common 技能清单：
  结果验证
  风险检查
  人工审批
```

### Workflow 是实验方案

实验方案规定：

```text
第一步做什么
第二步做什么
哪些步骤可以同时做
什么情况允许重试
什么情况必须停下来问人
什么结果算结束
```

### Agent 是项目负责人

项目负责人不一定亲自完成实验，而是：

```text
理解目标
选择方案
分配能力
协调顺序
处理意外
向用户报告
```

---

## 3. Skill 集合的层次

随着项目扩大，建议把 Skill 分成几层。

### 3.1 通用 Skill

通用 Skill 不依赖具体计算程序。

例如：

```text
CalculationResultValidationSkill
CalculationFailureTreatmentPlanningSkill
CalculationApprovalSkill
CalculationResourceEstimationSkill
```

它们处理的是：

```text
结果是否有效
是否需要重试
是否需要用户确认
资源消耗是否合理
```

这些 Skill 不应知道：

```text
SCF Done
%chk
GAUSS_EXEDIR
ORCA 的 ! 关键词
```

### 3.2 程序专用 Skill

例如 Gaussian：

```text
GaussianInputGenerationSkill
GaussianSinglePointResultExtractionSkill
GaussianFailureDiagnosisSkill
GaussianFailureCorrectionProposalSkill
GaussianCorrectionExecutionSkill
```

ORCA：

```text
OrcaInputGenerationSkill
OrcaSinglePointResultExtractionSkill
OrcaFailureDiagnosisSkill
OrcaFailureCorrectionProposalSkill
OrcaCorrectionExecutionSkill
```

这些 Skill 可以依赖各自的 Adapter 和 Parser。

### 3.3 工作流模板

工作流模板不一定属于某一种 Skill。

例如：

```text
SinglePointEnergyWorkflow
GeometryOptimizationWorkflow
FrequencyCalculationWorkflow
TadfMechanismWorkflow
SocCalculationWorkflow
MecpSearchWorkflow
```

工作流模板是 Skill 的组合，不是新的底层代码。

---

## 4. Skill 和 Workflow 的边界

### Skill 应该负责

```text
完成一个明确能力
读取自己的输入
产生自己的输出
报告自己的诊断
暴露自己的能力标签
```

### Skill 不应该负责

```text
决定整个科研流程
自行选择其他 Skill
绕过审批执行高风险动作
假设所有程序都使用同一种输入格式
把工作流状态塞进实现细节
```

### Workflow 应该负责

```text
节点顺序
依赖关系
并行可能性
数据传递
验证门
重试策略
人工审批
终止条件
```

### Workflow 不应该负责

```text
解析 Gaussian 文本
拼接 Gaussian 关键词
直接操作本机进程
理解 Gaussian 输出字段
```

这条边界非常重要：

```text
Workflow 负责“怎样组合”
Skill 负责“具体完成”
Adapter 负责“与程序交互”
Parser 负责“识别程序输出”
```

---

## 5. 当前已有的工作流模型

项目中已经存在：

```text
WorkflowDefinition
WorkflowNode
```

一份工作流定义可以抽象为：

```text
WorkflowDefinition
  Id
  Version
  Goal
  Nodes
```

每个节点：

```text
WorkflowNode
  Id
  Skill
  DependsOn
  Gate
```

含义是：

```text
Id
  节点名字

Skill
  要调用哪个 Skill

DependsOn
  必须先完成哪些节点

Gate
  节点完成后是否需要验证
```

这已经是一套最小 DAG 工作流模型。

---

## 6. DAG 是什么

DAG 是“有向无环图”。

可以简单理解为：

```text
方向表示依赖
不能形成循环
```

例如：

```text
生成输入
  ↓
运行计算
  ↓
提取结果
  ↓
验证结果
```

这是最简单的线性 DAG。

更复杂的并行 DAG：

```text
         ┌→ 计算分子 A → 提取 A ┐
准备输入 ┤                         ├→ 比较结果
         └→ 计算分子 B → 提取 B ┘
```

工作流引擎负责：

```text
检查依赖是否满足
选择可以运行的节点
传递上游结果
在节点完成后解锁下游节点
```

---

## 7. 从简单单点能工作流开始

目标：

```text
计算一个分子的单点能
```

最直白的工作流：

```text
GaussianInputGenerationSkill
  ↓
CalculationJobSubmissionSkill
  ↓
GaussianSinglePointResultExtractionSkill
  ↓
CalculationResultValidationSkill
```

如果任务失败：

```text
CalculationResultValidationSkill
  ↓
失败信息
  ↓
GaussianFailureDiagnosisSkill
  ↓
GaussianFailureCorrectionProposalSkill
```

这说明工作流不仅包含正常路径，也包含异常分支。

---

## 8. 静态工作流和动态工作流

### 静态工作流

节点和顺序提前写好。

适合：

```text
单点能
几何优化
频率计算
固定流程的 TADF 分析
固定顺序的 SOC 计算
```

优点：

```text
容易检查
容易复现
容易审计
不容易被随机改变
```

缺点：

```text
面对新情况不够灵活
```

### 动态工作流

Agent 根据当前情况现场组合 Skill。

适合：

```text
未知体系
未知计算异常
需要多轮探索
用户目标表述含糊
```

优点：

```text
适应性强
可以处理新问题
```

风险：

```text
不容易复现
必须加强规则和审批
容易产生错误组合
```

### 推荐方案：混合模式

```text
常见任务：
  使用经过验证的静态工作流

特殊情况：
  允许 Agent 在受控范围内动态修改

高风险动作：
  必须进入审批 Gate
```

ChemSculptor 最适合采用混合模式。

---

## 9. 工作流的数据传递

工作流节点之间不能只传一段字符串。

更合理的是传递明确的数据模型。

例如：

```text
输入生成节点输出：
  CalculationJob
  CalculationExecutionContext

计算完成节点输出：
  CompletedJobArtifact

结果提取节点输出：
  CalculationResult

验证节点输出：
  CalculationValidationReport
```

这样每个节点都知道自己拿到什么。

工作流引擎最终可以支持：

```text
节点输入绑定
节点输出绑定
条件分支
并行收集
错误分支
```

---

## 10. 重要：Skill 不一定都能自动执行

根据副作用，Skill 可以分为：

### 纯读取 Skill

```text
解析输出
提取能量
验证结果
诊断错误
```

这些通常可以自动执行。

### 状态改变 Skill

```text
生成新输入
修改输入
提交计算
取消计算
删除临时文件
```

这些必须明确：

```text
会写入什么文件
会使用多少资源
是否会覆盖现有结果
是否需要审批
```

### 高风险 Skill

例如：

```text
自动修改分子结构
自动改变电荷或自旋多重度
自动扩大基组
自动提交大量远程作业
```

这些必须有：

```text
规则检查
风险提示
人工审批
审计记录
```

---

## 11. 验证门 Gate

Gate 是工作流中的检查点。

例如：

```text
提取结果
  ↓
Gate: 能量是否存在
  ↓
正常时继续
异常时进入错误分支
```

Gate 可以检查：

```text
结构是否合理
SCF 是否收敛
是否正常终结
是否有最终能量
自旋污染是否可接受
频率是否存在虚频
结果是否超过合理范围
```

Gate 的职责是“允许或阻止继续”。

诊断 Skill 的职责是“解释为什么失败”。

两者不能混为一谈。

---

## 12. 异常处理工作流

以 SCF 震荡为例：

```text
提取结果
  ↓
验证失败
  ↓
GaussianFailureDiagnosisSkill
  ↓
通用 CalculationFailure
  ↓
CalculationFailureTreatmentPlanningSkill
  ↓
GaussianFailureCorrectionProposalSkill
  ↓
审批 Gate
  ↓
GaussianCorrectionExecutionSkill
  ↓
重新计算
```

如果是 SCF 震荡：

```text
通用异常：
  scf.convergence_failed

通用意图：
  improve_scf_convergence

Gaussian 建议：
  增加迭代上限
  改用 QC
  读取初始猜测
  使用新的检查点
```

通用工作流不知道 `SCF=QC`。

Gaussian 修正 Skill 负责把它翻译出来。

---

## 13. 为什么异常流程要形成子工作流

异常恢复不是一行代码，而是另一个流程。

它可以包含：

```text
诊断
通用规划
程序专用翻译
风险检查
人工审批
执行修正
重新验证
记录案例
```

所以应当把它设计成：

```text
主工作流
  ↓
正常分支

主工作流
  ↓
异常子工作流
  ↓
修正后的子作业
```

这样不会把异常逻辑塞进正常单点流程。

---

## 14. 工作流与父子作业

修正后的计算通常是一个新作业，不应覆盖原作业。

关系应记录为：

```text
ParentJobId
ChildJobId
Attempt
TriggeredByFailureCode
TreatmentPlanId
ApprovalId
```

例如：

```text
job-001
  原始单点计算
  SCF 震荡

job-002
  ParentJobId = job-001
  使用 QC 修正方案
  正常结束
```

这样可以回答：

```text
为什么要重算
是谁批准的
修改了什么
哪一次成功
```

---

## 15. 并行工作流

量子化学中的很多任务可以并行。

例如对比多个构象：

```text
准备构象
  ↓
┌→ 构象 A → 优化 → 能量 ┐
├→ 构象 B → 优化 → 能量 ┤
└→ 构象 C → 优化 → 能量 ┘
  ↓
比较结果
```

工作流需要知道：

```text
哪些节点可以并行
最大并行数量
每个作业使用多少 CPU 和内存
失败一个作业时其他作业如何处理
```

并行不是 Skill 自己的职责，而是工作流和调度器的职责。

---

## 16. 一个更完整的 TADF 设想

TADF 工作流可能是：

```text
准备分子结构
  ↓
几何优化
  ↓
频率验证
  ↓
基态单点能
  ↓
激发态计算
  ├→ S1 能量
  ├→ T1 能量
  └→ 振子强度
  ↓
计算 ΔE(ST)
  ↓
评估反向系间窜越
  ↓
生成结果解释
```

如果几何优化失败：

```text
进入几何优化异常子流程
```

如果激发态计算失败：

```text
进入激发态异常子流程
```

如果最终结果不合理：

```text
进入结果审查流程
```

因此，大型科研任务不是一条长脚本，而是：

```text
工作流
  + 子工作流
  + 条件分支
  + 回退路径
  + 人工决策
```

---

## 17. Agent 在工作流中的角色

Agent 可以做：

```text
把自然语言目标映射到工作流
补全缺失参数
检查前置条件
选择静态模板
在允许范围内组合 Skill
向用户提问
请求审批
解释结果
```

Agent 不应该：

```text
直接解析 Gaussian 文件
直接构造 Gaussian 关键词
绕过 Workflow 直接提交高风险计算
把所有错误都交给 LLM 临时判断
```

Agent 的可靠性来自：

```text
有明确的 Skill 契约
有可验证的工作流
有可追踪的数据模型
有强制 Gate
有审计记录
```

---

## 18. 能力选择与 Skill 查找

当前使用显式技能标识：

```text
gaussian.input-generation
gaussian.single-point-result-extraction
calculation.result-validation
```

未来可以增加更丰富的能力描述：

```text
Capability
Program
TaskType
InputContract
OutputContract
SideEffectLevel
EstimatedCost
RiskLevel
Version
```

例如：

```text
技能：
  OrcaSinglePointResultExtraction

能力：
  calculation.result-extraction

程序：
  ORCA

任务：
  SinglePoint

输入：
  StandardOutputArtifact

输出：
  CalculationResult
```

Agent 可以请求：

```text
提取程序为 ORCA 的单点计算结果
```

注册表按能力、程序和任务类型查找 Skill。

当前还没有实现这种自动能力选择，但目录和数据模型已经为它预留位置。

---

## 19. 工作流模板怎样组织

建议未来建立：

```text
workflows/
  single-point/
    gaussian.json
    orca.json

  geometry-optimization/
    gaussian.json
    orca.json

  tadf/
    gaussian.json

  soc/
    gaussian.json

  mecp/
    gaussian.json
```

每份 JSON 只描述：

```text
节点
SkillId
依赖关系
Gate
输入输出绑定
重试策略
审批策略
```

程序专用参数继续放在程序 Skill 或 Adapter 中。

---

## 20. 工作流版本管理

工作流必须版本化。

例如：

```text
single-point-gaussian v1.0.0
single-point-gaussian v1.1.0
```

变化可能是：

```text
增加验证门
增加 SCF 异常分支
增加 QC 修正路径
修改默认重试次数
```

每次运行记录应保存：

```text
WorkflowId
WorkflowVersion
SkillVersions
ProgramVersions
InputHashes
```

这样才能复现旧结果。

---

## 21. 当前代码与未来目标的差距

当前已经具备：

```text
ISkill 契约
ISkillRegistry
GaussianInputGenerationSkill
GaussianSinglePointResultExtractionSkill
CalculationResultValidationSkill
GaussianFailureDiagnosisSkill 框架
GaussianFailureCorrectionProposalSkill 框架
WorkflowDefinition 和 WorkflowNode 数据模型
```

当前尚未具备：

```text
完整的 Skill 能力检索
工作流模板目录
JSON 工作流输入输出绑定
动态工作流生成
异常子工作流执行
父子作业持久化
并行作业调度器
远程 HPC Skill
```

当前正常单点计算主要由 Agent 按固定顺序调用 Skill，而不是完全由 JSON
工作流模板驱动。

这是刻意分阶段实现，而不是遗漏。

---

## 22. 怎样新增一条工作流

### 第一步：写清科研目标

例如：

```text
判断分子是否具有 TADF 特征
```

### 第二步：确定输入和输出

```text
输入：
  分子结构
  电荷
  自旋多重度

输出：
  能量差
  振子强度
  可解释报告
```

### 第三步：列出需要的 Skill

```text
结构准备
几何优化
频率验证
单点能
激发态
SOC
结果分析
```

### 第四步：画 DAG

明确：

```text
哪些依赖
哪些并行
哪些是 Gate
哪些是异常分支
```

### 第五步：定义每个节点的输入输出

不要让节点依赖隐式字符串。

### 第六步：定义异常策略

```text
自动重试几次
哪些错误必须问人
哪些错误禁止继续
```

### 第七步：增加模板测试

```text
依赖检查
环检测
输入缺失测试
Gate 失败测试
异常分支测试
审批流程测试
```

---

## 23. 常见反模式

### 反模式一：Agent 直接做所有事情

```text
AgentService 里出现 SCF、Link、HPC 和文件解析
```

问题是无法扩展和测试。

### 反模式二：每个小函数都是 Skill

```text
ReadLineSkill
ReplaceStringSkill
AddKeywordSkill
```

问题是 Skill 目录会失去意义。

### 反模式三：Workflow 中包含程序细节

```text
工作流节点直接规定 Gaussian 的 SCF=QC
```

正确做法是由通用意图和 Gaussian 修正 Skill 决定。

### 反模式四：异常处理没有重试上限

```text
失败 → 修改 → 失败 → 修改 → 无限循环
```

必须在通用规划或工作流中设置：

```text
最大重试次数
最大成本
最大运行时间
人工确认条件
```

### 反模式五：没有记录父子作业

没有父子关系，就无法知道：

```text
重试从哪里来
修改了什么
哪一版是最终结果
```

---

## 24. 推荐的最终分化

```text
Skill 层
  负责能力

Catalog 层
  负责能力分类和查找

Workflow 层
  负责流程组合和状态

Agent 层
  负责目标解释和计划选择

Gate 层
  负责验证和放行

Repository 层
  负责状态、结果和审计

Adapter 层
  负责具体计算程序
```

它们之间的关系：

```text
Workflow 选择 Skill
Skill 使用 Adapter
Adapter 使用 Parser
Gate 检查 Skill 结果
Repository 保存全过程
Agent 决定使用哪条 Workflow
```

---

## 25. 一句话总结

```text
Skill 是能力，
Catalog 是能力目录，
Workflow 是能力组合，
Agent 是能力选择者，
Gate 是能力放行者，
Adapter 是程序翻译者，
Repository 是过程记录者。
```

ChemSculptor 的长期目标，是让科研任务的每一步都能回答：

```text
使用了什么能力
为什么选择它
输入是什么
输出是什么
是否通过验证
谁批准了下一步
结果保存在哪里
```
