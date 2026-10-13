# Frequency Analysis

## Skill 标识

```text
calculation.frequency-analysis
```

## 职责边界

针对最终采用的优化几何，完成一次 Gaussian `Freq` 振动分析。
恢复、重优化和归档由优化 Workflow 负责。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Compute/FrequencyAnalysisModels.cs
```

输出：

```text
FrequencyAnalysisResult
  FrequencyAnalysisResult.Frequencies
  FrequencyAnalysisResult.ImaginaryFrequencyCount
  FrequencyAnalysisResult.MinimumFrequency
  FrequencyAnalysisResult.IsMinimum
  FrequencyAnalysisResult.ZeroPointCorrection
  FrequencyAnalysisResult.ThermalCorrectionToEnergy
  FrequencyAnalysisResult.ThermalCorrectionToEnthalpy
  FrequencyAnalysisResult.ThermalCorrectionToGibbs
```

## 2. 参数

首期复用优化参数：

```text
engine
method
basis
dispersion-correction
symmetry
scf-iteration-limit
solvent
charge
multiplicity
```

后续预留：

```text
temperature
pressure
frequency-scale-factor
```

## 3. 翻译

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianRouteKeywordBuilder.cs
src/ChemSculptor.Compute.Gaussian/FrequencyAnalysis/
  GaussianFrequencyInputWriter.cs
```

验收：

```text
输入包含 Freq，不包含 opt 或 stable；保留方法、基组、色散和对称性。
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/FrequencyAnalysis/
  GaussianFrequencyOutputParser.cs
```

验收：

```text
能够解析频率、虚频、最低频率和热化学校正。
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Skills.Common/OptimizationRecovery/
  OptimizationFrequencyAnalysisSkill.cs
```

验收：

```text
只提交一次 Freq 作业；输入来自最终采用的优化作业。
```

## 6. 验证

目标：

```text
检查正常结束、频率是否存在、虚频数和热化学校正。
```

代码落点：

```text
src/ChemSculptor.Skills.Common/OptimizationRecovery/
  OptimizationFrequencyAnalysisSkill.cs
tests/ChemSculptor.Core.Tests/FrequencyAnalysisTests.cs
```

## 7. 异常

目标：

```text
定义频率输出缺失、正常结束缺失、解析失败和热化学缺失异常。
```

当前状态：

```text
已具备 NormalTermination 和解析结果；
专用 Frequency 异常码仍待补充。
```

## 8. 恢复

目标：

```text
虚频说明不是稳定最小点。
是否重新优化由优化 Workflow 决定，本 Skill 不自行重算。
```

## 9. 工作流

代码落点：

```text
src/ChemSculptor.Agent/OptimizationWorkflowDefinitionFactory.cs
```

顺序：

```text
波函数稳定性检查
-> 稳定性修正和复检
-> 振动分析
-> 处理方案
-> 科学数据记录
```

## 10. 接入

客户端显示：

```text
振动分析状态
频率数量
虚频数量
最低频率
是否为稳定最小点
零点校正和热校正
```

代码落点：

```text
src/ChemSculptor.Agent/AgentModels.cs
src/ChemSculptor.Agent/AgentService.cs
src/ChemSculptor.Api/Contracts.cs
src/ChemSculptor.Api/Endpoints/AgentEndpoints.cs
src/ChemSculptor.WinForms/MainForm.cs
.agents/skills/geometry-optimization/SKILL.md
```

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/FrequencyAnalysisTests.cs
tests/ChemSculptor.Core.Tests/OptimizationWorkflowTests.cs
```

必须覆盖：

```text
Freq 输入生成
频率输出解析
虚频识别
优化 Workflow 顺序
客户端摘要字段
```

## 未完成项

```text
Frequency 专用异常码
温度、压力、缩放因子参数
真实 Gaussian 端到端验收
```
