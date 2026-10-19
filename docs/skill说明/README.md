# 任务级 Skill 说明规则

本目录保存任务级 Skill 和完整 Workflow 的 11 步建设说明。

强制模板：

```text
契约
参数
翻译
结果
执行
验证
异常
恢复
工作流
接入
测试
```

每个说明文件必须列出：

```text
Skill ID
职责边界
每一步目标
代码落点
关联文件
验收证据
未完成项
```

命名规则：

```text
<skill-id>.md
```

例如：

```text
single-point-calculation.md
geometry-optimization.md
frequency-analysis.md
wavefunction-stability-check.md
tddft-single-point.md
absorption-spectrum.md
optimization-frequency.md
nto.md
```

任务级 Skill 只负责一次正常任务执行。异常、恢复、派生作业和归档
必须由外层 Workflow 负责。
