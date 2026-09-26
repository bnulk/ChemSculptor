# ChemSculptor Skill 集合教程

> 适用版本：`v0.20.0` 之后
> 面向对象：刚接触该架构、希望边阅读代码边理解设计思想的开发者
> 当前重点：正常单点计算已经按 Skill 集合组织；异常处理只保留框架

---

## 1. 这份教程解决什么问题

ChemSculptor 不希望最终变成：

```text
一个巨大的 AgentService
里面充满 Gaussian、ORCA、HPC、文件解析和异常处理代码
```

我们希望看到的是：

```text
用户目标
  → Skill
  → Skill
  → Skill
```

例如：

```text
单点计算
  → GaussianInputGenerationSkill
  → 本机计算后端
  → GaussianSinglePointResultExtractionSkill
  → CalculationResultValidationSkill
```

代码阅读时，第一层应该回答：

```text
系统有哪些能力？
```

第二层才回答：

```text
某个能力具体怎样实现？
```

这就是“Skill 集合”的核心思想。

---

## 2. 最重要的四个概念

### 2.1 Skill

Skill 是智能体可以选择和编排的能力。

例如：

```text
GaussianInputGenerationSkill
GaussianSinglePointResultExtractionSkill
CalculationResultValidationSkill
```

Skill 应该有：

```text
明确目标
明确输入
明确输出
明确能力标签
明确成功和失败语义
```

### 2.2 Adapter

Adapter 是 ChemSculptor 与具体计算程序之间的翻译层。

例如：

```text
Gaussian16ProgramAdapter
```

它知道：

```text
怎样生成 Gaussian 输入
怎样调用 g16
怎样构建执行上下文
怎样解析 Gaussian 输出
```

### 2.3 Parser 和 Translator

Parser 只负责把文本解析成结构化数据：

```text
Gaussian 文本
  → GaussianOutputParser
  → GaussianOutput
```

Translator 负责两种模型之间的翻译：

```text
GaussianOutput
  → GaussianResultTranslator
  → CalculationResult
```

### 2.4 Catalog

Catalog 是 Skill 集合的目录。

例如：

```text
CommonSkillCatalog
GaussianSkillCatalog
OrcaSkillCatalog
```

Catalog 让读者明确看到：

```text
这个程序族目前有哪些 Skill
```

---

## 3. 当前目录结构

```text
src/
  ChemSculptor.Skills.Common/
    CalculationResultValidation/
      CalculationResultValidationSkill.cs
      CalculationResultValidationSkillRequest.cs
      CalculationResultValidationSkillResult.cs
      CalculationResultValidationSkillDescriptor.cs
    CommonSkillCatalog.cs
    CommonSkillServiceRegistration.cs
    JsonSkill.cs

  ChemSculptor.Skills.Gaussian/
    GaussianInputGeneration/
      GaussianInputGenerationSkill.cs
      GaussianInputGenerationSkillRequest.cs
      GaussianInputGenerationSkillResult.cs
      GaussianInputGenerationSkillDescriptor.cs
    GaussianSinglePointResultExtraction/
      GaussianSinglePointResultExtractionSkill.cs
      GaussianSinglePointResultExtractionSkillRequest.cs
      GaussianSinglePointResultExtractionSkillResult.cs
      GaussianSinglePointResultExtractionSkillDescriptor.cs
    GaussianFailureDiagnosis/
      GaussianFailureDiagnosisSkill.cs
    GaussianFailureCorrectionProposal/
      GaussianFailureCorrectionProposalSkill.cs
      GaussianFailureCorrectionProposalSkillRequest.cs
      GaussianFailureCorrectionProposalSkillResult.cs
      GaussianFailureCorrectionProposalSkillDescriptor.cs
    GaussianSkillCatalog.cs
    GaussianSkillServiceRegistration.cs

  ChemSculptor.Skills.Orca/
    OrcaSinglePointResultExtraction/
    OrcaFailureDiagnosis/
    OrcaSkillCatalog.cs
    OrcaSkillServiceRegistration.cs

  ChemSculptor.Compute.Gaussian/
    Gaussian16ProgramAdapter.cs
    GaussianOutputParser.cs
    GaussianResultTranslator.cs
    GaussianProcessingPlanTranslator.cs

  ChemSculptor.Agent/
    只通过 ISkillRegistry、ISkillInvoker 和通用模型调用能力
```

---

## 4. 依赖方向

正常依赖方向是：

```text
API
  → Agent
  → Skill
  → Adapter
  → Parser / Translator
```

具体到 Gaussian：

