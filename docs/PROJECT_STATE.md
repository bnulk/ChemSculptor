# ChemSculptor 项目状态

## 当前版本

```text
0.43.0
```

## 文档维护原则

每次修改代码后，必须同步更新：

```text
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md
```

三项文档缺一不可，否则本次代码修改不视为完成。

职责：

```text
PROJECT_STATE.md
  更新当前架构、版本、边界和下一阶段

CHANGES.md
  记录改动目的、改动内容、验证结果和教程式说明

HANDOFF.md
  更新当前任务、修改文件、测试结果、遗留问题和下一步精确任务
```

最近一次代码验证：

```text
Release 构建：0 警告，0 错误
测试：83/83 通过
```

## 总体架构

```text
ChemSculptor.WinForms
  ↓ HTTP
ChemSculptor.Api
  ↓
ChemSculptor.Agent
  ↓
ChemSculptor.Conversation
  ↓
ChemSculptor.Core
  ↓
ChemSculptor.Skills.Common
  ↓
ChemSculptor.Compute
  ↓
ChemSculptor.Compute.Local
ChemSculptor.Compute.Gaussian
```

异常处理和科学数据：

```text
Compute + Anomaly
  ↓
ChemSculptor.ScientificData.Extraction
  ↓
ChemSculptor.ScientificData
  ↓
ChemSculptor.ScientificSummary
  ↓
ChemSculptor.Api
  ↓
ChemSculptor.WinForms
```

依赖原则：

```text
客户端不引用服务端项目
ScientificSummary 只引用 ScientificData
Api 引用 ScientificSummary
通用工作流不包含具体计算程序规则
报告和客户端科学摘要只从 ScientificData 读取
```

## 已完成模块

```text
工作流内核
计算模型与接口
本机计算后端
Gaussian 输入生成
Gaussian 输出解析
单点计算 API
WinForms 客户端
波函数稳定性检查
自旋多重度矫正
派生作业执行
派生作业稳定性复检
科学计算点数据结构
科学点程序专门数据
科学点原始文件引用
科学点规范文件主体和扩展名
科学数据文件仓储
计算结果到科学数据的提取
客户端科学摘要
客户端摘要 API
WinForms 领取和显示摘要
```

## 核心数据流

```text
客户端坐标和自然语言
  → 单点工作流
  → 计算与验证
  → 稳定性检查和矫正
  → ScientificResult
  → ScientificData 仓储
  → ClientScientificSummary
  → Api
  → WinForms
```

## 当前边界

```text
当前只实现单点计算
当前只实现 Gaussian 16 本机执行主链
远程 HPC 队列和调度尚未实现
几何优化、频率、TD-DFT 等任务尚未实现
科学点文件清单已进入 CalculationPoint
完整成果包下载尚未实现
对话框文本保存和逐点文本说明尚未实现
客户端摘要当前使用 Api 内存邮箱
```

## 下一阶段

阶段 0 已完成：科学点文件成果包规范已冻结。

阶段 1 已完成：科学点模型已经能够保存原始文件引用，
并支持规范文件主体、规范扩展名以及旧科学数据读取。

规范文档：

```text
docs/Scientific-Point-Artifact-Package-Spec.md
```

下一阶段是实现成果包生成服务：

```text
ScientificArchiveManifest
ScientificArchiveService
成果包清单 API
按科学点下载 API
```

后续依次完成：

```text
WinForms 保存完整成果包
对话文本和点摘要文本
历史科学数据兼容和回填
氧气端到端验证
```

阶段 1 验收结果：

```text
PointArtifactReference 已包含 CanonicalStem 和 CanonicalExtension
Artifact 模型 JSON 往返测试通过
旧科学成果 JSON 兼容读取测试通过
未修改计算层、工作流、API 和 WinForms
```

## 重要文档

```text
docs/PROJECT_STATE.md
docs/CHANGES.md
docs/HANDOFF.md

docs/Tutorials/ChemSculptor-Tutorial.md
docs/Tutorials/ChemSculptor-Skill-Collection-Tutorial.md
docs/Tutorials/Skill-Collections-and-Workflow-Tutorial.md
docs/Tutorials/ScientificData-Tutorial.md
docs/Tutorials/Oxygen-End-to-End-Tutorial.md
```

## 最近一次验证

```text
真实氧气任务
  → 单点计算
  → 稳定性检查 Finding
  → 三重态矫正
  → 稳定性复检 Passed
  → 科学数据仓储
  → 客户端摘要发送和领取
```

真实验证结果：

```text
原始多重度：1
修正后多重度：3
最终能量：-150.274273534 Hartree
科学点数量：2
矫正次数：1
客户端摘要：成功
```
