# 科学点文件成果包数据规范

## 1. 版本

```text
方案名称：Scientific Point Artifact Package
规范版本：1
适用范围：ChemSculptor 科学数据仓库导出成果包
```

本规范只定义数据、目录、命名和引用规则，不定义业务算法。

## 2. 核心原则

科学点是核心对象。计算作业、程序和原始文件用于解释点的来源。

```text
科学点
  ← 计算作业产生
  ← 计算程序执行
  ← 原始文件支持
  ← 验证和矫正形成血缘
```

强制规则：

```text
每个科学点必须记录 CalculationJobId
每个科学点必须包含 Artifacts 列表
每个 Artifact 必须有稳定标识
文件下载名必须由服务端统一生成
文本只能描述点，不能替代点
原始点、被取代点、失败点都必须保留
```

## 3. 核心对象

```text
ScientificResult
  ResultId
  RootWorkflowId
  PointSet
  Observables
  Narratives

CalculationPoint
  PointId
  CalculationJobId
  Status
  Geometry
  ElectronicState
  CalculationModel
  ProgramData
  Properties
  Validations
  Provenance
  Artifacts

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
```

## 4. 科学点标识

`PointId` 在一次 `ScientificResult` 内必须唯一且稳定。

推荐规则：

```text
point-<CalculationJobId>
```

同一科学点在重新导出成果包时，`PointId` 不应变化。

## 5. 文件稳定标识

`ArtifactId` 在科学点内必须唯一且稳定。

推荐基于以下内容生成：

```text
CalculationJobId
+ 规范化 RelativePath
+ ArtifactKind
```

要求：

```text
不依赖文件顺序
不依赖下载文件名
不依赖当前时间
重新提取同一作业时得到同一 ArtifactId
```

## 6. Artifact 必填字段

每个 `PointArtifactReference` 必须包含：

```text
ArtifactId
CalculationJobId
Kind
RelativePath
DownloadFileName
CanonicalStem
CanonicalExtension
MediaType
Length
Sha256
CanDownload
CanUseForRestart
```

`RelativePath` 相对于该作业的 `run` 目录。

`DownloadFileName` 是客户端保存时使用的逻辑文件名，不改变服务器原始文件。

`CanonicalStem` 是不包含扩展名的规范文件名主体。

`CanonicalExtension` 是规范文件扩展名，包含前导点。

正常情况下：

```text
DownloadFileName = CanonicalStem + CanonicalExtension
```

如果同一科学点内出现扩展名冲突，工具可以在
`CanonicalStem` 末尾增加数字后缀，但仍必须满足上述关系。

## 6.1 提取规则

科学结果提取器从计算作业及其 `CalculationResult.Artifacts`
生成科学点文件引用。

```text
原始点
  ← 原始作业对应的 CalculationResult.Artifacts

恢复点
  ← 恢复作业对应的 CalculationResult.Artifacts
```

每个文件引用记录产生它的 `CalculationJobId`。

提取器只保存文件引用，不复制文件，不创建文件，也不修改原始计算目录。

## 7. 文件类别

```text
Input
  输入文件

PrimaryOutput
  主要计算输出

SupportingOutput
  辅助输出、标准输出或标准错误

RestartState
  可用于恢复或继续计算的状态文件

Other
  其它相关文件
```

文件类别属于通用科学模型，不包含具体程序扩展名。

## 8. 下载文件命名

每个科学点必须生成统一的基本文件名：

```text
<Formula>-<Role>-m<Multiplicity>
```

示例：

```text
O2-original-m1
O2-recovery-m3
```

最终文件名：

```text
O2-original-m1.gjf
O2-original-m1.log
O2-original-m1.fchk
O2-original-m1.chk

O2-recovery-m3.gjf
O2-recovery-m3.log
O2-recovery-m3.fchk
O2-recovery-m3.chk
```

规则：

