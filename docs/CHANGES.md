# ChemSculptor 项目改动说明

> 维护约定：**每次代码改动后更新本文档**，记录版本、改动目的、改动内容和教程式说明。
> 版本规则：`MAJOR.MINOR.PATCH`。新增功能 +1 MINOR；修复问题 +1 PATCH；架构性大改动 +1 MAJOR。

---

## v0.49.1（2026-10-07）：阶段 8 氧气完整验证

### 版本

- 当前版本：`0.49.1`
- 日期：2026-10-07
- 版本类型：验证与文档

### 验证目的

完成阶段 8“完整验证”。

使用真实氧气任务验证从客户端消息提交、单点计算、稳定性检查、
矫正计算、科学数据仓储到成果包保存的完整链路。

### 真实任务

任务结果：

```text
原始点：01-original-m1
原始点多重度：1
原始点状态：Superseded

恢复点：02-recovery-m3
恢复点多重度：3
恢复点状态：Accepted

科学点数量：2
每点文件数量：6
```

输入、fchk 和主要输出共享统一基本文件名：

```text
O2-original-m1.gjf
O2-original-m1.fchk
O2-original-m1.log

O2-recovery-m3.gjf
O2-recovery-m3.fchk
O2-recovery-m3.log
```

### 成果包验证

按 WinForms 保存按钮当前使用的清单、叙述和下载规则执行等价流程。

生成目录：

```text
scientific-result-<rootJobId>/
  points/
    01-original-m1/
      point-summary.txt
      provenance.txt
      O2-original-m1.gjf
      O2-original-m1.fchk
      O2-original-m1.log
      ...
    02-recovery-m3/
      point-summary.txt
      provenance.txt
      O2-recovery-m3.gjf
      O2-recovery-m3.fchk
      O2-recovery-m3.log
      ...
  relations.json
  observables.json
  artifact-manifest.json
  narrative/
    final-summary.txt
    conversation.txt
    organization.txt
```

验证结果：

```text
原始点文件独立下载
恢复点文件独立下载
输入、fchk 和主要输出命名一致
对话文本成功保存
最终摘要成功保存
```

### 验证限制

当前环境无法自动点击 WinForms 保存按钮。

本次执行的是保存按钮所依赖的同一组 API 和目录写入规则，
不是实际鼠标点击 GUI 按钮。

### 构建与测试

```text
Release 构建：0 警告，0 错误
测试：93/93 通过
```

---

## v0.49.0（2026-10-07）：兼容和历史数据迁移

### 版本

- 当前版本：`0.49.0`
- 日期：2026-10-07
- 版本类型：功能新增

### 改动目的

完成阶段 7“兼容和历史数据迁移”。

旧 `ScientificResult` 即使没有文件引用，也可以生成清单和成果包，
并可通过计算作业结果逐项回填文件引用。

### 旧记录兼容

科学点清单增加：

```text
HasArtifactManifest
ArtifactManifestMessage
```

没有文件引用时：

```text
HasArtifactManifest = false
ArtifactManifestMessage = 没有文件清单。
```

成果包清单仍然成功返回，不因为缺少文件引用而报错。

WinForms 在保存旧点时显示：

```text
<PointId>：没有文件清单。
```

点叙述中也记录：

```text
文件清单：没有文件清单。
```

### 共享文件引用工厂

新增：

```text
ScientificArtifactReferenceFactory
```

新结果提取和历史回填现在使用同一套：

```text
CanonicalStem
下载文件名
ArtifactId
文件类别映射
可重启状态映射
```

避免两套命名和引用生成逻辑发生漂移。

### 回填服务

新增：

```text
IScientificArtifactBackfillService
ScientificArtifactBackfillService
```

回填规则：

```text
只处理 Artifacts 为空的科学点
读取科学点的 CalculationJobId
读取 CalculationJob
读取 CalculationResult
从 Artifacts 生成 PointArtifactReference
已有文件引用时不覆盖
只有实际发生回填时才保存 ScientificResult
```

因此可以：

```text
一次回填一个 ScientificResult
重复执行而不会重复添加引用
逐步迁移历史科学数据
```

### 回填 API

```text
POST /scientific-results/{resultId}/artifact-backfill
POST /scientific-artifacts/backfill
```

### 测试

新增：

```text
旧结果没有 Artifacts 时清单仍成功
旧点显示没有文件清单
从 CalculationResult 回填完整文件引用
回填名称符合 O2-original-m1.*
第二次回填不重复添加
```

验证结果：

```text
Release 构建：0 警告，0 错误
测试：93/93 通过
```

### 当前边界

回填依赖原有 CalculationJob 和 CalculationResult 仍然存在。

如果历史计算文件或结果已经删除，回填只能报告缺少文件清单。

---

## v0.48.0（2026-10-07）：生成叙述性文本

### 版本

- 当前版本：`0.48.0`
- 日期：2026-10-07
- 版本类型：功能新增

### 改动目的

完成阶段 6“生成叙述性文本”。

科学文本只从 `ScientificResult` 生成，并明确引用 `PointId`。
关系和物理量通过标识追溯到计算点。

### 新增服务

```text
IScientificNarrativeBuilder
DefaultScientificNarrativeBuilder

IScientificNarrativeService
ScientificNarrativeService
```

实现位置：

```text
ChemSculptor.ScientificSummary
```

### 生成内容

服务生成：

```text
ScientificPointNarrative.PointSummary
  单个科学点说明

ScientificPointNarrative.Provenance
  点的来源、父点、作业号和矫正次数

ScientificNarrativePackage.Organization
  点、关系和物理量的组织方式

ScientificNarrativePackage.FinalSummary
  最终科学摘要
```

`conversation.txt` 不由科学数据生成。
它由客户端作为会话记录保存，不作为科学结论。

### 追溯规则

```text
每个点文本包含 PointId
每个关系包含 RelationId、FromPointId 和 ToPointId
每个物理量包含 ObservableId、PointIds 和 RelationIds
最终摘要引用接受点和物理量依赖点
```

### 新增端点

```text
GET /scientific-results/{resultId}/narrative
GET /calculations/{jobId}/narrative
```

### 客户端成果包

WinForms 保存按钮现在：

```text
读取 artifact-manifest
读取 scientific-results/{resultId}/narrative
按点目录写 point-summary.txt
按点目录写 provenance.txt
写真实 relations.json
写真实 observables.json
写 narrative/final-summary.txt
写 narrative/organization.txt
写客户端 conversation.txt
```

原先的 relations.json 和 observables.json 空数组占位已移除。

### 测试

新增：

```text
叙述文本全部引用 PointId
provenance 记录 ParentPointId 和 CorrectionCount
organization 记录关系和物理量标识
final-summary 引用接受点和物理量依赖点
```

验证结果：

```text
Release 构建：0 警告，0 错误
测试：91/91 通过
```

### 当前边界

尚未通过实际 GUI 点击完成端到端成果包保存验证。

---

## v0.47.0（2026-10-07）：重写 WinForms 成果包保存

### 版本

- 当前版本：`0.47.0`
- 日期：2026-10-07
- 版本类型：功能重写

### 改动目的

完成阶段 5“重写 WinForms‘保存’按钮”。

客户端改为按成果包清单下载全部科学点文件，
并使用点目录组织修正前后文件。

### 保存目录

```text
scientific-result-<rootJobId>/
  points/
    01-original-m1/
    02-recovery-m3/
  relations.json
  observables.json
  artifact-manifest.json
  narrative/
    final-summary.txt
    conversation.txt
    organization.txt
```

阶段 6 在此基础上增加：

```text
point-summary.txt
provenance.txt
真实 relations.json
真实 observables.json
```

### 文件下载

客户端遍历清单中的全部科学点：

```text
原始点
恢复点
未来其它派生点
```

每个文件通过 `DownloadPath` 下载，并按
`DownloadFileName` 保存。

因此修正前后的文件都保留，输入、fchk 和输出
共享各自点的基本文件名。

### 验证

```text
Release 构建：0 警告，0 错误
测试：90/90 通过
```

---

## v0.46.0（2026-10-07）：增加成果包 API

### 版本

- 当前版本：`0.46.0`
- 日期：2026-10-07
- 版本类型：功能新增

### 改动目的

完成阶段 4“增加成果包 API”。

客户端可以通过一个清单获取全部科学点的文件信息，
再按科学点和文件标识下载单个文件。

### 新增端点

```text
GET /scientific-results/{resultId}/artifact-manifest
GET /scientific-results/{resultId}/artifacts/{pointId}/{artifactId}
GET /calculations/{jobId}/artifact-manifest
```

### 清单结构

```text
ResultId
RootJobId
Points[]
  PointId
  Sequence
  DirectoryName
  Status
  Multiplicity
  Files[]
    ArtifactId
    Kind
    DownloadFileName
    Length
    Sha256
    IsAvailable
    Error
    DownloadPath
```

`DirectoryName` 使用：

```text
<Sequence>-<Role>-m<Multiplicity>
```

当前角色：

```text
original
  原始点

recovery
  派生修正点
```

示例：

```text
01-original-m1
02-recovery-m3
```

### 按作业查询

`GET /calculations/{jobId}/artifact-manifest` 会先查找该作业
所属的科学成果。

查找支持：

```text
根工作流作业
科学成果中任意计算点的作业
恢复或派生作业
```

### 下载

清单中的 `DownloadPath` 指向：

```text
GET /scientific-results/{resultId}/artifacts/{pointId}/{artifactId}
```

下载端点复用阶段 3 的解析服务，因此仍然执行：

```text
run 目录限制
路径越界检查
链接检查
文件存在性检查
SHA-256 校验
```

### 旧端点兼容

原有端点未修改：

```text
GET /calculations/{jobId}/artifacts
GET /calculations/{jobId}/artifacts/{fileName}
```

### 测试

新增 API 合同测试：

```text
原始点和恢复点都进入清单
DirectoryName 和 Status 正确
Multiplicity 正确
统一文件命名正确
DownloadPath 正确
恢复作业可以定位所属科学成果
```

验证结果：

```text
Release 构建：0 警告，0 错误
测试：90/90 通过
```

### 当前边界

本阶段不修改 WinForms。

客户端保存完整成果包尚未实现。

---

## v0.45.0（2026-10-07）：建立科学点文件解析服务

### 版本

- 当前版本：`0.45.0`
- 日期：2026-10-07
- 版本类型：功能新增

### 改动目的

完成阶段 3“建立科学点文件解析服务”。

把科学成果中的相对文件引用解析为服务器上的真实文件，
同时保证客户端不能利用相对路径访问任意服务器目录。

### 新增接口

```text
IScientificArtifactResolver
  ResolveManifestAsync(resultId)
  OpenArtifactAsync(resultId, pointId, artifactId)
```

接口位于：

```text
ChemSculptor.ScientificData.Extraction.ArtifactResolution
```

### 清单解析

`ScientificArtifactResolver` 执行：

```text
读取 ScientificResult
遍历 PointSet.Points
验证科学点和文件引用的 CalculationJobId
解析对应作业的 run 目录
检查文件是否存在
计算并验证 SHA-256
生成下载文件名清单
```

解析结果逐文件报告：

```text
IsAvailable
IsSha256Valid
ExpectedSha256
ActualSha256
Error
```

### 单文件打开

`OpenArtifactAsync` 在打开文件前重新执行：

```text
成果、科学点和文件引用查询
CalculationJobId 一致性校验
run 目录定位
相对路径越界检查
文件系统链接检查
文件存在性检查
SHA-256 校验
```

校验通过后，返回从文件起点开始的可读流。

### 路径安全

服务不直接拼接客户端提供的路径，而是：

```text
先通过 ICalculationWorkspace.GetRunDirectory(jobId) 确定运行目录
再规范化相对路径
再确认最终路径位于运行目录之内
```

以下情况被拒绝：

```text
绝对路径
规范化后越出 run 目录的路径
路径中包含文件系统链接
文件引用的 CalculationJobId 与科学点不一致
未标记 CanDownload 的文件
```

### 部分缺失

一个文件缺失时，清单整体仍然成功：

```text
缺失文件标记 IsAvailable = false
缺失文件给出明确错误
其它存在的文件继续解析
其它文件仍可通过 OpenArtifactAsync 打开
```

### 测试

新增测试：

```text
完整文件解析、SHA-256 校验和打开
缺失 fchk 不影响 gjf 和 log
拒绝 run 目录之外的相对路径
拒绝文件引用与科学点 CalculationJobId 不一致
```

验证结果：

```text
Release 构建：0 警告，0 错误
测试：88/88 通过
```

### 当前边界

本阶段不修改：

```text
计算层
工作流
Api
WinForms
```

完整成果包清单 API 和客户端成果包下载尚未实现。

---

## v0.44.0（2026-10-07）：提取器填充完整文件引用

### 版本

- 当前版本：`0.44.0`
- 日期：2026-10-07
- 版本类型：功能完善

### 改动目的

完成阶段 2“提取器填充文件引用”。

让科学结果提取器从计算作业和 `CalculationResult` 中复制文件清单，
并把清单分别挂到原始点和恢复点上。

### 提取规则

```text
原始点
  ← 原始作业对应的 CalculationResult.Artifacts

恢复点
  ← 恢复作业对应的 CalculationResult.Artifacts
```

每个科学点设置自己的 `CalculationJobId`。

每项 `PointArtifactReference` 也记录产生该文件的
`CalculationJobId`。

### 文件名

提取器继续生成规范逻辑文件名：

```text
O2-original-m1.gjf
O2-original-m1.fchk
O2-original-m1.log

O2-recovery-m3.gjf
O2-recovery-m3.fchk
O2-recovery-m3.log
```

文件引用保存：

```text
CanonicalStem
CanonicalExtension
DownloadFileName
RelativePath
```

### 文件处理边界

提取器只把计算结果的 `Artifacts` 转换为引用。

本阶段不复制文件，不创建目标文件，也不修改原始计算目录。

### 测试

新增独立验收测试：

```text
原始点文件引用清单完整
恢复点文件引用清单完整
原始点和恢复点分别使用自己的 CalculationJobId
每项文件引用的 CanonicalStem 和 CanonicalExtension 正确
restart 状态文件正确标记 CanUseForRestart
模拟原始和恢复目录的文件集合与内容保持不变
```

验证结果：

```text
Release 构建：0 警告，0 错误
测试：84/84 通过
```

### 当前边界

本阶段不修改：

```text
计算层
工作流
Api
WinForms
```

完整成果包生成和下载仍未实现。

---

## v0.43.0（2026-10-06）：完成科学点文件引用模型

### 版本

- 当前版本：`0.43.0`
- 日期：2026-10-06
- 版本类型：功能完善

### 改动目的

完成阶段 1“扩展科学点模型”。

目标是让每个科学点的原始文件引用能够独立描述规范文件名，
同时保证已经保存的旧科学成果 JSON 仍然可以读取。

### 模型改动

`PointArtifactReference` 增加：

```text
CanonicalStem
CanonicalExtension
```

字段含义：

```text
CanonicalStem
  不包含扩展名的规范文件名主体

CanonicalExtension
  规范文件扩展名，包含前导点
```

正常情况下满足：

```text
DownloadFileName = CanonicalStem + CanonicalExtension
```

如果同一科学点内产生相同扩展名冲突，提取器会在
`CanonicalStem` 末尾加入数字后缀，避免下载文件名冲突。

### 提取器改动

`ScientificResultExtractor` 创建文件引用时现在同时填写：

```text
DownloadFileName
CanonicalStem
CanonicalExtension
```

氧气恢复点示例：

```text
CanonicalStem      = O2-recovery-m3
CanonicalExtension = .gjf
DownloadFileName   = O2-recovery-m3.gjf
```

### 兼容性

旧的科学成果 JSON 不含阶段 1 新增字段。

读取后：

```text
CalculationJobId 为空字符串
ProgramData 使用默认对象
Artifacts 使用空列表
```

因此旧数据不需要迁移即可继续读取。

### 测试

新增：

```text
PointArtifactReference JSON 往返测试
旧版 ScientificResult JSON 兼容读取测试
CanonicalStem 和 CanonicalExtension 提取结果断言
```

验证结果：

```text
Release 构建：0 警告，0 错误
测试：83/83 通过
```

### 当前边界

本阶段只扩展科学点模型和提取结果，不修改：

```text
计算层
工作流
Api
WinForms
```

完整成果包生成和下载仍未实现。

---

## v0.42.0（2026-10-06）：科学点程序数据与原始文件引用

### 版本

- 当前版本：`0.42.0`
- 日期：2026-10-06
- 版本类型：功能新增

### 改动目的

让科学数据仓库同时保存通用科学数据和程序专门数据。
每个科学点现在可以标识自己的计算作业，并保存该点对应的原始文件引用。

