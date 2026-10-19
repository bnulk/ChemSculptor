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
优化步骤验证器已实现
优化总体结果验证器已实现
优化收敛验证器已实现
收敛指标已保存每一项的数值、阈值和达标状态
单点异常代码已集中定义
优化独有异常代码已集中定义
OptimizationConvergenceCheck 已实现
异常发现已包含证据和摘要
优化异常诊断 Skill 已实现
优化修正规划 Skill 已实现
优化派生作业创建 Skill 已实现
优化派生作业执行 Skill 已实现
优化恢复验证 Skill 已实现
Gaussian 优化恢复输入可以重建 route
OptimizationWorkflowDefinitionFactory 已实现
工作流节点支持条件跳过
正常、SCF 异常、优化未收敛三条路径已验证
程序硬失败会中止并报告
优化文本和 Gaussian opt route 已接入任务解释
OptimizationCalculationService 已接入 Agent
Api 已返回优化收敛、能量、结构、力和位移
WinForms 已显示优化摘要
geometry-optimization Codex Skill 已建立
优化解析、验证、异常、route、Skill、工作流、Agent 和端到端测试已建立
优化主链已有 174 项自动化测试覆盖
波函数稳定性检查已接入优化工作流
稳定性跳过规则已支持否定词
稳定性不稳定分支已支持修正和复检
Gaussian 稳定性输入使用 guess=read geom=check stable
优化后振动分析 Skill 已实现
Gaussian Freq 输入和输出解析已实现
振动分析已接入优化工作流
客户端已返回频率和虚频摘要
docs/skill说明 已建立
任务级 Skill 11 步说明规则已写入编码约定
单点、优化、频率分析和稳定性检查说明已建立
TDDFT 单点计算 Skill 已实现
TDDFT 激发态和跃迁速度四极矩解析已实现
tddft-single-point Codex Skill 和 11 步说明已建立
冻结非氢原子优化约束已实现
HydrogensOnly 已接入自然语言提取、Resolver 和优化 Workflow
Gaussian 已生成 opt=(ModRedundant, ...) 和非氢原子 -1 标记
约束优化自动加入 nosymm，禁止程序调整坐标体系
冻结原子位移验证已接入优化结果验证器
优化恢复输入会保留冻结标记
TDDFT 单点计算 Workflow 已实现
吸收光谱 Workflow 已实现
吸收光谱支持默认优化、直接 TDDFT 和冻结非氢原子三种模式
吸收光谱模式已接入 Agent 路由和客户端反馈
吸收光谱科学数据归档已实现
吸收光谱恢复扩展框架已实现
真实 Gaussian 16 三种吸收光谱模式已验证
联合优化频率 Skill 已实现
联合优化频率 Workflow 已实现
联合优化频率后稳定性检查已接入
联合优化频率稳定性矫正和复检已接入
真实 Gaussian opt freq 作业已验证
NTO 单状态 Skill 已实现
NTO Workflow 和状态筛选已实现
Gaussian NTO 输入和输出适配已实现
```

## 当前边界

```text
首期只支持 Gaussian 16
首期只支持已冻结的规范参数键
ORCA、原始关键词扩展和审批机制暂不实现
优化任务已接入 Agent、API、WinForms 和完整工作流
只优化氢原子当前只支持明确写出“冻结非氢原子”等约束表达
吸收光谱专用异常恢复策略尚未实现，仅保留扩展框架
联合优化频率已包含波函数稳定性矫正，虚频修正仍未实现
NTO 当前只支持 Gaussian TDDFT 源结果
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：230/230 通过
真实 Gaussian 16：3/3 通过
真实 Gaussian opt freq：1/1 通过
```

## 尚未完成

```text
尚未使用真实 Gaussian 验证稳定性检查输出
尚未验证真实不稳定波函数的 stable=opt 恢复
尚未使用真实 Gaussian 验证 Freq 热化学输出
尚未实现吸收光谱专用异常恢复策略
尚未验证真实吸收光谱科学摘要和成果包客户端闭环
尚未实现联合优化频率虚频修正工作流
尚未执行真实 Gaussian NTO 端到端验收
尚未结构化解析 NTO 轨道占据数、轨道能量和图形文件
尚未使用真实 Gaussian 验证冻结非氢原子的优化输入和位移检查
尚未完成优化成果包下载和客户端科学摘要闭环
```

## 下一步精确任务

优化主链和稳定性扩展已经完成。下一阶段建议：

```text
使用真实 Gaussian 16 执行优化和稳定性端到端验收
使用真实含氢体系验证 HydrogensOnly 输入、优化结果和冻结位移检查
验证 WinForms 实际提交、显示和保存成果包
验证真实不稳定波函数的修正、重新优化和复检
验证真实振动分析频率、虚频和热化学校正
验证真实 TDDFT 激发态和跃迁矩输出
补齐 ORCA 优化适配
```

后续工作开始前先确认优先级，不自动扩展任务范围。

## 任务级 Skill 说明规则

每个新增任务级 Skill 必须同步新增：

```text
docs/skill说明/<skill-id>.md
```

该文件必须逐项列出 11 步的目标、代码落点、关联文件和验收证据。

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
