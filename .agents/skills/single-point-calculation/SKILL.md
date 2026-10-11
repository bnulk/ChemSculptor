---
name: single-point-calculation
description: "使用 ChemSculptor 完成一次稳定的单点计算；当任务需要 SCF 修复、重算或异常恢复时，转交完整单点工作流。"
---

# Single Point Calculation

## 适用范围

用于输入完整、目标是完成一次稳定单点计算的场景。

首期只支持：

```text
engine = Gaussian 16
task-type = SinglePoint
```

不用于几何优化、频率计算、激发态计算或自动 SCF 修复。

## 必需输入

必须明确：

```text
结构
电荷
多重度
方法
基组
色散校正
对称性
SCF 迭代上限
溶剂
引擎
```

缺少必需值时先提问，不要猜测。

详细参数和别名见：

- 参数目录：references/parameters.md
- Gaussian 映射：references/gaussian.md
- 校验规则：references/validation-rules.md

## 执行

优先调用 ChemSculptor 已有的单点计算入口，不重新实现输入生成、程序调用或
输出解析。

执行链路：

```text
用户文本
  -> 参数提取
  -> 参数 Resolver
  -> ResolvedSpec
  -> Gaussian route keyword translation
  -> Gaussian 输入文件
```

程序关键词必须经过规范参数和 Gaussian 翻译器，不允许把未知原始关键词直接
写入输入文件。

## 结果检查

基本检查：

```text
任务正常结束
SCF 已真正收敛
能量存在且有限
输出文件存在
结果与输入参数一致
```

详细规则见 references/validation-rules.md。

## 停止条件

遇到以下情况时停止正常单点路径：

```text
SCF 未收敛
输出缺少正常终结
缺少能量
参数冲突
参数缺失
当前阶段不支持的参数
```

停止后转交完整单点工作流或要求用户补充参数，不在本 Skill 内无限重试。
