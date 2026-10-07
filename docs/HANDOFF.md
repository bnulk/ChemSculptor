# 当前交接

## 当前阶段

阶段 4：增加成果包 API。已完成。

```text
按 resultId 返回成果包清单
按 jobId 返回所属成果包清单
按科学点和文件标识下载单个文件
原始点和恢复点都进入清单
旧 artifacts 端点保持兼容
```

## 已完成

```text
新增 ScientificArtifactEndpoints
新增 ScientificArtifactManifestResponse
新增 ScientificArtifactPointResponse
新增 ScientificArtifactFileResponse
新增 ScientificArtifactResponseMapper
新增 ScientificResultLookup
清单包含 DirectoryName、Status 和 Multiplicity
恢复作业可以定位所属科学成果
```

## 修改文件

```text
src/ChemSculptor.Api/Endpoints/ScientificArtifactEndpoints.cs
src/ChemSculptor.Api/ScientificArtifactResponseMapper.cs
src/ChemSculptor.Api/ScientificResultLookup.cs
src/ChemSculptor.Api/Contracts.cs
src/ChemSculptor.Api/Program.cs
src/ChemSculptor.Api/Endpoints/ClientSummaryEndpoints.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactPointManifest.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/ScientificArtifactResolver.cs
tests/ChemSculptor.Core.Tests/ScientificArtifactApiTests.cs
tests/ChemSculptor.Core.Tests/ChemSculptor.Core.Tests.csproj
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：90/90 通过
```

## 尚未完成

```text
阶段 4 没有遗留项。

阶段 5 尚未开始。
```

## 下一步精确任务

下一阶段由用户指定。当前建议让 WinForms 领取清单并保存成果包：

```text
客户端请求 artifact-manifest
按 DownloadPath 下载全部文件
按 DirectoryName 分目录保存
保存对话文本和点摘要文本
```

候选实现内容：

```text
WinForms 成果包保存流程
下载进度和错误提示
成果包目录结构验证
未来压缩和断点续传预留
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
