# Absorption Spectrum

## Workflow 标识

```text
absorption-spectrum
```

## 职责边界

负责吸收光谱的模式判定和阶段编排。优化阶段复用
`optimization-complete`，TDDFT 阶段复用 `tddft-single-point`。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Compute/AbsorptionSpectrumModels.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
```

验收：

```text
AbsorptionSpectrumRequest 可以表达三种模式。
AbsorptionSpectrumSubmissionResult 同时保留优化和 TDDFT 阶段结果。
```

## 2. 参数

代码落点：

```text
src/ChemSculptor.Compute/TaskInterpretation.cs
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
```

模式：

```text
OptimizeThenTddft
TddftOnly
ConstrainedOptimizationThenTddft
```

TDDFT 参数继续使用：

```text
tddft-state-count = 50
```

## 3. 翻译

吸收光谱工作流不直接拼接 Gaussian 关键词。TDDFT 阶段交给
`GaussianRouteKeywordBuilder` 翻译。

默认 TDDFT route：

```text
#p CAM-B3LYP/6-31G* em=GD3BJ scfcyc=200 td(nstates=50) nosymm
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute/TddftModels.cs
src/ChemSculptor.Agent/AbsorptionSpectrumWorkflowService.cs
src/ChemSculptor.ScientificData.Extraction/AbsorptionScientificDataRecordSkill.cs
```

吸收光谱结果包含：

```text
优化结果
TDDFT 结果
激发态数量
最强振子强度
最强吸收波长
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Agent/AbsorptionSpectrumWorkflowService.cs
```

默认链路：

```text
IOptimizationCalculationService
-> ITddftCalculationService
```

## 6. 验证

优化阶段使用优化工作流验证。

TDDFT 阶段要求：

```text
程序正常结束
至少解析到一个激发态
```

## 7. 异常

首期停止条件：

```text
优化阶段失败
优化阶段没有最终几何
TDDFT 阶段失败
“不优化”和“冻结非氢原子”同时出现
```

## 8. 恢复

代码落点：

```text
src/ChemSculptor.Compute/AbsorptionSpectrumModels.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
src/ChemSculptor.Agent/NoOpAbsorptionSpectrumRecoveryFramework.cs
```

当前只保留：

```text
吸收光谱失败上下文
稳定失败阶段和错误码
IAbsorptionSpectrumRecoveryFramework 扩展点
未实现恢复计划
```

优化阶段内部继续使用 `optimization-complete` 自带的恢复链。

## 9. 工作流

三种模式：

```text
默认：优化 -> TDDFT -> 反馈
不优化：TDDFT -> 反馈
约束：冻结非氢原子优化 -> TDDFT -> 反馈
```

每类模式在成功后执行吸收光谱科学数据归档。

## 10. 接入

代码落点：

```text
src/ChemSculptor.Compute/TaskInterpretation.cs
src/ChemSculptor.Agent/AgentService.cs
src/ChemSculptor.Agent/AgentServiceRegistration.cs
src/ChemSculptor.Api/Endpoints/AgentEndpoints.cs
src/ChemSculptor.WinForms/MainForm.cs
src/ChemSculptor.ScientificData.Extraction/AbsorptionScientificDataRecordSkill.cs
.agents/skills/absorption-spectrum/
```

客户端输入示例：

```text
吸收光谱
紫外，不优化
吸收光谱，冻结非氢原子
```

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/AbsorptionSpectrumInterpretationTests.cs
tests/ChemSculptor.Core.Tests/AbsorptionSpectrumWorkflowServiceTests.cs
tests/ChemSculptor.Core.Tests/TddftWorkflowDefinitionFactoryTests.cs
tests/ChemSculptor.Core.Tests/TddftCalculationServiceTests.cs
tests/ChemSculptor.Core.Tests/AgentServiceTests.cs
tests/ChemSculptor.Core.Tests/AbsorptionScientificDataRecordSkillTests.cs
tests/ChemSculptor.Core.Tests/AbsorptionSpectrumRealGaussianTests.cs
```

## 未完成项

```text
吸收光谱专用恢复策略。
吸收光谱科学摘要和成果包客户端闭环。
```
