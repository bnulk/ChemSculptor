---
name: absorption-spectrum
description: "使用 ChemSculptor 完成吸收光谱工作流；根据客户端表达选择优化后 TDDFT、直接 TDDFT 或冻结非氢原子后 TDDFT。"
---

# Absorption Spectrum

## 适用范围

用于客户端请求：

```text
吸收光谱
紫外
紫外可见
UV-Vis
```

工作流最终通过 TDDFT 单点计算产生激发态、波长和振子强度摘要。

## 三类模式

默认模式：

```text
优化 workflow
-> TDDFT 单点计算 workflow
-> 客户端反馈
```

直接模式：

```text
吸收光谱，不优化
-> TDDFT 单点计算 workflow
-> 客户端反馈
```

约束模式：

```text
吸收光谱，冻结非氢原子
-> 约束优化 workflow
-> TDDFT 单点计算 workflow
-> 客户端反馈
```

“不优化”和“冻结非氢原子”同时出现时拒绝执行。

## 复用边界

```text
优化阶段复用 optimization-complete
TDDFT 阶段复用 tddft-single-point 工作流
吸收光谱工作流只负责模式判定、阶段衔接和客户端反馈组装
```

优化完成后，TDDFT 使用优化得到的最终几何结构。

## 客户端反馈

至少返回：

```text
吸收光谱模式
优化作业标识
TDDFT 作业标识
激发态数量
最强吸收波长
最强吸收振子强度
吸收光谱科学成果标识
```

科学数据归档由 `AbsorptionScientificDataRecordSkill` 完成。

吸收光谱专用恢复当前只提供扩展框架
`IAbsorptionSpectrumRecoveryFramework`，具体恢复策略由后续实际情况补充。
