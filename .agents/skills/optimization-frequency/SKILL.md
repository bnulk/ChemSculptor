---
name: optimization-frequency
description: "使用 ChemSculptor 在同一个 Gaussian 作业中完成几何优化和振动分析，输入关键词为 opt freq。"
---

# Optimization Frequency

## 适用范围

用于一次提交中同时完成几何优化和振动分析：

```text
优化并做振动分析
优化后做振动分析
#p CAM-B3LYP/6-31G* opt freq
```

路由规则：

```text
优化           -> optimization
opt            -> optimization
优化 振动分析  -> optimization-frequency
优化后振动分析  -> optimization-frequency
opt freq       -> optimization-frequency
```

## 核心边界

```text
一个作业
一个 route
同一个输出文件
同时返回 OptimizationResult 和 FrequencyAnalysisResult
完成后执行一次波函数稳定性检查
```

Gaussian route 示例：

```text
#p CAM-B3LYP/6-31G* opt=(MaxCycles=100) Freq em=GD3BJ
```

## 不做什么

```text
不在 Skill 内部拆成两个作业
不在 Skill 内部自动修复虚频
不在 Skill 内部增加优化恢复策略
```

优化异常、SCF 修复和后续派生作业由外层 Workflow 决定。
波函数稳定性检查作为联合 Skill 完成后的外置 Workflow 节点执行。

## 结果

必须保留：

```text
优化是否收敛
优化轨迹
最终能量和结构
频率数量
虚频数量
最低频率
是否为稳定最小点
零点校正和热化学校正
```

虚频是科学提示，不自动等同于作业执行失败；联合验证默认将“无虚频”
作为建议项，而不是必需项。