```text
ChemSculptor.Api
  → ChemSculptor.Agent
  → ChemSculptor.Skills.Gaussian
  → ChemSculptor.Compute.Gaussian
  → ChemSculptor.Compute
```

不允许反向依赖：

```text
Compute.Gaussian 不能依赖 Agent
Parser 不能依赖 Skill
通用 Skill 不能依赖 Gaussian 专用类
```

当前 `ChemSculptor.Agent` 已不再直接引用：

```text
ChemSculptor.Compute.Gaussian
ChemSculptor.Compute.Local
ChemSculptor.InputProcessor
```

这表示 Agent 不知道 Gaussian 的输入格式、输出格式和命令行。

---

## 5. 基础 Skill 契约

内核原有契约是：

```text
ISkill
  Name
  Version
  Capabilities
  ExecuteAsync(TaskRequest)
  HealthAsync()
```

这是面向工作流和技能注册表的统一契约。

它不是强类型契约，因此需要一个通用桥接层。

---

## 6. JsonSkill：强类型请求与结果的桥梁

为了不使用 `object`，同时不让工作流直接依赖所有 Skill 类型，项目增加了：

```text
JsonSkill<TRequest, TResult>
```

技能继承它以后，只需要实现：

```text
ExecuteAsync(TRequest, CancellationToken)
```

基类负责：

```text
从 TaskRequest.Inputs["request"] 读取 JSON
反序列化为 TRequest
调用强类型执行方法
把 TResult 序列化为 TaskResult.Output
```

调用方向：

```text
Agent
  把通用请求序列化为 JSON
  → SkillJsonInvoker
  → ISkillRegistry.Resolve(skillId)
  → ISkill.ExecuteAsync(TaskRequest)
  → JsonSkill<TRequest, TResult>
  → 强类型技能逻辑
```

---

## 7. 为什么同时存在通用请求和专用请求

以结果提取为例。

通用请求：

```text
CalculationResultExtractionRequest
```

Gaussian 专用请求：

```text
GaussianSinglePointResultExtractionSkillRequest
  : CalculationResultExtractionRequest
```

为什么这样设计：

```text
Agent 只认识通用请求
Gaussian Skill 可以扩展专用请求
当前专用请求没有新增字段
以后需要时可以在专用请求中增加字段
```

结果类型也是同样结构：

```text
CalculationResultExtractionResult
GaussianSinglePointResultExtractionSkillResult
```

这使 Agent 与 Gaussian 类型保持解耦，并为以后扩展保留位置。

---

## 8. 当前 Skill 标识

技能标识集中定义在：

```text
src/ChemSculptor.Compute/CalculationSkillIds.cs
```

当前标识：

```text
gaussian.input-generation
gaussian.single-point-result-extraction
gaussian.failure-diagnosis
gaussian.failure-correction-proposal
calculation.result-validation
```

工作流和 Agent 通过标识调用能力。

---

## 9. 正常单点计算的完整流程

### 第一步：客户端发送原始文本

客户端只发送：

```text
SessionId
Text
CoordinateText
```

客户端不判断任务类型，也不生成计算参数。

### 第二步：会话层解释意图

```text
ConversationService
  → RuleBasedTaskInterpreter
  → SinglePoint
```

### 第三步：Agent 创建作业

`SinglePointCalculationExecutor` 创建：

```text
JobId
CalculationSpec
工作区目录
输入文件路径
运行文件路径
输出文件路径
```

### 第四步：调用输入生成 Skill

```text
SkillId:
  gaussian.input-generation

请求:
  CalculationInputGenerationRequest

实际执行:
  GaussianInputGenerationSkill
```

该 Skill 内部完成：

```text
解析坐标文本
转换为 CanonicalGeometry
调用 Gaussian16ProgramAdapter.WriteInputAsync
复制输入到 run 目录
调用 BuildExecutionContext
返回 CalculationJob 和 CalculationExecutionContext
```

### 第五步：提交本机进程

```text
SinglePointCalculationExecutor
  → IComputeBackend.SubmitAsync
  → LocalProcessBackend
  → g16
```

### 第六步：后台监控

```text
CalculationJobMonitor
  → 轮询 IComputeBackend.GetStatusAsync
```

### 第七步：调用结果提取 Skill

```text
SkillId:
  gaussian.single-point-result-extraction

实际执行:
  GaussianSinglePointResultExtractionSkill
```

该 Skill 内部完成：

```text
Gaussian16ProgramAdapter.ParseOutputAsync
  → GaussianOutputParser
  → GaussianOutput
  → GaussianResultTranslator
  → CalculationResult
```

### 第八步：调用结果验证 Skill

