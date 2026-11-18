# 全过程计算档案包规范

## 1. 方案标识

```text
方案名称：Calculation Run Archive Package
中文名称：全过程计算档案包
规范版本：1.0
状态：已冻结
适用范围：一个根工作流完成的全部计算过程
档案边界：RootWorkflowId
```

本规范只冻结全过程计算档案包的语义、边界、收录规则、目录结构、
缺失状态和验收标准，不规定具体实现代码。

## 2. 目的

全过程计算档案包用于回答以下问题：

```text
这次客户端请求最终做了什么
经过了哪些阶段
每个阶段提交了哪些作业
每个作业尝试使用了什么输入
每个作业尝试产生了什么输出
哪些作业失败、被拒绝或被取代
异常如何诊断和修正
最终科学结论由哪个作业产生
下载后能否复现和审计整个过程
```

全过程计算档案包不是只保存最后一个成功作业的文件。

## 3. 与科学点成果包的关系

ChemSculptor 同时保留两种包，二者职责不同：

```text
Scientific Point Artifact Package
  科学点成果包
  保存最终科学结果和科学点集合
  是计算部分与“数据转文章”部分的连接点

Calculation Run Archive Package
  全过程计算档案包
  保存根工作流下全部阶段和全部作业尝试
  用于复现、审计、排错、质量检查和长期归档
```

全过程计算档案包必须把科学点集合作为正式组成部分完整保留：

```text
全过程计算档案包
├─ science/
│    科学点集合、关系、物理量和科学叙述
└─ process/
     全部阶段、全部作业尝试和全部过程文件
```

不能用全过程计算档案包取代科学点集合，也不能用科学点集合
代替全部计算过程。

正式关系：

```text
RootWorkflowId
  决定全过程档案包含哪些执行证据

ScientificResult.PointSet
  决定最终科学结果包含哪些科学点

CalculationPoint.CalculationJobId
  把一个科学点追溯到接受它的计算作业

process/ 中保留的失败和被取代作业
  不自动进入科学结论
  但必须在全过程档案中保留
```

“数据转文章”部分正式消费 `science/`，不直接消费 `process/`。

## 4. 核心术语

### 4.1 根工作流

一次客户端科研请求对应的完整工作流运行。

```text
RootWorkflowId 是全过程档案的唯一范围标识
不得使用“最新作业”“最后一个作业”或“最终成果作业”替代
```

### 4.2 阶段

根工作流中的一个可识别计算阶段，例如：

```text
输入准备
结构优化
波函数稳定性检查
稳定性修正
恢复优化
振动分析
TDDFT 单点计算
NTO 计算
后处理
科学数据归档
```

阶段名称可以是领域语义，但阶段记录必须使用稳定阶段标识和顺序。

### 4.3 作业尝试

某个阶段提交的一次具体计算作业。

同一阶段可以有多个作业尝试：

```text
原始尝试
重试尝试
SCF 修正尝试
稳定性修正尝试
多重度修正尝试
优化恢复尝试
NTO 单状态作业
```

每个作业尝试必须独立保存，不能只保留最终成功尝试。

### 4.3.1 阶段与作业因果关系

全过程档案必须能够区分以下四类关系：

```text
StageContainsJob
  阶段明确包含一个作业尝试

StagePrecedesStage
  阶段按声明顺序位于另一个阶段之前，不自动等同于科学因果

JobDerivedFromJob
  一个作业由另一个作业派生，例如频率作业来自最终优化作业

JobRecoveryOfJob
  一个作业用于修正另一个作业的失败或异常
```

关系必须来自持久化证据，不能仅根据时间戳推断。首批证据字段为：

```text
Stages[].JobAttemptIds
Stages[].Order
JobAttempts[].ParentJobId
JobAttempts[].AttemptNumber
JobAttempts[].CorrectionPlanId
```

父作业缺失、跨根工作流、自引用、循环、阶段归属冲突和重复边必须
显式记录。阶段声明顺序不是必要条件；作业派生和恢复关系是必要条件。

### 4.4 持久化证据

根工作流结束后仍应保留、可复制、可校验并可用于复现或审计的文件和记录。

### 4.5 临时运行数据

计算程序或运行环境产生的暂存、交换、缓存、锁文件和自动临时文件。

### 4.6 科学点集合

