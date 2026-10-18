# Optimization Frequency

## Skill 标识

```text
calculation.optimization-frequency
```

## 职责边界

一个任务级 Skill 只提交一次 Gaussian `opt freq` 作业。几何优化轨迹、
振动频率和热化学校正来自同一份输出。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Compute/OptimizationFrequencyModels.cs
src/ChemSculptor.Compute/SkillExecutionContracts.cs
```

验收：

```text
OptimizationFrequencyCalculationSkillResult 同时包含
OptimizationResult 和 FrequencyAnalysisResult。
```

## 2. 参数

代码落点：

```text
src/ChemSculptor.Compute/CalculationModels.cs
src/ChemSculptor.Compute/CalculationDefaults.cs
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
src/ChemSculptor.Compute/CalculationParameterResolver.cs
src/ChemSculptor.Compute/RuleBasedCalculationParameterExtractor.cs
```

任务类型：

```text
OptimizationFrequency
```

复用优化参数：

```text
optimization-max-cycles
optimization-convergence
optimization-restart
optimization-initial-hessian
optimization-constraint-mode
```

## 3. 翻译

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianRouteKeywordBuilder.cs
```

验收：

```text
route 同时包含优化和频率任务关键词
不生成 SP
多个优化选项合并为一个 opt=(...)
```

示例：

```text
#p CAM-B3LYP/6-31G* opt=(CalcFC, MaxCycles=300) Freq em=GD3BJ
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutputParser.cs
src/ChemSculptor.Compute.Gaussian/GaussianResultTranslator.cs
src/ChemSculptor.Compute.Gaussian/FrequencyAnalysis/GaussianFrequencyOutputParser.cs
src/ChemSculptor.Skills.Common/CalculationWorkflow/CalculationResultExtractionWorkflowSkill.cs
```

共享输出文件同时产生：

```text
OptimizationResult
FrequencyAnalysisResult
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Skills.Common/OptimizationFrequencyCalculation/
src/ChemSculptor.Agent/OptimizationFrequencyCalculationService.cs
src/ChemSculptor.Agent/OptimizationFrequencyWorkflowDefinitionFactory.cs
```

执行链：

```text
prepare-input
-> submit
-> wait
-> extract-result
-> validate-optimization-frequency
```

## 6. 验证

代码落点：

```text
src/ChemSculptor.Skills.Common/CalculationResultValidation/
src/ChemSculptor.Skills.Common/OptimizationFrequencyCalculation/
  OptimizationFrequencyCalculationValidationSkill.cs
```

优化检查：

```text
复用 OptimizationStepValidator
复用 OptimizationResultValidator
复用 OptimizationConvergenceValidator
```

频率检查：

```text
正常结束
存在频率
存在最低频率
无虚频作为建议项
```

## 7. 异常

代码落点：

```text
src/ChemSculptor.Anomaly/Models/FrequencyAnalysisAnomalyCodes.cs
```

稳定错误码：

```text
frequency.not_normal_termination
frequency.missing_frequencies
frequency.imaginary_modes_present
```

## 8. 恢复

当前 Skill 不实现自动恢复。

边界：

```text
优化未收敛、SCF 异常和虚频处理由外层 Workflow 决定
任何恢复都应创建派生作业
不得覆盖原始 opt freq 作业
```

## 9. 工作流

代码落点：

```text
src/ChemSculptor.Agent/OptimizationFrequencyWorkflowDefinitionFactory.cs
```

当前工作流：

```text
optimization-frequency
-> optimization-frequency-stability-check
```

`optimization-frequency-stability-check` 使用最终联合作业的 Job 和
OptimizationResult 执行波函数稳定性检查。稳定性结果作为 Workflow
输出返回，但当前不自动执行稳定性修正。

后续完整 Workflow 可以复用优化异常、SCF 修复和频率科学处理能力，但不能
把多个作业拆回 Skill 内部。

## 10. 接入

代码落点：

```text
src/ChemSculptor.Compute/TaskInterpretation.cs
src/ChemSculptor.Agent/AgentService.cs
src/ChemSculptor.Agent/AgentServiceRegistration.cs
src/ChemSculptor.WinForms/MainForm.cs
.agents/skills/optimization-frequency/
```

客户端示例：

```text
优化
opt
优化并做振动分析
优化 振动分析
优化后做振动分析
#p CAM-B3LYP/6-31G* opt freq
```

只有“优化”或 `opt` 时进入普通优化 Skill；同时包含优化意图和频率分析
意图时，才进入联合优化频率 Skill。

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/OptimizationFrequencyRouteTests.cs
tests/ChemSculptor.Core.Tests/OptimizationFrequencyCalculationSkillTests.cs
tests/ChemSculptor.Core.Tests/OptimizationFrequencyRealGaussianTests.cs
tests/ChemSculptor.Core.Tests/AgentServiceTests.cs
```

真实 Gaussian 测试默认不运行，设置：

```text
CHEMSCULPTOR_RUN_REAL_GAUSSIAN=1
```

## 未完成项

```text
优化频率完整异常恢复工作流
虚频后的科学修正策略
真实复杂分子 opt freq 端到端验收
```
