---
name: tddft-single-point
description: "使用 ChemSculptor 完成一次 TDDFT 单点计算；当前阶段只负责正常执行和结果解析，不包含完整 TDDFT 异常恢复工作流。"
---

# TDDFT Single Point

## 适用范围

用于完成一次 TDDFT 单点计算。

首期默认：

```text
engine = Gaussian 16
task-type = TdDft
method = CAM-B3LYP
basis = 6-31G*
dispersion = GD3BJ
scf-iteration-limit = 200
tddft-state-count = 50
symmetry = Ignore
```

默认 route：

```text
#p cam-b3lyp/6-31G* em=GD3BJ scfcyc=200 td(nstates=50) nosymm
```

客户端可以输入受支持的 Gaussian route 和关键词，但服务器必须先转换为
规范参数，再重新翻译为程序关键词。

## 结果

必须解析：

```text
正常结束
基态能量
Excited State 状态
激发能、波长、振子强度和 <S**2>
轨道跃迁贡献
Ground to excited state transition velocity quadrupole moments
```

## 边界

当前提供任务级 Skill 和单点计算 Workflow。

异常恢复、重算和吸收光谱归档由后续工作负责。
