# TDDFT Single Point

## Skill 标识

```text
calculation.tddft-single-point
```

## 职责边界

完成一次 TDDFT 单点计算。暂时不实现 TDDFT 完整 Workflow。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Compute/TddftModels.cs
src/ChemSculptor.Compute/SkillExecutionContracts.cs
```

验收：

```text
TddftCalculationResult 包含激发态和跃迁矩。
TddftSinglePointCalculationSkillResult 可以 JSON 往返。
```

## 2. 参数

代码落点：

```text
src/ChemSculptor.Compute/CalculationOptionKeys.cs
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
src/ChemSculptor.Compute/CalculationDefaults.cs
src/ChemSculptor.Compute/RuleBasedCalculationParameterExtractor.cs
```

默认参数：

```text
method = CAM-B3LYP
basis = 6-31G*
dispersion-correction = GD3BJ
scf-iteration-limit = 200
tddft-state-count = 50
symmetry = Ignore
```

## 3. 翻译

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianRouteKeywordBuilder.cs
```

默认 route：

```text
#p cam-b3lyp/6-31G* em=GD3BJ scfcyc=200 td(nstates=50) nosymm
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/Tddft/GaussianTddftOutputParser.cs
```

解析：

```text
Excited State
轨道跃迁
transition velocity quadrupole moments
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Skills.Common/TddftCalculation/TddftSinglePointCalculationSkill.cs
```

执行链：

```text
prepare-input
-> submit
-> wait
-> extract-result
```

## 6. 验证

首期检查：

```text
正常结束
至少解析到一个激发态
```

后续 TDDFT Workflow 再扩展完整验证报告。

## 7. 异常

当前状态：

```text
任务级 Skill 不包含完整异常恢复。
TDDFT 专用异常码和恢复策略留待 TDDFT Workflow。
```

## 8. 恢复

当前不实现。

## 9. 工作流

当前不实现 TDDFT Workflow。

## 10. 接入

代码落点：

```text
src/ChemSculptor.Compute/TaskInterpretation.cs
src/ChemSculptor.Skills.Common/TddftCalculation/
src/ChemSculptor.Skills.Common/CommonSkillServiceRegistration.cs
.agents/skills/tddft-single-point/
```

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/TddftCalculationTests.cs
tests/ChemSculptor.Core.Tests/TddftCalculationSkillTests.cs
```
