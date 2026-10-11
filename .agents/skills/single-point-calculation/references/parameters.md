# Frozen Parameter Contract

## 规范参数

```text
engine
method
basis
dispersion-correction
symmetry
scf-iteration-limit
solvent
charge
multiplicity
```

## Defaults

```text
engine = Gaussian 16
method = CAM-B3LYP
basis = 6-31G*
dispersion-correction = None
symmetry = Default
scf-iteration-limit = 200
solvent = None
charge = 0
multiplicity = 系统根据总电子数推断
```

## 色散校正

```text
None
  关闭色散
  不加色散
  不考虑色散
  no dispersion

GD3BJ
  em=GD3BJ
  GD3BJ
  gd3bj
  D3BJ
  d3bj
  色散
  色散校正
  加色散
  经验色散

GD3
  em=GD3
  GD3
  gd3
  D3
  d3
```

规则：

```text
没有提到色散 -> None
提到色散但未指定类型 -> RequestDefault = GD3BJ
明确输入 GD3 -> GD3
明确输入 GD3BJ -> GD3BJ
```

## 对称性

```text
Default
  默认对称性
  程序默认对称性

Use
  symm
  symmetry
  使用对称性
  保持对称性
  开启对称性

Ignore
  nosymm
  no symmetry
  不使用对称性
  关闭对称性
  不要对称化
```

单独的“对称性”视为 KeyOnly，需要继续提问。

## SCF 迭代上限

```text
scfcyc=300
SCF 最多 300
SCF 最大 300
SCF 迭代上限 300
最多迭代 300 次
SCF cycles = 300
```

允许范围：

```text
1 到 1000
501 到 1000 给出范围警告
```

## 溶剂

```text
None
  气相
  无溶剂
  不加溶剂
  gas
  no solvent

Water
  水
  水中
  水溶剂
  water
  aqueous
  SCRF=(Solvent=Water)
```

`H2O` 首期不作为水溶剂别名。

## Charge / Multiplicity

```text
charge
  默认 0
  显式值进入 ResolvedParameters
  首期不触发审批

multiplicity
  如果没有显式值，根据电子数推断
  偶数电子 -> 1
  奇数电子 -> 2
```

如果显式 multiplicity 与总电子数奇偶不一致：

```text
优先要求用户补充或修改 charge
不猜测
不提交计算
```