### 通用与专门数据

新增：

```text
PointProgramData
  ProgramCode
  ProgramVersion
  InputFormat
  SchemaVersion
  Values
```

`PointCalculationModel` 增加：

```text
ProgramVersion
```

程序专门字段保存在 `PointProgramData.Values`，通用模型不解释具体程序语法。

### 科学点原始文件

新增：

```text
PointArtifactReference
  ArtifactId
  CalculationJobId
  Kind
  RelativePath
  DownloadFileName
  MediaType
  Length
  Sha256
  CanDownload
  CanUseForRestart
  Metadata
```

`CalculationPoint` 增加：

```text
CalculationJobId
ProgramData
Artifacts
```

### 文件命名

科学点提取时生成统一逻辑文件名：

```text
O2-original-m1.gjf
O2-original-m1.log
O2-original-m1.fchk

O2-recovery-m3.gjf
O2-recovery-m3.log
O2-recovery-m3.fchk
```

原始计算目录中的 `output.log` 等文件不重命名；逻辑文件名仅用于科学点标识和后续下载。

### Gaussian 原始文件

Gaussian 适配器现在声明以下产物：

```text
output.log
*.gjf
*.fchk
*.chk
stdout.log
stderr.log
```

### 验证

- Release 全解决方案构建：0 警告，0 错误
- 测试：81/81 通过
- 提取器测试验证：

```text
原始点和恢复点具有独立 CalculationJobId
程序专门数据写入 PointProgramData
原始点和恢复点分别保存文件引用
O2-original-m1 和 O2-recovery-m3 文件命名正确
```

### 当前边界

尚未实现：

```text
成果包清单 API
按科学点下载原始文件
WinForms 保存完整科学点成果包
对话文本和点摘要文本文件
历史科学数据回填
```

---

## v0.41.0（2026-10-05）：科学计算点数据结构

### 版本

- 当前版本：`0.41.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

建立“计算点”作为科学数据的基本单位。一个计算点表示确定几何、电子态和计算
模型下的一个科学数据单位。工作流以后产生或更新计算点，后续科研工作只消费
已经接受的计算点。

### 新增项目

```text
ChemSculptor.ScientificData
```

该项目当前只包含数据结构，不包含工作流、计算程序适配器或任何氧气实例数据。

### 核心结构

```text
CalculationPoint
  一个计算点

CalculationPointSet
  一组计算点及其关系

CalculationPointRelation
  两个计算点之间的关系

ScientificObservable
  由计算点集合导出的物理量

ScientificResult
  由点集合、点关系和导出物理量组成的科学成果
```

### 计算点的内容

```text
几何结构
组合体系中的组分
电子态
计算模型
能量、梯度、频率或其它性质
验证记录
来源和接受过程
```

`CalculationPointStatus` 明确区分：

```text
Candidate
Accepted
Rejected
Superseded
```

派生修正作业以后可以生成一个 `Accepted` 点，并在
`PointProvenance` 中记录：

```text
InitialJobId
AcceptedJobId
RootWorkflowId
ParentPointId
CorrectionCount
AcceptanceSummary
```

### 设计原则

```text
工作流负责生成、验证和修正计算点
计算点承载可复用的科学数据
点关系描述路径、组分、反应物和产物
导出物理量由点集计算得到
最终报告引用已经接受的点
```

### 当前边界

本次只建立数据结构。以下内容尚未实现：

```text
氧气修正点实例
计算点到文件仓储的写入
现有工作流自动生成计算点
从点集计算解离能或光谱
报告生成
```

### 科学数据文件仓储

新增：

```text
IScientificDataRepository
ScientificDataRepositoryOptions
FileScientificDataRepository
```

每项 `ScientificResult` 保存为一个独立 JSON 文件：

```text
<scientific-data-root>/
  <resultId>.json
```

仓储只认识科学数据模型，不读取作业、程序输出或异常记录。以后报告层只通过
`IScientificDataRepository` 读取材料。

当前已实现：

```text
保存科学成果
按标识读取科学成果
列出全部科学成果
JSON 字符串枚举持久化
```

### 计算层到科学数据提取

新增项目：

```text
ChemSculptor.ScientificData.Extraction
```

该项目负责把计算作业、通用计算结果、计算验证、稳定性检查、修正计划和派生
作业翻译为：

```text
ScientificResult
  CalculationPoint
  CalculationPointRelation
  ScientificObservable
```

核心服务：

```text
IScientificResultExtractor
ScientificResultExtractor

IScientificDataRecorder
ScientificDataRecorder
```

依赖方向：

```text
计算和异常处理
  -> ScientificData.Extraction
  -> ScientificData
  -> 报告层
```

`ChemSculptor.ScientificData` 仍然不依赖计算程序。报告层以后只读取
`IScientificDataRepository`。

### 工作流接入

单点工作流末尾新增：

```text
science-data-record
```

该节点使用：

```text
science.record-calculation-result
```

它读取：

```text
原始输入请求
原始计算结果
原始结果验证
原始稳定性检查
修正计划
派生作业执行结果
派生稳定性复检
```

然后生成并保存：

```text
ScientificResult
  CalculationPoint
  CalculationPointRelation
  ScientificObservable
```

工作流节点顺序：

```text
plan
  → science-data-record
```

今后报告生成不再读取计算作业或程序输出，只读取科学数据仓储。

### 客户端摘要框架

新增项目：

```text
ChemSculptor.ScientificSummary
```

该项目只引用：

```text
ChemSculptor.ScientificData
```

框架包含：

```text
ClientScientificSummary
ClientScientificSummarySection

IClientSummaryBuilder
IClientSummarySender
IClientSummaryService

ClientSummaryService
```

`ClientSummaryService` 的流程固定为：

```text
从 IScientificDataRepository 读取 ScientificResult
  → IClientSummaryBuilder 生成客户端摘要
  → IClientSummarySender 发送给客户端
```

当前不包含具体摘要文字、报告模板或网络发送实现。

### Api 客户端摘要接口

Api 现在依赖 `ChemSculptor.ScientificSummary`，并实现：

```text
DefaultClientSummaryBuilder
ApiClientSummarySender
ClientSummaryEndpoints
```

新增端点：

```text
GET  /scientific-results/{resultId}/client-summary
POST /scientific-results/{resultId}/client-summary/send/{clientId}
GET  /clients/{clientId}/scientific-summaries
GET  /calculations/{jobId}/client-summary
POST /calculations/{jobId}/client-summary/send/{clientId}
```

流程：

```text
ScientificData仓储
  → ScientificSummary
  → Api
  → 客户端
```

WinForms 在计算作业进入终态后，会按作业号请求客户端摘要，并把服务器生成的
科学摘要显示在对话区。客户端不读取科学数据文件。

### 配套教程

新增端到端教程：

```text
docs/Oxygen-End-to-End-Tutorial.md
```

该教程以真实氧气计算为例，按完整运行顺序说明：

```text
客户端提交
单点工作流
波函数稳定性检查
自旋多重度矫正
派生稳定性复检
科学数据提取与仓储
客户端摘要生成和领取
```

教程同时给出源码阅读顺序、API 端点、真实示例数据和运行命令。

新增：

```text
docs/ScientificData-Tutorial.md
```

教程按照以下顺序说明新项目：

```text
为什么需要计算点
各数据结构的作用
原始点和修正点的关系
点的验证与接受
解离能和光谱如何复用点
工作流以后如何接入科学数据
```

---

## v0.40.0（2026-10-05）：派生作业稳定性复检

### 版本

- 当前版本：`0.40.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

自旋多重度修正单点计算完成后，不能只根据“程序正常结束”判断修正有效。
必须对派生作业重新执行波函数稳定性检查，确认修正后的参考态是否稳定。

### 工作流变化

单点工作流在派生执行后增加复检节点：

```text
recovery-execution
  → recovery-stability-check
  → plan
```

复检节点继续使用已有的通用 Skill：

```text
anomaly.check-wavefunction-stability
```

没有新增一套重复的稳定性检查代码。

### 通用 Skill 的新输入

`WavefunctionStabilityCheckSkill` 现在支持三种输入来源：

```text
request
  显式构造的通用异常检查请求

validation
  原始单点计算工作流中的验证结果

recoveryExecution
  派生作业执行结果
```

收到 `recoveryExecution` 后，通用 Skill 从
`RecoveryJobExecutionResult.RecoveryJob` 读取派生作业，再按计算程序选择
对应的专用稳定性检查实现。

### 派生复检过程

```text
读取派生作业和派生结果
  ↓
复制派生作业的 .chk
  ↓
保留派生计算的方法、基组和多重度
  ↓
增加 guess=read geom=check stable
  ↓
创建并执行复检辅助作业
  ↓
解析复检结果
  ↓
为派生作业保存新的 AnomalyRecord
```

复检作业是独立作业，不会覆盖派生单点计算，也不会覆盖原始异常记录。

### 结果语义

```text
复检 Passed
  → 派生自旋多重度对应的波函数稳定

复检 Finding
  → 修正后仍存在稳定性问题

复检 Skipped
  → 当前计算模型不支持稳定性检查
```

复检通过目前只写入派生作业自己的异常记录。把该结果关联回原始异常记录，
并标记 `Resolved`，属于下一阶段的恢复结果评估。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：77/77 通过
- 真实 O2 默认单点计算：

```text
原始自旋多重度：1
原始稳定性：RHF → UHF 不稳定
修正后自旋多重度：3
派生单点能量：-150.274273534 Hartree
派生复检状态：Passed
复检摘要：Gaussian 报告当前波函数稳定
复检证据：The wavefunction is stable under the perturbations considered
复检辅助作业：job-aed554bf59f946229f141f8551c567c6-stability-1719d602
```

---

## v0.39.0（2026-10-05）：直接执行自旋多重度修正

### 版本

- 当前版本：`0.39.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

让已经创建的自旋多重度修正派生作业真正进入计算，而不是只停留在
“已准备、未执行”状态。对于默认基态任务，修正规划器已经给出预授权，
因此可以直接提交修改后的单点计算。

### 工作流变化

单点计算工作流新增：

```text
recovery-job
  → recovery-execution
  → plan
```

新增通用 Skill：

```text
anomaly.execute-recovery-job
```

这个 Skill 只读取通用修正意图和派生作业，不包含 Gaussian 专用规则。
具体输入格式、执行上下文和输出解析仍由计算程序适配器负责。

### 直接执行条件

当前只对同时满足以下条件的方案直接执行：

```text
修正意图：spin-multiplicity.change
RequiresApproval：false
派生作业已成功创建
存在可处理该计算方案的程序适配器
```

不满足条件时，执行节点返回结构化说明并停止本次执行，不伪造计算结果。

### 执行过程

```text
读取 correctionPlan
读取 recoveryJob
检查修正意图和预授权
构建程序执行上下文
保存 Running 状态
提交到 IComputeBackend
轮询直到 Completed、Failed 或 Canceled
执行程序专用后处理
解析通用 CalculationResult
保存派生作业和派生结果
把异常记录状态更新为 Recovering
```

### 结果语义

派生进程结束不等于计算成功。

```text
Completed
  → 继续解析输出
  → NormalTermination = true
       派生执行成功
  → NormalTermination = false
       派生执行未成功，但保存输出供诊断

Failed 或 Canceled
  → 不解析为成功结果
```

原始作业和派生作业的所有目录、输入、输出和结果继续分开保存。

### 教学要点

`RecoveryJobCreationSkill` 解决“准备什么输入”；
`RecoveryJobExecutionSkill` 解决“什么时候提交、等待和保存结果”。

把两者分开，可以以后增加审批流程，而不修改提交和等待代码。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：76/76 通过
- 新增测试验证：

```text
预授权的 spin-multiplicity.change
  → 提交派生作业
  → 等待 Completed
  → 保存作业和通用结果
  → 异常记录进入 Recovering
```

### 当前边界

以下能力仍未实现：

```text
wavefunction.optimize 的自动派生执行
人工审批后的执行恢复
比较原始结果和派生结果
确认原异常是否真正解决
```

---

## v0.38.0（2026-10-05）：默认基态与显式电子态目标

### 版本

- 当前版本：`0.38.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

默认把一般单点任务解释为寻找基态，避免同一自旋态内的稳定性修正频繁暂停。
当用户明确指定特定自旋态或激发态时，系统记录目标并限制自动修改范围。

### 通用模型

新增：

```text
ElectronicStateObjective
```

取值：

```text
Unknown
GroundState
TargetSpinState
TargetExcitedState
```

`CalculationSpec`、`CalculationRequest`、`InterpretedTask` 和
`ConversationIntent` 增加：

```text
ElectronicStateObjective
TargetMultiplicity
TargetStateLabel
```

### 会话解析

默认：

```text
单点计算
  → GroundState
```

明确指定自旋态：

```text
三重态单点计算
  → TargetSpinState
  → TargetMultiplicity = 3
```

明确指定激发态：

```text
S1 激发态单点计算
  → TargetExcitedState
  → TargetStateLabel = S1
```

当前阶段只登记激发态目标，Agent 返回“尚未实现激发态计算”。

### 修正规划

```text
GroundState
  波函数优化或切换更低自旋态
  自动预授权，不暂停

TargetSpinState
  只允许同一自旋态内修正
  不同自旋态方案不自动生成

TargetExcitedState
  不自动切换参考态
```

自动预授权的计划写入 `AnomalyRecord` 后，状态为：

```text
ReadyForRecovery
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：74/74 通过
- 默认 O2 单点计算：

```text
目标：GroundState
最低本征值电子态：Triplet
修正意图：spin-multiplicity.change
RequiresApproval：false
多重度：1 → 3
异常记录状态：ReadyForRecovery
```

- 指定单重态 O2：

```text
目标：TargetSpinState
目标多重度：1
稳定性检查：Finding
PlanCreated：false
说明：当前任务指定了特定自旋态，不能自动修改自旋多重度
```

### 当前边界

尚未实现：

```text
TD-DFT 等激发态计算
激发态根跟踪
修正方案到具体程序输入的翻译
自动创建修正重算作业
```

---

## v0.37.0（2026-10-05）：波函数稳定性修正方案

### 版本

- 当前版本：`0.37.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

从 Gaussian 稳定性矩阵中选择能量下降最大的本征值，并生成通用修正方案。

### 通用模型

新增：

```text
SpinMultiplicityNames
WavefunctionStabilityTransition
WavefunctionStabilityEigenvector
CorrectionIntentCodes
WavefunctionStabilityCorrectionPlanningResult
```

`WavefunctionStabilityResult` 增加：

```text
CurrentMultiplicity
Eigenvectors
```

### Gaussian 解析

`GaussianWavefunctionStabilityParser` 现在解析：

```text
Eigenvector 编号
电子态名称
自旋多重度
Eigenvalue
S**2
轨道跃迁分量
```

支持：

```text
Singlet
Doublet
Triplet
Quartet
Quintet
Sextet
Septet
Octet
```

### 通用规划器

新增：

```text
WavefunctionStabilityCorrectionPlanner
```

策略：

```text
选择数值最小的 Eigenvalue
  ↓
取得对应电子态的自旋多重度
  ↓
与当前自旋多重度比较
  ├── 相同 → wavefunction.optimize
  └── 不同 → spin-multiplicity.change
```

### Skill 和工作流

新增通用 Skill：

```text
anomaly.plan-wavefunction-stability-correction
```

单点工作流增加节点：

```text
validate
  ↓
stability-check
  ↓
stability-correction-plan
  ↓
plan
```

修正计划写入 `AnomalyRecord`，状态变为 `AwaitingApproval`。

### 测试

新增测试覆盖：

```text
稳定性矩阵本征向量解析
电子态名称到自旋多重度映射
选择最低本征值
相同多重度生成波函数优化方案
不同多重度生成修改自旋多重度方案
没有负本征值时不生成降低能量方案
工作流包含 stability-correction-plan 节点
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：69/69 通过
- 真实 O2 单点计算：

```text
稳定性本征向量数量：6
最低本征值电子态：Triplet
修正意图：spin-multiplicity.change
原自旋多重度：1
目标自旋多重度：3
异常记录状态：AwaitingApproval
```

### 当前边界

当前只生成修正方案，尚未：

```text
人工审批
把通用修正方案翻译为 Gaussian 输入
创建派生修正作业
执行修正重算
```

---

## v0.36.0（2026-10-05）：默认 SCF 迭代上限 200

### 版本

- 当前版本：`0.36.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

为通用计算方案增加默认 SCF 迭代上限，并由具体程序适配器翻译为程序关键词。

### 通用默认值

新增：

```text
CalculationOptionKeys.ScfIterationLimit
```

默认值：

```text
200
```

