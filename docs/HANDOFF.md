# 当前交接

## 当前阶段

阶段 0：冻结科学点文件成果包规范。

```text
新增数据规范文档
确定科学点必填字段
确定 Artifact 稳定标识
确定下载文件命名规则
确定成果包目录结构
确定点和文本的关系
不修改现有代码和工作流
```

## 已完成

```text
科学点文件成果包规范已建立
成果包目录结构已确定
文件命名规则已确定
原始点、被取代点、失败点的保留规则已确定
文本与科学点的关系已确定
```

## 修改文件

```text
docs/Scientific-Point-Artifact-Package-Spec.md
docs/PROJECT_STATE.md
docs/HANDOFF.md
```

## 验证结果

本次只新增和修改文档，不修改代码。

```text
无代码构建和测试变更
```

## 尚未完成

```text
ScientificArchiveManifest 尚未实现
ScientificArchiveService 尚未实现
成果包清单 API 尚未实现
按科学点下载 API 尚未实现
WinForms 尚未保存完整科学点成果包
```

## 下一步精确任务

阶段 1：实现成果包清单和文件解析服务：

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

本阶段要求：

```text
不修改 CalculationPoint
不修改提取器
不修改 WinForms
不修改工作流
增加 Manifest 和路径解析测试
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