`ScientificResult.PointSet`，包含科学点、状态、来源、验证、关系和物理量。

## 5. “完整”的正式定义

全过程档案包中的“完整”只表示证据采集和封装完整，不表示计算一定成功。

档案包完整必须同时满足：

```text
根工作流范围明确
全部阶段记录存在
全部作业尝试记录存在
每个阶段和作业的关联存在
全部应保留输入文件存在
全部应保留输出文件存在
全部检查点状态已记录
全部规范化结果状态已记录
全部验证结果状态已记录
全部参数审计状态已记录
全部后处理产物状态已记录
每个预期文件都有包含、缺失或排除状态
每个已包含文件都有长度和 SHA-256
科学点集合已按冻结时状态保存
manifest.json 与 checksums.sha256 有效
```

以下情况不自动使档案包不完整：

```text
某个计算作业失败
某个计算作业被取消
SCF 没有收敛
优化没有收敛
稳定性检查发现不稳定
某个阶段被明确跳过
某个科学点被拒绝或被取代
某个程序没有产生可选后处理产物
```

只要这些情况本身有完整记录，它们就是全过程证据。

## 6. 包级状态

档案包状态与计算结论状态必须分开。

### 6.1 档案完整性

```text
Complete
  全部预期证据可用且通过校验

Partial
  包已生成，但存在缺失、不可读、摘要不一致或安全拒绝的证据

Failed
  包没有成功生成，或 manifest/checksum 不能作为有效入口
```

规定：

```text
计算失败 + 证据完整 = Complete
计算成功 + 关键证据缺失 = Partial
无法完成封装或完整性入口无效 = Failed
```

### 6.2 计算结论状态

```text
Succeeded
PartiallySucceeded
Failed
Canceled
InProgress
Unknown
```

计算结论状态来自工作流和科学结果，不改变档案完整性定义。

### 6.3 时间截面

每个档案包必须有冻结时间 `CapturedAt`。

```text
完整档案只针对已结束的根工作流
已结束包括 Succeeded、PartiallySucceeded、Failed 和 Canceled
运行中的工作流只能生成临时快照，不能标记为 Complete
运行中快照必须标记 InProgress 或 Partial，并写明 CapturedAt
```

## 7. 收录范围

### 7.1 请求和工作流证据

必须收录：

```text
原始客户端请求文本
规范化请求
工作流选择结果
参数解析结果
工作流定义快照
工作流运行状态
阶段顺序
节点状态
阶段和作业关联
时间线
环境记录
会话文本
用户确认和澄清记录
```

### 7.2 阶段证据

每个阶段必须有一个阶段记录，包含：

```text
StageId
阶段类型
阶段顺序
所属根工作流
状态
开始时间和结束时间
输入引用
输出引用
关联作业 ID
跳过原因
失败原因
诊断
关联科学点 ID
```

阶段状态至少支持：

```text
Planned
Running
Completed
Failed
Skipped
Canceled
Unknown
```

明确跳过的阶段也必须记录，不能从时间线中消失。

### 7.3 作业证据

每个作业尝试必须收录：

```text
CalculationJob 记录
所属阶段
JobId
ParentJobId
RootWorkflowId
AttemptNumber
CalculationSpec
参数审计
作业状态
工作区逻辑标识
输入来源
程序输入文件
程序主要输出
stdout 和 stderr
检查点文件
规范化结果
验证报告
处理方案
诊断和错误
后处理产物
全部其它持久化文件
```

作业记录不得只保存最终 `AcceptedJobId`。

### 7.4 输入证据

按适用情况收录：

```text
用户原始坐标
规范化几何
几何验证报告
每个作业的 source input
每个作业实际提交的程序输入
输入模板变量和模板标识
规范参数和程序参数翻译结果
重启或续算使用的输入状态
```

若某作业在输入生成前失败：

```text
输入文件标记 NotExpected
失败原因必须保留
不得伪造空输入文件
```

### 7.5 输出证据

按适用情况收录：

```text
程序主要输出
非空 stdout
非空 stderr
检查点文件
格式化检查点
规范化 result.json
validation.json
processing-plan.json
program-processing-plan.json
诊断文件
```

### 7.6 后处理与派生证据

凡由本根工作流产生并服务于结果解释、图形、报告或继续计算的持久化文件，
都必须进入清单，例如：

