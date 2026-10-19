---
name: nto
description: "从已有 TDDFT 激发态结果选择状态，并基于检查点逐个计算自然过渡轨道（NTO）。"
---

# Natural Transition Orbitals

## 适用范围

用于已有正常结束的 TDDFT 结果，并已完成激发态解析的场景。

当前只支持 Gaussian 16。

## Workflow

```text
源 TDDFT 作业和结果
-> 激发态选择
-> 每个选中状态创建一个 NTO 派生作业
-> 汇总 NTO 结果
```

## 选择规则

默认：

```text
只统计 f > 0 的激发态
按 f 降序取前 5 个
若 max(f) / min(正 f) > 10
则只保留 f >= max(f) / 10 的状态
```

指定波长范围：

```text
先筛选波长范围，再执行默认选择规则
```

全部 NTO：

```text
包含 f = 0 的暗态
不应用前 5 和振子强度阈值
```

默认或波长范围内没有 `f > 0` 状态时：

```text
不提交 NTO 作业
返回正常空结果
不生成异常记录
```

## 单个 NTO 作业

```text
源 TDDFT 输入
+ 源检查点
+ 激发态编号 n
-> Gaussian NTO 作业
```

核心关键词：

```text
Geom=AllCheck
Guess=(Read,Only)
Density=(Check,Transition=n)
Pop=(Minimal,NTO,SaveNTO)
nosymm
```

## 边界

```text
NtoTransitionCalculationSkill 只负责一个激发态
Nto Workflow 负责筛选和多个 NTO 作业的编排
NTO 作业不覆盖源 TDDFT 作业
```

CASSCF 源结果解析暂不实现，但源状态契约保留 `StateId` 扩展空间。
