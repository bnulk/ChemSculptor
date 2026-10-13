# Single Point Calculation

## Skill 标识

```text
calculation.single-point
```

## 职责边界

完成一次稳定的单点计算。异常诊断、修正和重算由外层的
单点完整 Workflow 负责。

## 1. 契约

目标：

```text
输入完整坐标和计算方案，输出 CalculationResult 和验证报告。
```

代码落点：

```text
src/ChemSculptor.Compute/CalculationModels.cs
src/ChemSculptor.Compute/SkillExecutionContracts.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
```

验收：

```text
SinglePointCalculationSkillResult 可以 JSON 往返。
结果包含 Job、Result、Report、Wait 和 Steps。
```

## 2. 参数

目标：

```text
支持方法、基组、色散、对称性、SCF、溶剂、电荷和多重度。
```

代码落点：

```text
src/ChemSculptor.Compute/CalculationOptionKeys.cs
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
src/ChemSculptor.Compute/CalculationParameterResolver.cs
src/ChemSculptor.Compute/RuleBasedCalculationParameterExtractor.cs
```

验收：

```text
参数可以提取、解析、覆盖并保留来源。
```

## 3. 翻译

目标：

```text
把规范参数翻译为 Gaussian SP route。
```

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianRouteKeywordBuilder.cs
src/ChemSculptor.Compute.Gaussian/GaussianInputWriter.cs
```

验收：

```text
route 包含 SP，不包含 opt 或 freq。
```

## 4. 结果

目标：

```text
解析正常结束、能量、SCF 收敛和迭代次数。
```

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutput.cs
src/ChemSculptor.Compute.Gaussian/GaussianOutputParser.cs
src/ChemSculptor.Compute.Gaussian/GaussianResultTranslator.cs
```

验收：

```text
CalculationResult 包含能量、SCF 状态和失败类别。
```

## 5. 执行

目标：

```text
通过 prepare-input、submit、wait、extract-result、validate 完成一次任务。
```

代码落点：

```text
src/ChemSculptor.Skills.Common/SinglePointCalculation/
src/ChemSculptor.Skills.Common/CalculationWorkflow/
```

验收：

```text
一次调用只创建一个计算作业。
```

## 6. 验证

目标：

```text
复用 SinglePointCalculationResultValidator。
```

代码落点：

```text
src/ChemSculptor.Skills.Common/CalculationResultValidation/
```

验收：

```text
正常结束、SCF、能量和输出文件均检查。
```

## 7. 异常

目标：

```text
使用 scf、output、normal_termination 和 energy 异常码。
```

代码落点：

```text
src/ChemSculptor.Anomaly/Models/CommonAnomalyCodes.cs
```

验收：

```text
每个异常具有稳定代码、证据和摘要。
```

## 8. 恢复

目标：

```text
恢复不属于本 Skill，由单点完整 Workflow 负责。
```

代码落点：

```text
src/ChemSculptor.Agent/SinglePointWorkflowDefinitionFactory.cs
src/ChemSculptor.Skills.Common/AnomalyWorkflow/
```

## 9. 工作流

目标：

```text
正常路径、稳定性检查、稳定修正、派生单点作业和复检。
```

代码落点：

```text
src/ChemSculptor.Agent/SinglePointWorkflowDefinitionFactory.cs
```

## 10. 接入

目标：

```text
接入 AgentService、Api、WinForms 和 Codex Skill。
```

代码落点：

```text
src/ChemSculptor.Agent/AgentService.cs
src/ChemSculptor.Api/Endpoints/AgentEndpoints.cs
src/ChemSculptor.WinForms/MainForm.cs
.agents/skills/single-point-calculation/
```

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/SinglePointCalculationSkillTests.cs
tests/ChemSculptor.Core.Tests/SinglePointCalculationServiceTests.cs
tests/ChemSculptor.Core.Tests/GaussianOutputParserTests.cs
tests/ChemSculptor.Core.Tests/SinglePointWorkflowDefinitionFactoryTests.cs
tests/ChemSculptor.Core.Tests/AgentServiceTests.cs
```

## 未完成项

```text
真实 Gaussian 稳定性复检和成果包客户端点击验收。
```