```text
光谱状态表
NTO 权重表
NTO 检查点
轨道或密度 cube
图像
CSV 和 JSON 数据表
可复现脚本
程序后处理原始日志
```

通用规范不硬编码具体程序和扩展名。具体程序通过适配器声明候选产物规则。

### 7.7 科学结果证据

必须收录冻结时的：

```text
ScientificResult
CalculationPointSet
CalculationPoint
CalculationPointRelation
ScientificObservable
科学叙述包
点摘要
来源说明
最终摘要
```

科学点集合不得因为全过程归档而被改写。

特别规定：

```text
原始点和恢复点都保留
Candidate、Accepted、Rejected、Superseded 状态必须保留
不得把被拒绝或被取代的点改成 Accepted
不得删除失败点
没有可下载文件的点仍保留结构和状态
```

## 8. 排除范围

### 8.1 默认排除

以下内容默认不进入档案包：

```text
scratch 目录
临时交换文件
程序缓存
操作系统临时目录
锁文件
正在写入或未稳定的文件
进程崩溃转储，除非被显式声明为诊断证据
零字节 stdout 或 stderr
构建和运行缓存
与根工作流无关的其它工作区文件
```

### 8.2 排除不等于静默丢弃

排除项必须写入 `exclusions.json`，记录：

```text
排除类别
作业或目录引用
数量
总字节数
排除原因
能否重新生成
是否影响复现
```

默认只记录排除统计，不复制排除文件内容。

### 8.3 显式证据优先

如果一个 scratch 或缓存文件被工作流明确记录为完成科学解释所必需，
应在归档前把它提升为正式后处理产物，并赋予明确逻辑路径和文件角色。

未提升的临时文件不进入档案包。

### 8.4 未分类持久化文件

对于根工作流管理的作业目录内、且不属于默认排除项的未分类普通文件：

```text
默认放入 other/
在清单中标记为 Other
不得因为适配器尚未识别而静默遗漏
```

如果文件不能安全读取或有路径风险：

```text
标记为 SecurityRejected 或 Unreadable
档案完整性至少降为 Partial
保留问题记录
```

### 8.5 路径边界

档案采集边界必须是最小必要根目录集合：

```text
根工作流输入和几何目录
该根工作流下全部作业目录
该根工作流明确声明的后处理目录
科学数据仓储中属于该根工作流的记录
```

禁止把整个服务器 home、工作区根目录或任意外部目录递归打包。

## 9. 条件必需证据

“全部输入输出”按作业实际到达的状态判定，不要求不存在阶段产生空文件。

| 作业到达状态 | 条件必需证据 |
|---|---|
| Created | job.json、阶段关联、请求和工作流记录 |
| InputGenerated | 上述证据、参数审计、source input、程序输入 |
| Queued | 上述证据、队列或提交记录 |
| Running | 上述证据、已产生的输出和标准流 |
| Completed | 上述证据、主要输出、非空 stdout/stderr、已知检查点 |
| Parsed | 上述证据、result.json、产物描述、解析诊断 |
| Validated | 上述证据、validation.json |
| Failed | 失败诊断、已产生的全部输入输出和程序返回信息 |
| Canceled | 取消诊断、已产生的全部输入输出和取消时间 |

规则：

```text
没有到达某个状态时，该状态新增的证据为 NotExpected
到达某个状态后，该状态要求的证据缺失为 Missing
Missing 必须使档案完整性降为 Partial
```

## 10. 文件证据状态

每个预期文件必须获得以下状态之一：

```text
Included
  已收录，文件可读并完成 SHA-256 校验

ExcludedByPolicy
  按临时文件、缓存或范围规则排除

NotExpected
  当前作业状态不应产生该文件

Missing
  当前作业状态要求该文件，但文件中不存在

Unreadable
  文件存在但无法读取或无法稳定打开

HashMismatch
  实际 SHA-256 与来源记录不一致

SecurityRejected
  路径越界、绝对路径、链接穿越或其它安全规则拒绝
```

禁止把 `Missing` 文件从清单中删除。缺失本身必须可见。

## 11. 文件身份和角色

### 11.1 稳定身份

文件身份至少由以下内容确定：

```text
RootWorkflowId
StageId
CalculationJobId
逻辑相对路径
文件角色
```