`CalculationDefaults` 在创建默认单点方案时写入：

```text
scf-iteration-limit = 200
```

同时新增计算参数：

```text
SCF 迭代上限 = 200
```

### Gaussian 翻译

`GaussianInputWriter` 将通用参数翻译为：

```text
scfcyc=200
```

普通单点路线：

```text
#p CAM-B3LYP/6-31G* SP scfcyc=200
```

稳定性检查保留该上限：

```text
#p CAM-B3LYP/6-31G* scfcyc=200 guess=read geom=check stable
```

### 语义说明

Gaussian 的 `scfcyc` 表示 SCF 最大迭代次数，不表示强制至少迭代 200 次。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：65/65 通过
- 真实 O2 单点计算：

```text
主计算路线：#p CAM-B3LYP/6-31G* SP scfcyc=200
稳定性路线：#p CAM-B3LYP/6-31G* scfcyc=200 guess=read geom=check stable
稳定性结果：Finding
不稳定类型：RHF-to-UHF
```

---

## v0.35.0（2026-10-05）：稳定性辅助计算

### 版本

- 当前版本：`0.35.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

让 Gaussian 波函数稳定性检查不仅解析已有输出，还能从原始单点输入生成并执行
辅助稳定性检查作业。

### 新增输入写入器

```text
GaussianWavefunctionStabilityInputWriter
GaussianWavefunctionStabilityInputWriteResult
```

处理过程：

```text
读取原始 Gaussian 输入
  ↓
检查是否为不支持的 ONIOM 输入
  ↓
替换 %chk 为派生作业检查点
  ↓
复制原始 .chk，保留原方法和基组，并在路线中增加
guess=read geom=check stable
  ↓
写入新的稳定性检查输入
```

### 专用 Skill

`GaussianWavefunctionStabilityCheckSkill` 现在：

```text
已有稳定性输出
  → 直接解析

没有稳定性输出
  → 创建派生作业
  → 复制原始 .chk
  → 生成无坐标的稳定性检查输入
  → 提交到 IComputeBackend
  → 等待完成
  → 解析输出
  → 返回 AnomalyCheckResult
```

返回结果中通过：

```text
AuxiliaryJobId
```

记录稳定性检查派生的作业。

### ONIOM

原始 Gaussian 输入路线中包含 ONIOM 时，专用 Skill 返回：

```text
Status = Skipped
SkippedReason = 当前 ONIOM 输入不支持波函数稳定性检查
```

工作流节点仍然存在，但不会尝试执行稳定性检查。

### 依赖注入

新增：

```text
GaussianWavefunctionStabilityInputWriter
```

并注册到 Gaussian 服务集合。

### 测试

新增测试覆盖：

```text
没有已有输出时执行派生稳定性检查
派生输入使用 guess=read geom=check stable
派生输入不再重复包含坐标
原始 .chk 复制到派生检查目录
辅助作业标识写入 AnomalyCheckResult
ONIOM 输入返回 Skipped
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：65/65 通过

### 工作流接入

单点工作流现在固定为：

```text
validate
  ↓
stability-check
  ↓
plan
```

通用稳定性检查 Skill 会把 `AnomalyCheckResult` 保存为 `AnomalyRecord`。

### 真实端到端验证

中性 O2 单点计算已通过完整流程：

```text
主单点计算：Validated
稳定性检查：Finding
异常代码：wavefunction-instability
不稳定类型：RHF-to-UHF
辅助作业：正常终结
异常记录：已写入
```

### 当前边界

尚未实现：

```text
异常诊断 Skill
修正方案 Skill
审批和修正重算
案例记忆
```

---

## v0.34.0（2026-10-05）：波函数稳定性检查双 Skill

### 版本

- 当前版本：`0.34.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

按照“通用 Skill + 专用 Skill”原则实现波函数稳定性检查：

```text
通用 Skill
  anomaly.check-wavefunction-stability

Gaussian 专用 Skill
  gaussian.wavefunction-stability.check
```

### 通用 Skill

新增：

```text
ChemSculptor.Skills.Common.AnomalyWorkflow.WavefunctionStabilityCheckSkill
```

职责：

```text
接收通用 AnomalyCheckRequest
通过 IAnomalyProviderRegistry 查找适用实现
选择 Descriptor.Code = wavefunction-stability 的检查
返回统一 AnomalyCheckResult
没有实现时返回 Skipped 和原因
```

### Gaussian 专用 Skill

新增：

```text
ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability
  .GaussianWavefunctionStabilityCheckSkill
```

职责：

```text
判断当前计算程序是否为 Gaussian 16
读取波函数稳定性检查输出路径
调用 GaussianWavefunctionStabilityParser
返回通用 AnomalyCheckResult
```

专用 Skill 名称：

```text
gaussian.wavefunction-stability.check
```

能力包含：

```text
anomaly.check-wavefunction-stability
gaussian.wavefunction-stability
```

### 检查标识

`AnomalyCheckDescriptor` 增加：

```text
ImplementationId
Program
```

Gaussian 实现：

```text
Code = wavefunction-stability
ImplementationId = gaussian.wavefunction-stability.check
Program = Gaussian 16
```

`AnomalyProviderRegistry` 使用 `ImplementationId` 区分不同程序实现，
因此后续 ORCA 等程序可以注册同一通用检查代码。

### 接入

- `ChemSculptor.Api` 注册 `IAnomalyProviderRegistry`
- 启动时把全部 `IAnomalyCheck` 注册到异常提供器注册表
- 通用 Skill 和 Gaussian 专用 Skill 都登记到依赖注入

### 测试

新增测试覆盖：

```text
通用 Skill 选择 Gaussian 专用 Skill
通用 Skill 调用专用 Skill 后返回 Finding
返回结果包含 gaussian.wavefunction-stability.check
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：61/61 通过

### 当前边界

当前专用 Skill 只处理已经生成的稳定性输出，尚不创建辅助计算作业。
单点计算工作流也尚未加入该通用检查节点。

---

## v0.33.0（2026-10-05）：统一异常检查契约

### 版本

- 当前版本：`0.33.0`
- 日期：2026-10-05
- 版本类型：架构与功能调整

### 改动目的

把原 `IAnomalyDetector` 和只返回异常列表的检测方式提升为统一的
`IAnomalyCheck`：

```text
执行检查
  ↓
返回 AnomalyCheckResult
```

这样检查可以通过、发现异常、跳过、无法判断或执行失败。

### 新增接口

```text
IAnomalyCheck
```

包含：

```text
Descriptor
CanCheck
CheckAsync
```

检查状态增加：

```text
Inconclusive
```

当前状态为：

```text
NotRun
Passed
Finding
Inconclusive
Skipped
ExecutionFailed
Canceled
```

### Gaussian 稳定性检查

`GaussianWavefunctionStabilityDetector` 已重构为：

```text
GaussianWavefunctionStabilityCheck
```

它现在返回：

```text
Stable
  AnomalyCheckStatus.Passed

Unstable
  AnomalyCheckStatus.Finding
  包含 wavefunction-instability

缺少输出
  AnomalyCheckStatus.Skipped
  记录跳过原因

无法判断
  AnomalyCheckStatus.Inconclusive
```

原有：

```text
GaussianWavefunctionStabilityParser
```

继续负责把 Gaussian 输出翻译为通用稳定性结果。

### 注册表

异常提供器注册表改为注册和列出：

```text
IAnomalyCheck
```

不再注册和列出 `IAnomalyDetector`。

### 测试

新增或更新测试覆盖：

```text
IAnomalyCheck 注册和重复检查
稳定输出返回 Passed
不稳定输出返回 Finding
缺少输出返回 Skipped
无法判断返回 Inconclusive
Finding 包含科学异常和证据
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：60/60 通过

### 当前边界

当前检查读取已经生成的 Gaussian 稳定性输出。尚未实现：

```text
创建稳定性检查辅助作业
通用 anomaly.check-wavefunction-stability Skill
Gaussian wavefunction-stability.check 专用 Skill
接入单点计算工作流
诊断、审批和修正重算
```

---

## v0.32.1（2026-10-05）：通用 Skill 与专用 Skill 配对原则

### 版本

- 当前版本：`0.32.1`
- 日期：2026-10-05
- 版本类型：架构约定

### 改动目的

确立永久原则：

```text
凡是需要调用具体计算程序的能力，
都必须同时具备通用 Skill 和专用 Skill。
```

### 职责

通用 Skill：

```text
由工作流引用
名称不含具体程序
提供稳定的通用契约
通过注册表选择具体程序实现
统一返回通用结果
```

专用 Skill：

```text
名称可以包含程序名
服从通用 Skill 契约
生成具体程序输入
调用具体计算程序
解析具体程序输出
翻译为通用结果
```

示例：

```text
通用：
  anomaly.check-wavefunction-stability

专用：
  gaussian.wavefunction-stability.check
```

工作流只能引用通用 Skill ID，不能引用专用 Skill ID。

### 当前状态

当前波函数稳定性检测器是过渡实现。后续重构时，应增加：

```text
anomaly.check-wavefunction-stability
gaussian.wavefunction-stability.check
```

并让专用 Skill 返回 `AnomalyCheckResult`，而不是只返回异常发现列表。

### 文档

更新：

```text
docs/Coding-Conventions.md
docs/Anomaly-Handling-Framework.md
```

本次只修改架构约定文档，不修改代码。

---

## v0.32.0（2026-10-05）：波函数稳定性检查处理

### 版本

- 当前版本：`0.32.0`
- 日期：2026-10-05
- 版本类型：功能新增

### 改动目的

建立波函数稳定性检查的第一段完整链路：

```text
读取 Gaussian 稳定性输出
  ↓
翻译为通用稳定性结果
  ↓
不稳定时生成科学异常发现
```

本阶段处理已经生成的稳定性检查输出，不负责创建辅助计算作业，也不自动修改多重度。

### 检查机制

新增：

```text
AnomalyCheckMechanism
```

取值：

```text
OutputArtifact
RuntimeSignal
AuxiliaryCalculation
```

波函数稳定性检查使用：

```text
AuxiliaryCalculation
```

### 通用模型

新增：

```text
WavefunctionStabilityStatus
WavefunctionStabilityResult
CommonAnomalyCodes
AnomalyContextKeys
```

状态包括：

```text
NotPerformed
Stable
Unstable
Inconclusive
```

不稳定异常代码：

```text
wavefunction-instability
```

### Gaussian 解析和检测

新增：

```text
GaussianWavefunctionStabilityParser
GaussianWavefunctionStabilityDetector
```

解析器负责把 Gaussian 输出翻译为通用结果。

检测器负责在不稳定时生成：

```text
Category = Scientific
Code = wavefunction-instability
Severity = Error
RequiresScientificJudgment = true
IsBlocking = true
```

解析器和检测器已经登记到 Gaussian 服务注册中，后续异常工作流可以从依赖注入
容器取得该检测器。

`IAnomalyDetector` 现在通过 `AnomalyCheckDescriptor` 声明检查代码、名称、分类和机制。

### 测试

新增测试覆盖：

```text
稳定波函数输出解析
不稳定波函数输出解析
没有明确结论时返回 Inconclusive
不稳定结果生成科学异常发现
检查机制为 AuxiliaryCalculation
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：57/57 通过

### 当前边界

尚未实现：

```text
创建 Gaussian 稳定性检查辅助作业
把稳定性检查节点接入单点计算工作流
诊断和修正方案
人工审批
派生重算
```

---

## v0.31.0（2026-10-04）：异常检查模型和存储

### 版本

- 当前版本：`0.31.0`
- 日期：2026-10-04
- 版本类型：功能新增

### 改动目的

完成异常处理第一阶段的基础模型和存储结构，为后续接入多项单点计算异常检查做准备。

### 新增模型

```text
AnomalyCheckStatus
AnomalyCheckDescriptor
AnomalyCheckResult
AnomalyRecordStatus
AnomalyRecord
CommonAnomalyCheckCodes
```

检查状态包括：

```text
NotRun
Passed
Finding
Skipped
ExecutionFailed
Canceled
```

波函数稳定性检查的通用代码为：

```text
wavefunction-stability
```

### 存储结构

新增：

```text
AnomalyStoragePaths
IAnomalyRepository
FileAnomalyRepository
```

每条异常记录写入：

```text
jobs/<jobId>/results/anomaly/<recordId>.json
```

记录中统一保存：

```text
异常检查
异常评估
诊断报告
修正计划
审批决定
恢复尝试
最终结果
```

### 跳过规则

所有单点计算流程都应安排波函数稳定性检查。当前计算模型或计算程序不支持该检查时，
流程节点仍然存在，但检查结果为 `Skipped`，并记录具体原因。

ONIOM 等暂不支持波函数稳定性检查的模型不会从工作流中删除该节点。

### 测试

新增测试覆盖：

```text
异常记录按作业保存和读取
跳过状态和原因可以落盘
可以列出同一作业的多个异常记录
非法异常记录标识不能形成路径穿越
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：53/53 通过

### 当前边界

本阶段只定义模型和存储，尚未：

```text
把异常检查节点接入单点计算工作流
实现波函数稳定性检查 Skill
实现 Gaussian 稳定性检查
实现诊断、审批和恢复执行
```

---

## v0.30.0（2026-10-04）：异常处理项目骨架

### 版本

- 当前版本：`0.30.0`
- 日期：2026-10-04
- 版本类型：架构项目

### 改动目的

异常处理会持续增加，因此先建立独立、通用、可扩展的项目骨架。主工作流暂时不接入，
后续每一种异常通过注册和 Skill 模块扩展。

### 新增项目

```text
src/ChemSculptor.Anomaly/
```

### 项目结构

```text
src/ChemSculptor.Anomaly/
├── ChemSculptor.Anomaly.csproj
├── Abstractions/
│   ├── IAnomalyDetector.cs
│   ├── IAnomalyDiagnoser.cs
│   ├── ICorrectionPlanner.cs
│   ├── IApprovalPolicy.cs
│   └── IRecoveryValidator.cs
├── Models/
│   ├── AnomalyCategory.cs
│   ├── AnomalySeverity.cs
│   ├── AnomalyDescriptor.cs
│   ├── AnomalyEvidence.cs
│   ├── AnomalyFinding.cs
│   ├── AnomalyAssessment.cs
│   ├── AnomalyContext.cs
│   ├── DiagnosisReport.cs
│   ├── CorrectionModels.cs
│   ├── ApprovalModels.cs
│   └── RecoveryModels.cs
└── Registry/
    ├── IAnomalyProviderRegistry.cs
    └── AnomalyProviderRegistry.cs
```

### 当前边界

项目当前只定义：

```text
执行异常与科学异常分类
异常证据和发现
异常评估
诊断报告
修正候选和计划
审批决定
恢复尝试和恢复结果
检测器、诊断器、修正规划器、审批策略和恢复验证器接口
异常提供器注册表
```

当前还没有：

```text
异常工作流 Skill
氧气波函数稳定性异常
Gaussian 稳定性检查
审批策略实现
派生重算实现
案例记忆
```

### 测试

新增注册表测试：

```text
异常检测器可以注册并列出
重复异常代码不会被静默覆盖
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：50/50 通过

### 文档

更新：

```text
docs/Anomaly-Handling-Framework.md
```

---

## v0.29.0（2026-10-04）：按电子数自动确定默认多重度

### 版本

- 当前版本：`0.29.0`
- 日期：2026-10-04
- 版本类型：功能新增

### 改动目的

不再把所有体系默认成单重态。解析出分子几何后，根据总电子数选择默认自旋多重度：

```text
总电子数 = 全部原子序数之和 - 总电荷

偶数电子 → 自旋多重度 1
奇数电子 → 自旋多重度 2
```

该规则只保证电子数奇偶性与多重度匹配，不判断分子真实基态。例如中性 O2 有 16 个
电子，因此仍会得到默认多重度 1，供后续波函数稳定性检查发现其科学问题。

### 实现

新增：

```text
ChemSculptor.InputProcessor/Chemistry/ElectronCountCalculator.cs
```

`ElectronCountCalculator` 根据原子列表和总电荷计算总电子数。

`CalculationDefaults` 的变化：

```text
创建方案时先将多重度标记为 0，表示尚未解析几何
坐标解析后根据总电子数写入默认多重度
参数来源更新为 System
用户显式指定的多重度不会被覆盖
```

调用位置：

```text
解析几何
  ↓
计算总电子数
  ↓
应用默认多重度
  ↓
生成计算程序输入文件
```

### 依赖

`ChemSculptor.InputProcessor` 现在引用：

```text
ChemSculptor.FundamentalConstants
```

用于通过元素符号查询原子序数。

### 测试

新增测试覆盖：

