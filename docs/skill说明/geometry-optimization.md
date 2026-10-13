# Geometry Optimization

## Skill 标识

```text
calculation.optimization
```

## 职责边界

完成一次正常的几何优化作业。轨迹、异常、恢复和归档由
`optimization-complete` Workflow 负责。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Compute/OptimizationModels.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
```

验收：

```text
OptimizationResult 包含轨迹、步骤、最终能量、最终几何和收敛状态。
```

## 2. 参数

代码落点：

```text
src/ChemSculptor.Compute/CalculationOptionKeys.cs
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
src/ChemSculptor.Compute/CalculationParameterResolver.cs
```

首期参数：

```text
optimization-max-cycles
optimization-convergence
optimization-restart
optimization-initial-hessian
wavefunction-stability-check
```

## 3. 翻译

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianRouteKeywordBuilder.cs
src/ChemSculptor.Compute.Gaussian/GaussianInputWriter.cs
```

验收：

```text
包含 opt，不包含 SP；多个优化选项合并为一个 opt=(...)。
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutput.cs
src/ChemSculptor.Compute.Gaussian/GaussianOutputParser.cs
src/ChemSculptor.Compute.Gaussian/GaussianResultTranslator.cs
```

验收：

```text
解析每一步的能量、SCF、几何和收敛指标。
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Skills.Common/OptimizationCalculation/
```

验收：

```text
只提交一个优化作业，不在 Skill 内拆成多个单点作业。
```

## 6. 验证

代码落点：

```text
src/ChemSculptor.Skills.Common/CalculationResultValidation/
src/ChemSculptor.Skills.Common/OptimizationCalculation/
```

验收：

```text
分别验证优化步骤、总体结果和最终收敛指标。
```

## 7. 异常

代码落点：

```text
src/ChemSculptor.Anomaly/Models/OptimizationAnomalyCodes.cs
src/ChemSculptor.Anomaly/Checks/OptimizationConvergenceCheck.cs
```

## 8. 恢复

代码落点：

```text
src/ChemSculptor.Skills.Common/OptimizationRecovery/
src/ChemSculptor.Skills.Gaussian/Anomaly/Recovery/
```

首期策略：

```text
增加 maxcycles
Restart
CalcFC
ReadFC
复用 SCF 修复
```

## 9. 工作流

代码落点：

```text
src/ChemSculptor.Agent/OptimizationWorkflowDefinitionFactory.cs
```

关键顺序：

```text
优化
-> 收敛异常检查
-> 优化恢复
-> 波函数稳定性检查
-> 稳定性修正和复检
-> 振动分析
-> 处理方案
-> 科学数据
```

## 10. 接入

代码落点：

```text
src/ChemSculptor.Compute/TaskInterpretation.cs
src/ChemSculptor.Agent/AgentService.cs
src/ChemSculptor.Agent/OptimizationCalculationService.cs
src/ChemSculptor.Api/
src/ChemSculptor.WinForms/
.agents/skills/geometry-optimization/
```

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/OptimizationParserTests.cs
tests/ChemSculptor.Core.Tests/OptimizationValidatorTests.cs
tests/ChemSculptor.Core.Tests/OptimizationAnomalyTests.cs
tests/ChemSculptor.Core.Tests/OptimizationRouteKeywordBuilderTests.cs
tests/ChemSculptor.Core.Tests/OptimizationCalculationSkillTests.cs
tests/ChemSculptor.Core.Tests/OptimizationWorkflowTests.cs
tests/ChemSculptor.Core.Tests/OptimizationAgentIntegrationTests.cs
tests/ChemSculptor.Core.Tests/OptimizationEndToEndTests.cs
```

## 未完成项

```text
真实 Gaussian 优化与稳定性恢复端到端验收。
```
