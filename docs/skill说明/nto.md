# NTO

## Skill 标识

```text
calculation.nto-transition
```

## Workflow 标识

```text
nto
```

## 职责边界

`NtoTransitionCalculationSkill` 只计算一个激发态的 NTO。
`NtoWorkflowService` 负责读取源 TDDFT、选择状态、调用多个单状态 Skill
并聚合结果。

## 1. 契约

代码落点：

```text
src/ChemSculptor.Compute/NtoModels.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
```

核心对象：

```text
NtoRequest
NtoSelectedState
NtoTransitionRequest
NtoTransitionResult
NtoWorkflowResult
```

`NtoSelectedState.StateId` 使用源作业和激发态编号形成稳定标识，例如：

```text
tddft:<sourceJobId>:state:1
```

## 2. 参数

选择模式：

```text
Default
WavelengthRange
All
```

默认和波长范围模式使用：

```text
只考虑 f > 0
按 f 降序最多选择 5 个
最大/最小正 f 比超过 10 时，保留 f >= max(f)/10
```

全部模式：

```text
包含 f = 0 的暗态
不应用振子强度阈值
```

## 3. 翻译

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/Nto/GaussianNtoInputWriter.cs
```

输入生成沿用源 TDDFT route 和参数，然后追加：

```text
Geom=AllCheck
Guess=(Read,Only)
Density=(Check,Transition=n)
Pop=(Minimal,NTO,SaveNTO)
nosymm
```

## 4. 结果

代码落点：

```text
src/ChemSculptor.Compute.Gaussian/Nto/GaussianNtoOutputParser.cs
```

结果包含：

```text
源作业标识
源激发态编号和标签
NTO 作业标识
正常结束状态
NTO 输出识别状态
输出文件
产物清单
诊断
```

## 5. 执行

代码落点：

```text
src/ChemSculptor.Skills.Common/NtoCalculation/
  NtoTransitionCalculationSkill.cs
```

每个选中状态只创建一个派生 NTO 作业，源检查点会复制到派生作业运行目录。

## 6. 验证

源 TDDFT 必须：

```text
正常结束
存在激发态
存在输入文件
存在检查点
```

单个 NTO 作业必须：

```text
作业完成
输出正常结束
识别到 NTO 输出
状态编号与源状态一致
```

没有 `f > 0` 状态属于正常业务结果，不属于异常。

## 7. 异常

建议错误码：

```text
nto.source_missing
nto.source_not_normal
nto.excited_state_missing
nto.checkpoint_missing
nto.selection_empty
nto.normal_termination_missing
nto.output_missing
nto.nto_not_parsed
```

## 8. 恢复

当前不做 NTO 自动恢复。

派生 NTO 作业失败时保留失败状态和诊断，后续可以增加 NTO 专用重试或诊断 Skill。

## 9. 工作流

代码落点：

```text
src/ChemSculptor.Agent/NtoWorkflowService.cs
```

流程：

```text
读取源 TDDFT Job 和 Result
-> NtoStateSelector
-> 逐个调用 NtoTransitionCalculationSkill
-> 聚合 NtoTransitionResult
```

多个状态中部分失败时，保留成功结果并返回 `Partial`。

## 10. 接入

代码落点：

```text
src/ChemSculptor.Agent/AgentServiceRegistration.cs
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
src/ChemSculptor.Skills.Common/CommonSkillServiceRegistration.cs
```

当前 NTO Workflow 通过源 TDDFT 作业标识启动，客户端后续接入时优先使用
最近一次 TDDFT 结果作为默认源，同时允许显式指定 `sourceTddftJobId`。

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/NtoStateSelectorTests.cs
tests/ChemSculptor.Core.Tests/GaussianNtoTests.cs
tests/ChemSculptor.Core.Tests/NtoWorkflowServiceTests.cs
```

已覆盖：

```text
默认前 5 选择
数量级阈值
波长范围
全部模式包含暗态
无正 f 状态的正常空结果
Transition 编号写入
多个状态调用和聚合
```

## 未完成项

```text
真实 Gaussian NTO 端到端验收
NTO 输出中的具体轨道占据数、轨道能量和轨道图形结构化解析
CASSCF 源结果支持
NTO 专用异常恢复
```