```text
中性 O2 为 16 个电子，默认多重度 1
O2+ 为 15 个电子，默认多重度 2
O2- 为 17 个电子，默认多重度 2
未知元素明确失败
用户显式指定的多重度不会被覆盖
默认规则会将参数来源标记为 System
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：48/48 通过
- 真实 O2 单点计算：

```text
Gaussian 输入：0 1
结果多重度：1
状态：Validated
能量：-150.210765371 Hartree
正常终结：true
```

### 文档

更新：

```text
docs/Calculation-Api-Reference.md
docs/SinglePoint-Calculation-Walkthrough.md
```

---

## v0.28.0（2026-10-04）：基本常数与元素数据项目

### 版本

- 当前版本：`0.28.0`
- 日期：2026-10-04
- 版本类型：功能新增

### 改动目的

把旧程序中的 `PhysConst`、`Atoms` 和 `Atom` 整理为可复用的基础数据项目，
同时区分物理常数、单位换算、元素数据和元素结构。

### 新增项目

```text
src/ChemSculptor.FundamentalConstants/
├── Physics/
│   └── PhysicalConstants.cs
├── Units/
│   └── UnitConversions.cs
└── Chemistry/
    └── Elements/
        ├── AtomicMassKind.cs
        ├── ChemicalElement.cs
        └── ElementCatalog.cs
```

包含：

```text
Physics.PhysicalConstants
Units.UnitConversions
Chemistry.Elements.ChemicalElement
Chemistry.Elements.ElementCatalog
Chemistry.Elements.AtomicMassKind
```

### 主要调整

```text
SI 定义常数使用当前定义值
其他物理常数采用 CODATA 2022 值
单位换算与物理常数分离
元素符号统一使用规范大小写
元素查询改为明确的 TryGet 形式
未知元素不再静默返回虚拟元素
元素质量继续保留旧程序迁移值
元素质量增加定义类型和来源字段
删除重复的原子序数索引字典，直接使用数组下标
```

### 元素数据范围

```text
0   X   Dummy
1   H
...
54  Xe
```

当前原子质量不是标准原子量全集，也不是同位素质量数据库。后续需要精确同位素质量时，
应建立独立的同位素数据模型。

### 测试

新增测试覆盖：

```text
物理常数关系
常用单位换算
元素符号查询
原子序数查询
未知元素处理
元素目录范围
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：42/42 通过

### 文档

新增：

```text
docs/Fundamental-Constants-Guide.md
```

---

## v0.27.2（2026-10-03）：异常处理框架文档

### 版本

- 当前版本：`0.27.2`
- 日期：2026-10-03
- 版本类型：设计文档

### 改动目的

在实现异常诊断与修正子工作流之前，先固定异常处理的结构、边界、审批原则、
重算血缘和扩展方式，避免后续每加入一种异常都修改主工作流。

### 新增内容

新增：

```text
docs/Anomaly-Handling-Framework.md
```

文档包含：

```text
执行异常与科学异常的分类
两条独立处理路线
核心模型
通用修正意图
审批策略
派生作业与血缘
预算和停止条件
通用技能规划
存储结构
新异常扩展步骤
分阶段实施建议
```

### 当前状态

本次只增加设计文档，不实现异常处理代码。

### 验证

- 本次不涉及代码构建和测试
- 未修改现有运行逻辑

---

## v0.27.1（2026-10-01）：WinForms 客户端模型分层

### 版本

- 当前版本：`0.27.1`
- 日期：2026-10-01
- 版本类型：结构调整

### 改动目的

把 WinForms 客户端模型从单个 `ClientModels.cs` 按用途拆成三个子目录，
让界面状态、请求协议和响应协议的边界在目录和命名空间中都清晰可见。

### 新结构

```text
src/ChemSculptor.WinForms/Models/
├── Ui/
│   └── UiModels.cs
├── Requests/
│   └── RequestModels.cs
└── Responses/
    └── ResponseModels.cs
```

对应命名空间：

```text
ChemSculptor.WinForms.Models.Ui
ChemSculptor.WinForms.Models.Requests
ChemSculptor.WinForms.Models.Responses
```

### 分类内容

```text
UiModels.cs
  ChatMessage
  ChatSession
  CalculationJobItem

RequestModels.cs
  AgentMessageRequestDto

ResponseModels.cs
  AgentMessageResultDto
  CalculationStatusDto
  CalculationResultDto
  CalculationValidationDto
  CalculationValidationCheckDto
  CalculationDiagnosticDto
  CalculationArtifactManifestDto
  CalculationArtifactFileDto
```

### 行为

- 字段和属性没有改变。
- HTTP 请求与响应格式没有改变。
- WinForms 界面和运行逻辑没有改变。
- `ClientModels.cs` 已删除，内容分别移入三个新文件。
- `MainForm.cs` 只增加三个命名空间引用。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：35/35 通过
- 新增教程：`docs/WinForms-Client-Models-Guide.md`

---

## v0.27.0（2026-10-01）：计算产物清单与结果备份

### 版本

- 当前版本：`0.27.0`
- 日期：2026-10-01
- 版本类型：功能新增

### 改动目的

单点计算结束后，服务器不仅需要返回能量和验证报告，还需要明确告诉客户端：

```text
这次计算由哪个程序完成
产生了哪些文件
每个文件是什么用途
哪些文件可以用于以后恢复或继续计算
```

这样，客户端“保存”按钮只需要下载服务器给出的清单，不需要理解某个计算程序的
文件扩展名，也不需要在将来为每种计算程序分别修改客户端。

### 结果模型

`CalculationResult` 新增：

```text
Artifacts
```

每一项是 `CalculationArtifactDescriptor`，主要字段：

```text
FileName
  文件名称

RelativePath
  相对于计算运行目录的路径

Kind
  产物用途

MediaType
  下载时使用的媒体类型

Length
  文件字节数

Sha256
  文件内容摘要

CanUseForRestart
  是否可以用于恢复或继续计算
```

产物用途使用通用枚举：

```text
Input
PrimaryOutput
SupportingOutput
RestartState
Other
```

这些名称不包含 Gaussian、ORCA 或其他程序名称。

### 适配器职责

`IQuantumProgramAdapter` 的产物接口由简单的文件模式改为：

```text
GetArtifactPatterns
```

每个模式说明：

```text
文件模式
通用用途
媒体类型
是否可用于恢复
```

Gaussian 适配器声明：

```text
output.log  → PrimaryOutput
*.gjf       → Input
*.fchk      → RestartState，可用于恢复或继续计算
```

通用流程仍然不包含 `.gjf`、`.fchk` 或具体程序名称。

同时把原先放在通用 `ChemSculptor.Compute` 中的 `gaussian.*` 技能标识移入
`ChemSculptor.Skills.Gaussian`。技能 ID 和运行行为保持不变，只修正代码归属。

### 通用产物收集器

新增：

```text
CalculationArtifactCollector
```

它负责：

```text
根据适配器规则枚举运行目录文件
生成相对路径
记录文件长度
计算 SHA-256 摘要
合并映射到同一文件的重复规则
防止 result.json 中的相对路径穿越运行目录
```

### 结果提取

`CalculationResultExtractionWorkflowSkill` 在解析完成并补齐通用字段后：

```text
读取适配器的产物规则
  ↓
扫描运行目录
  ↓
生成产物描述
  ↓
写入 result.json
```

因此 `result.json` 同时记录科学结果和备份清单。

### Gaussian 后处理

Gaussian 正常结束后，适配器检查 `.chk`，调用 `formchk` 生成 `.fchk`。

如果 `formchk` 失败：

```text
记录 Warning 诊断
不阻止计算结果、结果提取和验证继续完成
```

### API

新增：

```text
GET /calculations/{jobId}/artifacts
GET /calculations/{jobId}/artifacts/{fileName}
```

第一个接口返回文件清单，第二个接口下载单个文件。

`GET /calculations/{jobId}/result` 也返回 `artifacts` 列表。

下载接口优先读取 `result.json` 中的产物描述；只有旧结果没有清单时，才根据
适配器规则重新扫描运行目录。

### WinForms

客户端“保存”按钮改为：

```text
选择目标目录
  ↓
GET /calculations/{jobId}/artifacts
  ↓
逐个下载服务器清单中的文件
```

客户端不压缩文件，不判断具体扩展名，也不解析计算输出。

### 结果示例

真实水分子计算中的 `result.json` 片段：

```json
{
  "program": "Gaussian 16",
  "artifacts": [
    {
      "fileName": "job-....fchk",
      "kind": "RestartState",
      "sha256": "...",
      "canUseForRestart": true
    },
    {
      "fileName": "job-....gjf",
      "kind": "Input",
      "sha256": "...",
      "canUseForRestart": false
    },
    {
      "fileName": "output.log",
      "kind": "PrimaryOutput",
      "sha256": "...",
      "canUseForRestart": false
    }
  ]
}
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：35/35 通过
- 真实 Gaussian 水分子单点计算：`Validated`
- 最终能量：`-76.3801013836 Hartree`
- `result.json` 包含 `Gaussian 16` 和三个产物描述
- `/artifacts` 返回 `.fchk`、`.gjf`、`output.log`
- 下载 `output.log` 后重新计算 SHA-256，与清单记录一致

### 当前边界

`.fchk` 已标记为可用于恢复或继续计算，但真正“从备份启动后续计算”的服务尚未实现。
当前先保证：

```text
知道使用的是什么程序
知道有哪些文件
知道每个文件是什么
可以验证备份文件没有变化
以后具备实现恢复流程的数据基础
```

---

## v0.26.0（2026-09-30）：工作流节点全面通用化

### 版本

- 当前版本：`0.26.0`
- 日期：2026-09-30
- 版本类型：架构约束强化

### 改动目的

确立并执行永久原则：

```text
所有工作流节点必须使用通用能力名称。
具体计算程序只能作为适配器实现，不得进入工作流定义。
```

### 工作流技能

旧名称：

```text
gaussian.input-generation
calculation.submit
calculation.wait
gaussian.single-point-workflow-extraction
calculation.workflow-validation
calculation.workflow-processing-plan
```

新名称：

```text
calculation.prepare-input
calculation.submit
calculation.wait
calculation.extract-result
calculation.workflow-validation
calculation.workflow-processing-plan
```

### 实现调整

新增通用技能：

```text
CalculationInputPreparationSkill
CalculationResultExtractionWorkflowSkill
```

新增程序适配器注册表：

```text
IQuantumProgramAdapterRegistry
QuantumProgramAdapterRegistry
```

具体程序适配器只负责：

```text
输入文件名称
输入文件格式
命令行参数
环境变量
输出解析
输出翻译
```

通用流程只负责：

```text
准备输入
提交
等待
提取结果
验证
生成处理方案
```

### 删除的程序专用工作流技能

```text
GaussianInputGenerationSkill
GaussianSinglePointWorkflowExtractionSkill
gaussian.input-generation
gaussian.single-point-workflow-extraction
```

`SinglePointCalculationService` 不再自行使用 `.gjf`。输入文件名由
`IQuantumProgramAdapter.GetInputFileName` 提供。

### 永久编码约定

在 `docs/Coding-Conventions.md` 中新增“通用流程与具体程序分离”规则。

以后通用流程层中出现具体程序名称、扩展名、关键词、环境变量或输出规则，
视为架构违规。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：32/32 通过
- 实际工作流技能：

```text
calculation.prepare-input
calculation.submit
calculation.wait
calculation.extract-result
calculation.workflow-validation
calculation.workflow-processing-plan
```

- 实际计算状态：`Validated`
- 实际能量：`-76.3801013836 Hartree`
- 实际验证：`Passed`

---

## v0.25.0（2026-09-29）：声明式工作流接管单点计算

### 版本

- 当前版本：`0.25.0`
- 日期：2026-09-29
- 版本类型：架构升级（工作流驱动）

### 改动目的

让单点计算不再由 `SinglePointCalculationService` 直接按代码顺序调用技能，而是
由声明式 DAG 工作流接管。

现在工作流：

```text
input-generation
  → submit
  → wait
  → extract
  → validate
  → plan
```

对应技能：

```text
gaussian.input-generation
  → calculation.submit
  → calculation.wait
  → gaussian.single-point-workflow-extraction
  → calculation.workflow-validation
  → calculation.workflow-processing-plan
```

### 工作流模型增强

`WorkflowNode` 新增：

```text
Inputs
```

输入映射支持：

```text
$input.名称
  引用工作流初始输入

节点标识
  引用上游节点输出
```

`WorkflowRun` 新增：

```text
Inputs
```

`WorkflowEngine` 新增 `SubmitAsync(definition, inputs)` 重载。

### 新增工作流技能

Common：

```text
CalculationSubmissionSkill
CalculationWaitSkill
CalculationWorkflowValidationSkill
CalculationWorkflowProcessingPlanSkill
```

Gaussian：

```text
GaussianSinglePointWorkflowExtractionSkill
```

### 服务调整

`SinglePointCalculationService` 现在：

```text
创建计算作业
创建工作流输入
提交 WorkflowDefinition
立即返回 jobId
由后台工作流完成提交、等待、提取、验证和方案生成
```

新增：

```text
ISinglePointWorkflowEngine
SinglePointWorkflowEngine
SinglePointWorkflowDefinitionFactory
```

删除旧的直接监控路径：

```text
CalculationJobMonitor
CalculationJobMonitorOptions
ICalculationJobMonitor
RuleBasedCalculationProcessingPlanner
ICalculationProcessingPlanner
```

处理方案逻辑移动到：

```text
CalculationProcessingPlanFactory
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：32/32 通过
- 实际工作流状态：`Passed`
- 实际节点状态：

```text
input-generation = Passed
submit = Passed
wait = Passed
extract = Passed
validate = Passed
plan = Passed
```

- 实际计算状态：`Validated`
- 实际能量：`-76.3801013836 Hartree`
- 实际验证：`Passed`

---

## v0.24.0（2026-09-27）：WinForms 交互闭环

### 版本

- 当前版本：`0.24.0`
- 日期：2026-09-27
- 版本类型：新增功能（第九阶段，WinForms 交互）

### 改动目的

把 WinForms 从“多个并列调试动作”收敛为一条明确交互链路：

```text
选择坐标 txt
  → 输入原始自然语言
  → 单击发送
  → 等待服务器返回 jobId
  → 轮询计算状态
  → 读取结果和验证报告
  → 显示最终能量和验证摘要
```

客户端仍然不引用任何 ChemSculptor 服务器项目，也不处理化学参数。

### 界面调整

移除：

```text
发送坐标
提交任务
```

保留：

```text
选择 txt
输入原始文本
发送
取消计算
保存结果
```

### 提交链路

WinForms 继续调用：

```text
POST /agent/messages
```

请求只包含：

```text
SessionId
Text
CoordinateText
```

服务器理解 `Text`，返回：

```text
JobId
Status
```

### 轮询链路

客户端根据 `JobId` 调用：

```text
GET /calculations/{jobId}/status
```

结束后读取：

```text
GET /calculations/{jobId}/result
GET /calculations/{jobId}/validation
```

显示内容包括：

```text
能量
能量单位
程序
方法
基组
正常终结
失败类别
验证状态
验证摘要
未通过的验证项
输出文件路径
```

### 取消

“取消计算”按钮调用：

```text
POST /calculations/{jobId}/cancel
```

### 客户端模型

新增客户端 DTO：

```text
CalculationJobItem
CalculationStatusDto
CalculationResultDto
CalculationValidationDto
CalculationValidationCheckDto
CalculationDiagnosticDto
```

删除旧的客户端任务、几何提交和单点调试 DTO。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：32/32 通过
- 实际 Agent 提交路径：通过
- 初始状态：`Running`
- 最终状态：`Validated`
- 实际能量：`-76.3801013836 Hartree`
- 实际验证：`Passed`

---

## v0.23.1（2026-09-27）：修正单点计算提交契约

### 版本

- 当前版本：`0.23.1`
- 日期：2026-09-27
- 版本类型：接口修正

### 改动目的

纠正客户端提交契约中的职责越界。

客户端只能提交：

```text
coordinateText
text
```

客户端不能提交：

```text
charge
multiplicity
method
basis
program
overrides
sessionId
```

客户的原始文本应在服务器端解释，不能由客户端提前转换成计算参数。

### API 请求

```json
{
  "coordinateText": "O 0.000000 0.000000 0.117300\nH 0.000000 0.757200 -0.469200\nH 0.000000 -0.757200 -0.469200",
  "text": "计算水分子的单点能"
}
```

### 代码调整

`SinglePointCalculationRequest` 只保留：

```text
CoordinateText
Text
```

删除：

```text
SessionId
Goal
Overrides
CalculationParameterRequest
```

`CalculationEndpoints` 现在只把两个客户端字段转换为：

