# 当前交接

## 当前阶段

阶段 6：生成叙述性文本。已完成。

```text
科学文本只从 ScientificResult 生成
point-summary 和 provenance 明确引用 PointId
organization 记录点、关系和物理量
final-summary 引用接受点和物理量依赖点
conversation 由客户端保存，不作为科学结论
```

## 已完成

```text
新增 IScientificNarrativeBuilder
新增 DefaultScientificNarrativeBuilder
新增 IScientificNarrativeService
新增 ScientificNarrativeService
新增 /scientific-results/{resultId}/narrative
新增 /calculations/{jobId}/narrative
WinForms 保存真实 relations.json 和 observables.json
WinForms 为每个科学点保存 point-summary.txt 和 provenance.txt
```

## 修改文件

```text
src/ChemSculptor.ScientificSummary/Abstractions/IScientificNarrativeBuilder.cs
src/ChemSculptor.ScientificSummary/Abstractions/IScientificNarrativeService.cs
src/ChemSculptor.ScientificSummary/Models/ScientificPointNarrative.cs
src/ChemSculptor.ScientificSummary/Models/ScientificNarrativePackage.cs
src/ChemSculptor.ScientificSummary/DefaultScientificNarrativeBuilder.cs
src/ChemSculptor.ScientificSummary/ScientificNarrativeService.cs
src/ChemSculptor.Api/Endpoints/ScientificNarrativeEndpoints.cs
src/ChemSculptor.Api/Program.cs
src/ChemSculptor.WinForms/MainForm.cs
src/ChemSculptor.WinForms/Models/Responses/ResponseModels.cs
tests/ChemSculptor.Core.Tests/ScientificNarrativeBuilderTests.cs
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：91/91 通过
```

## 尚未完成

```text
尚未通过实际 GUI 点击完成端到端成果包保存验证
尚未生成完整成果包 checksums
尚未实现压缩和断点续传
```

## 下一步精确任务

下一阶段由用户指定。建议：

```text
在 WinForms 中实际提交氧气计算并点击保存
核对 points/01-original-m1 和 points/02-recovery-m3
核对输入、fchk、输出共享统一基本文件名
核对 point-summary、provenance、organization 和 final-summary
核对 relations.json、observables.json 和 conversation.txt
```

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

任何一项未更新，本阶段都不得标记为完成。
