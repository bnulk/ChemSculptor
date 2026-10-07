# 当前交接

## 当前阶段

阶段 2：提取器填充文件引用。已完成。

```text
原始点引用原始作业的完整文件清单
恢复点引用恢复作业的完整文件清单
每个点和每项引用都记录自己的 CalculationJobId
只保存文件引用，不复制文件
不修改原始计算目录
```

## 已完成

```text
ScientificResultExtractor 已把原始作业文件清单写入原始点
ScientificResultExtractor 已把恢复作业文件清单写入恢复点
文件引用已填写 CalculationJobId、RelativePath、CanonicalStem、
CanonicalExtension 和 DownloadFileName
restart 状态文件的 CanUseForRestart 已由测试覆盖
模拟原始和恢复目录的文件集合与内容保持不变
```

## 修改文件

```text
tests/ChemSculptor.Core.Tests/ScientificResultExtractorTests.cs
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

## 验证结果

```text
Release 构建：0 警告，0 错误
测试：84/84 通过
```

## 尚未完成

```text
阶段 2 没有遗留项。

阶段 3 尚未开始。
```

## 下一步精确任务

建议的阶段 3：实现成果包清单和文件解析服务：

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