```text
CalculationRequest.CoordinateText
CalculationRequest.Goal
```

服务内部的服务器侧参数覆盖框架仍保留，但不通过当前客户端 API 暴露。

### 验证

- 文档中的请求体已修正
- 客户端化学参数不再出现在计算端点

---

## v0.23.0（2026-09-27）：单点计算 API 端点

### 版本

- 当前版本：`0.23.0`
- 日期：2026-09-27
- 版本类型：新增功能（第八阶段，API 端点）

### 改动目的

把 `/calculations` 变成单点计算服务的统一 HTTP 门面。

Api 端点不再分别直接调用 Agent 或查询服务，而是统一调用：

```text
ISinglePointCalculationService
```

### 完整端点

```text
POST /calculations/single-point
GET  /calculations/{jobId}
GET  /calculations/{jobId}/status
GET  /calculations/{jobId}/result
GET  /calculations/{jobId}/validation
POST /calculations/{jobId}/cancel
```

### 提交请求

`POST /calculations/single-point` 现在支持：

```text
SessionId
Goal
CoordinateText
Overrides
```

覆盖参数格式：

```json
[
  {
    "name": "charge",
    "value": "0"
  },
  {
    "name": "multiplicity",
    "value": "1"
  }
]
```

### 提交响应

成功时返回 HTTP 202：

```text
Succeeded
JobId
Status
InputFilePath
OutputFilePath
Message
Diagnostics
```

失败时返回 HTTP 400。

### 查询和取消

状态：

```text
GET /calculations/{jobId}
GET /calculations/{jobId}/status
```

结果：

```text
GET /calculations/{jobId}/result
```

验证：

```text
GET /calculations/{jobId}/validation
```

取消：

```text
POST /calculations/{jobId}/cancel
```

取消规则：

```text
Running 或 Queued
  → 可以取消

Validated、Failed、Canceled
  → 返回 HTTP 409
```

### 服务能力

`ISinglePointCalculationService` 新增：

```text
CancelAsync
```

### API 文档

新增：

```text
docs/Calculation-Api-Reference.md
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：32/32 通过
- 实际 HTTP 提交：成功
- 实际状态：`Validated`
- 实际能量：`-76.3801013836 Hartree`
- 实际验证：`Passed`
- 实际检查数量：14
- 已完成作业再次取消：HTTP 409

---

## v0.22.0（2026-09-27）：单点计算服务

### 版本

- 当前版本：`0.22.0`
- 日期：2026-09-27
- 版本类型：新增功能（第七阶段，单点计算服务）

### 改动目的

把原先位于 `SinglePointCalculationExecutor` 中的编排逻辑提升为正式服务：

```text
ISinglePointCalculationService
  → SinglePointCalculationService
```

Agent 不再直接编排输入生成、进程提交和作业监控，而是调用单点计算服务。

新增边界：

```text
Agent
  负责会话意图到服务调用

SinglePointCalculationService
  负责作业创建、Skill 调用、后端提交、监控启动和查询
```

### 计算请求

`CalculationRequest` 新增：

```text
CoordinateText
```

当前服务会：

```text
创建默认 CalculationSpec
应用允许的 Overrides
创建 CalculationJob 和工作区
调用 gaussian.input-generation
提交 IComputeBackend
启动 CalculationJobMonitor
保存 Running 状态
```

当前支持的覆盖参数：

```text
program
method
basis
charge
multiplicity
```

### 作业模型

`CalculationJob` 新增：

```text
Goal
SourceInputFilePath
```

含义：

```text
Goal
  本次计算的科研目标

SourceInputFilePath
  input 目录中的原始输入文件

InputFilePath
  run 目录中的实际执行输入文件
```

### 服务接口

`ISinglePointCalculationService` 现在提供：

```text
SubmitAsync
GetJobAsync
GetResultAsync
GetValidationAsync
```

`SubmitAsync` 返回：

```text
SinglePointCalculationSubmissionResult
  Succeeded
  Error
  Message
  Job
  Diagnostics
```

### 代码调整

删除：

```text
SinglePointCalculationExecutor
```

新增：

```text
SinglePointCalculationService
```

AgentService 现在只依赖：

```text
IConversationService
ISinglePointCalculationService
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：31/31 通过
- 实际水分子计算状态：`Validated`
- 实际能量：`-76.3801013836 Hartree`
- 实际验证：`Passed`
- 实际检查数量：14

---

## v0.21.2（2026-09-27）：把验证项区分为必要、建议和信息检查

### 版本

- 当前版本：`0.21.2`
- 日期：2026-09-27
- 版本类型：验证框架增强

### 改动目的

明确：

```text
Normal termination 是正常完成的必要条件
Normal termination 不是验证通过的充分条件
```

因此验证框架必须允许以后继续加入：

```text
能量合理性
自旋污染
结构合理性
频率虚频
多结果一致性
```

### 改动内容

新增验证要求等级：

```text
Required
Recommended
Informational
```

新增验证范围：

```text
Structure
ProgramOutput
Numerical
ScientificPlausibility
CrossResultConsistency
Other
```

每个 `CalculationValidationCheck` 现在记录：

```text
Requirement
Scope
Passed
Severity
ExpectedValue
ActualValue
Message
```

整体判定规则：

```text
任一 Required 检查失败
  → Failed

Required 全部通过，但存在 Recommended 或 Informational 失败
  → PassedWithWarnings

全部 Required 通过，且没有建议项或信息项失败
  → Passed
```

当前正常终结相关检查属于：

```text
Required + ProgramOutput
```

未来科学合理性检查可以注册为：

```text
Required + ScientificPlausibility
```

而不需要修改正常终结检查。

### 返回摘要

验证报告新增 `Summary`：

```text
全部必要验证通过。正常终结是必要条件，但不是充分条件。
必要检查 14/14 项通过。
```

### API 字段

`GET /calculations/{jobId}/validation` 现在返回：

```text
Summary
Checks[].Requirement
Checks[].Scope
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：29/29 通过
- 新增测试：
  - Recommended 科学检查失败时得到 `PassedWithWarnings`
  - Required 科学检查失败时得到 `Failed`
- 实际 Gaussian 计算：14/14 必要检查通过

---

## v0.21.1（2026-09-27）：增加正常终结的通用与 Gaussian 输出检验

### 版本

- 当前版本：`0.21.1`
- 日期：2026-09-27
- 版本类型：功能增强

### 改动目的

在单点结果验证中增加两层正常终结检查：

```text
通用检查
  输出结果声明正常终结

Gaussian 专用检查
  output.log 的最后一个非空行包含 Normal termination
```

通用检查保证 Agent 收到的通用结果结构正确。

Gaussian 专用检查保证原始输出文件本身具有正常终结证据。

### 改动内容

通用验证器新增：

```text
calculation.output_normal_termination
```

新增 Gaussian 输出验证器：

```text
GaussianSinglePointOutputValidator
```

新增检查：

```text
gaussian.output_last_line_normal_termination
```

检查规则：

```text
读取 Gaussian output.log
从末尾向前跳过空行
检查最后一个非空行是否包含
Normal termination
```

`CalculationResultValidationSkill` 从“选择第一个验证器”改为“合并全部适用
验证器”，因此通用验证和 Gaussian 专用验证会同时执行。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：27/27 通过
- 实际计算状态：`Validated`
- 实际检查数量：14
- 实际验证器：
  - `single-point-result-validator`
  - `gaussian-single-point-output-validator`
- 实际最后非空行：
  - `Normal termination of Gaussian 16 at Sun Sep 27 13:22:07 2026.`

---

## v0.21.0（2026-09-27）：单点计算结果验证

### 版本

- 当前版本：`0.21.0`
- 日期：2026-09-27
- 版本类型：新增功能（第六阶段，结果验证）

### 改动目的

把 `CalculationResultValidationSkill` 从“检查正常结束和能量是否存在”的占位逻辑，
升级为结构化单点结果验证。

验证目标是确认：

```text
结果确实属于当前作业
结果与计算方案一致
程序正常结束
能量有效
输出文件存在
```

本次仍不进行科学合理性判断，也不执行异常纠错。

### 验证模型

新增：

```text
CalculationValidationStatus
CalculationValidationCheck
```

报告现在包含：

```text
Passed
Status
ValidatorName
ValidatedAt
Checks
Issues
```

### 验证策略

新增：

```text
ICalculationResultValidator
SinglePointCalculationResultValidator
```

`CalculationResultValidationSkill` 根据任务类型选择验证器，而不是把所有验证规则
堆在 Skill 本身。

当前单点验证包括 12 项检查：

```text
作业标识一致
计算程序一致
计算方法一致
计算基组一致
总电荷一致
自旋多重度一致
正常结束
FailureKind 为 None
存在最终能量
能量为有限数值
能量单位为 Hartree
输出文件存在
```

### 接入正常链路

`CalculationJobMonitor` 现在按以下顺序处理：

```text
提取通用结果
  → 调用 CalculationResultValidationSkill
  → 保存 validation.json
  → 验证通过时状态设为 Validated
  → 生成通用处理方案
  → 保存 result.json、processing-plan.json、manifest.json
```

如果验证失败，作业状态进入 `Failed`，验证问题同时进入通用结果诊断。

### 新增 API

```text
GET /calculations/{jobId}/validation
```

返回：

```text
Passed
Status
ValidatorName
ValidatedAt
Checks
Issues
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：25/25 通过
- 实际水分子计算状态：`Validated`
- 实际能量：`-76.3801013836 Hartree`
- 实际验证状态：`Passed`
- 实际检查数量：12
- `validation.json` 已生成

---

## v0.20.2（2026-09-26）：新增 Skill 集合与工作流组织教程

### 版本

- 当前版本：`0.20.2`
- 日期：2026-09-26
- 版本类型：文档

### 改动目的

新增一份偏设计思想的教程，说明：

```text
Skill 集合与工作流的区别
Skill、Catalog、Workflow、Agent、Gate 的职责
静态工作流与动态工作流
DAG、依赖和并行
数据契约和版本管理
SCF 异常的修正子工作流
TADF 多阶段工作流
父子作业和审计
当前实现与未来目标的边界
```

### 改动内容

新增：

```text
docs/Skill-Collections-and-Workflow-Tutorial.md
```

并在 `README.md` 中增加教程链接。

本次不修改运行代码。

### 验证

- 文档链接检查
- 与当前 WorkflowDefinition、WorkflowNode 和 Skill 结构对照

---

## v0.20.1（2026-09-26）：新增 Skill 集合学习教程

### 版本

- 当前版本：`0.20.1`
- 日期：2026-09-26
- 版本类型：文档

### 改动目的

新增一份面向项目维护者的教程，集中解释：

```text
Skill、Adapter、Parser、Translator、Catalog 的区别
为什么 Agent 不应直接依赖具体程序
当前 Skill 项目的目录和依赖方向
一次正常单点计算的 Skill 调用顺序
JsonSkill 和 SkillJsonInvoker 的工作方式
怎样新增一个 Skill
怎样新增 ORCA 程序族
异常处理框架当前做到什么程度
```

### 改动内容

新增：

```text
docs/ChemSculptor-Skill-Collection-Tutorial.md
```

并在 `README.md` 中增加教程链接。

本次不修改运行代码。

### 验证

- 文档链接检查
- 与当前 v0.20.0 代码结构对照

---

## v0.20.0（2026-09-26）：按显式 Skill 集合重组正常计算链路

### 版本

- 当前版本：`0.20.0`
- 日期：2026-09-26
- 版本类型：架构调整（Skill 目录与调用边界）

### 改动目的

把正常单点计算从“Agent 直接调用 Gaussian 技术类”改为“Agent 只通过
`ISkillRegistry` 和通用模型调用 Skill”。

阅读代码时先看到能力：

```text
GaussianInputGenerationSkill
GaussianSinglePointResultExtractionSkill
CalculationResultValidationSkill
```

需要深入时再看实现：

```text
GaussianInputWriter
GaussianOutputParser
GaussianResultTranslator
Gaussian16ProgramAdapter
```

### 新增项目

```text
src/ChemSculptor.Skills.Common
src/ChemSculptor.Skills.Gaussian
src/ChemSculptor.Skills.Orca
```

### 正常计算链路

```text
SinglePointCalculationExecutor
  → GaussianInputGenerationSkill
  → IComputeBackend
  → CalculationJobMonitor
  → GaussianSinglePointResultExtractionSkill
  → CalculationResultValidationSkill
  → CalculationProcessingPlanner
```

Agent 不再直接引用 `ChemSculptor.Compute.Gaussian` 或
`ChemSculptor.Compute.Local`。

### 技能目录

Common：

```text
calculation.result-validation
```

Gaussian：

```text
gaussian.input-generation
gaussian.single-point-result-extraction
gaussian.failure-diagnosis
gaussian.failure-correction-proposal
```

ORCA：

```text
目录和注册框架已建立，当前没有已实现技能。
```

### 异常处理框架

当前不执行异常处理，只保留：

```text
GaussianFailureDiagnosisSkill
GaussianFailureCorrectionProposalSkill
通用 CalculationFailure
通用 CalculationProcessingPlan
GaussianProcessingPlan
```

其中异常诊断技能当前报告“框架尚未实现”，避免在未完成逻辑时被误用。

### 通用技能调用

新增：

```text
ISkillInvoker
SkillJsonInvoker
JsonSkill<TRequest, TResult>
```

Agent 使用通用请求和结果类型调用技能；Gaussian 技能在内部把它们转换为
Gaussian 专用请求、解析结果和程序上下文。

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：23/23 通过
- 实际计算：状态 `Parsed`
- 实际能量：`-76.3801013836 Hartree`
- `/skills/` 能列出：
  - `gaussian.input-generation`
  - `gaussian.single-point-result-extraction`
  - `calculation.result-validation`
  - `gaussian.failure-diagnosis`
  - `gaussian.failure-correction-proposal`

---

## v0.19.0（2026-09-26）：计算结果与处理方案的双向翻译框架

### 版本

- 当前版本：`0.19.0`
- 日期：2026-09-26
- 版本类型：架构调整（程序专用模型与通用模型解耦）

### 改动目的

避免智能体直接理解 Gaussian 的关键词和输出格式。现在由 Gaussian 模块负责
两端的翻译：

```text
Gaussian 文本输出
  → GaussianOutputParser
  → GaussianOutput
  → GaussianResultTranslator
  → CalculationResult
  → 通用 CalculationProcessingPlanner
  → CalculationProcessingPlan
  → GaussianProcessingPlanTranslator
  → GaussianProcessingPlan
  → ProgramProcessingPlan
```

Agent 只处理 `CalculationResult`、`CalculationProcessingPlan` 和
`ProgramProcessingPlan`，不包含 Gaussian 路线、Link 名称或输出格式规则。

### 改动内容

新增 Gaussian 专用数据结构：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutput.cs
src/ChemSculptor.Compute.Gaussian/GaussianProcessingPlan.cs
```

新增翻译器：

```text
src/ChemSculptor.Compute.Gaussian/GaussianResultTranslator.cs
src/ChemSculptor.Compute.Gaussian/GaussianProcessingPlanTranslator.cs
```

新增通用处理方案：

```text
src/ChemSculptor.Compute/CalculationProcessingModels.cs
src/ChemSculptor.Agent/ICalculationProcessingPlanner.cs
src/ChemSculptor.Agent/RuleBasedCalculationProcessingPlanner.cs
```

通用模型包括：

```text
CalculationProcessingOutcome
CalculationProcessingActionType
CalculationProcessingAction
CalculationProcessingPlan
ProgramProcessingAction
ProgramProcessingPlan
CalculationFailureKind
```

处理结果保存为三个文件：

```text
results/result.json
results/processing-plan.json
results/program-processing-plan.json
```

### 教程式说明

#### 一、为什么要分成两层

如果 Agent 直接读取：

```text
Normal termination of Gaussian 16
SCF Done: E(RCAM-B3LYP) = ...
```

那么以后增加 ORCA 时，Agent 就必须继续增加 ORCA 专用判断，逐渐变成“所有
计算程序的大杂烩”。

现在职责变成：

```text
Agent
  只知道“正常完成、缺少能量、需要重试、需要用户决定”

Gaussian 模块
  知道这些通用状态怎样对应 Gaussian 的输入和关键词
```

#### 二、正常结果的流程

```text
Gaussian 输出正常结束并含有能量
  → GaussianOutput.NormalTermination = true
  → CalculationResult.FailureKind = None
  → CalculationProcessingPlan.Outcome = Completed
  → 没有处理动作
```

#### 三、异常结果的流程

例如输出没有最终能量：

```text
GaussianOutput.Energy = null
  → CalculationResult.FailureKind = EnergyMissing
  → 通用方案产生 RetryAsIs
  → Gaussian 模块翻译为 RerunSameInput
