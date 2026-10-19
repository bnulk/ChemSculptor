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

当前 NTO Workflow 通过源 TDDFT 作业标识启动。Agent 当前自动使用同一会话
最近一次 TDDFT 作业；`NtoRequest.SourceTddftJobId` 保留显式源字段，客户端
自然语言尚未开放单独指定源作业。

客户端自然语言接入规则：

```text
文本出现 NTO
-> CalculationTaskType.Nto

NTO 300-500
-> WavelengthRange
-> 默认单位 nm

NTO 前出现“全部”
全部吸收的 NTO
全部波长的 NTO
-> All，包含暗态和全部波长
```

源 TDDFT 使用同一会话最近一次成功提交的 TDDFT 作业。不存在源作业时返回
`NoTddftSource` 和“请先完成 TDDFT 计算”的提示，不提交 NTO 作业。

Api 返回：

```text
NtoSelectionMode
NtoSelectedStateCount
NtoJobIds
```

WinForms 显示选择模式、选中状态数量和派生 NTO 作业标识。

## 11. 测试

代码落点：

```text
tests/ChemSculptor.Core.Tests/NtoInterpretationTests.cs
tests/ChemSculptor.Core.Tests/NtoStateSelectorTests.cs
tests/ChemSculptor.Core.Tests/GaussianNtoTests.cs
tests/ChemSculptor.Core.Tests/NtoWorkflowServiceTests.cs
tests/ChemSculptor.Core.Tests/AgentServiceTests.cs
```

已覆盖：

```text
NTO 关键词识别
短横连接的数字按 nm 波长范围解释
中文连接词和单位
NTO 前“全部吸收”“全部波长”等表达
反向波长范围拒绝
无 TDDFT 源提示
复用当前会话最近 TDDFT 作业
全部 NTO 无正 f 状态返回正常空结果
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
