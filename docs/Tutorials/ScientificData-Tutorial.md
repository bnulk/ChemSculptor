# ChemSculptor.ScientificData 教程

## 1. 这个项目解决什么问题

在加入 `ChemSculptor.ScientificData` 之前，数据主要围绕“计算作业”保存：

```text
作业
输入文件
输出文件
结果
异常记录
恢复尝试
```

这些数据可以解释“程序运行过什么”，但不适合直接表达科研结果。

化学计算通常面对的是势能面：

```text
一个几何结构
一个电子态
一个计算模型
  ↓
一个计算点
```

几何优化产生一些点。IRC 产生路径上的一系列点。解离能、光谱和反应能都由
若干计算点导出。

因此，`ChemSculptor.ScientificData` 把“计算点”作为科学数据的基本单位。

## 2. 核心模型

这个项目只表达四件事：

```text
点
点的集合
点之间的关系
由点导出的物理量
```

最后组合成一个科学成果：

```text
ScientificResult
  ├── CalculationPointSet
  │     ├── CalculationPoint
  │     └── CalculationPointRelation
  └── ScientificObservable
```

## 3. 什么是一个计算点

一个计算点不是单纯的坐标，而是：

```text
几何结构
组分
电子态
计算模型
```

确定之后得到的一个科学数据单位。

同一个氧气几何结构可以有两个不同的点：

```text
O2，Singlet 电子态
O2，Triplet 电子态
```

坐标相同，但电子态和势能面不同，所以不是同一个点。

计算点上保存的性质包括：

```text
能量
梯度
Hessian
频率
偶极矩
其它数值、文本或数组
```

## 4. 项目文件结构

```text
ChemSculptor.ScientificData/
  Models/
    ScientificDataEnums.cs
    PointGeometry.cs
    PointElectronicState.cs
    PointCalculationModel.cs
    PointProvenance.cs
    ScientificProperty.cs
    CalculationPoint.cs
    CalculationPointRelation.cs
    CalculationPointSet.cs
    ScientificObservable.cs
    ScientificResult.cs
```

当前项目只包含数据结构，不包含：

```text
工作流
计算程序适配器
文件仓储
数据库访问
报告生成
```

## 5. 建议的阅读顺序

```text
1. ScientificDataEnums.cs
2. PointGeometry.cs
3. PointElectronicState.cs
4. PointCalculationModel.cs
5. ScientificProperty.cs
6. PointProvenance.cs
7. CalculationPoint.cs
8. CalculationPointRelation.cs
9. CalculationPointSet.cs
10. ScientificObservable.cs
11. ScientificResult.cs
```

## 6. CalculationPoint

`CalculationPoint` 是核心对象。

主要字段：

```text
Id
Name
Kind
Status
Geometry
Components
ElectronicState
CalculationModel
Properties
Validations
Provenance
Labels
Metadata
CreatedAt
UpdatedAt
```

### 6.1 点的类型

```text
Unknown
Minimum
TransitionState
Intermediate
Fragment
PathPoint
SeparatedState
```

IRC 上的每个离散点可以保存为 `PathPoint`。优化得到的极小点可以保存为
`Minimum`。计算解离能时，氧原子可以保存为 `Fragment`。

### 6.2 点的接受状态

```text
Candidate
Accepted
Rejected
Superseded
```

`Accepted` 表示该点已经通过必要验证，可以作为后续科学工作的输入。

原始氧气单重态计算可以保存为：

```text
Candidate 或 Rejected
```

修正后的氧气三重态计算可以保存为：

```text
Accepted
```

失败尝试仍然保留，但不能自动进入下游工作流。

## 7. PointGeometry

`PointGeometry` 保存计算点的几何结构。

```text
GeometryId
SourceGeometryId
CoordinateText
CanonicalHash
Atoms
```

每个 `PointAtom` 保存：

```text
Index
Element
AtomicNumber
X
Y
Z
```

`CanonicalHash` 用于比较几何结构。将来做几何去重或查找相同点时，不应只比较
浮点数字符串。

`PointComponent` 用于描述组合体系：

```text
ComponentId
Label
AtomIndices
Charge
Multiplicity
```

例如 A 与 B 的复合物可以表示为一个点，其中包含两个组分。

## 8. PointElectronicState

电子态是计算点身份的一部分。

```text
Kind
Charge
Multiplicity
StateLabel
Description
```

`Kind` 可以表示：

```text
GroundState
SpinState
ExcitedState
```

## 9. PointCalculationModel

计算模型描述这个点属于哪一个“方法学世界”：

```text
Program
Method
Basis
Environment
Parameters
```

同一个几何结构和电子态，如果用不同方法或基组计算，也应该视为不同的科学
数据点。

## 10. ScientificProperty

性质使用通用结构保存：

```text
Name
Kind
NumericValue
TextValue
Unit
Source
Metadata
```

例如能量可以表示为：

```text
Name = energy
Kind = Energy
NumericValue = 数值
Unit = Hartree
```

