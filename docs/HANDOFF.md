# 当前交接

## 当前阶段

阶段 1：扩展科学点模型。已完成。

```text
CalculationPoint 保存 CalculationJobId、ProgramData 和 Artifacts
PointArtifactReference 保存规范文件主体和规范扩展名
Artifact 模型支持 JSON 序列化和反序列化
旧科学成果 JSON 保持可读取
```

## 已完成

```text
CalculationPoint 已增加 CalculationJobId
CalculationPoint 已增加 ProgramData
CalculationPoint 已增加 Artifacts
PointArtifactReference 已增加 CanonicalStem
PointArtifactReference 已增加 CanonicalExtension
提取器已填写规范文件名主体和扩展名
旧数据兼容读取已由测试覆盖
```

## 修改文件

```text
src/ChemSculptor.ScientificData/Models/PointArtifactReference.cs
src/ChemSculptor.ScientificData.Extraction/ScientificResultExtractor.cs
tests/ChemSculptor.Core.Tests/ScientificResultExtractorTests.cs
tests/ChemSculptor.Core.Tests/FileScientificDataRepositoryTests.cs
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：83/83 通过
```

## 尚未完成

```text
阶段 1 没有遗留项。

阶段 2 尚未开始。
```

## 下一步精确任务

建议的阶段 2：实现成果包清单和文件解析服务：

```text
新增 ScientificArchiveManifest
新增 ScientificArchivePoint
新增 ScientificArchiveFile
新增 IScientificArchiveService

服务根据 ScientificResult 和 CalculationPoint.Artifacts
解析服务器 run 目录中的真实文件

本阶段先不增加 API
本阶段先不修改 WinForms
```

候选实现内容：

```text
ScientificArchiveManifest
ScientificArchivePoint
ScientificArchiveFile
IScientificArchiveService
根据 ScientificResult 和 CalculationPoint.Artifacts 解析 run 目录文件
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