```text
SkillId:
  calculation.result-validation

实际执行:
  CalculationResultValidationSkill
```

当前验证：

```text
是否正常结束
是否有最终能量
FailureKind 是否为 None
```

### 第九步：生成通用处理方案

```text
CalculationProcessingPlan
  Outcome
  Summary
  Actions
```

正常计算会得到：

```text
Outcome = Completed
Actions = 空
```

### 第十步：保存文件

```text
jobs/<jobId>/manifest.json
jobs/<jobId>/results/result.json
jobs/<jobId>/results/processing-plan.json
```

---

## 10. 逐个理解三个核心 Skill

### 10.1 GaussianInputGenerationSkill

这是“准备 Gaussian 输入并建立运行上下文”的能力。

输入包含：

```text
CalculationJob
CalculationSpec
CoordinateText
输入、运行输入和输出路径
```

输出包含：

```text
更新后的 CalculationJob
CalculationExecutionContext
诊断信息
```

它的内部实现可以变化，但 Skill 契约保持稳定。

### 10.2 GaussianSinglePointResultExtractionSkill

这是“从 Gaussian 输出中取得通用结果”的能力。

它不直接向 Agent 返回 Gaussian 字段，而是返回：

```text
CalculationResult
```

Agent 因此只看到：

```text
Energy
NormalTermination
FailureKind
Diagnostics
```

看不到：

```text
SCF Done
RCAM-B3LYP
Link
GaussianOutput
```

### 10.3 CalculationResultValidationSkill

这是通用验证能力。

它不关心计算程序名称，只检查通用结果。

以后可以在这里增加：

```text
能量范围检查
自旋污染检查
结构合理性检查
与其他计算结果的一致性检查
```

这些规则不需要 Gaussian 模块参与。

---

## 11. Catalog 和注册

每个程序族有一个 Catalog。

当前：

```text
CommonSkillCatalog
  calculation.result-validation

GaussianSkillCatalog
  gaussian.input-generation
  gaussian.single-point-result-extraction
  gaussian.failure-diagnosis
  gaussian.failure-correction-proposal

OrcaSkillCatalog
  当前为空
```

启动时：

```text
AddCommonSkills
AddGaussianSkills
AddOrcaSkills
```

随后 API 从依赖注入中取出全部 `ISkill`，统一注册到：

```text
ISkillRegistry
```

显式注册的优点是：

```text
容易阅读
容易调试
不会依赖反射扫描
新增 Skill 时位置明确
```

---

## 12. 异常处理框架目前处于什么状态

当前已经保留：

```text
GaussianFailureDiagnosisSkill
GaussianFailureCorrectionProposalSkill
CalculationFailure
CalculationProcessingPlan
GaussianProcessingPlan
ProgramProcessingPlan
```

但是：

```text
没有执行异常诊断
没有自动修改 Gaussian 输入
没有自动重新提交修正作业
```

`GaussianFailureDiagnosisSkill` 当前会明确报告：

```text
Gaussian 异常诊断框架尚未实现
```

`GaussianFailureCorrectionProposalSkill` 目前能够把通用处理方案翻译成
Gaussian 方案，但不会被正常链路自动执行。

这样既保留了未来结构，也不会让未完成逻辑悄悄运行。

---

## 13. 怎样新增一个 Skill

以增加：

```text
GaussianFrequencyResultExtractionSkill
```

为例。

### 第一步：建立文件夹

```text
ChemSculptor.Skills.Gaussian/
  GaussianFrequencyResultExtraction/
```

### 第二步：创建四个文件

```text
GaussianFrequencyResultExtractionSkill.cs
GaussianFrequencyResultExtractionSkillRequest.cs
GaussianFrequencyResultExtractionSkillResult.cs
GaussianFrequencyResultExtractionSkillDescriptor.cs
```

### 第三步：定义技能标识

在 `CalculationSkillIds` 中增加：

```text
gaussian.frequency-result-extraction
```

### 第四步：定义通用请求和结果

如果需要跨模块调用，先在 `ChemSculptor.Compute` 中定义通用模型。

### 第五步：让专用请求继承通用请求

```text
GaussianFrequencyResultExtractionSkillRequest
  : CalculationFrequencyResultExtractionRequest
```

### 第六步：实现 Skill

继承：

```text
JsonSkill<
  GaussianFrequencyResultExtractionSkillRequest,
  GaussianFrequencyResultExtractionSkillResult>
```

### 第七步：加入 Catalog

在 `GaussianSkillCatalog` 中增加技能标识。

### 第八步：加入 DI 注册

在 `GaussianSkillServiceRegistration` 中注册为：

