# 当前交接

## 当前阶段

阶段 7：兼容和历史数据迁移。已完成。

```text
旧 ScientificResult 没有文件清单时不报错
旧点在清单中显示“没有文件清单”
从 CalculationJob 和 CalculationResult 回填引用
回填幂等，可逐项执行
新结果和历史回填共用文件引用工厂
```

## 已完成

```text
新增 ScientificArtifactReferenceFactory
新增 IScientificArtifactBackfillService
新增 ScientificArtifactBackfillService
新增 POST /scientific-results/{resultId}/artifact-backfill
新增 POST /scientific-artifacts/backfill
清单增加 HasArtifactManifest 和 ArtifactManifestMessage
WinForms 显示旧点没有文件清单
点叙述显示文件清单状态
```

## 修改文件

```text
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/ScientificArtifactReferenceFactory.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Backfill/IScientificArtifactBackfillService.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Backfill/ScientificArtifactBackfillService.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Backfill/Models/ScientificArtifactBackfillResult.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Backfill/Models/ScientificArtifactBackfillPointResult.cs
src/ChemSculptor.Api/Endpoints/ScientificArtifactBackfillEndpoints.cs
src/ChemSculptor.Api/Program.cs
src/ChemSculptor.Api/Contracts.cs
src/ChemSculptor.Api/ScientificArtifactResponseMapper.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/Models/ScientificArtifactPointManifest.cs
src/ChemSculptor.ScientificData.Extraction/ArtifactResolution/ScientificArtifactResolver.cs
src/ChemSculptor.ScientificSummary/DefaultScientificNarrativeBuilder.cs
src/ChemSculptor.WinForms/MainForm.cs
src/ChemSculptor.WinForms/Models/Responses/ResponseModels.cs
tests/ChemSculptor.Core.Tests/ScientificArtifactBackfillServiceTests.cs
tests/ChemSculptor.Core.Tests/ScientificArtifactResolverTests.cs
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：93/93 通过
```

## 尚未完成

```text
阶段 7 没有代码遗留项
尚未通过实际 GUI 点击完成端到端成果包保存验证
尚未生成完整成果包 checksums
尚未实现压缩和断点续传
```

## 下一步精确任务

下一阶段由用户指定。建议：

```text
选择一个没有 Artifacts 的旧 ScientificResult
调用 artifact-backfill 回填
确认第二次调用不回填
确认旧记录不调用回填也可显示“没有文件清单”
```

## 注意事项

```text
科学点是核心
作业只是计算过程
文本用于描述点的来源和组织方式
文本不得加入未进入 ScientificResult 的计算结论
conversation.txt 是客户端会话记录
原始点、被取代点和失败点都应保留
回填依赖旧 CalculationJob 和 CalculationResult 仍然存在
```

## 强制文档规则

每次修改代码后，必须同时更新：

```text
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

任何一项未更新，本阶段都不得标记为完成。
