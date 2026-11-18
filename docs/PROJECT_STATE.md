# ChemSculptor 项目状态

## 当前版本

```text
0.75.0
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
测试：292/292 通过
真实 Gaussian 16：3/3 通过
真实 Gaussian opt freq：1/1 通过
真实 Gaussian O2 稳定性修正：1/1 通过
真实 Gaussian O2 振动解析：1/1 通过
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

报告契约与成果包边界：

```text
ScientificData + Compute 的持久化证据
  ↓
ChemSculptor.Reporting
  → ReportPackageRequest
  → ReportEvidenceSnapshot
  → ReportDocument
  → ReportPackageManifest
```

当前只完成契约定义。证据快照读取、文档生成、Markdown 渲染、
成果包组装以及 API 和客户端接入尚未实现。

计算成果包分为两个明确层次：

```text
科学点成果包
  保存最终科学结论
  科学点集合是计算部分与“数据转文章”部分的连接点

全过程计算档案包
  以一个 RootWorkflowId 为边界
  保存全部阶段、全部作业尝试和全部持久化执行证据
  同时完整保留科学点集合
```

全过程档案规范已经冻结，根工作流持久化证据账本和作业完整清单已经实现。
账本按 RootWorkflowId 保存阶段、作业尝试和状态，服务重启后可以
恢复全部已登记索引；仓储可以读取 manifest、发现缺失和孤立作业，并把
科学成果、科学点和来源作业关联起来。完整文件发现、成果包组装和客户端下载尚未实现。
当前客户端“保存”仍以最终科学点成果包和最新作业入口为主。

依赖原则：

```text
客户端不引用服务端项目
ScientificSummary 只引用 ScientificData
Api 引用 ScientificSummary
通用工作流不包含具体计算程序规则
报告和客户端科学摘要只从 ScientificData 读取
ChemSculptor.Reporting 只引用 Compute 和 ScientificData
Reporting 不依赖 Agent、Api、WinForms、具体 Workflow 或 Gaussian
```

## 已完成模块

```text
工作流内核
计算模型与接口
本机计算后端
Gaussian 输入生成
Gaussian 输出解析
单点计算 API
单点计算复合 Skill
规范计算参数键
计算参数值模型
规范计算参数目录
对话计算参数提取器
计算参数 Resolver
参数问询闭环
Agent 参数问询接入
ResolvedSpec 服务接入
Gaussian route 关键词翻译
计算参数审计
参数问题客户端显示
项目级 Codex Skill
Gaussian route 行输入
Route 输出信息量
默认 GD3BJ 色散
优化任务契约
优化参数键与目录
优化参数默认值
优化参数提取与解析
优化 Gaussian route 关键词翻译
优化输出轨迹解析
优化结果翻译
优化复合 Skill
优化结果验证节点
优化步骤验证器
优化总体结果验证器
优化收敛验证器
优化异常错误码
优化收敛异常检查
优化异常诊断 Skill
优化修正规划 Skill
优化恢复作业创建 Skill
优化恢复作业执行 Skill
优化恢复验证 Skill
Gaussian 优化恢复输入重建
优化完整工作流工厂
工作流条件分支
优化异常检查 Skill
优化恢复分支选择
优化程序硬失败报告
优化处理方案 Skill
优化科学数据记录 Skill
优化波函数稳定性检查分支
波函数稳定性显式跳过规则
优化稳定性修正和复检
客户端波函数稳定性摘要
优化后振动分析 Skill
冻结非氢原子的优化约束
冻结原子输入标记和约束验证
约束优化自动加入 nosymm
Gaussian Freq 输入翻译
Gaussian 频率输出解析
客户端振动分析摘要
任务级 Skill 说明文档
TDDFT 单点计算 Skill
TDDFT 激发态解析
TDDFT 跃迁速度四极矩解析
TDDFT Codex Skill
TDDFT 单点计算 Workflow
联合优化频率 Skill
联合优化频率 Workflow
联合优化频率后稳定性检查
联合优化频率稳定性矫正和派生恢复
真实 Gaussian opt freq 验证
NTO 单状态 Skill
NTO Workflow 和状态选择
Gaussian NTO 输入和输出适配
NTO 自然语言、波长范围和全部模式
当前会话最近 TDDFT 源作业复用
NTO 客户端选择模式和作业摘要
优化轨迹按 Gaussian 实际输出顺序映射
稳定性修正同步更新多重度输入
恢复作业科学成果可由根作业 ID 查找
验证失败时作业不会标记为 Validated
振动解析区分线型 3N-5 和非线型 3N-6
频率解析只采用最后一个谐波频率块
振动模式数不一致会产生诊断
吸收光谱 Workflow
吸收光谱三种前置模式
吸收光谱 Agent 路由和客户端反馈
吸收光谱科学数据归档
吸收光谱恢复扩展框架
真实 Gaussian 吸收光谱验证
优化对话路由
OptimizationCalculationService
优化 API 显示字段
WinForms 优化摘要
几何优化 Codex Skill
优化测试矩阵
优化端到端测试
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
报告核心契约
报告证据快照模型
报告文档中间表示
成果包文件清单模型
报告契约统一 JSON 序列化
全过程计算档案包规范
科学点集合与全过程执行证据分层
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
主执行链当前覆盖单点计算和正常几何优化
当前只实现 Gaussian 16 本机执行主链
单点计算正常路径由计算复合 Skill 封装
优化正常路径由优化复合 Skill 封装
顶层工作流负责异常恢复、SCF 处理和质量记录
远程 HPC 队列和调度尚未实现
优化任务已完成契约、参数、翻译、结果和正常执行链
优化验证已拆分为步骤、总体结果和逐项收敛指标
优化异常检查已建立，包含证据、摘要和稳定错误码
优化恢复链已建立，支持派生作业和重新优化
优化完整工作流定义已建立
优化工作流已接入 Agent、Api 和 WinForms
优化正常、异常、恢复和归档链已有自动化测试
优化最终几何已接入波函数稳定性检查
优化工作流在稳定性处理之后执行振动分析
SOC 和 MECP 等任务尚未实现
TDDFT 单点计算 Workflow 已实现
吸收光谱 Workflow 和科学数据归档已实现
吸收光谱专用恢复策略尚未实现，仅保留扩展框架
联合优化频率已包含波函数稳定性矫正，虚频修正策略仍未实现
NTO 当前只支持 Gaussian TDDFT 源结果，默认使用当前会话最近一次 TDDFT 作业
尚未执行真实 NTO 端到端验收
科学点文件清单已进入 CalculationPoint
WinForms 已按清单保存完整成果包
关系、物理量和科学叙述已可追溯
成果包 checksums、压缩和断点续传尚未实现
客户端摘要当前使用 Api 内存邮箱
报告层只完成核心契约，尚未读取证据或生成成果包
报告层尚未接入 Api 和 WinForms
全过程档案第 2 步根工作流证据账本已实现
全过程档案第 3 步作业完整枚举、筛选和科学关联审计已实现
当前保存入口仍以最新作业和最终科学点成果包为主
作业清单同时保留展示筛选结果和完整审计结果
scratch、缓存和临时文件的排除清单尚未实现
```

## 下一阶段

全过程计算档案包：

```text
第 1 步：包语义和验收标准已冻结
第 2 步：根工作流持久化证据账本已完成
第 3 步：作业仓储完整枚举、筛选和科学关联审计已完成
第 4 步：报告包范围契约扩展尚未实现
第 5 步：阶段与作业因果关系尚未实现
第 6 步：全过程产物发现策略尚未实现
第 7 步：完整文件清单和 SHA-256 尚未实现
第 8 步：全过程证据快照尚未实现
第 9 步：通用报告章节提供者尚未实现
第 10 步：确定性 Markdown 渲染尚未实现
第 11 步：领域章节提供者尚未实现
第 12 步：成果包组装器尚未实现
第 13 步：成果包 API 尚未实现
第 14 步：客户端完整保存尚未实现
第 15 步：完整测试矩阵尚未实现
第 16 步：真实长流程验收尚未实现
```

第 1 步规范：

```text
docs/Calculation-Run-Archive-Package-Spec.md
```

第 1 步已完成：

```text
以 RootWorkflowId 冻结全过程档案边界
冻结全部阶段和全部作业尝试的收录范围
冻结输入、输出、检查点、规范化结果、验证、参数审计和后处理产物规则
冻结失败、取消、跳过、被拒绝和被取代作业的保留规则
冻结 scratch、缓存、临时文件和锁文件的排除规则
冻结缺失、不可读、摘要不一致和安全拒绝状态
冻结 science/ 与 process/ 的分层
明确计算失败不等于档案包不完整
```

第 2 步已完成：

```text
新增 CalculationWorkflowEvidenceLedger
新增 FileCalculationWorkflowEvidenceLedgerRepository
账本路径固定为 workflow-runs/<RootWorkflowId>/ledger.json
同一作业重复保存只更新，不产生重复尝试
失败、取消、父作业和恢复作业均保留
作业可以显式指定 EvidenceStageId
缺失显式阶段时按任务类型回退到默认阶段
稳定性检查、稳定性修正、优化恢复和频率作业使用显式阶段
吸收光谱创建根工作流并传播给优化和 TDDFT
服务重启后可以用新的仓储实例按 RootWorkflowId 读取阶段和作业索引
临时文件写入后原子替换，JSON 使用 camelCase 和字符串枚举
```

第 3 步已完成：

```text
新增 ICalculationWorkflowInventoryRepository
按 RootWorkflowId 读取账本并枚举全部作业尝试
读取 manifest.json 并区分 Present、Missing、Invalid
发现账本未登记但可归属根工作流的孤立作业目录
审计阶段与作业尝试的双向悬空引用
支持阶段、状态、任务类型、程序、父作业和恢复尝试筛选
新增 ScientificWorkflowInventoryService
映射科学成果、科学点、直接作业、初始作业、接受作业、文件和验证作业
核对科学点根工作流标识
展示筛选不影响关联审计使用的完整作业清单
缺失科学成果、缺失作业和根标识冲突均生成稳定问题码
```

下一步待用户确认；若继续全过程档案包，第 4 步是报告包范围契约扩展。

计算成果包与报告生成层：

```text
第 0 步：核心契约已完成
第 1 步：证据快照构建尚未开始
第 2 步：文件清单和 SHA-256 尚未开始
第 3 步：通用章节提供者尚未开始
第 4 步：确定性 Markdown 渲染尚未开始
第 5 步：成果包组装尚未开始
第 6 步：Api 接入尚未开始
第 7 步：客户端保存接入尚未开始
第 8 步：吸收光谱和 NTO 内容提供者尚未开始
第 9 步：跨 Workflow 通用性测试尚未开始
```

第 0 步已完成：

```text
定义 ReportPackageRequest
定义 ReportEvidenceSnapshot
定义 ReportDocument
定义 ReportPackageManifest
固定 camelCase、字符串枚举和模式版本 1.0
证明核心契约可以 JSON 往返
证明通用文档契约不依赖吸收光谱或 NTO 类型
```

下一步：实现 `ReportEvidenceSnapshotBuilder`，根据 `ResultId` 和关联作业
生成不可变证据快照，不在本阶段实现渲染或成果包下载。

优化任务建设进度：

```text
第 1 步：契约已完成
第 2 步：参数已完成
第 3 步：Gaussian 关键词翻译已完成
第 4 步：结果已完成
第 5 步：执行已完成
第 6 步：验证已完成
第 7 步：异常已完成
第 8 步：恢复已完成
第 9 步：工作流已完成
第 10 步：接入已完成
第 11 步：测试已完成

优化主链 11 步已完成，后续工作由用户指定。
```

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
空 stdout/stderr 不再进入文件清单
文件清单由适配器上下文动态发现
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
docs/skill说明/README.md
docs/skill说明/single-point-calculation.md
docs/skill说明/geometry-optimization.md
docs/skill说明/frequency-analysis.md
docs/skill说明/wavefunction-stability-check.md
docs/skill说明/tddft-single-point.md
docs/Tutorials/Codex-Skill-Authoring-Tutorial.md
docs/Tutorials/ChemSculptor-Skill-Collection-Tutorial.md
docs/Parameter-Interaction-Design.md
docs/Tutorials/Skill-Collections-and-Workflow-Tutorial.md
docs/Tutorials/ScientificData-Tutorial.md
docs/Tutorials/Scientific-Repository-Upgrade-Tutorial.md
docs/Tutorials/Oxygen-End-to-End-Tutorial.md
docs/Tutorials/Report-Package-Step-00-Contracts-Tutorial.md
docs/Calculation-Run-Archive-Package-Spec.md
docs/Coding-Conventions.md
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
