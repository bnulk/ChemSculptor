# Wavefunction Stability Check

## Skill 标识

```text
anomaly.check-optimization-stability
```

## 职责边界

检查最终采用的优化几何对应波函数是否稳定；不在检查 Skill 内修正或重算。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Anomaly/Models/AnomalyCheckResult.cs
src/ChemSculptor.Anomaly/Models/WavefunctionStabilityResult.cs
src/ChemSculptor.Skills.Common/OptimizationRecovery/
  OptimizationStabilityCheckSkill.cs
```

## 2. 参数

规范化参数：

```text
wavefunction-stability-check = Check | Skip
```

代码落点：

```text
src/ChemSculptor.Compute/CalculationOptionKeys.cs
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
src/ChemSculptor.Compute/RuleBasedCalculationParameterExtractor.cs
```

否定词规则：

```text
不做、不检查、跳过、忽略、取消 + 波函数稳定性
-> Skip
```

## 3. 翻译

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/Anomaly/WavefunctionStability/
  GaussianWavefunctionStabilityInputWriter.cs
```

Gaussian 关键词：

```text
guess=read geom=check stable
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/Anomaly/WavefunctionStability/
  GaussianWavefunctionStabilityParser.cs
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Skills.Common/AnomalyWorkflow/
  WavefunctionStabilityCheckSkill.cs
src/ChemSculptor.Skills.Common/OptimizationRecovery/
  OptimizationStabilityCheckSkill.cs
```

## 6. 验证

目标：

```text
检查辅助作业正常结束、稳定性结果存在，并判断稳定或不稳定。
```

## 7. 异常

代码落点：

```text
src/ChemSculptor.Anomaly/Models/CommonAnomalyCodes.cs
```

## 8. 恢复

优化不稳定时：

```text
同多重度 -> stable=opt
不同多重度 -> 修改多重度
```

恢复后必须重新优化并复检。

## 9. 工作流

代码落点：

```text
src/ChemSculptor.Agent/OptimizationWorkflowDefinitionFactory.cs
```

稳定分支进入振动分析；不稳定分支进入稳定性恢复。

## 10. 接入

客户端显示：

```text
Stable
Unstable
Skipped
Failed
```

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/OptimizationStabilityCheckTests.cs
tests/ChemSculptor.Core.Tests/OptimizationWorkflowTests.cs
tests/ChemSculptor.Core.Tests/RuleBasedCalculationParameterExtractorTests.cs
```