```text
ISkill
```

### 第九步：增加测试

至少测试：

```text
技能能解析请求
技能能返回结果
正确结果通过验证
错误结果能产生诊断
技能能被 ISkillRegistry 找到
```

---

## 14. 怎样新增 ORCA

ORCA 不需要修改通用验证 Skill。

新增：

```text
OrcaInputGenerationSkill
OrcaSinglePointResultExtractionSkill
OrcaFailureDiagnosisSkill
OrcaFailureCorrectionProposalSkill
```

在：

```text
ChemSculptor.Compute.Orca
```

中实现 ORCA 专用 Parser 和 Adapter。

在：

```text
OrcaSkillServiceRegistration
```

中注册 ORCA Skill。

Agent 的通用流程保持不变。

---

## 15. 推荐阅读顺序

不要一开始就钻入 Parser。

建议顺序：

```text
1. CalculationSkillIds.cs
2. CommonSkillCatalog.cs
3. GaussianSkillCatalog.cs
4. GaussianInputGenerationSkill.cs
5. GaussianSinglePointResultExtractionSkill.cs
6. CalculationResultValidationSkill.cs
7. SkillJsonInvoker.cs
8. SinglePointCalculationExecutor.cs
9. CalculationJobMonitor.cs
10. Gaussian16ProgramAdapter.cs
11. GaussianOutputParser.cs
12. GaussianResultTranslator.cs
```

先看能力，再看编排，最后看具体实现。

---

## 16. 设计规则

### 规则一：Skill 是能力，不是辅助函数

适合：

```text
提取单点能
验证结果
诊断 Gaussian 异常
生成 Gaussian 输入
```

不适合：

```text
读取一行文本
替换一个字符串
查找一个正则表达式
```

### 规则二：Agent 不依赖具体程序

Agent 可以知道：

```text
ISkillRegistry
ISkillInvoker
CalculationResult
CalculationProcessingPlan
```

Agent 不应知道：

```text
GaussianOutputParser
SCF Done
%chk
GAUSS_EXEDIR
```

### 规则三：程序专用 Skill 可以依赖程序 Adapter

```text
GaussianSinglePointResultExtractionSkill
  → IQuantumProgramAdapter
```

这是合理依赖。

### 规则四：通用 Skill 不能依赖程序专用类型

```text
CalculationResultValidationSkill
```

只能依赖通用结果模型。

### 规则五：一个 Skill 一个文件夹

Skill 请求、结果、描述和实现放在一起。

### 规则六：先定义契约，再写实现

先确定：

```text
输入是什么
输出是什么
成功是什么
失败是什么
```

再写 Parser、Adapter 和算法。

---

## 17. 当前实现的一个权衡

当前 `JsonSkill` 使用 JSON 作为通用调用边界。

优点是：

```text
Agent 不需要引用具体 Skill 项目
不直接依赖 Gaussian 类型
请求和结果可以记录
以后远程技能也容易扩展
```

代价是：

```text
多一次序列化
调试时需要同时看通用模型和专用模型
请求错误可能在反序列化阶段才暴露
```

当前阶段这个代价可以接受。

以后如果全部技能都在同一进程内，可以增加强类型技能调度器，而不必修改 Skill
本身的设计。

---

## 18. 常见问题

### 为什么专用请求类现在是空的？

因为当前字段全部来自通用请求。

空类不是无意义，它建立了稳定类型和以后的扩展位置。

### 为什么 Agent 不直接调用 GaussianOutputParser？

因为 Agent 应该只表达“提取结果”，而不是“怎样解析 Gaussian 文本”。

### 为什么异常诊断和结果提取要分开？

结果提取是确定性读取。

异常诊断需要识别错误原因、判断重试策略和可能请求人工确认，目标不同。

### 为什么先做正常流程？

因为正常流程能先证明：

```text
输入生成
进程运行
输出解析
结果验证
结果保存
```

这条基础链路稳定后，异常处理才有可靠落点。

### 为什么 ORCA 目录先为空？

目录表示未来扩展边界，不代表必须有占位代码。

没有实现的逻辑不应该伪装成已实现的 Skill。

---

## 19. 一句话理解整个架构

```text
Workflow 选择 Skill；
Agent 调用 Skill；
Skill 表达能力；
Adapter 负责程序；
Parser 负责文本；
Translator 负责模型转换；
Catalog 负责组织；
Registry 负责查找；
Test 负责证明。
```

ChemSculptor 的目标不是把每个函数包装成 Skill，而是让每个真正的科学能力都
有清晰的名称、输入、输出和实现边界。