```text
同一科学点的不同文件共享基本文件名
扩展名保留原始文件语义
不同科学点必须使用不同的基本文件名
名称只包含字母、数字、点、连字符和下划线
```

## 9. 成果包目录结构

```text
scientific-result-<rootWorkflowId>/
  manifest.json
  environment.json
  relations.json
  observables.json
  checksums.sha256

  points/
    01-original-m1/
      point.json
      program.json
      provenance.txt
      summary.txt
      artifacts/
        O2-original-m1.gjf
        O2-original-m1.log
        O2-original-m1.fchk
        O2-original-m1.chk

    02-recovery-m3/
      point.json
      program.json
      provenance.txt
      summary.txt
      artifacts/
        O2-recovery-m3.gjf
        O2-recovery-m3.log
        O2-recovery-m3.fchk
        O2-recovery-m3.chk

  narratives/
    final-summary.txt
    organization.txt
    conversation.txt
```

## 10. 目录名

科学点目录使用：

```text
<Sequence>-<Role>-m<Multiplicity>
```

示例：

```text
01-original-m1
02-recovery-m3
```

`Sequence` 从 `01` 开始。顺序表示点在结果中的组织顺序，不表示先后执行顺序。

## 11. manifest.json

清单是成果包的入口。

必须包含：

```text
ArchiveSpecVersion
ResultId
RootWorkflowId
CreatedAt
Points[]
TotalLength
Metadata
```

每个点：

```text
PointId
Sequence
DirectoryName
Status
CalculationJobId
Multiplicity
PointFile
ProgramFile
Files[]
```

每个文件：

```text
ArtifactId
PointId
CalculationJobId
Kind
RelativePath
DownloadFileName
MediaType
Length
Sha256
CanDownload
CanUseForRestart
```

## 12. 点的保留规则

无论状态如何，都必须保留：

```text
Candidate
Accepted
Rejected
Superseded
```

失败科学点也保留：

```text
计算失败
程序异常终止
验证失败
矫正失败
复检失败
```

如果没有可下载文件，仍保留点记录，并显式写出：

```text
Files = []
```

不得因为点不是最终 Accepted 点而删除其原始数据。

## 13. 文本与点的关系

文本是点的描述层：

```text
point.json
  点的结构化事实

program.json
  程序专门数据

provenance.txt
  点的来源和矫正过程

summary.txt
  单个点的自然语言说明

organization.txt
  智能体对多个点的组织说明

final-summary.txt
  面向人的最终摘要

conversation.txt
  客户端与智能体的交流记录
```

文本必须引用点：

```text
PointId
RelationId
ObservableId
```

文本不得替代结构化数据，也不得包含无法追溯到科学点的最终数值。

## 14. 完整性校验

成果包必须包含：

```text
checksums.sha256
```

每个可下载文件至少记录：

```text
相对路径
字节数
SHA-256
```

下载完成后必须能够验证：

```text
文件数量一致
文件路径一致
长度一致
摘要一致
```

## 15. 环境记录

`environment.json` 记录：

```text
ChemSculptor 版本
科学数据规范版本
代码提交标识
程序名称
程序版本
操作系统
处理器架构
计算时间和运行目录标识
```

程序专门细节放入对应点的 `program.json`。

## 16. 兼容性

```text
规范版本必须写入 manifest.json
新增字段必须保持向后兼容
旧科学数据可以读入，但没有文件引用时不得伪造
完整成果包必须包含规范版本 1 的文件清单
```

## 17. 本阶段不包含

```text
不实现成果包生成服务
不实现文件下载 API
不修改 WinForms
不修改现有工作流
不实现文本自动写作
不实现历史数据回填
```

## 18. 阶段 0 验收

```text
已固定科学点和文件的必要字段
已确定下载文件命名规则
已确定成果包目录结构
已确定原始点、派生点和失败点的保留规则
已确定文本与点的关系
未修改现有代码和工作流
```
