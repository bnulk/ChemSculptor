# 单点计算 Skill 与完整工作流

> 适用版本：`v0.51.0` 之后
> 目标：区分“一次稳定的单点计算”与“完成单点计算工作流”

## 1. 分层

```text
单点计算 Skill
  calculation.single-point

原子能力 Skill
  calculation.prepare-input
  calculation.submit
  calculation.wait
  calculation.extract-result
  calculation.workflow-validation

完整单点计算工作流
  单点计算 Skill
  → 波函数稳定性检查
  → 修正规划
  → 派生恢复作业
  → 恢复复检
  → 处理方案
  → 科学数据记录
```

`calculation.single-point` 只负责正常计算路径。异常诊断、SCF
修正、重算和最终处理由外层工作流负责。

## 2. 输入契约

`SinglePointCalculationSkill` 使用：

```text
CalculationInputGenerationRequest
  Job
  Spec
  CoordinateText
  InputFilePath
  RunInputFilePath
  OutputFilePath
```

`Spec` 包含：

```text
TaskType
Program
Method
Basis
Charge
Multiplicity
ElectronicStateObjective
Solvent
ExtraOptions
Parameters
```

## 3. 执行方式

复合 Skill 按固定顺序调用原子能力：

```text
calculation.prepare-input
  → calculation.submit
  → calculation.wait
  → calculation.extract-result
  → calculation.workflow-validation
```

输入模板、程序调用和输出解析仍由程序适配器负责：

```text
IQuantumProgramAdapter.WriteInputAsync
IComputeBackend
IQuantumProgramAdapter.PostProcessAsync
IQuantumProgramAdapter.ParseOutputAsync
```

## 4. 结果契约

`SinglePointCalculationSkillResult` 包含：

```text
Succeeded
Error
Passed
Job
Result
Report
Wait
Steps
```

`CalculationResult` 中的单点关键字段：

```text
Energy
EnergyUnit
ScfConverged
ScfIterations
NormalTermination
FailureKind
OutputFilePath
Artifacts
Diagnostics
```

## 5. 基本校验

单点结果验证包含：

```text
任务标识、程序、方法、基组一致
电荷和自旋多重度一致
程序正常结束
SCF 已真正收敛
存在有限能量
能量单位为 Hartree
输出文件存在
```

`ScfConverged` 为空也会被视为不能确认收敛。

## 6. 错误码映射

Gaussian 解析器当前识别：

```text
SCF Done
Convergence failure
SCF has not converged
No convergence in SCF
Error termination
Normal termination of Gaussian
```

通用失败类别：

```text
ProcessFailed
OutputMissing
NormalTerminationMissing
ProgramError
EnergyMissing
Canceled
Unknown
ScfNotConverged
```

## 7. 边界

```text
单点 Skill 不直接拼接 Gaussian 输入
单点 Skill 不直接启动 g16
单点 Skill 不直接解析 Gaussian 文本
单点 Skill 不自动无限重试
SCF 修复属于外层完整工作流
```

