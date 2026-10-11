# 当前交接

## 当前阶段

单点计算 Skill 与完整工作流已完成分层。

```text
calculation.single-point 负责正常单点计算路径
原子 Skill 负责输入准备、提交、等待、提取和验证
完整工作流负责稳定性检查、修正、恢复和科学数据记录
```

## 当前边界

```text
单点计算 Skill 不自行修复 SCF
SCF 修复仍由完整工作流和异常处理 Skill 负责
恢复分支没有派生作业时安全跳过复检
SCF 收敛状态和迭代次数已经进入通用计算结果
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：96/96 通过
```

## 尚未完成

```text
尚未实现 SCF 不收敛修正 Skill
尚未给 WorkflowNode 增加条件路由
恢复分支仍采用执行后跳过的方式
```

## 下一步精确任务

下一阶段由用户指定。建议：

```text
实现 SCF 不收敛诊断和修正方案
给 WorkflowNode 增加条件路由
增加单点计算 Skill 的端到端集成测试
```

## 注意事项

```text
科学点是核心
作业只是计算过程
文本用于描述点的来源和组织方式
文本不得加入未进入 ScientificResult 的计算结论
conversation.txt 是客户端会话记录
原始点、被取代点和失败点都应保留
```

## 强制文档规则

每次修改代码后，必须同时更新：

```text
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

任何一项未更新，本阶段都不得标记为完成。
