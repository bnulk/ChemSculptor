# 当前交接

## 当前阶段

固定项目协作文档结构。

```text
新增 docs/PROJECT_STATE.md
新增 docs/HANDOFF.md
建立 docs/Tutorials/
移动已有 Tutorial 文档
CHANGES.md 保持不变
```

## 已完成

```text
PROJECT_STATE.md 已建立
HANDOFF.md 已建立
docs/Tutorials/ 已建立
现有 Tutorial 文档已移入 docs/Tutorials/
```

## 修改文件

```text
docs/PROJECT_STATE.md
docs/HANDOFF.md

docs/Tutorials/ChemSculptor-Tutorial.md
docs/Tutorials/ChemSculptor-Skill-Collection-Tutorial.md
docs/Tutorials/Skill-Collections-and-Workflow-Tutorial.md
docs/Tutorials/ScientificData-Tutorial.md
docs/Tutorials/Oxygen-End-to-End-Tutorial.md
```

## 验证结果

本次只移动和新增 Markdown 文档，不修改代码。

最近一次代码验证仍然有效：

```text
Release 构建：0 警告，0 错误
测试：81/81 通过
```

## 尚未完成

```text
CalculationPoint 尚未增加 CalculationJobId
CalculationPoint 尚未增加 Artifacts
提取器尚未填充科学点文件引用
成果包清单 API 尚未实现
按科学点下载 API 尚未实现
WinForms 尚未保存完整科学点成果包
conversation.txt 尚未保存
final-summary.txt 尚未作为独立文件保存
历史科学数据尚未回填文件引用
```

## 下一步精确任务

只实现阶段 1：

```text
扩展 ChemSculptor.ScientificData 中的 CalculationPoint
  增加 CalculationJobId
  增加 Artifacts

新增 PointArtifactReference
  ArtifactId
  Kind
  RelativePath
  MediaType
  Length
  Sha256
  CanonicalStem
  CanonicalExtension
  CanDownload
```

本阶段要求：

```text
不修改提取器
不修改 API
不修改 WinForms
不修改工作流
增加必要的序列化测试
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