不得使用数组位置、当前时间或下载名作为唯一身份。

### 11.2 文件角色

文件角色至少支持：

```text
RequestInput
GeometryInput
ProgramInput
PrimaryOutput
ConsoleOutput
RestartState
NormalizedResult
ValidationEvidence
ParameterAudit
PostProcessOutput
ScientificData
Narrative
Report
Manifest
Checksum
Other
```

### 11.3 重复文件

同一物理文件可以因多个逻辑角色被引用，但必须满足：

```text
逻辑身份不同
引用关系清楚
包内只有一个权威副本
清单记录全部逻辑角色
```

## 12. 目录结构

```text
calculation-run-<rootWorkflowId>/
  manifest.json
  checksums.sha256
  README.md
  report.md
  report.json
  exclusions.json

  science/
    scientific-result.json
    point-set.json
    relations.json
    observables.json
    narratives/
      final-summary.txt
      organization.txt

  process/
    request.json
    conversation.txt
    workflow-definition.json
    workflow-run.json
    timeline.json
    environment.json

    stages/
      01-<stageId>/
        stage.json
        jobs/
          attempt-00_<jobId>/
            job.json
            parameters.json
            artifacts.json

            input/
              source/
              generated/

            output/
              primary/
              console/

            checkpoints/

            parsed/
              result.json
              validation.json
              processing-plan.json
              program-processing-plan.json

            postprocess/
            other/

  scripts/
    reproducible/
```

说明：

```text
stages/ 按阶段顺序编号
jobs/ 按尝试顺序编号
attempt-00 是原始尝试
attempt-01 及以后是重试、修正或恢复尝试
同一作业的输入输出不跨作业目录混合
科学点目录与过程作业目录通过标识关联，不复制成两套互相矛盾的结论
```

## 13. manifest.json

`manifest.json` 是档案包唯一正式入口。

包级至少包含：

```text
ArchiveSpecVersion
PackageId
RootWorkflowId
ResultId
CapturedAt
ArchiveCompleteness
ComputationOutcome
Stages[]
SciencePointSet
Files[]
ExclusionsSummary
TotalBytes
Warnings
```

每个阶段至少包含：

```text
StageId
StageType
Order
Status
StartedAt
CompletedAt
JobAttemptIds[]
SkippedReason
Diagnostics[]
SciencePointIds[]
```

每个作业尝试至少包含：

```text
CalculationJobId
StageId
ParentJobId
AttemptNumber
State
Program
TaskType
InputEvidence[]
OutputEvidence[]
CheckpointEvidence[]
NormalizedResult
Validation
ParameterAudit
PostProcessEvidence[]
Diagnostics[]
```

每个文件至少包含：

```text
ArtifactId
LogicalPath
PackagePath
Role
SourceType
SourceReferenceId
CalculationJobId
StageId
MediaType
Status
Length
Sha256
RequiredForCompleteness
Reproducible
```

禁止写入：

```text
服务器绝对文件路径
服务器凭据
环境变量中的秘密值
临时下载地址
客户端本机路径
```

## 14. checksums.sha256

必须包含包内除 `checksums.sha256` 自身之外的每个实际文件。

每行至少表达：

```text
SHA-256
相对包根目录的路径
```

校验要求：

```text
文件存在
长度与 manifest 一致
SHA-256 与 manifest 和 checksums.sha256 一致
manifest 中 Included 文件都能在包中找到
包内不存在未进入 manifest 的普通文件
```

## 15. 科学点集合的冻结语义

`science/point-set.json` 必须是冻结时刻科学点集合的规范副本。

必须保持：

```text
PointId
CalculationJobId
Status
Provenance
Validations
Artifacts
Relations
Observables
```

全过程档案可以增加指向阶段和作业的索引，但不得修改科学点原有含义。

如果某个科学点引用最后被拒绝的作业：

```text
科学点状态保持原样
该作业在 process/ 中完整保存
不得为了归档方便改成 Accepted
```

## 16. 缺失和失败规则

### 16.1 计算失败

计算失败时必须保留：

```text
失败作业
失败输入
已产生输出
标准输出和标准错误
程序返回信息
诊断
验证报告
修正计划和派生作业
```

### 16.2 输入缺失

```text
作业已开始但输入不存在：Missing，包 Partial
作业未开始且输入本来不应存在：NotExpected，不降低完整性
```