```

例如 Gaussian 报错结束：

```text
GaussianOutput.ErrorTermination = true
  → CalculationResult.FailureKind = ProgramError
  → 通用方案产生 ReviewOutput
  → Gaussian 模块翻译为 InspectOutput
```

当前阶段只生成方案并保存，不自动执行重试或输入修改。

#### 四、文件含义

```text
result.json                    通用计算结果
processing-plan.json           智能体可以理解的通用处理方案
program-processing-plan.json   翻译后的程序专用处理方案
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：23/23 通过
- 实际 Gaussian 计算：状态 `Parsed`
- 实际能量：`-76.3801013836 Hartree`
- 通用处理方案：`outcome = Completed`
- Gaussian 专用处理方案生成成功

---

## v0.18.0（2026-09-26）：Gaussian 输出解析与规范化结果

### 版本

- 当前版本：`0.18.0`
- 日期：2026-09-26
- 版本类型：新增功能（第五阶段，Gaussian 输出解析）

### 改动目的

让单点计算不再只停留在“启动 g16”和取得输出文件，而是继续完成：

```text
等待计算进程结束
读取 output.log
判断 Normal termination
提取最终 SCF 能量
保存 result.json
提供状态和结果查询接口
```

### 改动内容

新增 Gaussian 输出解析器：

```text
src/ChemSculptor.Compute.Gaussian/GaussianOutputParser.cs
```

解析内容：

- 识别 `Normal termination of Gaussian 16`
- 识别 `Error termination`
- 提取最后一次 `SCF Done: E(...) = ...`
- 支持 Gaussian 的 `D` 指数格式
- 生成结构化诊断信息

新增文件仓储：

```text
src/ChemSculptor.Compute/FileCalculationRepository.cs
```

保存位置：

```text
jobs/<jobId>/manifest.json
jobs/<jobId>/results/result.json
```

新增后台作业监控器：

```text
src/ChemSculptor.Agent/CalculationJobMonitor.cs
```

工作方式：

```text
轮询 IComputeBackend.GetStatusAsync
  → 状态进入 Completed / Failed / Canceled
  → 读取 output.log
  → 调用 IQuantumProgramAdapter.ParseOutputAsync
  → 保存 result.json
  → 更新 manifest.json 中的作业状态
```

新增查询服务：

```text
ICalculationQueryService
CalculationQueryService
```

新增 API：

```text
GET /calculations/{jobId}/status
GET /calculations/{jobId}/result
```

### 教程式说明

#### 一、为什么解析要放在后台

Gaussian 可能运行数秒、数分钟甚至数小时。提交 API 不能一直等待进程结束，
否则 HTTP 请求会长时间占用连接。

现在的流程是：

```text
提交 API 立即返回 Running
后台监控器继续等待
Gaussian 结束后自动解析并保存结果
客户端稍后查询状态或结果
```

#### 二、怎样判断计算成功

不能只依赖进程退出码。解析器还会检查：

```text
Normal termination of Gaussian 16
```

并提取：

```text
SCF Done: E(RCAM-B3LYP) = -76.3801014 A.U.
```

只有同时满足以下条件，作业才进入 `Parsed`：

```text
进程状态为 Completed
输出中存在 Normal termination
成功提取到最终能量
```

#### 三、如何查询结果

查询状态：

```powershell
Invoke-RestMethod `
    -Uri "http://127.0.0.1:5093/calculations/<jobId>/status" `
    -Method Get
```

查询规范化结果：

```powershell
Invoke-RestMethod `
    -Uri "http://127.0.0.1:5093/calculations/<jobId>/result" `
    -Method Get
```

结果示例：

```json
{
  "energy": -76.3801013836,
  "energyUnit": "Hartree",
  "normalTermination": true,
  "program": "Gaussian 16",
  "method": "CAM-B3LYP",
  "basis": "6-31G*",
  "charge": 0,
  "multiplicity": 1
}
```

#### 四、当前边界

已经具备：

```text
输出解析
能量提取
作业状态保存
规范化结果保存
状态查询 API
结果查询 API
```

尚未具备：

```text
科学结果验证门
自动查错与纠错
WinForms 自动轮询最终能量
远程 HPC 后端
作业队列
```

### 验证

- Release 全解决方案构建：0 警告 0 错误
- 测试：19/19 通过
- 端到端实测：水的 CAM-B3LYP/6-31G* 单点计算状态进入 `Parsed`
- 实测能量：`-76.3801013836 Hartree`
- 实测结果文件：`jobs/<jobId>/results/result.json`

---

## v0.17.1（2026-09-26）：输入文件复制到运行目录后执行

### 版本

- 当前版本：`0.17.1`
- 日期：2026-09-26
- 版本类型：问题修复 / 工作区布局调整

### 改动目的

让计算程序始终在自己的 `run` 目录中执行，使 Gaussian 生成的检查点文件、
临时文件和输出文件自然落在同一个目录，避免结果分散到 `input` 目录。

### 改动内容

- `SinglePointCalculationExecutor` 生成原始输入后，把 `.gjf` 复制到 `run` 目录。
- `CalculationJob.InputFilePath` 指向 `run` 目录中的实际执行输入。
- `Gaussian16ProgramAdapter` 构建上下文时使用 `run` 目录中的输入文件。
- Gaussian `%chk` 改为相对文件名，例如 `job-xxxx.chk`。
- API 返回的 `InputFilePath` 仍指向 `input` 目录中的原始输入文件。

目录变化：

```text
jobs/<jobId>/input/
  molecule.xyz
  <jobId>.gjf

jobs/<jobId>/run/
  <jobId>.gjf
  <jobId>.chk
  output.log
  stdout.log
  stderr.log
```

### 教程式说明

原来的 `g16` 参数直接指向：

```text
<作业目录>\input\<jobId>.gjf
```

这样 `%chk` 的绝对路径也会指向 `input` 目录。现在执行器先把输入复制到：

```text
<作业目录>\run\<jobId>.gjf
```

然后 `g16` 以 `run` 作为工作目录，并接收 `run` 中的输入文件。Gaussian 输入
中的检查点设置为：

```text
%chk=<jobId>.chk
```

相对路径以工作目录为基准，因此检查点自然写入 `run`。

### 验证

- `dotnet build ChemSculptor.slnx --no-restore --nologo`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx --no-build --nologo`：16/16 通过
- 端到端实测：`run` 中生成 `.gjf`、`.chk`、`output.log`、`stdout.log` 和 `stderr.log`
- 端到端实测：`input` 中只保留原始 `.gjf` 和 `molecule.xyz`

---

## v0.17.0（2026-09-26）：本机执行后端与 Gaussian 单点计算启动

### 版本

- 当前版本：`0.17.0`
- 日期：2026-09-26
- 版本类型：新增功能（第四阶段，本机执行后端）

### 改动目的

让单点计算不再停留在“生成 Gaussian 输入文件”，而是可以继续调用本机
`g16`，在后台启动计算，并保存标准输出、标准错误和程序输出文件。

本阶段仍然不实现作业队列。用户提交后立即启动本机进程，后续再替换为远程
HPC 或集群后端。

### 改动内容

新增项目：

```text
src/ChemSculptor.Compute.Local
```

新增类型：

```text
LocalProcessBackendOptions
LocalProcessState
LocalProcessBackend
```

`LocalProcessBackend` 实现 `IComputeBackend`：

- 使用 `ProcessStartInfo` 和 `ArgumentList` 启动本机程序。
- 使用 `UseShellExecute = false`，不依赖命令行字符串拼接。
- 同时捕获标准输出和标准错误。
- 进程退出后写入 `stdout.log` 和 `stderr.log`。
- 退出码为 0 时标记 `Completed`，否则标记 `Failed`。
- 支持取消并终止进程树。

新增 Gaussian 16 适配器：

```text
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapterOptions.cs
```

适配器负责：

- 判断计算方案是否为 Gaussian 16。
- 根据通用计算方案生成 `.gjf`。
- 构建 `g16 输入文件 输出文件` 的命令行参数。
- 从 `PATH` 或现有 `GAUSS_EXEDIR` 解析 Gaussian 可执行文件目录。
- 把 `GAUSS_EXEDIR` 显式传给子进程。

计算公式模型调整：

- `CalculationExecutionContext.Arguments`：程序启动参数。
- `CalculationExecutionContext.EnvironmentVariables`：子进程环境变量。
- `CalculationJob.RunDirectory`：本次作业的运行目录。
- `ICalculationWorkspace.GetJobOutputPath`：取得程序输出文件路径。

单点执行链路调整：

```text
SinglePointCalculationExecutor
  → 解析坐标
  → 创建作业工作区
  → Gaussian16ProgramAdapter 生成输入文件
  → Gaussian16ProgramAdapter 构建执行上下文
  → IComputeBackend.SubmitAsync
  → LocalProcessBackend 启动 g16
```

接口调整：

- `SinglePointExecutionResult`、`AgentResult` 和 API 响应新增输出文件路径。
- WinForms 在收到服务器响应后显示输出文件路径。

### 教程式说明

#### 一、为什么不能把 g16 直接写进 Agent

Agent 负责编排，不应该知道 Gaussian 的输入格式、命令行参数和环境变量。
因此本次把具体程序细节放在 `ChemSculptor.Compute.Gaussian`：

```text
Agent
  只知道 IQuantumProgramAdapter 和 IComputeBackend

Gaussian16ProgramAdapter
  知道 Gaussian 16 要怎样生成输入、怎样启动

LocalProcessBackend
  知道怎样在本机启动一个进程并跟踪它
