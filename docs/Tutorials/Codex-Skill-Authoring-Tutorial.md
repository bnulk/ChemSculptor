# Codex Skill 编写教程

> 官方依据：[Codex skills](https://developers.openai.com/codex/skills/)
> 结构校验依据：Codex 内置 `skill-creator` 的 `SKILL.md`、`init_skill.py`
> 和 `quick_validate.py`
> 本教程目的：为 ChemSculptor 后续代码和 Skill 组织定下强制规矩

---

## 0. 先记住一句话

```text
Codex Skill 是给智能体的可复用工作说明。
运行时 ISkill 是系统执行能力。
Workflow 是完成任务的过程编排。
```

这三者不能混为一谈。

---

## 1. 三种 Skill 必须分开

### 1.1 Codex Skill

官方结构：

```text
skill-name/
|-- SKILL.md                 必需
|-- agents/
|   `-- openai.yaml          可选
|-- scripts/                 可选
|-- references/              可选
`-- assets/                  可选
```

用途：

```text
告诉 Codex 什么时候使用这个能力
告诉 Codex 输入应该是什么
告诉 Codex 应该调用哪些工具或脚本
告诉 Codex 结果如何验证
告诉 Codex 什么时候停止或转交其他工作流
```

它不是一个 C# 服务，也不是一个运行中的工作流。

### 1.2 ChemSculptor 运行时 ISkill

位置：

```text
src/ChemSculptor.Skills.Common
src/ChemSculptor.Skills.Gaussian
src/ChemSculptor.Skills.Orca
```

特征：

```text
实现 ISkill
有 Name、Version、Capabilities
由 WorkflowEngine 按节点调用
输入和输出通过 TaskRequest、TaskResult 或 JsonSkill 传递
```

示例：

```text
calculation.prepare-input
calculation.submit
calculation.wait
calculation.extract-result
calculation.workflow-validation
```

### 1.3 完整工作流

位置示例：

```text
src/ChemSculptor.Agent/SinglePointWorkflowDefinitionFactory.cs
```

它负责：

```text
节点顺序
异常恢复
重试策略
人工审批
科学数据记录
```

它不负责：

```text
解析 Gaussian 文本
拼接程序关键词
实现程序启动细节
```

---

## 2. 当前单点计算案例的正确分层

### 2.1 Codex Skill

建议名称：

```text
single-point-calculation
```

建议位置：

```text
.agents/skills/single-point-calculation/
```

官方默认可发现的用户级位置是：

```text
$CODEX_HOME/skills/<skill-name>/
```

项目级 Skill 按本仓库约定放在：

```text
.agents/skills/<skill-name>/
```

它回答：

```text
如何完成一次稳定的单点计算
需要哪些输入
应该调用哪个运行能力
如何检查 SCF 是否收敛
什么时候转交完整工作流
```

### 2.2 运行时复合 Skill

```text
calculation.single-point
```

它回答：

```text
如何在系统内部完成一次正常单点计算
```

它内部调用：

```text
calculation.prepare-input
  -> calculation.submit
  -> calculation.wait
  -> calculation.extract-result
  -> calculation.workflow-validation
```

### 2.3 完整工作流

```text
single-point-complete
  -> calculation.single-point
  -> 稳定性检查
  -> 修正规划
  -> 派生恢复作业
  -> 恢复复检
  -> 处理方案
  -> 科学数据记录
```

它回答：

```text
如何围绕用户目标完成单点计算、错误处理和结果归档
```

---

## 3. Skill 命名规则

强制规则：

```text
使用小写字母、数字和连字符
不能以连字符开头或结尾
不能包含连续连字符
长度不能超过 64 个字符
目录名必须与 name 一致
```

正确：

```text
single-point-calculation
scf-convergence-repair
gaussian-result-extraction
```

错误：

```text
SinglePointCalculation
single_point_calculation
single--point
-single-point-
```

命名要表达能力，不要表达流程阶段：

```text
好：single-point-calculation
差：step-one-run-gaussian
```

---

## 4. SKILL.md 规则

### 4.1 Frontmatter

必须包含：

```yaml
---
name: single-point-calculation
description: "使用 ChemSculptor 完成一次稳定的单点计算；当任务需要 SCF 修复、重算或异常恢复时，转交完整单点工作流。"
---
```

允许的字段：

```text
name
description
license
allowed-tools
metadata
```

项目默认只使用：

```text
name
description
metadata
```

规则：

```text
description 必须说明“做什么”和“什么时候适用”
description 不写长篇流程
description 不超过 1024 个字符
description 不能包含尖括号
description 不能保留 TODO 占位
```

### 4.2 SKILL.md 正文

正文只保留使用这个 Skill 时必须知道的规则：

```text
适用范围
输入契约摘要
执行步骤
结果契约摘要
基本验证
停止条件和转交流程
参考文件入口
脚本入口
```

正文不放大段程序文档，不复制完整 API 手册，不写通用软件教程。

推荐结构：

````markdown
# Single Point Calculation

## 适用范围
...

## 输入契约
...

## 执行步骤
...

## 结果契约
...

## 基本验证
...

## 停止条件
...

## 参考资料
- 输入契约：references/input-contract.md
- 结果契约：references/result-contract.md
- 错误码：references/error-codes.md
````

---

## 5. 渐进披露规则

官方 Skill 分三层加载：

```text
第一层：name + description
  用于发现 Skill

第二层：SKILL.md 正文
  决定使用 Skill 后加载

第三层：references、scripts、assets
  只在当前任务需要时加载
```

项目强制规则：

```text
SKILL.md 必须短小、明确、可路由
详细输入输出模型放 references
重复的确定性操作放 scripts
模板、样例文件放 assets
不要默认加载所有 reference
不要复制 reference 内容和 SKILL.md
```

---

## 6. 资源目录规则

### 6.1 scripts

只在以下情况创建：

```text
同一个机械操作会被反复执行
脚本能提高可靠性
脚本能避免每次重写相同代码
```

单点计算 Skill 可放：

```text
scripts/validate_single_point_result.ps1
```

职责：

```text
读取结果 JSON
检查 ScfConverged
检查 Energy
检查 NormalTermination
检查输出文件存在
失败时返回非零退出码
```

脚本必须实际运行验证，不能在未测试的情况下交付。

### 6.2 references

单点计算 Skill 建议：

```text
references/input-contract.md
references/result-contract.md
references/validation-rules.md
references/error-codes.md
references/gaussian.md
```

职责：

```text
input-contract.md：结构、电荷、多重度、方法、基组、溶剂、引擎
result-contract.md：能量、SCF 收敛、迭代次数、输出文件、错误状态
validation-rules.md：正常结束、能量存在、SCF 真正收敛
error-codes.md：通用失败类别和错误码
gaussian.md：仅在 Gaussian 任务中使用
```

程序专用知识不能写进通用输入契约。

### 6.3 assets

只放最终输出需要复制的文件：

```text
模板
图片
字体
图标
起步项目
```

不要把说明文档伪装成 asset。

### 6.4 agents/openai.yaml

可选，用于 UI 展示和调用策略。

默认允许自动发现：

```yaml
policy:
  allow_implicit_invocation: true
```

只有在用户明确要求时，才关闭自动调用：

```yaml
policy:
  allow_implicit_invocation: false
```

---

## 7. 当前单点计算 Skill 的推荐文件树

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

注意：

```text
这不是 C# 运行时 Skill 的目录
它不替代 ChemSculptor.Skills.Common
它只告诉 Codex 如何正确使用运行时能力
```

---

## 8. 单点计算 SKILL.md 示例

````markdown
---
name: single-point-calculation
description: "使用 ChemSculptor 完成一次稳定的单点计算；当任务需要 SCF 修复、重算或异常恢复时，转交完整单点工作流。"
---

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

优先调用 ChemSculptor 已有的单点计算入口，不重新实现输入生成、
程序调用或输出解析。

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

---

## 9. 必须执行的校验

### 9.1 初始化

```powershell
python C:\Users\bnulk\.codex\skills\.system\skill-creator\scripts\init_skill.py `
  single-point-calculation `
  --path .agents\skills `
  --resources references,scripts
```

### 9.2 结构校验

```powershell
python C:\Users\bnulk\.codex\skills\.system\skill-creator\scripts\quick_validate.py `
  .agents\skills\single-point-calculation
```

必须看到：

```text
Skill is valid!
```

### 9.3 行为验证

使用真实请求测试：

```text
给一个完整单点输入
验证 Skill 能识别输入契约
验证 Skill 能调用正确运行入口
验证 Skill 会检查 SCF 收敛
验证 SCF 不收敛时转交工作流
验证没有生成未经验证的科学结论
```

---

## 10. ChemSculptor 强制规矩

以后新增 Codex Skill，必须满足：

```text
1. 每个 Skill 一个目录。
2. 每个目录必须有 SKILL.md。
3. name 必须是小写连字符格式，且少于 64 个字符。
4. description 必须说明能力和适用条件。
5. SKILL.md 只写必要规则。
6. 细节进入 references。
7. 重复确定性操作进入 scripts。
8. 最终输出模板进入 assets。
9. 不创建 README、安装指南、变更日志等附属文档。
10. 不把完整工作流塞进一个 Skill。
11. 不把程序专用细节放进通用 Skill。
12. 不复制 ChemSculptor 运行时 ISkill 的实现。
13. 新增或修改 Skill 后必须运行 quick_validate.py。
14. 新增脚本必须实际运行验证。
15. Skill 变化必须同步项目文档。
```

---

## 11. 最常见的错误

错误一：把 Codex Skill 当成运行时 Skill。

```text
错误思路：
创建 SKILL.md 就等于系统有 calculation.single-point 能力

正确思路：
SKILL.md 负责指导调用
C# ISkill 负责执行能力
Workflow 负责组合过程
```

错误二：把完整工作流塞进一个 Skill。

```text
错误：
single-point-calculation
  包含诊断、修复、重算、归档

正确：
single-point-calculation 只做一次稳定单点计算
完整工作流负责异常恢复和重算
```

错误三：所有细节都堆在 SKILL.md。

```text
错误：
把全部 API 手册、错误码和 Gaussian 样例都写进正文

正确：
正文只保留路由和规则
详细内容进入 references
```

错误四：程序专用规则污染通用 Skill。

```text
错误：
通用输入契约写 %chk、GAUSS_EXEDIR、SCF=QC

正确：
通用契约保持程序无关
Gaussian 细节进入 references/gaussian.md
```

错误五：没有校验就认为 Skill 完成。

```text
错误：
文件创建完成就结束

正确：
quick_validate.py 通过
脚本实际运行通过
真实任务行为验证通过
```

---

## 12. 一句话规矩

```text
Codex Skill 负责告诉智能体怎么做，
运行时 Skill 负责具体执行，
Workflow 负责完成整个任务。
```
