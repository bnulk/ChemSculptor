# Codex Skill 编写教程：以单点计算为例

> 官方依据：[Codex skills](https://developers.openai.com/codex/skills/)
> 结构校验依据：Codex 内置 `skill-creator` 与 `quick_validate.py`
> 实例：ChemSculptor 当前的单点计算
> 目标：照着本教程一步一步做，就能写出一个合规的 Codex Skill

---

## 开始前：先分清三个东西

当前单点计算里同时存在三个层次：

```text
1. Codex Skill
   .agents/skills/single-point-calculation/SKILL.md

2. ChemSculptor 运行时 Skill
   calculation.single-point
   src/ChemSculptor.Skills.Common/SinglePointCalculation/SinglePointCalculationSkill.cs

3. 完整单点计算工作流
   src/ChemSculptor.Agent/SinglePointWorkflowDefinitionFactory.cs
```

它们的关系是：

```text
Codex Skill
  告诉 Codex 怎样正确使用系统

运行时报 Skill
  真正执行一次正常单点计算

Workflow
  负责异常、修复、重算和结果归档
```

因此：

```text
Codex Skill 不复制 C# 实现
运行时 Skill 不读取 SKILL.md
Workflow 不依赖 SKILL.md 才能运行
```

---

## 第一步：决定这个 Codex Skill 负责什么

先写清楚边界，再创建文件。

单点计算的 Codex Skill 负责：

```text
识别一次稳定单点计算请求
检查输入契约是否完整
调用 ChemSculptor 已有的单点计算入口
读取结果契约
检查正常结束、能量存在、SCF 真正收敛
把异常情况转交完整工作流
```

它不负责：

```text
自己生成 Gaussian 输入
自己调用 g16
自己解析 Gaussian 文本
自己修复 SCF 不收敛
自己无限重试
```

结论：

```text
Codex Skill 名称：single-point-calculation
运行时 Skill 标识：calculation.single-point
```

---

## 第二步：创建目录

项目级 Skill 统一放在：

```text
.agents/skills/single-point-calculation/
```

用户级 Skill 默认放在：

```text
$CODEX_HOME/skills/single-point-calculation/
```

单点计算 Skill 使用下面的目录：

```text
.agents/skills/single-point-calculation/
|-- SKILL.md
|-- agents/
|   `-- openai.yaml
|-- references/
|   |-- input-contract.md
|   |-- result-contract.md
|   |-- validation-rules.md
|   |-- error-codes.md
|   `-- gaussian.md
`-- scripts/
    `-- validate_single_point_result.ps1
```

规则：

```text
只有一个必需文件：SKILL.md
只有确实需要时才创建 scripts、references、assets
不要创建 README.md、CHANGELOG.md、安装指南
```

---

## 第三步：写 SKILL.md 的 frontmatter

文件：

```text
.agents/skills/single-point-calculation/SKILL.md
```

开头必须这样写：

```yaml
---
name: single-point-calculation
description: "使用 ChemSculptor 完成一次稳定的单点计算；当任务需要 SCF 修复、重算或异常恢复时，转交完整单点工作流。"
---
```

逐条解释：

```text
name
  必须等于目录名
  只能用小写字母、数字、连字符
  不能超过 64 个字符
  不能以连字符开头或结尾
  不能出现连续连字符

description
  说明“做什么”
  说明“什么时候适用”
  不超过 1024 个字符
  不能包含尖括号
  不能保留 TODO
```

当前单点计算实例中：

```text
name = single-point-calculation
description 明确提到 SCF 修复、重算和异常恢复要转交完整工作流
```

这就避免 Codex 把 Codex Skill 误当成 SCF 修复流程。

---

## 第四步：写 SKILL.md 正文

正文只保留必要规则。推荐内容如下：

````markdown
# Single Point Calculation

## 适用范围

用于输入完整、目标是完成一次稳定单点计算的场景。

不用于几何优化、频率计算、激发态计算或自动 SCF 修复。

## 必需输入

```text
结构
电荷
多重度
方法
基组
溶剂
引擎
```

缺少任何必需输入时先补齐，不要猜测。

## 执行

优先调用 ChemSculptor 已有的单点计算入口。

运行时能力标识：

```text
calculation.single-point
```

不要重新实现输入生成、程序调用或输出解析。

## 结果契约

```text
Energy
EnergyUnit
ScfConverged
ScfIterations
NormalTermination
FailureKind
OutputFilePath
Diagnostics
```

## 基本验证

```text
任务正常结束
SCF 已真正收敛
能量存在且有限
输出文件存在
结果与输入参数一致
```

## 停止条件

```text
SCF 未收敛
输出缺少正常终结
缺少能量
引擎不支持
输入契约不完整
```

停止后转交完整单点工作流处理，不在本 Skill 内无限重试。

## 参考资料

- 输入契约：references/input-contract.md
- 结果契约：references/result-contract.md
- 校验规则：references/validation-rules.md
- 错误码：references/error-codes.md
- Gaussian 细节：references/gaussian.md
````

注意：

```text
正文不复制 C# 代码
正文不复制完整 API 文档
正文只做路由和必要约束
```

---

## 第五步：写输入契约 reference

问一个简单问题：

```text
调用 calculation.single-point 前，必需知道什么？
```

当前 C# 类型：

```text
CalculationInputGenerationRequest
```

文件：

```text
.agents/skills/single-point-calculation/references/input-contract.md
```

内容：

````markdown
# Single Point Input Contract

调用 calculation.single-point 前必须提供：

```text
structure
charge
multiplicity
method
basis
solvent
engine
```

字段含义：

```text
structure
  分子结构，使用 ChemSculptor 支持的坐标文本格式

charge
  总电荷，整数

multiplicity
  自旋多重度，正奇数或正偶数，必须与电子数一致

method
  方法或泛函，例如 CAM-B3LYP

basis
  基组，例如 6-31G*

solvent
  溶剂模型；气相时明确写 gas 或为空

engine
  计算引擎，例如 Gaussian 16
```

缺少必需字段时停止，不要猜测。

程序专用参数不要写进本文件；Gaussian 细节见 gaussian.md。
````

规则：

```text
输入契约与程序无关
不要出现 %chk、GAUSS_EXEDIR、SCF=QC 等 Gaussian 关键词
```

---

## 第六步：写结果契约 reference

问：

```text
一次稳定单点计算完成后，必须返回什么？
```

当前 C# 类型：

```text
SinglePointCalculationSkillResult
CalculationResult
```

文件：

```text
.agents/skills/single-point-calculation/references/result-contract.md
```

内容：

````markdown
# Single Point Result Contract

必需字段：

```text
Energy
EnergyUnit
ScfConverged
ScfIterations
NormalTermination
FailureKind
OutputFilePath
Diagnostics
```

字段含义：

```text
Energy
  最终单点能量

EnergyUnit
  能量单位，当前为 Hartree

ScfConverged
  SCF 是否真正收敛

ScfIterations
  最终 SCF 迭代次数

NormalTermination
  程序是否正常结束

FailureKind
  通用失败类别

OutputFilePath
  程序主要输出文件

Diagnostics
  解析和验证诊断
```

只满足 NormalTermination 不足以通过。
ScfConverged 必须为 true，能量必须存在且有限。
````

这直接对应当前代码：

```text
CalculationResult.ScfConverged
CalculationResult.ScfIterations
CalculationResult.NormalTermination
CalculationResult.Energy
CalculationResult.FailureKind
```

---

## 第七步：写校验规则 reference

问：

```text
什么情况才算“稳定完成一次单点计算”？
```

文件：

```text
.agents/skills/single-point-calculation/references/validation-rules.md
```

内容：

````markdown
# Single Point Validation Rules

必需检查：

```text
1. 作业标识一致
2. 程序一致
3. 方法一致
4. 基组一致
5. 电荷一致
6. 多重度一致
7. 程序正常结束
8. SCF 已真正收敛
9. 能量存在
10. 能量有限
11. 能量单位为 Hartree
12. 输出文件存在
```

任一必需检查失败：

```text
不要报告为成功
不要继续生成科学结论
转交完整单点工作流
```
````

这直接对应当前 C# 验证器：

```text
SinglePointCalculationResultValidator
```

特别强调：

```text
NormalTermination = true
不等于
SCF 收敛
```

---

## 第八步：写错误码 reference

问：

```text
失败时，Codex 应该看到哪些稳定错误码？
```

文件：

```text
.agents/skills/single-point-calculation/references/error-codes.md
```

内容：

````markdown
# Single Point Error Codes

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

Gaussian 解析器当前识别：

```text
SCF Done
Convergence failure
SCF has not converged
No convergence in SCF
Error termination
Normal termination of Gaussian
```

遇到 ScfNotConverged：

```text
停止正常单点 Skill 路径
转交完整单点工作流
不要在本 Skill 内自动修改 Gaussian 关键词
```
````

程序专用错误码不要写进通用错误码文件。
如果错误码只适用于 Gaussian，放入 `gaussian.md`。

---

## 第九步：写 Gaussian 专用 reference

文件：

```text
.agents/skills/single-point-calculation/references/gaussian.md
```

内容：

````markdown
# Gaussian Single Point Notes

仅当 engine 为 Gaussian 16 时读取本文件。

输入文件：

```text
.gjf
```

主要输出：

```text
output.log
```

检查点：

```text
.chk
.fchk
```

关键输出标记：

```text
SCF Done
Normal termination of Gaussian 16
Error termination
```

SCF 收敛失败标记：

```text
Convergence failure
SCF has not converged
No convergence in SCF
```

不要在这里放自动修复策略。
SCF 修复属于独立工作流或独立 SCF 修复 Skill。
````

规则：

```text
程序专用知识只放本文件
通用 SKILL.md 不出现 Gaussian 关键词
```

---

## 第十步：写可复用脚本

只有当同一个操作会反复执行时，才创建脚本。

当前单点计算适合创建：

```text
.agents/skills/single-point-calculation/scripts/validate_single_point_result.ps1
```

职责：

```text
读取 result.json
检查 normalTermination
检查 scfConverged
检查 energy
检查 outputFilePath 是否存在
失败时返回非零退出码
```

脚本示意：

```powershell
param(
    [Parameter(Mandatory = $true)]
    [string]$ResultPath
)

$result = Get-Content -Raw $ResultPath | ConvertFrom-Json

if (-not $result.normalTermination) {
    Write-Error "计算没有正常结束。"
    exit 1
}

if (-not $result.scfConverged) {
    Write-Error "SCF 没有真正收敛。"
    exit 1
}

if ($null -eq $result.energy) {
    Write-Error "结果缺少能量。"
    exit 1
}

if (-not (Test-Path $result.outputFilePath)) {
    Write-Error "输出文件不存在。"
    exit 1
}

Write-Output "单点计算结果验证通过。"
exit 0
```

脚本必须实际运行验证，不能只写不测。

---

## 第十一步：写 agents/openai.yaml

文件：

```text
.agents/skills/single-point-calculation/agents/openai.yaml
```

内容：

```yaml
interface:
  display_name: "Single Point Calculation"
  short_description: "Run stable single-point calculations"
```

规则：

```text
short_description 必须为 25 到 64 个字符
options 只写官方允许的字段
默认允许自动发现
只有明确要求时才关闭自动调用
```

---

## 第十二步：初始化、校验、测试

### 12.1 初始化

如果目录不存在，可以先用官方脚本：

```powershell
python C:\Users\bnulk\.codex\skills\.system\skill-creator\scripts\init_skill.py `
  single-point-calculation `
  --path .agents\skills `
  --resources references,scripts
```

### 12.2 结构校验

```powershell
python C:\Users\bnulk\.codex\skills\.system\skill-creator\scripts\quick_validate.py `
  .agents\skills\single-point-calculation
```

必须看到：

```text
Skill is valid!
```

### 12.3 行为测试

用完整输入测试：

```text
结构完整
电荷明确
多重度明确
方法明确
基组明确
溶剂明确
引擎明确
```

期望行为：

```text
识别 single-point-calculation
读取 input-contract.md
调用 calculation.single-point
读取 result-contract.md
检查 ScfConverged
检查 Energy
检查 NormalTermination
```

异常测试：

```text
提供 SCF 不收敛结果
期望 Skill 停止正常路径
期望转交完整单点工作流
不要自动修复 SCF
```

---

## 以后新增 Skill 的固定规矩

每条都是硬性要求：

```text
1. 每个 Skill 一个独立目录。
2. 每个目录必须有 SKILL.md。
3. 目录名必须等于 frontmatter 的 name。
4. name 必须是小写连字符格式，长度小于 64。
5. description 必须说明能力和适用条件。
6. description 不得包含 TODO 或尖括号。
7. SKILL.md 只写必要规则。
8. 详细契约进入 references。
9. 重复确定性操作进入 scripts。
10. 输出模板放入 assets。
11. 不创建 README、安装指南、变更日志。
12. 不把完整 workflow 塞进一个 Skill。
13. 不把程序专用规则写进通用 Skill。
14. 不复制运行时 ISkill 的实现。
15. 新增或修改后必须运行 quick_validate.py。
16. 新增脚本必须实际运行验证。
17. Skill 变化必须同步项目文档。
```

---

## 你现在应该做什么

按照顺序做，不要跳步：

```text
1. 建 single-point-calculation 目录
2. 写 SKILL.md
3. 写 input-contract.md
4. 写 result-contract.md
5. 写 validation-rules.md
6. 写 error-codes.md
7. 写 gaussian.md
8. 写 validate_single_point_result.ps1
9. 写 agents/openai.yaml
10. 运行 quick_validate.py
11. 用正常单点请求测试
12. 用 SCF 不收敛请求测试
```

---

## 最终规矩

```text
Codex Skill 负责指导怎么做。
运行时 Skill 负责真正执行。
Workflow 负责完成整个任务。
三者只要混在一起，架构就会失控。
```