```

以后增加 ORCA 时，可以新增 ORCA 适配器；以后连接远程 HPC 时，可以新增远程
执行后端。Agent 的主流程不需要复制一份。

#### 二、一次完整调用发生了什么

客户端发送“单点计算”和坐标后：

1. `AgentService` 从会话层得到单点计算意图。
2. `SinglePointCalculationExecutor` 解析坐标，并创建 `job-...` 作业目录。
3. `CalculationDefaults` 提供 CAM-B3LYP、6-31G*、电荷 0、多重度 1、4 核等默认值。
4. `Gaussian16ProgramAdapter` 生成：

```text
<作业目录>\input\<jobId>.gjf
```

5. 适配器构建执行上下文：

```text
可执行文件：g16
参数：<jobId>.gjf <output.log>
运行目录：<作业目录>\run
环境变量：GAUSS_EXEDIR=<Gaussian 可执行文件目录>
```

6. `LocalProcessBackend` 启动进程并立即返回作业编号。
7. 后台监控任务等待 Gaussian 结束，然后保存：

```text
stdout.log
stderr.log
output.log
```

#### 三、为什么要传递 GAUSS_EXEDIR

只把 `g16.exe` 放进 `PATH` 并不一定足够。第一次端到端实测时，Gaussian
启动了，但报告：

```text
No executable for file l1.exe.
Search path GAUSS_EXEDIR is ""
```

原因是当前进程没有继承 Gaussian 启动脚本中的 `GAUSS_EXEDIR`。现在适配器会：

1. 如果系统已有 `GAUSS_EXEDIR`，直接沿用。
2. 如果没有，就从 `PATH` 找到 `g16.exe` 并取得它所在目录。
3. 把这个目录作为 `GAUSS_EXEDIR` 传给本机子进程。

这一步只属于 Gaussian 适配器，不污染通用本机后端。

#### 四、如何运行和检查

启动 API：

```powershell
dotnet run --project src/ChemSculptor.Api --urls http://127.0.0.1:5091
```

提交一个水的单点计算：

```powershell
$body = @{
    coordinateText = "O 0.000000 0.000000 0.117300`nH 0.000000 0.757200 -0.469200`nH 0.000000 -0.757200 -0.469200"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "http://127.0.0.1:5091/calculations/single-point" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

响应会包含：

```text
jobId
status = Running
inputFilePath
outputFilePath
```

Gaussian 结束后，在输出文件中搜索：

```text
Normal termination of Gaussian 16
```

#### 五、当前阶段的能力边界

已经具备：

```text
本机后台启动 g16
保存输入、输出和错误日志
记录运行状态
返回输入和输出文件位置
```

尚未具备：

```text
作业队列
并行数量控制
计算任务状态 API
取消计算 API
Gaussian 输出自动解析
远程 HPC 执行后端
```

这些能力保留在后续阶段，不改变当前接口方向。

### 验证

- `dotnet build ChemSculptor.slnx --no-restore --nologo`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx --no-build --nologo`：16/16 通过
- 本机端到端实测：水的 CAM-B3LYP/6-31G* 单点计算正常结束
- 实测关键结果：`HF=-76.3801014`
- 实测输出：`Normal termination of Gaussian 16`
- 实测 `stderr.log` 为空

---

## v0.16.0（2026-09-25）：客户端不再生成化学参数

### 版本

- 当前版本：`0.16.0`
- 日期：2026-09-25
- 版本类型：职责边界调整（客户端只发送文本和坐标）

### 改动目的

贯彻“客户端只负责传送，不处理客户信息”的原则：

```text
客户端不决定电荷
客户端不决定自旋多重度
客户端不选择计算方法
客户端只发送原始文本和坐标文本
```

所有化学与计算相关参数由服务器端默认方案、规则和后续交互决定。

### 改动内容

WinForms：

- `AgentMessageRequestDto` 移除 `Charge` 和 `Multiplicity`。
- `SinglePointCalculationRequestDto` 移除 `Charge` 和 `Multiplicity`。
- `SendAgentMessageAsync` 不再写入这两个字段。

Api：

- `AgentMessageRequest` 移除 `Charge` 和 `Multiplicity`。
- `SinglePointCalculationRequest` 移除 `Charge` 和 `Multiplicity`。
- `AgentEndpoints` 与 `CalculationEndpoints` 不再映射这两个字段。

Agent：

- `AgentRequest` 移除 `Charge` 和 `Multiplicity`。
- `AgentSinglePointRequest` 移除 `Charge` 和 `Multiplicity`。
- `AgentService` 调用执行器时只传坐标。
- `SinglePointCalculationExecutor.ExecuteAsync` 改为只接收坐标文本。

Compute：

- 电荷和多重度继续由 `CalculationDefaults` 在服务器端提供：

```text
Charge       = 0
Multiplicity = 1
```

### 当前行为

```text
客户端
  → SessionId + Text + CoordinateText
  → Agent
  → Conversation 解释意图
  → 单点执行器使用服务器默认方案
  → 生成 Gaussian 输入文件
```

用户以后要修改电荷或多重度时，必须通过服务器端参数交互、风险检查和人工确认完成。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：15/15 通过

---

## v0.15.0（2026-09-25）：拆分独立智能体编排层

### 版本

- 当前版本：`0.15.0`
- 日期：2026-09-25
- 版本类型：架构拆分（Api 变薄，编排独立）

### 改动目的

把“HTTP 路由”和“智能体编排”分开，避免 Api 随着任务类型增加而变重：

```text
Api：只做 HTTP 适配
Agent：负责意图到计算执行的编排
Conversation：负责会话、消息、意图和回复
Compute：负责计算模型与执行
```

### 改动内容

新增项目：

```text
src/ChemSculptor.Agent
```

新增类型：

```text
AgentRequest
AgentSinglePointRequest
AgentResult
IAgentService
AgentService
AgentServiceRegistration
```

迁移内容：

```text
SinglePointCalculationExecutor
  从 ChemSculptor.Api 迁移到 ChemSculptor.Agent
```

Api 调整：

- `AgentEndpoints` 只把 HTTP 请求转换为 `AgentRequest` 并调用 `IAgentService`。
- `CalculationEndpoints` 只把 HTTP 请求转换为 `AgentSinglePointRequest` 并调用 `IAgentService`。
- Api 不再直接引用 Conversation、Compute、Compute.Gaussian。
- Api 通过 `AgentServiceRegistration.AddAgentServices` 注册智能体服务。

依赖关系：

```text
Api → Agent
Agent → Conversation → Compute → InputProcessor
Agent → Compute.Gaussian
```

### 处理流程

```text
WinForms 原始文本
   ↓ HTTP
Api /agent/messages
   ↓
IAgentService.HandleMessageAsync
   ↓
ConversationService
   ↓
RuleBasedTaskInterpreter
   ↓
AgentService 根据意图调用 SinglePointCalculationExecutor
   ↓
生成 Gaussian 输入文件
   ↓
Api 返回 HTTP 响应
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：15/15 通过

---

## v0.14.0（2026-09-23）：独立会话层与原始消息处理

### 版本

- 当前版本：`0.14.0`
- 日期：2026-09-23
- 版本类型：新增会话层（不引入 LLM）

### 改动目的

按“客户端只发送原始文本，服务器负责解释”的原则，把会话、消息、意图和回复从 Api 中独立出来，形成 `ChemSculptor.Conversation`。

当前阶段仍只识别单点计算，但处理形式已经改为：

```text
客户端原始文本
   ↓
Conversation 解释意图
   ↓
服务器根据意图调用计算执行器
```

### 改动内容

新增项目：

```text
src/ChemSculptor.Conversation
```

新增模型：

```text
ConversationSession
ConversationMessage
ConversationRequest
ConversationIntent
ConversationQuestion
ConversationReply
```

新增接口与实现：

```text
IConversationRepository
IConversationService
InMemoryConversationRepository
ConversationService
```

Api 调整：

- `AgentEndpoints` 改为先调用 `IConversationService`。
- Conversation 返回结构化 `ConversationReply` 和 `ConversationIntent`。
- 只有在意图为单点计算时，才调用 `SinglePointCalculationExecutor`。
- `Program.cs` 注册会话服务和内存会话仓储。

WinForms 调整：

- `AgentMessageRequestDto` 增加 `SessionId`。
- 发送消息时携带当前会话标识。
- 客户端仍然不解释用户文本。

测试新增：

```text
ConversationServiceTests
  ├── “单点计算”被识别为单点任务
  ├── 未知任务被拒绝
  └── 用户消息和智能体回复写入会话历史
```

### 处理流程

```text
WinForms 发送原始文本
   ↓
POST /agent/messages
   ↓
ConversationService.HandleMessageAsync
   ├── 创建或读取会话
   ├── 保存用户消息
   ├── 调用 ITaskInterpreter
   ├── 生成 ConversationIntent
   └── 保存智能体回复
   ↓
AgentEndpoints 检查 Intent
   ├── 单点计算 → SinglePointCalculationExecutor
   └── 其他     → 返回暂不支持
   ↓
返回 AgentMessageResponse
```

### 设计边界

```text
Conversation：会话、消息、意图、回复
Compute：计算模型、默认方案、任务解释
Compute.Gaussian：Gaussian 输入生成
Api：HTTP 路由与编排
WinForms：界面与 HTTP 客户端
```

Conversation 不生成 Gaussian 输入文件，也不直接处理 HTTP。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：13/13 通过

---

## v0.13.0（2026-09-22）：交互窗口“单点计算”命令

### 版本

- 当前版本：`0.13.0`
- 日期：2026-09-22
- 版本类型：客户端交互命令（不启动计算程序）

### 改动目的

不增加按钮，改为通过交互窗口命令触发单点计算调试流程：

```text
用户输入：单点计算
点击：发送
```

客户端读取当前选择的坐标文件，调用服务器已有端点，并显示输入文件生成结果。

### 改动内容

WinForms：

- 新增命令常量 `单点计算`。
- `SendTextAsync` 识别该命令后调用 `TriggerSinglePointAsync`。
- 新增 `TriggerSinglePointAsync`：
  - 读取当前选择的 txt 坐标文件
  - 组装 `SinglePointCalculationRequestDto`
  - 调用 `POST /calculations/single-point`
  - 显示作业标识、状态和输入文件路径

服务器：

- 复用已有 `/calculations/single-point` 端点。
- 当前只生成 Gaussian 输入文件，不启动 g16。

### 使用方式

```text
1. 启动 Api 和 WinForms
2. 在 WinForms 顶部选择 samples/water.xyz.txt
3. 在底部输入框输入：单点计算
4. 点击“发送”
```

对话区会显示：

```text
单点计算已触发：job-xxxx，状态 InputGenerated。
输入文件：<工作区路径>\input\job-xxxx.gjf
Gaussian 输入文件已生成，尚未启动计算程序。
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：10/10 通过
- 服务器端输入生成链路已完成人工验证

---

## v0.12.1（2026-09-22）：移除 WinForms 临时单点计算调试按钮

### 版本

- 当前版本：`0.12.1`
- 日期：2026-09-22
- 版本类型：界面调整（行为保持不变）

### 改动目的

为后续通过交互窗口命令触发单点计算做准备，先移除 WinForms 中的临时“单点计算（调试）”按钮，恢复原有布局。

### 改动内容

- 移除 `MainForm` 中的 `_triggerSinglePointButton` 字段、初始化和事件订阅。
- 移除按钮对应的 `OnTriggerSinglePointClick` 与 `TriggerSinglePointAsync` 方法。
- 移除不再使用的 `System.Text.Json` 引用。
- 保留以下服务器端和客户端模型：
  - `POST /calculations/single-point`
  - `SinglePointCalculationRequest`
  - `SinglePointCalculationResponse`
  - WinForms 中的单点请求与响应 DTO

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- WinForms 布局恢复为：
  - 发送
  - 发送坐标
  - 提交任务

---

## v0.12.0（2026-09-22）：WinForms 触发单点计算调试链路

### 版本

- 当前版本：`0.12.0`
- 日期：2026-09-22
- 版本类型：调试链路（不启动计算程序）

### 改动目的

让 WinForms 客户端可以触发一次单点计算调试请求，服务器完成：

```text
接收坐标
解析坐标
创建工作区
生成 Gaussian 输入文件
返回作业标识与文件路径
```

当前不启动 Gaussian，也不解析输出，仅用于打通客户端到输入文件生成的链路。

### 改动内容

InputProcessor：

- 新增 `CanonicalGeometryMapper`，把旧解析结果转换为规范几何模型。

Api：

- 新增 `/calculations/single-point` 端点。
- 新增单点计算请求、响应和错误响应契约。
- Api 引用 `ChemSculptor.Compute` 与 `ChemSculptor.Compute.Gaussian`。
- `Program.cs` 注册工作区与 Gaussian 输入生成器，并将依赖注入调用改为显式静态调用。

WinForms：

- 新增“单点计算（调试）”按钮。
- 新增单点计算请求与响应客户端模型。
- 按钮触发时发送坐标文本、默认电荷 0 和默认多重度 1。
- 服务器返回后，在对话区显示作业标识、状态和生成的输入文件路径。

工作区：

- `%ProgramData%\ChemSculptor` 作为首选根目录。
- 如果 ProgramData 不可写，回退到当前用户 LocalAppData。
- 如果 LocalAppData 也不可写，回退到系统临时目录，保证受限环境可调试。

### 调试调用链

```text
WinForms“单点计算（调试）”按钮
  → POST /calculations/single-point
  → GeometryTextParser 解析坐标
  → CanonicalGeometryMapper 转换几何
  → WorkspaceManager 创建作业目录
  → GaussianInputWriter 生成 .gjf
  → 返回 InputGenerated 与输入文件路径
```

### 当前限制

```text
不启动 g16
不解析计算输出
不保存长期计算作业记录
内存暂用 4GB 调试默认值，后续根据电子数计算
电荷和多重度暂用 0 和 1，后续接入交互确认
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：10/10 通过
- 端到端调用 `/calculations/single-point`：
  - 返回状态 `InputGenerated`
  - Gaussian 输入文件成功生成
  - 文件包含 `%nprocshared=4`
  - 文件包含 `#p CAM-B3LYP/6-31G* SP`

---

## v0.11.0（2026-09-20）：Gaussian 输入文件生成第三阶段

### 版本

- 当前版本：`0.11.0`
- 日期：2026-09-20
- 版本类型：新增输入生成（不执行计算程序）

### 改动目的

把默认单点计算方案和规范几何转换为 Gaussian 输入文件，为后续本机执行和输出解析打基础。

当前阶段只生成输入文件：

```text
不启动 Gaussian
不解析输出
不写入结果
```

### 改动内容

新增项目：

```text
src/ChemSculptor.Compute.Gaussian
```

新增类型：

```text
GaussianInputOptions
  内存、核数、检查点路径和标题

GaussianInputWriter
  生成 Gaussian 单点输入文件
```

默认输入内容：

```text
%chk=<与输入文件同名的 .chk>
%mem=<调用方提供的内存设置>
%nprocshared=4

#p CAM-B3LYP/6-31G* SP

标题

0 1
O x y z
H x y z
...
```

### 设计说明

输入生成与程序执行分离：

```text
CalculationSpec
  提供任务类型、方法、基组、电荷和多重度

CanonicalGeometry
  提供原子坐标

GaussianInputOptions
  提供内存、核数、检查点路径和标题

GaussianInputWriter
  只负责生成输入文件
```

当前只支持：

```text
CalculationTaskType.SinglePoint
```

其他任务类型返回 `NotSupportedException`。

### 验证

新增测试：

```text
GaussianInputWriterTests
  ├── 默认 CAM-B3LYP/6-31G* 单点输入内容正确
  └── 非单点任务被拒绝
```

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：10/10 通过
- 未启动任何计算程序

---

## v0.10.0（2026-09-20）：计算工作区管理第二阶段

### 版本

- 当前版本：`0.10.0`
- 日期：2026-09-20
- 版本类型：新增工作区管理（不执行计算）

### 改动目的

实现计算工作区的目录规则与创建逻辑，为后续保存几何资产、生成输入文件、运行计算和保存结果提供统一文件结构。

默认工作区根目录：

```text
%ProgramData%\ChemSculptor
```

### 改动内容

`ChemSculptor.Compute` 新增：

```text
CalculationWorkspaceOptions
  工作区根目录与各子目录名称配置

CalculationWorkspacePaths
  几何文件、作业清单、坐标、输出、结果等固定文件名

WorkspaceManager
  实现 ICalculationWorkspace
  负责路径生成、目录创建和标识校验
```

`ICalculationWorkspace` 新增：

```text
EnsureGeometryWorkspaceAsync
EnsureJobWorkspaceAsync
```

新增测试：

```text
WorkspaceManagerTests
  ├── 路径生成符合约定
  ├── 创建工作区时目录实际存在
  └── 非法标识被拒绝
```

### 目录结构

```text
%ProgramData%\ChemSculptor/
  geometries/
    <geometryId>/
      original.txt
      canonical.json
      validation.json

  jobs/
    <jobId>/
      manifest.json
      input/
        molecule.xyz
      run/
        output.log
      results/
        result.json
        summary.txt
        validation.json
```

### 安全约束

- 目录名只允许字母、数字、连字符和下划线
- 禁止路径分隔符和 `..`
- 路径由 ID 生成，不使用用户提供的文件名
- 工作区根目录可配置，不写死在代码中

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：8/8 通过
- 未调用任何计算程序

---

## v0.9.0（2026-09-20）：计算模型与接口第一阶段

### 版本

- 当前版本：`0.9.0`
- 日期：2026-09-20
- 版本类型：新增计算框架（仅模型与接口，不执行计算）

### 改动目的

建立计算任务的数据结构和扩展点，为后续本机 Gaussian 单点计算、远程集群和 ORCA 适配预留统一基础。

当前阶段只定义：

```text
计算任务类型
计算方案
计算参数
计算作业
计算结果
执行上下文
风险与审批模型
```

不实现具体程序执行、输入文件生成和输出解析。

### 改动内容

新增项目：

```text
src/ChemSculptor.Compute
```

新增模型：

```text
CalculationTaskType
CalculationSpec
CalculationParameter
CalculationRequest
CalculationTask
CalculationJob
CalculationResult
CalculationExecutionContext
CalculationValidationReport
RiskAssessment
CalculationQuestion
CalculationPlan
ApprovalDecision
```

新增扩展接口：

```text
IQuantumProgramAdapter
IComputeBackend
ICalculationQueue
ICalculationScheduler
ICalculationWorkspace
ICalculationRepository
ICalculationParameterValidator
IScientificRiskEvaluator
IApprovalService
ICalculationPlanner
ISinglePointCalculationService
```

新增默认方案：

```text
CalculationDefaults
  默认程序：Gaussian 16
  默认任务：SinglePoint
  默认方法：CAM-B3LYP
  默认基组：6-31G*
  默认电荷：0
  默认多重度：1
  默认核数：4
```

### 设计说明

计算模型保持程序无关：

```text
模型和接口中不绑定具体程序
程序名称使用字符串保存
只有默认方案中写入默认程序 Gaussian 16
Gaussian / ORCA 的差异由后续程序适配器实现
```

计算与执行分离：

```text
CalculationSpec
  描述“怎么算”

CalculationJob
  描述“算哪一次”

IQuantumProgramAdapter
  负责输入生成、命令构建、输出解析

IComputeBackend
  负责本机或远程执行

ICalculationScheduler
  负责并发控制，当前只预留接口
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 代码中除默认程序 `Gaussian 16` 外，不包含具体计算程序绑定

---

## v0.8.0（2026-09-15）：技能命名统一去除 Container

### 版本

- 当前版本：`0.8.0`
- 日期：2026-09-15
- 版本类型：架构命名重构（接口路径与字段名同步调整）

### 改动目的

为避免“技能容器”“DI 容器”“Docker 容器”三个概念混淆，统一技能相关命名，不再使用 `Container`：

```text
IContainerRegistry → ISkillRegistry
ContainerRegistry  → SkillRegistry
ISkillContainer    → ISkill
EchoSkillContainer → EchoSkill
ContainerDescriptor → SkillDescriptor
WorkflowNode.Container → WorkflowNode.Skill
TaskRequest.ContainerId → TaskRequest.SkillId
```

### 改动内容

Domain：

- `ISkillContainer` 改为 `ISkill`
- `IContainerRegistry` 改为 `ISkillRegistry`
- `ContainerDescriptor` 改为 `SkillDescriptor`
- `WorkflowNode.Container` 改为 `WorkflowNode.Skill`
- `TaskRequest.ContainerId` 改为 `TaskRequest.SkillId`

Core：

- `ContainerRegistry.cs` 改为 `SkillRegistry.cs`
- `EchoSkillContainer.cs` 改为 `EchoSkill.cs`
- `WorkflowEngine` 改为通过 `ISkillRegistry` 解析并调用 `ISkill`

Api：

- `RegisterContainerRequest` 改为 `RegisterSkillRequest`
- `ContainerEndpoints` 改为 `SkillEndpoints`
- 路由 `/containers` 改为 `/skills`
- 路由 `/containers/register` 改为 `/skills/register`
- 示例工作流 JSON 字段 `container` 改为 `skill`

Tests：

- 测试实现 `RecordingContainer` 改为 `RecordingSkill`
- 所有技能接口与注册表引用同步更新

文档：

- `README.md` 和教程中的技能命名同步更新
- `docs/Coding-Conventions.md` 增加技能命名约定：技能相关命名禁止使用 `Container`

### 教程式说明

命名职责现在非常明确：

```text
DI 容器（DI Container）
  负责创建对象和管理生命周期

SkillRegistry
  负责登记和查找技能

ISkill / EchoSkill
  负责执行具体科学能力

Docker 容器
  以后作为技能的运行环境
```

接口路径变化：

```text
旧：GET  /containers
新：GET  /skills

旧：POST /containers/register
新：POST /skills/register
```

工作流定义变化：

```json
旧：{ "id": "structure", "container": "echo" }
新：{ "id": "structure", "skill": "echo" }
```

### 验证

- `dotnet build ChemSculptor.slnx`：构建通过
- `dotnet test ChemSculptor.slnx`：测试通过
- 技能相关命名扫描不再出现 `Container`（WinForms 自带 `SplitContainer` 除外）

---

## v0.7.0（2026-09-13）：几何接收与解析框架骨架

### 版本

- 当前版本：`0.7.0`
- 日期：2026-09-13
- 版本类型：新增框架（不包含真实解析逻辑）

### 改动目的

为复杂几何输入（例如 ONIOM 分层模型）建立可扩展的处理框架，把以下四层职责分开：

```text
原始文件层 → 解析器层 → 规范模型层 → 验证层
```

当前只搭建接口、模型和流程骨架，不实现任何具体格式解析，也不改动现有 `/geometries` 的简单 XYZ 行为。

### 改动内容

在 `ChemSculptor.InputProcessor` 下新增 `GeometryIntake` 框架：

```text
GeometryIntakeModels.cs
  原始文件、规范几何、原子、片段、层、链接原子、约束、诊断

GeometryParserFramework.cs
  解析器接口、解析结果、解析器注册表
  XYZ / Gaussian / ORCA / ONIOM 解析器骨架

GeometryValidationFramework.cs
  验证器接口、验证报告、骨架验证器

GeometryAssetFramework.cs
  几何资产、资产仓储接口、内存实现

GeometryIntakeService.cs
  串联“提交 → 选择解析器 → 解析 → 验证 → 保存资产”
```

### 教程式说明

#### 处理流程

```text
RawGeometrySubmission（原始提交）
   ↓
GeometryParserRegistry（选择解析器）
   ↓
IGeometryParser（格式解析器，当前为骨架）
   ↓
CanonicalGeometry（规范几何模型）
   ↓
IGeometryValidator（验证器，当前为骨架）
   ↓
GeometryAsset（原始文件 + 解析结果 + 验证报告）
   ↓
InMemoryGeometryAssetRepository（保存资产）
```

#### 分层设计要点

- 接收层只保存原始文件，不解释格式
- 每种格式对应一个解析器，不再把不同格式的判断堆进同一个方法
- 解析器统一输出 `CanonicalGeometry`
- ONIOM 的层、片段、链接原子、约束属于规范模型，不属于计算任务参数
- 验证器独立于解析器，便于以后增加坐标、分层、链接原子等检查

#### 当前状态

```text
已建立：接口、模型、注册表、资产仓储、接收服务
未实现：具体格式解析、具体验证规则
未接入：Api 端点仍使用原有简单 XYZ 解析流程
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 现有 `/geometries` 行为和已有客户端功能未改变

---

## v0.6.1（2026-09-12）：补充标准中文 C# 注释并最终确定编码约定

### 版本

- 当前版本：`0.6.1`
- 日期：2026-09-12
- 版本类型：代码注释与规范补充（不改变运行行为）

### 改动目的

为现有代码补充标准中文 C# 注释，并把以下原则正式确立为项目约定：

1. 禁止顶层语句
2. 去除可选语法糖
3. `async / await` 不属于要避免的语法糖，必须保留
4. 不使用扩展方法调用写法，改为显式静态调用
5. 注释统一使用标准 C# 注释风格与中文说明

### 改动内容

为以下项目补充 XML 注释与关键逻辑注释：

```text
ChemSculptor.Domain
ChemSculptor.Core
ChemSculptor.InputProcessor
ChemSculptor.Api
ChemSculptor.WinForms
ChemSculptor.Core.Tests
```

注释规范：

- 公共类型、接口、方法和属性使用 `/// <summary>` XML 注释
- 关键内部逻辑使用 `//` 中文行注释
- 注释说明职责、输入输出、边界条件与平台必需机制的原因

同步更新：

```text
docs/Coding-Conventions.md
  增加注释规范章节
  明确 async/await 例外
  明确扩展方法必须显式静态调用
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过

---

## v0.6.0（2026-09-10）：全项目去除可选语法糖

### 版本

- 当前版本：`0.6.0`
- 日期：2026-09-10
- 版本类型：代码风格与架构重构（行为保持一致）

### 改动目的

按 `docs/Coding-Conventions.md` 的最终口径重构已有代码：

- 保留 `async / await`
- 不使用扩展方法调用写法
- 去除其他可选语法糖，采用传统、显式写法

### 改动内容

Domain：

- `record`、`required`、`init` 改为普通类与可读写属性
- 集合表达式、对象初始化器改为显式构造与逐项赋值

Core：

- LINQ、Lambda、集合表达式、对象初始化器、表达式体成员改为 `for` / `foreach`、命名方法与显式赋值
- `WorkflowStateRules` 改为显式构建转换表
- `WorkflowEngine` 改为显式循环调度，不使用 LINQ

InputProcessor：

- 几何与请求模型改为普通类
- LINQ、集合表达式、范围切片改为循环、显式集合与 `Substring`

Api：

- 所有端点改为显式静态调用：
  `EndpointRouteBuilderExtensions.MapGet / MapPost / MapGroup`
- 端点处理改为命名静态方法，不再使用 Lambda
- 匿名响应类型改为显式响应类
- `Program.cs` 保持传统 `Main`，依赖查询不再使用扩展方法
- `POST /client/jobs` 输入从 multipart 表单改为 `text/plain` 原始文本，避免表单与防伪依赖

WinForms：

- 保持三区对话界面
- 事件 Lambda 改为命名事件方法
- LINQ、switch 表达式、字符串插值、范围切片、`??=`、对象初始化器与集合表达式全部改为传统写法
- 提交任务改为直接发送 `text/plain` 文本，与 Api 新输入方式一致

Tests：

- 测试数据构造改为显式对象与集合
- `Assert.All` 的 Lambda 改为 `foreach`

### 教程式说明

这次改动不改变系统架构，只改变代码表达方式。核心映射如下：

```text
record              → class + 可读写属性
对象初始化器         → new + 逐项属性赋值
集合表达式 []        → new List<T>() / new T[] { }
LINQ                → for / foreach
Lambda              → 命名方法
扩展方法 app.MapGet  → EndpointRouteBuilderExtensions.MapGet(app, ...)
匿名类型 new { }     → 显式响应类
字符串插值 $"..."    → 字符串拼接
三元 ? :             → if / else
?? / ??=            → if 判断
switch 表达式        → if / else
范围切片 [..n]       → Substring
```

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 运行时回归：
  - 根端点返回 13 个接口
  - `/geometries` 接收水分子坐标，返回 `H2O`、3 个原子
  - `/client/jobs` 接收纯文本任务，任务最终为 `Passed` 且结果可读取

---

## v0.5.1（2026-09-10）：Api 顶层语句改为传统 Main 写法

### 版本

- 当前版本：`0.5.1`
- 日期：2026-09-10
- 版本类型：代码风格重构（行为不变）

### 改动目的

按用户学习习惯，把 `ChemSculptor.Api` 的顶层语句改写为传统 `public static async Task Main` 形式，便于对照学习，不改变任何运行行为。

### 改动内容

- 重写 `src/ChemSculptor.Api/Program.cs`：
  - 新增 `namespace ChemSculptor.Api` 与 `public static class Program`
  - 原顶层启动语句全部移入 `Main(string[] args)`
  - 服务注册、示例工作流载入、端点挂载、`app.Run()` 顺序保持不变

### 验证

- `dotnet build src/ChemSculptor.Api/ChemSculptor.Api.csproj`：0 警告 0 错误

---

## v0.5.0（2026-09-07）：WinForms 对话式界面框架

### 版本

- 当前版本：`0.5.0`
- 日期：2026-09-07
- 版本类型：界面重构（仅 WinForms，不涉及 Api/Core/Domain）

### 改动目的

把 WinForms 界面从“工具按钮面板”升级为 Codex 风格的对话式外壳：左侧会话列表 + 中间对话流 + 底部输入区，为以后“自然语言 → 服务器理解 → 人工确认”的人机协同流程预留界面形态。

### 改动内容

仅修改 `ChemSculptor.WinForms`：

- 重写 `MainForm.cs`：新增左侧会话列表、中间消息流、底部输入与操作区。
- 新增本地会话模型：`ChatSession`、`ChatMessage`（仅内存，会话切换不丢失当前运行状态）。
- 保留并接入现有能力：
  - 选择 txt
  - 发送坐标到 `POST /geometries`
  - 提交任务到 `POST /client/jobs`
  - 轮询任务状态与结果
  - 保存最近一份结果 txt
- 自然语言输入当前只记录为会话消息，并提示“理解功能后续接入”，不假装服务器已支持对话。

### 教程式说明

#### 界面结构

```text
左侧：会话列表（新建会话 / 切换会话）
中间：对话流（用户消息、系统消息、错误与提示）
底部：自然语言输入 + 发送
顶部：服务地址、选择 txt、保存结果
```

#### 当前可用的三条真实链路

```text
1. 输入文本 → 记录为用户消息（暂不发送服务器）
2. 选择坐标 txt → 发送坐标 → /geometries → 系统消息显示分子式
3. 选择任务 txt → 提交任务 → /client/jobs → 轮询状态 → 结果消息
```

### 验证

- `dotnet build src/ChemSculptor.WinForms/ChemSculptor.WinForms.csproj`：0 警告 0 错误
- Api/Core/Domain/Tests 未改动

---

## v0.4.0（2026-09-06）：服务器端接收分子坐标（阶段 A 第一切片）

### 版本

- 当前版本：`0.4.0`
- 日期：2026-09-06
- 版本类型：新增功能（MINOR）

### 改动目的

按照阶段 A 推进“坐标进、结论出”的最小闭环，先打通第一环：客户端把分子坐标文本发送到服务器，服务器完成接收、解析并返回结构化确认。

### 改动内容

`ChemSculptor.InputProcessor` 新增几何文本解析能力：

- `GeometryModels.cs`：`GeometryAtom`、`MolecularGeometry`。
- `GeometryTextParser.cs`：`IGeometryTextParser` 接口和 XYZ 文本解析实现，支持标准 XYZ 文本（原子数行、注释行、`元素 x y z` 坐标行），输出分子式和原子列表，并对非坐标行/数量不一致给出诊断。

`ChemSculptor.Api` 新增坐标接收端点：

- 新增 `Endpoints/GeometryEndpoints.cs`：`POST /geometries`，接收 `text/plain` 坐标文本，解析后返回分子式、原子数、原子坐标和诊断。
- `Program.cs` 注册 `IGeometryTextParser`，挂载端点，并把 `POST /geometries` 加入根端点列表。

`ChemSculptor.WinForms` 增加坐标发送入口：

- 新增“发送坐标”按钮，复用当前选择的 txt。
- 新增 `GeometryAtomDto`、`GeometrySubmitResult` 本地模型。
- 发送成功后显示服务器返回的分子式、原子数和原子列表。

新增示例文件：

```text
samples/water.xyz.txt
```

### 教程式说明

#### 数据流

```text
WinForms 选择 water.xyz.txt
  → 点击“发送坐标”
  → POST /geometries（text/plain 原始坐标文本）
  → GeometryTextParser 解析 XYZ
  → 返回 JSON：{ formula, atomCount, atoms }
```

#### XYZ 文本格式

```text
3
water molecule
O 0.000000 0.000000 0.117300
H 0.000000 0.757200 -0.469200
H 0.000000 -0.757200 -0.469200
```

第 1 行是原子数，第 2 行是名称，之后每行是 `元素 x y z`。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 端到端实测：向 `POST /geometries` 发送 `samples/water.xyz.txt`，服务器返回 `H2O`、3 个原子及完整坐标，诊断为空。

---

## v0.3.0（2026-09-03）：WinForms 与 ChemSculptor 服务器端独立

### 版本

- 当前版本：`0.3.0`
- 日期：2026-09-03
- 版本类型：架构调整 + 新增功能（MINOR）

### 改动目的

按用户的界面约束调整架构：

1. WinForms 中禁止出现任何 `using ChemSculptor`，界面不再引用内核或领域模型。
2. WinForms 只负责：接收客户的 txt 输入 → 发送给 ChemSculptor → 被动轮询反馈 → 接收结束信息和结果 txt。
3. 单机版和以后的服务器版保持同构：单机时 ChemSculptor 本体运行在本地 Api；部署到服务器后，WinForms 只改服务地址即可。

### 改动内容

`ChemSculptor.WinForms` 重写为纯 HTTP 客户端：

- 移除对 `ChemSculptor.Core`、`ChemSculptor.Domain` 的项目引用。
- 删除 `Services/IChemSculptorService.cs` 和 `Services/LocalChemSculptorService.cs`。
- 新增本地 DTO：`Models/ClientJobSummary.cs`。
- `MainForm` 通过 `HttpClient` 完成：选择 txt → 上传到 `/client/jobs` → 定时轮询 `/client/jobs/{id}/status` → 结果就绪后读取 `/client/jobs/{id}/result`，并可保存为 txt。
- 界面层不再使用任何 ChemSculptor 命名空间。

新增 `ChemSculptor.InputProcessor`（客户输入解析工程）：

- `IClientInputParser`：输入解析器接口，未来可扩展 JSON/二进制解析器。
- `TextClientInputParser`：当前文本解析实现。
- `ProcessedClientRequest`：解析结果，包含工作流 Id、目标描述和原始文本。
- 文本请求格式 v1：支持 `workflow:` 和 `goal:` 行；未指定工作流时默认 `tadf_mechanism_diagnosis`。

`ChemSculptor.Api` 增加客户端作业能力：

- 新增 `Client/ClientJob.cs`、`Client/ClientJobService.cs`。
- 新增 `Endpoints/ClientJobEndpoints.cs`。
- `Program.cs` 注册 `TextClientInputParser` 和 `ClientJobService`，并挂载客户端作业端点。
- `ChemSculptor.Api.csproj` 引用 `ChemSculptor.InputProcessor`。
- 作业在 Api 进程内后台执行（单用户阶段不建队列，按用户决定预留位置）。

### 教程式说明

#### 现在的整体形态

```text
WinForms 客户端（纯界面，零 ChemSculptor 依赖）
   │ 选择用户 txt → HTTP 上传
   │ 定时轮询状态
   │ 下载结果 txt
   ▼
ChemSculptor.Api（本地运行 = 单机版；以后部署到服务器 = 服务器版）
   ├── InputProcessor：把客户输入文件解析成内核可用请求
   └── WorkflowEngine + 技能容器
```

#### 一次完整数据流

```text
用户选择 txt
  → WinForms POST /client/jobs（multipart 上传原始文本）
  → Api 保存任务，后台执行
  → InputProcessor 解析 workflow:/goal: 行
  → Api 选择工作流模板并调用 WorkflowEngine
  → WinForms 定时轮询 GET /client/jobs/{id}/status
  → 状态变为 Passed/Failed 且 HasResult=true
  → WinForms GET /client/jobs/{id}/result，得到结果 txt
```

#### 文本请求格式示例（v1，占位）

```text
workflow: tadf_mechanism_diagnosis
goal: 判断超分子体系是否为 TADF 并定位主要发光通道
```

#### 新增端点

| 方法 | 路径 | 作用 |
|---|---|---|
| `POST` | `/client/jobs` | 接收客户 txt，创建客户端任务 |
| `GET` | `/client/jobs/{id}/status` | 轮询任务状态 |
| `GET` | `/client/jobs/{id}/result` | 下载结果 txt |

#### 如何运行

先启动 Api：

```powershell
dotnet run --project src/ChemSculptor.Api --urls http://127.0.0.1:5080
```

再运行 WinForms：

```powershell
dotnet run --project src/ChemSculptor.WinForms
```

WinForms 顶部的服务地址默认是 `http://127.0.0.1:5080`；以后服务器版只需改成远程地址。

### 验证

- `dotnet build ChemSculptor.slnx`：0 警告 0 错误
- `dotnet test ChemSculptor.slnx`：5/5 通过
- 客户端/服务器独立模式代码已按当前代码跑通（用户环境确认）；本文档更新仅涉及记录同步，不改动代码。

---

## v0.2.0（2026-09-02）：WinForms 单机版（历史过渡，已从当前代码中移除）

> 本节是历史记录。v0.2.0 描述的 WinForms 直连内核形态在当前仓库已不存在，相关文件已删除；当前客户端形态以 v0.3.0 为准。

### 版本

- 当前版本：`0.2.0`（历史）
- 日期：2026-09-02
- 版本类型：新增功能（MINOR）

### 当时做了什么

- 把 WinForms 加入解决方案，作为“单机版操作界面”。
- WinForms 曾直接引用 `ChemSculptor.Core` 与 `ChemSculptor.Domain`。
- 提供过 `IChemSculptorService` / `LocalChemSculptorService` 服务层，以及“载入示例 / 刷新 / 执行所选”按钮。
- `MainForm` 曾订阅事件总线，把 `task.started`、`task.completed` 等事件实时显示到日志框。

### 与当前代码的差异

当前 `ChemSculptor.WinForms` 已不含上述服务和按钮：

- csproj 不再引用 `ChemSculptor.Core` 与 `ChemSculptor.Domain`，没有任何项目引用。
- 不存在 `Services/` 文件夹，不存在 `IChemSculptorService`、`LocalChemSculptorService`。
- 不存在“载入示例 / 刷新 / 执行所选”等直连内核按钮。
- 界面只保留纯 HTTP 客户端功能：服务地址、选择 txt、提交任务、轮询状态、显示并保存结果。

v0.2.0 的代码形态已被 v0.3.0 取代，仅作为过程记录保留。

### 验证（当时）

- v0.2.0 时代构建与测试通过；其代码现已删除，不再作为运行基线。

---

## v0.1.0（历史）：框架骨架

首次建立的 C# 框架骨架：Domain 契约、Core 极简内核、Api 宿主、Tests 测试，以及示例工作流和教程文档。本次改动从 v0.1.0 开始登记版本号。

---

## 未来改动记录模板

以后每次改动后，在文档顶部追加以下格式：

```markdown
## vX.Y.Z（日期）：改动标题

### 版本
- 当前版本：X.Y.Z
- 日期：YYYY-MM-DD
- 版本类型：新增功能 / 修复 / 架构调整

### 改动目的
为什么做这次改动。

### 改动内容
改了哪些项目/文件，新增、修改、删除了什么。

### 教程式说明
这次改动在系统里扮演什么角色，和已有部分的关系，如何运行验证。

### 验证
- 构建结果
- 测试结果
- 其他验证方式
```
