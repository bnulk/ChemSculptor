# 当前交接

## 当前阶段

阶段 3：建立科学点文件解析服务。已完成。

```text
根据 PointArtifactReference 解析服务器真实文件
解析清单并逐文件报告可用性
打开经过完整验证的单个文件
拒绝 run 目录之外的服务器路径
缺失文件不影响其它文件下载
```

## 已完成

```text
新增 IScientificArtifactResolver
新增 ScientificArtifactResolver
新增清单、科学点、文件和打开结果模型
服务已在科学数据提取项目注册
路径越界、链接、JobId 不一致和 SHA-256 均被验证
缺失文件只影响自身，不影响同一成果中的其它文件
```

## 修改文件

```text
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/IScientificArtifactResolver.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/ScientificArtifactResolver.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactManifest.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactPointManifest.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactFileManifest.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactContent.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactOpenResult.cs
src/ChemSculptor.ScientificData.Extraction/ScientificDataExtractionServiceRegistration.cs
tests/ChemSculptor.Core.Tests/ScientificArtifactResolverTests.cs
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：88/88 通过
```

## 尚未完成

```text
阶段 3 没有遗留项。

阶段 4 尚未开始。
```

## 下一步精确任务

下一阶段由用户指定。当前建议继续实现成果包清单和下载 API：

```text
组合 ScientificArtifactManifest 形成成果包清单
新增成果包清单端点
新增按科学点下载端点
客户端领取完整科学点文件成果包
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
