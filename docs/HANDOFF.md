# 当前交接

## 当前阶段

对话式计算参数方案已落地前十三步。

```text
calculation.single-point 保持现有分层
规范参数键已经集中定义
参数值模型和规范参数目录已经建立
规则版参数提取器已经实现
参数 Resolver 已经实现
参数问询闭环已经实现
AgentService 已接入参数问询
SinglePointCalculationService 已接入 ResolvedSpec
Gaussian route 关键词翻译已实现
参数审计已写入 parameters.json
参数问题和已应用参数已显示到客户端对话区
第十二步测试矩阵已补齐
项目级 Codex Skill 已同步
Gaussian route 行输入已支持
Skill / Workflow 11 步强制模板已写入编码约定
Route 输出信息量已建模
默认 GD3BJ 色散已启用
优化任务契约已冻结
优化参数键、目录和默认值已建立
优化参数提取与解析已实现
优化参数已按任务类型隔离
```

## 当前边界

```text
首期只支持 Gaussian 16
首期只支持已冻结的规范参数键
ORCA、原始关键词扩展和审批机制暂不实现
优化任务尚未实现程序翻译、Skill 和执行服务
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：147/147 通过
```

## 尚未完成

```text
尚未实现优化参数到 Gaussian 关键词的翻译
尚未实现优化 Skill
尚未实现优化工作流和优化计算服务
```

## 下一步精确任务

下一步严格执行优化第 3 步：

```text
TaskType.Optimization          -> opt
optimization-max-cycles = 100  -> opt=maxcycles=100
optimization-convergence       -> opt=Tight / opt=VeryTight
optimization-restart = true    -> opt=restart + geom=check guess=read
optimization-initial-hessian   -> opt=CalcFC / opt=ReadFC
```

翻译后必须增加确定性顺序测试，并保持通用层不拼接程序输入。

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
