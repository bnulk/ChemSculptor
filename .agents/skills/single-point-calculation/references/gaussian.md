# Gaussian Route Keyword Mapping

仅当 engine = Gaussian 16 时使用本文件。

## Mapping

```text
method + basis
  -> CAM-B3LYP/6-31G*

task-type = SinglePoint
  -> SP

dispersion-correction = None
  -> 不输出

dispersion-correction = GD3
  -> em=GD3

dispersion-correction = GD3BJ
  -> em=GD3BJ

symmetry = Default
  -> 不输出

symmetry = Use
  -> 不输出额外关键词

symmetry = Ignore
  -> nosymm

scf-iteration-limit = 300
  -> scfcyc=300

solvent = None
  -> 不输出

solvent = Water
  -> SCRF=(Solvent=Water)
```

## 输出示例

```text
#p CAM-B3LYP/6-31G* SP em=GD3BJ nosymm scfcyc=300 SCRF=(Solvent=Water)
```

## 约束

```text
关键词顺序不作为语义契约
用户原始文本不直接进入输入文件
未登记的 raw keyword 必须拒绝
charge 和 multiplicity 写入独立行，不进入 route line
```
