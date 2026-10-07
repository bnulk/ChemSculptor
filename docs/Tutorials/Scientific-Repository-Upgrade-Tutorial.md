# ChemSculptor 科学仓库升级教程

## 1. 这份教程要解决什么问题

这份教程总结当前工程刚刚完成的“科学仓库升级”。

升级前，工程主要围绕“计算作业”保存数据：

```text
CalculationJob
CalculationResult
输入文件
输出文件
异常和恢复过程
```

这些数据可以回答：

```text
程序运行过什么
什么时候运行
用什么方法运行
是否正常结束
```

但它们不能直接回答真正的科研问题：

```text
哪些数据已经进入科学结论
哪个点是原始点
哪个点是修正后的点
哪些物理量由哪些点导出
论文或报告中的结论对应哪些原始文件
```

科学仓库升级的核心变化，是从“作业仓库”升级为“科学点仓库”。

## 2. 核心思想

整个升级可以压缩成一句话：

```text
科学成果由点和点的集合组成，
点之间的关系描述了组织结构，
物理量由点或关系导出，
文本只负责描述点，不能替代点。
```

核心对象之间的关系：

```text
ScientificResult
  ├── CalculationPointSet
  │     ├── CalculationPoint
  │     └── CalculationPointRelation
  └── ScientificObservable
```

每个计算点还可以拥有自己的原始文件引用：

```text
CalculationPoint
  ├── CalculationJobId
  ├── Geometry
  ├── ElectronicState
  ├── CalculationModel
  ├── Properties
  ├── Validations
  ├── Provenance
  └── Artifacts
```

## 3. 四个关键概念

### 3.1 科学点

一个科学点表示：

```text
确定几何
+ 确定电子态
+ 确定计算模型
= 一个科学数据单位
```

例如氧气：

```text
O2，Singlet
O2，Triplet
```

即使几何相同，只要电子态不同，就是不同的科学点。

### 3.2 计算作业

作业是产生科学点的过程：

```text
CalculationJob
  → CalculationResult
  → CalculationPoint
```

作业可以失败、重跑、被替代和保留历史。

科学点则是最终参与科研解释的数据单位。

### 3.3 文件引用

科学点不直接保存文件内容，而是保存引用：

```text
PointArtifactReference
  ├── ArtifactId
  ├── CalculationJobId
  ├── Kind
  ├── RelativePath
  ├── DownloadFileName
  ├── CanonicalStem
  ├── CanonicalExtension
  ├── MediaType
  ├── Length
  ├── Sha256
  ├── CanDownload
  └── CanUseForRestart
```

`RelativePath` 指向服务器 `run` 目录中的真实文件。

`DownloadFileName` 是客户端保存时使用的逻辑文件名。

### 3.4 叙述文本

叙述文本不是科学结果本身：

```text
point-summary
  说明一个点是什么

provenance
  说明点从哪里来

organization
  说明点和点之间如何组织

final-summary
  描述最终接受点和物理量

conversation
  保存客户端会话
```

前三类和最终摘要只从 `ScientificResult` 生成。

`conversation.txt` 是客户端会话记录，不作为科学结论。

## 4. 升级后的整体架构

```text
WinForms 客户端
  ↓ HTTP
ChemSculptor.Api
  ↓
ChemSculptor.Agent / Conversation
  ↓
计算、验证、异常处理和科学数据提取
  ↓
ChemSculptor.ScientificData
  ↓
ChemSculptor.ScientificSummary
  ↓
Artifact API / Narrative API
  ↓
WinForms 成果包保存
```

依赖原则：

```text
ScientificData 保持纯数据模型
ScientificSummary 只读取 ScientificData
客户端不引用服务端项目
通用工作流不包含具体计算程序规则
报告和摘要只从 ScientificData 读取
```

## 5. 阶段 0：冻结文件成果包规范

### 阶段目标

在写代码之前，先固定规则：

```text
每个点有哪些字段
每个文件如何命名
成果包目录如何组织
原始点、被取代点和失败点如何保留
文本和点是什么关系
```

规范文档：