### 16.3 输出缺失

```text
作业到达 Running 或 Completed 但主要输出缺失：Missing，包 Partial
进程启动失败且程序没有创建输出：允许 Missing，但必须保留 stderr 和进程诊断
```

### 16.4 检查点缺失

```text
程序已明确生成检查点路径但文件缺失：Missing
任务类型本来不需要检查点：NotExpected
检查点不能下载但文件存在：Unreadable 或 SecurityRejected
```

### 16.5 验证缺失

```text
工作流声明该阶段应执行验证但 validation.json 缺失：Missing，包 Partial
阶段被明确跳过：NotExpected，并记录跳过原因
```

### 16.6 参数审计缺失

```text
作业已进入 InputGenerated 但 parameters.json 缺失：Missing，包 Partial
作业在参数解析前失败：NotExpected
```

### 16.7 后处理缺失

```text
工作流记录后处理应产生某文件但缺失：Missing，包 Partial
后处理未执行或被明确跳过：NotExpected，并记录原因
后处理失败：保留失败日志和诊断，不伪造结果文件
```

## 17. 排序和确定性

为了可审计和可测试，清单必须使用稳定排序：

```text
阶段按 Order 和 StageId 排序
作业按 AttemptNumber 和 CalculationJobId 排序
文件按 LogicalPath 排序
科学点按 PointId 排序
关系按 RelationId 排序
物理量按 ObservableId 排序
诊断按稳定代码和时间排序
```

相同冻结证据重新生成清单时：

```text
标识不变
包含状态不变
SHA-256 不变
排序不变
```

允许变化的时间字段必须集中记录在包级元数据中，不得混入科学点身份。

## 18. 安全和可移植性

```text
只允许访问根工作流允许的目录
拒绝绝对路径
拒绝目录穿越
拒绝未经允许的符号链接或重解析点
服务器绝对路径不进入包
文件名使用规范化相对路径
科学数据中的服务器路径转换为逻辑引用
```

## 19. 本阶段不包含

```text
不实现根工作流持久化索引
不实现全部作业枚举
不实现文件复制和打包
不实现 checksums.sha256 生成器
不实现 API 和 WinForms 下载
不实现 Markdown 报告渲染
不修改科学点集合结构
```

## 20. 第一步验收标准

第一步完成必须同时满足：

```text
1. 已明确全过程档案以 RootWorkflowId 为边界
2. 已明确全部阶段、全部作业尝试和全部持久化证据必须保留
3. 已明确输入、输出、检查点、规范化结果、验证、参数审计和后处理产物的收录规则
4. 已明确失败、取消、跳过、被拒绝和被取代作业同样保留
5. 已明确 scratch、缓存、临时文件和锁文件的排除规则
6. 已明确排除项必须生成可见清单，不允许静默丢弃
7. 已明确未分类持久化文件进入 other/ 而不是被忽略
8. 已明确缺失、不可读、摘要不一致和安全拒绝的状态
9. 已明确计算失败和档案包不完整的区别
10. 已明确科学点集合是最终科学结果和“数据转文章”的连接点
11. 已明确全过程档案不得取代或改写科学点集合
12. 已明确 manifest.json 和 checksums.sha256 是档案完整性的正式入口
13. 已明确包内不得包含服务器绝对路径和秘密信息
14. 已明确本阶段不写实现代码
```

## 21. 后续实现验收场景

本规范的实现阶段至少需要验证以下场景：

```text
单点计算只有一次作业尝试
优化包含原始优化、稳定性检查、修正和恢复优化
优化后振动分析包含独立频率作业
吸收光谱包含优化、TDDFT 和科学归档
NTO 包含多个派生状态作业
某作业在输入生成前失败
某作业运行失败但 stderr 完整
某预期输出缺失
某检查点摘要不一致
某临时 scratch 被排除且 exclusion 可见
服务重启后仍能按 RootWorkflowId 找回全部作业
科学点集合在归档前后保持同一语义
```

## 22. 冻结结论

```text
全过程计算档案包以一个根工作流为单位
计算失败不等于档案失败
科学点集合保留为计算与写文章的正式连接点
全部作业尝试和持久化证据与科学点集合并存
缺失和排除必须显式，不得静默丢失
manifest.json 和 checksums.sha256 定义档案完整性
```
