# 当前交接

## 当前阶段

科学点程序数据和原始文件引用。

```text
CalculationPoint 增加 CalculationJobId
CalculationPoint 增加 ProgramData
CalculationPoint 增加 Artifacts
新增 PointProgramData
新增 PointArtifactReference
extractor 填充程序数据和文件引用
Gaussian 适配器增加 chk、stdout、stderr
```

## 已完成

```text
科学点通用数据与程序专门数据已分离
原始点和恢复点分别保存 CalculationJobId
原始点和恢复点分别保存文件引用
文件下载逻辑名已统一
Release 构建通过
测试 81/81 通过
```

## 修改文件

```text
src/ChemSculptor.ScientificData/Models/CalculationPoint.cs
src/ChemSculptor.ScientificData/Models/PointCalculationModel.cs
src/ChemSculptor.ScientificData/Models/PointProgramData.cs
src/ChemSculptor.ScientificData/Models/PointArtifactReference.cs
src/ChemSculptor.ScientificData/Models/ScientificDataEnums.cs
src/ChemSculptor.ScientificData.Extraction/ScientificResultExtractor.cs
src/ChemSculptor.Compute.Gaussian/Gaussian16ProgramAdapter.cs
tests/ChemSculptor.Core.Tests/ScientificResultExtractorTests.cs
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

本次已修改代码并完成验证：

```text
Release 构建：0 警告，0 错误
测试：81/81 通过
```

## 尚未完成

```text
成果包清单 API 尚未实现
按科学点下载 API 尚未实现
WinForms 尚未保存完整科学点成果包
conversation.txt 尚未保存
final-summary.txt 尚未作为独立文件保存
历史科学数据尚未回填文件引用
新科学点文件引用尚未通过真实 O2 流程再次验证
```

## 下一步精确任务

只实现成果包清单和按科学点下载：

```text
新增 ScientificArchiveManifest
新增 ScientificArchivePoint
新增 ScientificArchiveFile
新增 IScientificArchiveService

服务根据 ScientificResult 和 CalculationPoint.Artifacts
解析服务器 run 目录中的真实文件

API 增加：
GET /scientific-results/{resultId}/archive-manifest
GET /scientific-results/{resultId}/archive/{pointId}/{artifactId}
GET /calculations/{jobId}/archive-manifest
GET /calculations/{jobId}/archive/{pointId}/{artifactId}
```

本阶段要求：

```text
不修改 CalculationPoint 模型
不修改提取器
不修改 WinForms
不修改工作流
增加服务测试和路径安全测试
完成后运行 Release 构建和测试
```

## 注意事项

```text
科学点是核心
作业只是计算过程
文本用于描述点的来源和组织方式
所有科学点相关原始文件最终都应被标识和下载
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