```text
docs/Scientific-Point-Artifact-Package-Spec.md
```

### 为什么要先冻结规范

如果先写代码再想命名和目录，很容易出现：

```text
不同模块产生不同文件名
客户端和服务端各自解释路径
后续结果包无法稳定解析
```

因此先固定“共同语言”，再让代码逐步实现。

### 统一命名

```text
<Formula>-<Role>-m<Multiplicity>
```

示例：

```text
O2-original-m1
O2-recovery-m3
```

最终文件：

```text
O2-original-m1.gjf
O2-original-m1.fchk
O2-original-m1.log
```

## 6. 阶段 1：扩展科学点模型

### 阶段目标

让 `CalculationPoint` 能够保存自己的原始文件引用。

主要文件：

```text
src/ChemSculptor.ScientificData/Models/CalculationPoint.cs
src/ChemSculptor.ScientificData/Models/PointArtifactReference.cs
src/ChemSculptor.ScientificData/Models/PointProgramData.cs
```

新增字段：

```text
CalculationPoint.CalculationJobId
CalculationPoint.ProgramData
CalculationPoint.Artifacts
```

`PointArtifactReference` 增加：

```text
CanonicalStem
CanonicalExtension
```

关系：

```text
DownloadFileName = CanonicalStem + CanonicalExtension
```

例如：

```text
CanonicalStem      = O2-recovery-m3
CanonicalExtension = .gjf
DownloadFileName   = O2-recovery-m3.gjf
```

## 7. 阶段 2：提取器填充文件引用

### 阶段目标

从计算作业和 `CalculationResult` 中复制文件清单到每个科学点。

主要文件：

```text
src/ChemSculptor.ScientificData.Extraction/ScientificResultExtractor.cs
```

数据流：

```text
原始点
  ← 原始作业 CalculationResult.Artifacts

恢复点
  ← 恢复作业 CalculationResult.Artifacts
```

每个点设置自己的：

```text
CalculationJobId
Artifacts
```

### 只保存引用

提取器只做：

```text
读取 CalculationResult.Artifacts
转换 PointArtifactReference
设置下载名
设置 SHA-256
设置用途
```

它不：

```text
复制文件
移动文件
修改原始 run 目录
```

### 共享工厂

阶段 2 之后，文件引用创建逻辑被进一步抽取为：

```text
ScientificArtifactReferenceFactory
```

新结果提取调用统一工厂，避免命名逻辑分散。

## 8. 阶段 3：建立文件解析服务

### 阶段目标

把 `PointArtifactReference.RelativePath` 解析为服务器真实文件。

主要文件：

```text
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/IScientificArtifactResolver.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/ScientificArtifactResolver.cs
```

接口：

```text
IScientificArtifactResolver
  ResolveManifestAsync(resultId)
  OpenArtifactAsync(resultId, pointId, artifactId)
```

### 清单解析

```text
读取 ScientificResult
遍历 PointSet.Points
验证科学点和文件引用的 CalculationJobId
解析对应作业 run 目录
检查文件是否存在
验证 SHA-256
生成下载名
```

### 安全规则

解析服务必须拒绝：

```text
绝对路径
规范化后越出 run 目录的路径
包含文件系统链接的路径
CalculationJobId 不一致的文件引用
未标记 CanDownload 的文件
```

核心思想是：

```text
客户端不能指定任意服务器路径
客户端只能通过 resultId、pointId、artifactId 请求
服务器自己解析真实路径
```

### 缺失文件

单个文件缺失时：

```text
该文件标记为不可用
给出明确错误
其它文件继续解析
其它文件仍可下载
```

## 9. 阶段 4：增加成果包 API

### 阶段目标

客户端通过一个清单获取全部科学点的文件。

主要文件：

```text
src/ChemSculptor.Api/Endpoints/ScientificArtifactEndpoints.cs
src/ChemSculptor.Api/ScientificArtifactResponseMapper.cs
src/ChemSculptor.Api/ScientificResultLookup.cs
```

新增端点：

```text
GET /scientific-results/{resultId}/artifact-manifest
GET /scientific-results/{resultId}/artifacts/{pointId}/{artifactId}
GET /calculations/{jobId}/artifact-manifest
```

