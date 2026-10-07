# ChemSculptor 项目状态

## 当前版本

```text
0.50.0
```

## 文档维护原则

每次修改代码后，必须同步更新：

```text
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

三项文档缺一不可，否则本次代码修改不视为完成。

职责：

```text
PROJECT_STATE.md
  更新当前架构、版本、边界和下一阶段

CHANGES.md
  记录改动目的、改动内容、验证结果和教程式说明

HANDOFF.md
  更新当前任务、修改文件、测试结果、遗留问题和下一步精确任务
```

最近一次代码验证：

```text
Release 构建：0 警告，0 错误
测试：90/90 通过
```

## 总体架构

```text
ChemSculptor.WinForms
  ↓ HTTP
ChemSculptor.Api
  ↓
ChemSculptor.Agent
  ↓
ChemSculptor.Conversation
  ↓
ChemSculptor.Core
  ↓
ChemSculptor.Skills.Common
  ↓
ChemSculptor.Compute
  ↓
ChemSculptor.Compute.Local
ChemSculptor.Compute.Gaussian
```

异常处理和科学数据：

```text
Compute + Anomaly
  ↓
ChemSculptor.ScientificData.Extraction
  ↓
ChemSculptor.ScientificData
  ↓
ChemSculptor.ScientificSummary
  ↓
ChemSculptor.Api
  ↓
ChemSculptor.WinForms
```

依赖原则：

```text
客户端不引用服务端项目
ScientificSummary 只引用 ScientificData
Api 引用 ScientificSummary
通用工作流不包含具体计算程序规则
报告和客户端科学摘要只从 ScientificData 读取
```

## 已完成模块

```text
工作流内核
计算模型与接口
本机计算后端
Gaussian 输入生成
Gaussian 输出解析
单点计算 API
WinForms 客户端
波函数稳定性检查
自旋多重度矫正
派生作业执行
派生作业稳定性复检
科学计算点数据结构
科学点程序专门数据
科学点原始文件引用
科学点规范文件主体和扩展名
科学点文件解析服务
科学点成果包 API
WinForms 成果包保存
科学数据叙述生成
科学数据叙述 API
科学数据文件仓储
计算结果到科学数据的提取
客户端科学摘要
客户端摘要 API
WinForms 领取和显示摘要
```

## 核心数据流

```text
客户端坐标和自然语言
  → 单点工作流
  → 计算与验证
  → 稳定性检查和矫正
  → ScientificResult
  → ScientificData 仓储
  → ClientScientificSummary
  → Api
  → WinForms
```

## 当前边界

```text
当前只实现单点计算
当前只实现 Gaussian 16 本机执行主链
远程 HPC 队列和调度尚未实现
几何优化、频率、TD-DFT 等任务尚未实现
科学点文件清单已进入 CalculationPoint
WinForms 已按清单保存完整成果包
关系、物理量和科学叙述已可追溯
成果包 checksums、压缩和断点续传尚未实现
客户端摘要当前使用 Api 内存邮箱
```

## 下一阶段

阶段 0 已完成：科学点文件成果包规范已冻结。

阶段 1 已完成：科学点模型已经能够保存原始文件引用，
并支持规范文件主体、规范扩展名以及旧科学数据读取。

阶段 2 已完成：原始点和恢复点各自从对应计算作业的
`CalculationResult.Artifacts` 获得完整文件引用清单。

阶段 3 已完成：科学点文件解析服务能够把文件引用解析为
服务器真实文件，并执行路径安全、存在性和 SHA-256 校验。

阶段 4 已完成：客户端可以通过成果包清单读取原始点和恢复点
的全部文件信息，并按结果、科学点和文件标识下载单个文件。

阶段 5 已完成：WinForms 按成果包清单下载全部点文件，
并按科学点目录组织成果包。

阶段 6 已完成：科学文本只从 ScientificResult 生成，
点摘要、来源、组织方式和最终摘要均引用 PointId，
关系和物理量可追溯到点。

阶段 7 已完成：真实氧气任务通过单点计算、稳定性检查、三重态矫正、
科学数据仓储和成果包保存等价流程。

规范文档：

```text
docs/Scientific-Point-Artifact-Package-Spec.md
```

下一阶段由用户指定。建议完成实际 GUI 端到端验证和成果包完整性校验：

```text
WinForms 实际点击保存验证
完整成果包 checksums
断点续传和大成果包压缩
```

当前边界：

```text
relations.json 和 observables.json 已使用叙述包真实数据
conversation.txt 是客户端会话记录，不属于科学结论
成果包保存尚未通过实际 GUI 点击端到端验证
```

阶段 5 和阶段 6 验收结果：

```text
修正前后的文件都下载
输入、fchk、输出使用统一基本文件名
point-summary 和 provenance 明确引用 PointId
关系和物理量可以追溯到点
对话内容保存到 narrative/conversation.txt
新记录文件引用完整
```

阶段 7 验证结果：

```text
真实 O2 任务完成
原始点和恢复点均进入成果包
两个科学点各有独立文件
输入、fchk、主要输出基本文件名一致
conversation.txt 和 final-summary.txt 成功保存
当前没有自动化点击 WinForms 保存按钮
```

阶段 1 验收结果：

```text
PointArtifactReference 已包含 CanonicalStem 和 CanonicalExtension
Artifact 模型 JSON 往返测试通过
未修改计算层、工作流、API 和 WinForms
```

阶段 2 验收结果：

```text
原始点引用原始作业的完整文件清单
恢复点引用恢复作业的完整文件清单
每个科学点和每项文件引用都记录自己的 CalculationJobId
只保存引用，不复制文件，不修改原始计算目录
```

阶段 3 验收结果：

```text
文件不存在时返回明确错误
不允许访问作业 run 目录之外的任意服务器路径
缺失的 fchk 不影响其它文件解析和下载
清单解析和单文件打开共用同一组安全和完整性规则
```

阶段 4 验收结果：

```text
按 resultId 返回成果包清单
按 jobId 返回所属成果包清单
原始点和恢复点都进入清单
文件名符合统一命名
单文件下载路径可生成
```

## 重要文档

```text
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md

docs/Tutorials/ChemSculptor-Tutorial.md
docs/Tutorials/ChemSculptor-Skill-Collection-Tutorial.md
docs/Tutorials/Skill-Collections-and-Workflow-Tutorial.md
docs/Tutorials/ScientificData-Tutorial.md
docs/Tutorials/Scientific-Repository-Upgrade-Tutorial.md
docs/Tutorials/Oxygen-End-to-End-Tutorial.md
```

## 最近一次验证

```text
真实氧气任务
  → 单点计算
  → 稳定性检查 Finding
  → 三重态矫正
  → 稳定性复检 Passed
  → 科学数据仓储
  → 客户端摘要发送和领取
```

真实验证结果：

```text
原始多重度：1
修正后多重度：3
最终能量：-150.274273534 Hartree
科学点数量：2
矫正次数：1
客户端摘要：成功
```
