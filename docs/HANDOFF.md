# 当前交接

## 当前阶段

全过程计算档案包第 6 步已完成。

```text
已冻结 Calculation Run Archive Package 规范
档案边界固定为 RootWorkflowId
全部阶段和全部作业尝试必须保留
输入、输出、检查点、规范化结果、验证、参数审计和后处理证据已定义
失败、取消、跳过、被拒绝和被取代作业必须显式保留
scratch、缓存、临时交换文件和锁文件默认排除
排除项必须写入 exclusions.json
缺失、不可读、摘要不一致和安全拒绝状态已定义
计算失败不等于档案包不完整
science/ 保存科学点集合
process/ 保存全部执行证据
科学点集合继续作为计算与“数据转文章”的连接点
```

规范：

```text
docs/Calculation-Run-Archive-Package-Spec.md
```

第 2 步根工作流持久化证据账本已完成：

```text
新增 CalculationWorkflowEvidenceLedger
新增 FileCalculationWorkflowEvidenceLedgerRepository
账本路径固定为 workflow-runs/<RootWorkflowId>/ledger.json
单点、优化、联合优化频率、TDDFT 和吸收光谱支持 RootWorkflowId
吸收光谱创建根工作流并传播给优化和 TDDFT
NTO、稳定性检查和恢复作业继承来源作业根标识
作业可以显式指定 EvidenceStageId
缺失显式阶段时按任务类型回退到默认阶段
稳定性检查、稳定性修正、优化恢复和频率作业使用显式阶段
同一作业重复保存只更新，不产生重复尝试
失败、取消、父作业和恢复作业均保留
作业改归阶段时不会在旧阶段留下悬空引用
JSON 使用 camelCase 和字符串枚举
临时文件写入后再原子替换 ledger.json
仓储实例重建后仍可按 RootWorkflowId 读取全部已登记索引
本阶段不复制计算文件、不生成报告、不组装档案包、不改 API 和 WinForms
```

第 3 步作业完整清单和科学关联审计已完成：

```text
新增 FileCalculationWorkflowInventoryRepository
按 RootWorkflowId 枚举账本中的全部作业尝试
读取 manifest.json 并区分 Present、Missing、Invalid
发现账本未登记但可归属根工作流的孤立作业目录
审计阶段与作业尝试的双向悬空引用
支持阶段、状态、任务类型、程序、父作业和恢复尝试筛选
新增 ScientificWorkflowInventoryService
映射科学成果、科学点、直接作业、初始作业、接受作业、文件和验证作业
核对科学点根工作流标识
JobInventory 保存展示筛选结果
AuditJobInventory 保存不受筛选影响的完整关联审计结果
缺失科学成果、缺失作业和根标识冲突均显式报告
不复制文件、不生成报告、不组装档案包、不改 API 和 WinForms
```

第 4 步统一成果包范围契约已完成：

```text
新增 ReportPackageScope
新增 ReportPackageScopeKind
新增 ReportCalculationConclusionStatus
科学点成果包以 ResultId 为边界
全过程计算档案包以 RootWorkflowId 为边界
全过程档案必须同时包含科学点、执行证据和排除项清单
request、snapshot、document 和 manifest 均携带 Scope
范围对象可以 JSON 往返
目录只接受安全规范相对路径
science 与 process 必须彼此不同
计算失败但证据完整时档案可以 Complete
运行中或结论未知的根工作流不能标记档案 Complete
外层标识与范围标识不一致时返回稳定问题码
本阶段不读取仓储、不复制文件、不生成摘要、不组装包、不改 API 和 WinForms
```

第 5 步阶段与作业因果关系已完成：

```text
新增 CalculationWorkflowCausalityGraph
新增 Stage / JobAttempt 节点
新增 StageContainsJob、StagePrecedesStage、JobDerivedFromJob、JobRecoveryOfJob
每条边记录 EvidenceKind、SourceField 和 IsRequired
新增 ICalculationWorkflowCausalityRepository
新增 FileCalculationWorkflowCausalityRepository
因果关系图固定写入 workflow-runs/<RootWorkflowId>/causality.json
构建时强制读取完整作业清单，不传入展示筛选条件
使用 Stages[].JobAttemptIds 建立阶段包含作业关系
使用 Stages[].Order 建立声明顺序关系，并标记为非必要条件
使用 JobAttempts[].ParentJobId 建立派生关系
使用 CorrectionPlanId 或 AttemptNumber 建立恢复关系
父作业缺失、跨根工作流、自引用、循环、重复边和阶段冲突均生成稳定问题码
临时文件原子替换后写入，节点、边和问题确定性排序
JSON 使用 camelCase 和字符串枚举，服务重启后可按 RootWorkflowId 读取
新增 12 项测试和第五步教程
本阶段不读取计算输出文件，不生成报告，不组装档案包，不改 API 和 WinForms
```

