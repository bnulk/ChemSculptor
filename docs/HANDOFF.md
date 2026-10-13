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
优化参数已翻译为单个 Gaussian opt 关键词
优化重启已映射为 geom=check 和 guess=read
优化输出中的轨迹和收敛指标已解析
优化输出已翻译为 OptimizationResult
calculation.optimization 已实现
calculation.validate-optimization 已实现
```

## 当前边界

```text
首期只支持 Gaussian 16
首期只支持已冻结的规范参数键
ORCA、原始关键词扩展和审批机制暂不实现
优化任务尚未接入 Agent、API 和完整工作流
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：154/154 通过
```

## 尚未完成

```text
尚未实现完整的优化验证规则扩展
尚未实现优化异常、恢复和重算工作流
尚未实现 OptimizationCalculationService 与 Agent 接入
```

## 下一步精确任务

下一步严格执行优化第 6 步：

```text
把当前 validate-optimization 的必要检查扩展为完整验证规则
明确必需、推荐和信息性检查
覆盖正常收敛、未收敛、SCF 失败、输出缺失和几何缺失
生成稳定、可复现的验证报告
```

验证层不得把异常修正逻辑混入正常优化 Skill；
修正和重算继续留给第 7、8 步。

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