清单结构：

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
```

### 按作业查询

`ScientificResultLookup` 根据 `jobId` 查找所属科学成果。

它支持：

```text
根作业
恢复作业
派生作业
```

## 10. 阶段 5：重写 WinForms“保存”按钮

### 阶段目标

客户端不再平铺保存最近一个作业的文件，而是按成果包清单保存整个成果包。

主要文件：

```text
src/ChemSculptor.WinForms/MainForm.cs
src/ChemSculptor.WinForms/Models/Responses/ResponseModels.cs
```

保存目录：

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

阶段 6 完成后，每个点目录进一步包含：

```text
point-summary.txt
provenance.txt
```

### 保存流程

```text
GET /calculations/{jobId}/artifact-manifest
创建 scientific-result-<rootJobId>
遍历 Points
创建点目录
遍历 Files
按 DownloadPath 下载
按 DownloadFileName 保存
读取 narrative
写点摘要和来源
写关系、物理量、最终摘要和组织方式
写客户端对话记录
```

### 路径安全

客户端使用 `GetSafePathSegment`：

```text
拒绝空段
拒绝 "." 和 ".."
拒绝根路径
拒绝非法文件名字符
拒绝包含目录部分的字符串
```

服务端已经做了路径安全，客户端再做一次防御性检查。

## 11. 阶段 6：生成叙述性文本

### 阶段目标

让文本只从 `ScientificResult` 生成。

主要文件：

```text
src/ChemSculptor.ScientificSummary/DefaultScientificNarrativeBuilder.cs
src/ChemSculptor.ScientificSummary/ScientificNarrativeService.cs
src/ChemSculptor.Api/Endpoints/ScientificNarrativeEndpoints.cs
```

新增服务：

```text
IScientificNarrativeBuilder
DefaultScientificNarrativeBuilder

IScientificNarrativeService
ScientificNarrativeService
```

### 生成内容

```text
PointSummary
  点是什么

Provenance
  点从哪里来

Organization
  点和关系如何组织

FinalSummary
  最终接受点和物理量
```

### 追溯规则

```text
PointSummary 必须包含 PointId
Provenance 必须包含 PointId
Relation 必须包含 FromPointId 和 ToPointId
Observable 必须包含 PointIds
FinalSummary 引用 AcceptedPointId
```

### 文本边界

文本生成器不读取：

```text
计算输出中的原始文本判断
客户端对话中的结论
LLM 外部知识
没有进入 ScientificResult 的解释
```

它只把科学数据转换为可阅读文本。

## 12. 阶段 7：完整氧气验证

### 验证目标

用真实 O2 任务验证：

```text
客户端提交
单点计算
稳定性检查
矫正计算
科学数据仓储
原始点文件下载
恢复点文件下载
对话文本保存
最终摘要保存
```

### 验证结果

真实任务生成两个科学点：

```text
01-original-m1
  多重度：1
  状态：Superseded
  能量：-150.210765371 Hartree

02-recovery-m3
  多重度：3
  状态：Accepted
  能量：-150.274273534 Hartree
```

每个点有 6 个计算文件：

```text
.gjf
.fchk
.log
.chk
补充日志
```

成果包中可以看到：

```text
O2-original-m1.gjf
O2-original-m1.fchk
O2-original-m1.log

O2-recovery-m3.gjf
O2-recovery-m3.fchk
O2-recovery-m3.log
```

### 验证限制

当前验证按照 WinForms 保存按钮使用的同一组 API 和目录规则执行等价流程。

实际 GUI 鼠标点击仍需人工补做一次。

## 13. 一次完整的 O2 数据流

```text
客户端提交 O2 坐标和“单点计算”
  ↓
Agent 识别单点任务
  ↓
创建 CalculationJob
  ↓
计算输入生成
  ↓
Gaussian 单点计算
  ↓
计算结果解析
  ↓
波函数稳定性检查
  ↓
发现 RHF -> UHF 不稳定
  ↓
创建恢复作业
  ↓
三重态恢复点计算
  ↓