第 6 步全过程产物发现策略已完成：

```text
新增 CalculationWorkflowArtifactDiscoverySnapshot、文件条目、作业摘要、
文件角色、必要性、发现状态、排除汇总和稳定问题码
新增 CalculationWorkflowArtifactDiscoveryPolicy
新增 CalculationWorkflowArtifactPathPolicy
新增 ICalculationWorkflowArtifactDiscoveryRepository
新增 FileCalculationWorkflowArtifactDiscoveryRepository
固定写入 workflow-runs/<RootWorkflowId>/artifacts-discovery.json
使用完整作业清单，不继承界面展示筛选
覆盖正常、失败、取消和恢复作业尝试
扫描作业根、input、run 和 results 目录
识别输入、输出、检查点、规范化结果、验证、参数审计和处理方案等角色
Gaussian 程序输入和主要输出按作业状态决定是否必要
未知持久化文件进入 Other，不静默忽略
scratch、缓存、临时文件和零字节控制流进入排除汇总
必要文件缺失或被排除时 IsComplete = false
拒绝路径越界和重解析点，逻辑路径不包含服务器绝对路径
JSON 临时文件原子替换，服务重启后可读取
新增 6 项测试和第 6 步教程
本阶段不复制文件、不计算 SHA-256、不生成最终 manifest/checksums、
不组装档案包、不改 API、WinForms 和报告层
```

计算成果包与报告生成层第 0 步已完成。

```text
新增 ChemSculptor.Reporting 独立项目
定义 ReportPackageRequest
定义 ReportEvidenceSnapshot
定义 ReportDocument
定义 ReportPackageManifest
固定 camelCase、字符串枚举和模式版本 1.0
新增统一 JSON 序列化入口
新增 5 项核心契约测试
新增第 0 步教程
尚未实现快照构建、章节生成、渲染、组装、API 和客户端接入
```

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
NTO 自然语言接入已实现
NTO 波长范围和全部模式已实现
Agent 已复用当前会话最近一次 TDDFT 作业
Api 和 WinForms 已返回 NTO 选择模式和作业摘要
优化轨迹解析兼容 Gaussian“先 SCF、后 Step number”顺序
稳定性检查输入会移除 opt 和 Freq 任务关键词
稳定性修正会同步写入目标自旋多重度
真实单重态 O2 已修正为三重态并通过稳定性复检
科学成果支持按父作业和根工作流标识查找
验证失败时作业不会标记为 Validated
振动解析已支持双原子和线型分子的 3N-5
振动解析只采用最后一个 Harmonic frequencies 块
振动模式数与 3N-5 或 3N-6 不一致时生成诊断
报告核心契约已冻结
报告层仅依赖 Compute 和 ScientificData
报告契约 JSON 往返已验证
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
当前会话没有 TDDFT 源时返回 NoTddftSource，不提交 NTO
报告层只完成第 0 步契约和统一包范围
证据快照尚未读取 ScientificData 仓储
报告文档尚未渲染为 Markdown
成果包清单当前只定义数据结构，不执行文件发现和 SHA-256 计算
Api 和 WinForms 尚未接入新报告层
全过程档案第 1 至 6 步已完成
当前保存仍以最新作业和最终科学点成果包为主
作业清单已支持完整枚举和筛选
科学成果、科学点、来源作业和运行目录的缺失关联审计已实现
阶段与作业因果关系图已实现
全过程产物发现快照、排除汇总和安全路径检查已实现
全过程文件复制、SHA-256 清单、组装和下载尚未实现
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：323/323 通过
真实 Gaussian 16：3/3 通过
真实 Gaussian opt freq：1/1 通过
真实 Gaussian O2 稳定性修正：1/1 通过
真实 Gaussian O2 振动解析：1/1 通过
```

## 尚未完成

```text
尚未验证同多重度不稳定波函数通过 stable=opt 恢复
尚未使用真实 Gaussian 验证 Freq 热化学输出
尚未实现吸收光谱专用异常恢复策略
尚未验证真实吸收光谱科学摘要和成果包客户端闭环
尚未实现联合优化频率虚频修正工作流
尚未执行真实 Gaussian NTO 端到端验收
尚未结构化解析 NTO 轨道占据数、轨道能量和图形文件
尚未增加 NTO 结果专题科学数据归档
尚未使用真实 Gaussian 验证冻结非氢原子的优化输入和位移检查
尚未完成优化成果包下载和客户端科学摘要闭环
尚未实现 ReportEvidenceSnapshotBuilder
尚未实现报告文件清单和 SHA-256 校验
尚未实现通用章节提供者
尚未实现确定性 Markdown 渲染器
尚未实现成果包组装器
尚未接入成果包 API 和客户端保存
尚未以吸收光谱和 NTO 作为内容提供者验收通用层
尚未实现全过程 manifest.json 与 checksums.sha256
尚未实现全过程档案 API 和客户端下载
```

## 下一步精确任务

下一步精确任务：等待用户确认是否进入全过程档案第 7 步。

```text
第 7 步目标：把 artifacts-discovery.json 中的 Present 项转换为最终
文件清单，计算 SHA-256，校验长度，并生成 checksums.sha256 的基础数据。
必须保持发现项与最终清单项的可追溯关系，不能把发现阶段的 Present
直接改写成已归档的 Included。
当前第 6 步已完成，不在未确认前继续修改报告层或文件组装层。
```

第 6 步产物发现完成后，再按用户指定推进文件清单、档案组装和客户端下载。

历史阶段建议：

```text
使用真实 Gaussian 16 执行优化和稳定性端到端验收
继续验证同多重度 stable=opt 修正路径
使用真实含氢体系验证 HydrogensOnly 输入、优化结果和冻结位移检查
验证 WinForms 实际提交、显示和保存成果包
验证真实不稳定波函数的修正、重新优化和复检
验证真实振动分析频率、虚频和热化学校正
验证真实 TDDFT 激发态和跃迁矩输出
验证真实 NTO 输入、检查点复用和输出解析
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