名字和数值分开保存，方便以后进行单位转换、比较和报告生成。

## 11. PointProvenance

`PointProvenance` 解释这个点是如何被接受的：

```text
InitialJobId
AcceptedJobId
RootWorkflowId
ParentPointId
CorrectionCount
AcceptedBy
AcceptanceSummary
```

对于氧气稳定性修正：

```text
InitialJobId
  原始单重态作业

AcceptedJobId
  派生三重态作业

CorrectionCount
  1
```

这里保存的是来源关系，不保存具体程序关键词。程序细节仍属于计算和异常处理
模块。

## 12. PointValidationRecord

验证记录保存一项检查的结果：

```text
Id
Code
Status
Summary
IsRequired
JobId
ValidatedAt
```

状态包括：

```text
NotRun
Passed
Finding
Skipped
Inconclusive
Failed
```

当前代码没有写入氧气稳定性检查结果。以后工作流可以在这里添加具体记录。

## 13. CalculationPointRelation

点之间通过关系连接：

```text
DerivedFrom
Path
ComponentOf
Reactant
Product
Conformer
Isomer
```

每个关系包含：

```text
FromPointId
ToPointId
Label
Sequence
Description
Metadata
```

IRC 使用 `Path` 和 `Sequence` 表达点的先后顺序。反应能使用 `Reactant` 和
`Product` 表达反应物与产物。

## 14. CalculationPointSet

`CalculationPointSet` 是一组相关点：

```text
Points
Relations
Labels
Metadata
```

它不是简单列表，而是点、关系和检索信息的组合。

解离能工作流可以有一个点集：

```text
O2 基态点
O 原子基态点
  ↓
关系：O2 解离为两个 O
```

## 15. ScientificObservable

物理量由点集导出：

```text
Id
Name
Kind
NumericValue
TextValue
Unit
Formula
PointIds
RelationIds
Summary
Metadata
```

解离能是一个 `ScientificObservable`，不是计算点本身。

可以把关系写成：

```text
解离能
  ← O2 已接受点
  ← O 原子已接受点
```

这样最终报告不仅可以显示一个数字，还可以追溯到它使用了哪些点。

## 16. ScientificResult

`ScientificResult` 是完整科学成果：

```text
Id
Title
Summary
Status
PointSet
Observables
Metadata
CreatedAt
CompletedAt
```

状态包括：

```text
Draft
Complete
Partial
Failed
```

对于氧气稳定性修正的例子，正确的结构是：

```text
ScientificResult
  PointSet
    Original Singlet point
      状态：Rejected
    Corrected Triplet point
      状态：Accepted
      矫正次数：1
  Observables
    氧气基态能量
```

当前代码没有创建这个氧气实例，只提供了保存它所需的数据结构。

## 17. “接受点”和“最终结果”的区别

单个计算点使用 `Accepted`：

```text
这个计算已经通过必要验证，可以用于后续工作。
```

整个科研任务使用 `ScientificResult`：

```text
这个科研任务已经完成，并产生了一个或多个导出物理量。
```

因此：

```text
Accepted CalculationPoint
  是科学计算图的输入

ScientificResult
  是科研任务的输出
```

## 18. 与工作流的关系

工作流应该做四件事：

```text
生成候选点
验证候选点
执行修正
接受最终点
```

工作流的输出应是：

```text
ScientificResult
  PointSet
  Observable
  Process metadata
```

几何优化、IRC、光谱和解离能工作流都不需要重新定义点。它们只需要定义：

```text
读取哪些点
产生哪些关系
计算哪些 Observable
```

## 19. 与现有项目的关系

```text
ChemSculptor.Compute
  负责具体计算和程序执行

ChemSculptor.Anomaly
  负责发现异常和生成修正

ChemSculptor.ScientificData
  负责保存科学点和科学成果

ChemSculptor.Agent
  负责编排工作流

ChemSculptor.WinForms
  负责显示和用户交互
```

`ChemSculptor.ScientificData` 不引用计算程序，也不处理文件执行。这样可以避免
科学数据模型被某个程序的具体关键词污染。

## 20. 当前边界

目前尚未实现：

```text
将 AnomalyRecord 转换为 CalculationPoint
将 RecoveryAttempt 转换为 PointProvenance
将修正后的派生作业写入 Accepted point
计算点的文件仓储
点集的查询和去重
解离能、光谱或反应能计算
文章和报告生成
```

这些功能以后应作为独立服务和 Skill 加入，而不是继续向数据类中添加执行逻辑。

## 21. 最重要的设计规则

```text
一个点必须包含几何、电子态和计算模型
原始失败点不进入下游科学工作流
修正后的点可以被接受，但历史仍保留
点关系表达路径、组分和反应关系
物理量由点集导出
最终结论必须能追溯到具体接受点
```

这套结构为以后的几何优化、IRC、光谱、反应能和多分子工作流提供了共同的
科学数据语言。
