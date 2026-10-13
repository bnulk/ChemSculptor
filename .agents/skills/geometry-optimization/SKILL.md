---
name: geometry-optimization
description: "使用 ChemSculptor 完成一次稳定的几何优化；异常时转交 optimization-complete 工作流进行诊断、修正和恢复。"
---

# Geometry Optimization

## 适用范围

用于输入完整、目标是完成一次几何优化的场景。

首期只支持：

```text
engine = Gaussian 16
task-type = Optimization
```

不用于频率计算、激发态计算或 MECP 搜索。

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
优化最大循环次数
优化收敛标准
溶剂
引擎
```

支持对话输入：

```text
优化水分子使用 opt，maxcycles=200
```

也支持受控的 Gaussian route 行：

```text
#p cam-b3lyp/6-31G* opt em=GD3BJ nosymm
```

`#p/#n/#t/#` 只控制输出级别。`opt` 表示几何优化；没有显式任务关键词时
默认是单点计算。

## 执行

优化只提交一个作业，不允许在内部拆成多个单点作业。正常路径：

```text
calculation.optimization
  -> optimization validation
  -> optimization anomaly check
  -> processing plan
  -> scientific data record
```

异常路径：

```text
diagnosis
  -> SCF repair branch / optimization repair branch
  -> recovery job
  -> recovery execution
  -> recovery validation
```

## 结果检查

必须显示或保留：

```text
是否收敛
优化步数
最终能量
最终结构
最大力 / RMS 力
最大位移 / RMS 位移
修正次数
最终采用的作业
波函数稳定性状态
```

## 波函数稳定性

优化正常收敛后默认执行波函数稳定性检查。Gaussian 使用：

```text
guess=read geom=check stable
```

如果客户端明确输入：

```text
不做波函数稳定性检查
跳过波函数稳定性检查
```

则规范参数为：

```text
wavefunction-stability-check = Skip
```

工作流可以跳过检查，但必须记录用户来源和跳过原因，不得显示为“稳定”。

如果稳定性检查发现不稳定：

```text
同自旋多重度 -> stable=opt 波函数修正 -> 重新优化
不同自旋多重度 -> 修改自旋多重度 -> 重新优化
```

修正必须创建派生优化作业，复检必须针对最终采用的优化作业。

## 振动分析

波函数稳定性检查通过或用户明确跳过后，工作流对最终优化结构执行一次
Gaussian `Freq` 分析。输入保留方法、基组、色散、对称性等参数，并把
优化任务关键词替换为 `Freq`，使用：

```text
guess=read geom=check
```

必须保留：

```text
振动频率数量
虚频数量
最低频率
是否为稳定最小点
零点校正和热校正
```

## 停止条件

```text
程序硬失败
输出缺失
正常结束标志缺失
几何无效
恢复预算耗尽
```

硬失败必须中止并报告，不得伪造成功结果。