## 本次全过程档案第 6 步关联文件

```text
src/ChemSculptor.Compute/CalculationWorkflowArtifactDiscoveryModels.cs
src/ChemSculptor.Compute/CalculationWorkflowArtifactDiscoveryPolicy.cs
src/ChemSculptor.Compute/CalculationWorkflowArtifactPathPolicy.cs
src/ChemSculptor.Compute/FileCalculationWorkflowArtifactDiscoveryRepository.cs
src/ChemSculptor.Compute/CalculationModels.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
src/ChemSculptor.Compute/CalculationWorkspacePaths.cs
src/ChemSculptor.Compute/WorkspaceManager.cs
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
src/ChemSculptor.Agent/AgentServiceRegistration.cs
tests/ChemSculptor.Core.Tests/CalculationWorkflowArtifactDiscoveryTests.cs
docs/Tutorials/Calculation-Run-Archive-Step-06-Artifact-Discovery-Tutorial.md
docs/Calculation-Run-Archive-Package-Spec.md
docs/CHANGES.md
docs/PROJECT_STATE.md
docs/HANDOFF.md
```

## 本次全过程档案第 5 步关联文件

```text
src/ChemSculptor.Compute/CalculationWorkflowCausalityModels.cs
src/ChemSculptor.Compute/FileCalculationWorkflowCausalityRepository.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
src/ChemSculptor.Compute/CalculationWorkspacePaths.cs
src/ChemSculptor.Agent/AgentServiceRegistration.cs
tests/ChemSculptor.Core.Tests/CalculationWorkflowCausalityTests.cs
docs/Tutorials/Calculation-Run-Archive-Step-05-Causality-Tutorial.md
docs/Calculation-Run-Archive-Package-Spec.md
docs/CHANGES.md
docs/PROJECT_STATE.md
docs/HANDOFF.md
```

## 本次全过程档案第 4 步关联文件

```text
src/ChemSculptor.Reporting/ReportContracts.cs
src/ChemSculptor.Reporting/ReportPackageScope.cs
src/ChemSculptor.Reporting/ReportPackageRequest.cs
src/ChemSculptor.Reporting/ReportEvidenceSnapshot.cs
src/ChemSculptor.Reporting/ReportDocument.cs
src/ChemSculptor.Reporting/ReportPackageManifest.cs
tests/ChemSculptor.Core.Tests/ReportContractTests.cs
docs/Tutorials/Calculation-Run-Archive-Step-04-Scope-Tutorial.md
docs/CHANGES.md
docs/PROJECT_STATE.md
docs/HANDOFF.md
```