恢复点稳定性复检
  ↓
ScientificResultExtractor
  ↓
ScientificResult
  ├── 原始点
  ├── 恢复点
  ├── DerivedFrom 关系
  └── 单点能量
  ↓
ScientificData 仓储
  ↓
Artifact Manifest
  ↓
Narrative Package
  ↓
客户端成果包
```

## 14. 成果包目录

```text
scientific-result-<rootJobId>/
  points/
    01-original-m1/
      point-summary.txt
      provenance.txt
      O2-original-m1.gjf
      O2-original-m1.fchk
      O2-original-m1.log
      O2-original-m1.chk
    02-recovery-m3/
      point-summary.txt
      provenance.txt
      O2-recovery-m3.gjf
      O2-recovery-m3.fchk
      O2-recovery-m3.log
      O2-recovery-m3.chk
  relations.json
  observables.json
  artifact-manifest.json
  narrative/
    final-summary.txt
    conversation.txt
    organization.txt
```

这个目录同时回答：

```text
科学结论是什么
结论来自哪些点
点和点之间是什么关系
每个点由哪个作业产生
每个点的原始文件在哪里
```

## 15. 关键 API

```text
GET /scientific-results/{resultId}/artifact-manifest
GET /scientific-results/{resultId}/artifacts/{pointId}/{artifactId}
GET /calculations/{jobId}/artifact-manifest

GET /scientific-results/{resultId}/narrative
GET /calculations/{jobId}/narrative
```

## 16. 建议的代码阅读顺序

```text
1. ScientificDataEnums.cs
2. CalculationPoint.cs
3. CalculationPointRelation.cs
4. CalculationPointSet.cs
5. ScientificObservable.cs
6. ScientificResult.cs
7. PointArtifactReference.cs
8. ScientificArtifactReferenceFactory.cs
9. ScientificResultExtractor.cs
10. ScientificArtifactResolver.cs
11. ScientificArtifactEndpoints.cs
12. ScientificArtifactResponseMapper.cs
13. DefaultScientificNarrativeBuilder.cs
14. ScientificNarrativeService.cs
15. ScientificNarrativeEndpoints.cs
16. MainForm.SaveArtifactsAsync
17. ScientificArtifactResolverTests.cs
18. ScientificNarrativeBuilderTests.cs
```

## 17. 设计经验

### 17.1 先定义科学对象，再定义文件

不要从文件反推科学含义。

正确方向是：

```text
科学点
  → 文件引用
  → 文件下载
```

### 17.2 作业和科学点分开

```text
作业
  解释计算过程

科学点
  参与科研解释
```

### 17.3 原始路径和下载名分开

```text
RelativePath
  服务器真实位置

DownloadFileName
  客户端逻辑名称
```

### 17.4 文本不能反向创造数据

叙述文本只能引用已有科学点。

如果文本写了一个点，必须能找到：

```text
PointId
物理量
关系
原始文件
```

### 17.5 路径安全必须在服务端

客户端的 `pointId` 和 `artifactId` 只是逻辑标识。

服务器必须：

```text
自己查科学数据
自己查找作业目录
自己规范化路径
自己检查越界
自己验证 SHA-256
```

## 18. 后续扩展

以后增加新计算程序、优化、频率、IRC 或反应能时，应继续遵守：

```text
先增加科学点字段或关系
再增加文件引用
再增加通用叙述
最后由具体程序模块实现提取
```

可以继续增加：

```text
points/<...>/wavefunction.txt
points/<...>/frequencies.json
points/<...>/thermochemistry.json

relations.json
observables.json
checksums.sha256
package.zip
```

核心不变：

```text
点是科学仓库的中心
文件是点的证据
关系描述点之间结构
物理量由点和关系导出
文本只描述点
```

## 19. 最后总结

这次升级完成了从“保存计算过程”向“保存可追溯科学成果”的转变。

最终得到的是一个可以被下载、校验、解释和复用的科学成果包：

```text
科学点
+ 点间关系
+ 导出物理量
+ 原始文件引用
+ 叙述文本
= 科学仓库升级
```