## 本次全过程档案第 3 步关联文件

```text
src/ChemSculptor.Compute/CalculationWorkflowInventoryModels.cs
src/ChemSculptor.Compute/FileCalculationWorkflowInventoryRepository.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
src/ChemSculptor.Compute/WorkspaceManager.cs
src/ChemSculptor.Agent/ScientificWorkflowInventoryModels.cs
src/ChemSculptor.Agent/IScientificWorkflowInventoryService.cs
src/ChemSculptor.Agent/ScientificWorkflowInventoryService.cs
src/ChemSculptor.Agent/AgentServiceRegistration.cs
tests/ChemSculptor.Core.Tests/CalculationWorkflowInventoryTests.cs
tests/ChemSculptor.Core.Tests/ScientificWorkflowInventoryServiceTests.cs
tests/ChemSculptor.Core.Tests/WorkspaceManagerTests.cs
docs/CHANGES.md
docs/PROJECT_STATE.md
docs/HANDOFF.md
```

## 本次全过程档案第 2 步关联文件

```text
src/ChemSculptor.Compute/CalculationWorkflowEvidenceLedger.cs
src/ChemSculptor.Compute/FileCalculationWorkflowEvidenceLedgerRepository.cs
src/ChemSculptor.Compute/CalculationInterfaces.cs
src/ChemSculptor.Compute/CalculationModels.cs
src/ChemSculptor.Compute/CalculationWorkspaceOptions.cs
src/ChemSculptor.Compute/CalculationWorkspacePaths.cs
src/ChemSculptor.Compute/WorkspaceManager.cs
src/ChemSculptor.Compute/FileCalculationRepository.cs
src/ChemSculptor.Compute/OptimizationModels.cs
src/ChemSculptor.Compute/OptimizationFrequencyModels.cs
src/ChemSculptor.Compute/TddftModels.cs
src/ChemSculptor.Compute/AbsorptionSpectrumModels.cs
src/ChemSculptor.Agent/AgentServiceRegistration.cs
src/ChemSculptor.Agent/AbsorptionSpectrumWorkflowService.cs
src/ChemSculptor.Agent/OptimizationCalculationService.cs
src/ChemSculptor.Agent/OptimizationFrequencyCalculationService.cs
src/ChemSculptor.Agent/SinglePointCalculationService.cs
src/ChemSculptor.Agent/TddftCalculationService.cs
src/ChemSculptor.ScientificData.Extraction/AbsorptionScientificDataRecordSkill.cs
src/ChemSculptor.Skills.Common/NtoCalculation/NtoTransitionCalculationSkill.cs
src/ChemSculptor.Skills.Common/OptimizationRecovery/OptimizationFrequencyAnalysisSkill.cs
src/ChemSculptor.Skills.Gaussian/Anomaly/Recovery/GaussianRecoveryJobProvider.cs
src/ChemSculptor.Skills.Gaussian/Anomaly/WavefunctionStability/GaussianWavefunctionStabilityCheckSkill.cs
tests/ChemSculptor.Core.Tests/CalculationWorkflowEvidenceLedgerTests.cs
tests/ChemSculptor.Core.Tests/AbsorptionSpectrumWorkflowServiceTests.cs
tests/ChemSculptor.Core.Tests/TddftCalculationServiceTests.cs
tests/ChemSculptor.Core.Tests/GaussianRecoveryJobProviderTests.cs
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

任何一项未更新，本阶段都不得标记为完成。

## 本次第 0 步关联文件

```text
ChemSculptor.slnx
src/ChemSculptor.Reporting/ChemSculptor.Reporting.csproj
src/ChemSculptor.Reporting/ReportContracts.cs
src/ChemSculptor.Reporting/ReportPackageRequest.cs
src/ChemSculptor.Reporting/ReportEvidenceSnapshot.cs
src/ChemSculptor.Reporting/ReportDocument.cs
src/ChemSculptor.Reporting/ReportPackageManifest.cs
src/ChemSculptor.Reporting/ReportContractSerializer.cs
tests/ChemSculptor.Core.Tests/ChemSculptor.Core.Tests.csproj
tests/ChemSculptor.Core.Tests/ReportContractTests.cs
docs/Tutorials/Report-Package-Step-00-Contracts-Tutorial.md
```

## 本次全过程档案第 1 步关联文件

```text
docs/Calculation-Run-Archive-Package-Spec.md
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```
