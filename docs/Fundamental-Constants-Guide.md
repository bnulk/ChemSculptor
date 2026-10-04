# ChemSculptor 基本常数与元素数据教程

> 适用版本：`v0.28.0` 之后  
> 项目位置：`src/ChemSculptor.FundamentalConstants`  
> 目标：说明物理常数、单位换算和元素数据应该如何分类和维护

---

## 1. 为什么不放在一个类里

旧代码把三类内容放在 `FundamentalConstants` 目录中：

```text
PhysConst
  物理常数和单位换算

Atoms
  元素数据目录

Atom
  元素数据结构
```

这些内容相互有关，但不是同一种东西。

```text
物理常数
  自然界的常数，例如光速、普朗克常数

单位换算
  由物理常数推导出的计算辅助量

元素数据
  一组可查询的化学参考数据

元素模型
  表示一条元素记录的数据结构
```

把它们分开后，修改单位换算不会影响物理常数的定义，扩展元素数据也不会改动单位换算。

---

## 2. 新项目结构

新增项目：

```text
src/ChemSculptor.FundamentalConstants/
├── ChemSculptor.FundamentalConstants.csproj
├── Physics/
│   └── PhysicalConstants.cs
├── Units/
│   └── UnitConversions.cs
└── Chemistry/
    └── Elements/
        ├── AtomicMassKind.cs
        ├── ChemicalElement.cs
        └── ElementCatalog.cs
```

对应命名空间：

```text
ChemSculptor.FundamentalConstants.Physics
ChemSculptor.FundamentalConstants.Units
ChemSculptor.FundamentalConstants.Chemistry.Elements
```

这个项目不引用其他 ChemSculptor 项目，也不包含计算、工作流或 GUI 逻辑。

它只提供最底层的公共数据，因此可以被未来的输入处理、计算、验证和技能项目引用。

当前还没有其他业务项目引用它。等实际代码需要使用常数或元素数据时，再添加项目引用，避免为了“感觉完整”而制造无意义依赖。

---

## 3. PhysicalConstants：物理常数

位置：

```text
Physics/PhysicalConstants.cs
```

这里存放独立于分子、基组和计算程序的物理常数。

当前包括：

```text
Pi
SpeedOfLightInVacuum
PlanckConstant
ReducedPlanckConstant
BoltzmannConstant
AvogadroConstant
MolarGasConstant
ElementaryCharge
ElectronMass
AtomicMassConstant
BohrRadius
HartreeEnergy
FineStructureConstant
VacuumElectricPermittivity
VacuumMagneticPermeability
SpeedOfLightInAtomicUnits
```

命名原则：

```text
使用含义清楚的完整名称
名称表达物理量和单位含义
不使用 h、kb、na 这类过短缩写
```

常量值的来源分为两类：

```text
SI 定义值
  光速、普朗克常数、玻尔兹曼常数、阿伏伽德罗常数、元电荷

CODATA 2022
  电子质量、原子质量常数、玻尔半径、哈特里能量、精细结构常数
```

例如：

```csharp
double hartreeInJoules =
    PhysicalConstants.HartreeEnergy;

double avogadro =
    PhysicalConstants.AvogadroConstant;
```

---

## 4. UnitConversions：单位换算

位置：

```text
Units/UnitConversions.cs
```

换算因子不是新的自然常数，而是由物理常数推导出来的计算辅助量。

例如：

```text
1 Bohr → Angstrom
1 Hartree → eV
1 Hartree → cm^-1
1 Hartree → kcal/mol
1 atomic mass unit → kg
1 atomic unit dipole → Debye
```

使用示例：

```csharp
double energyInHartree = 0.5;
double energyInElectronVolts =
    energyInHartree * UnitConversions.HartreeToElectronVolts;
```

关键原则：

```text
物理常数放在 PhysicalConstants
单位换算放在 UnitConversions
换算尽量写成由物理常数推导的表达式
不要在两个类中重复同一个数值
```

例如：

```text
X Hartree → X × HartreeToElectronVolts

不是：
X Hartree → X × 27.21138
```

这样如果以后更新 CODATA 值，只需要修改物理常数，不需要寻找散落在代码中的旧数字。

---

## 5. ChemicalElement：元素数据结构

位置：

```text
Chemistry/Elements/ChemicalElement.cs
```

这是只读数据结构，表示一条元素记录：

```text
AtomicNumber
  原子序数

Symbol
  规范大小写形式的元素符号

Name
  元素名称

AtomicMass
  旧程序迁移的一般原子质量，单位为 u

AtomicMassKind
  原子质量的定义类型

AtomicMassSource
  原子质量来源
```

示例：

```csharp
ChemicalElement element =
    ElementCatalog.All[8];

int number = element.AtomicNumber;
string symbol = element.Symbol;
double mass = element.AtomicMass;
```

### 原子质量的重要说明

当前 `AtomicMass` 保留旧程序中的值。

它不是严格的“标准原子量”，也不是某个特定同位素的质量。

例如旧数据中：

```text
H  = 1.00783
C  = 12.000
N  = 14.00307
O  = 15.99491
```

这些值适合继续支持旧程序的普通坐标和基础计算，但不应该被误认为完整的同位素数据库。

以后如果进行需要精确同位素质量的计算，应增加独立的数据层，而不是偷偷修改当前字段的含义：

```text
ElementStandardAtomicWeight
Isotope
IsotopeMass
IsotopeAbundance
```

---

## 6. ElementCatalog：元素数据目录

位置：

```text
Chemistry/Elements/ElementCatalog.cs
```

它负责提供元素查询，不负责计算化学结果。

当前保留旧程序覆盖范围：

```text
虚拟元素：0，X，Dummy
真实元素：1 到 54，H 到 Xe
```

查询示例：

```csharp
ChemicalElement element;

bool found = ElementCatalog.TryGetBySymbol(
    "o",
    out element);

if (found)
{
    int atomicNumber = element.AtomicNumber;
}
```

元素符号查询忽略大小写：

```text
"O"、"o"、"o " 都可以找到氧
```

按原子序数查询：

```csharp
ChemicalElement element;

bool found = ElementCatalog.TryGetByAtomicNumber(
    8,
    out element);
```

这里的原子序数查询不需要第二本字典，因为元素数据已经按原子序数排列：

```text
ElementArray[0]  → 原子序数 0
ElementArray[1]  → 原子序数 1
ElementArray[8]  → 原子序数 8
```

因此内部可以直接访问：

```text
ElementArray[atomicNumber]
```

符号查询仍然需要一本索引，但它只保存：

```text
"O" → 8
```

得到原子序数 8 后，再通过数组取得氧元素。这样元素数据只有一份，不会在符号字典中重复保存整个 `ChemicalElement`。

数组下标与原子序数相同必须作为一个严格约束，并由测试检查：

```text
ElementArray[n].AtomicNumber == n
```

如果以后允许缺号、乱序或从其他来源分段加载元素，才需要重新考虑使用原子序数词典。

未知元素不会再返回“成功并给出 0”，而是返回 `false`。

这比旧代码更安全，因为调用者可以明确区分：

```text
没有找到元素
找到了虚拟元素 0
```

---

## 7. 旧类与新类的对应关系

| 旧类型 | 新位置 | 变化 |
|---|---|---|
| `PhysConst` 中的物理常数 | `PhysicalConstants` | 使用完整名称，更新 SI 和 CODATA 2022 值 |
| `PhysConst` 中的单位换算 | `UnitConversions` | 与物理常数分离，尽量由常数推导 |
| `Atom` | `ChemicalElement` | 改为只读结构，字段含义更明确 |
| `Atoms.allAtoms` | `ElementCatalog.All` | 改为只读元素列表 |
| `SymbolToNumber` | `TryGetBySymbol` | 不再用静默返回 0 表示失败 |
| `NumberToSymbol` | `TryGetByAtomicNumber` | 明确返回是否找到 |
| `NumberToMass` | `ChemicalElement.AtomicMass` | 原子质量属于元素数据，不再单独提供易混淆方法 |

旧程序中的元素质量数值保留，因此迁移不会改变现有元素数据本身。

---

## 8. 项目位置和依赖方向

依赖方向应当是：

```text
ChemSculptor.FundamentalConstants
        ↑
InputProcessor / Compute / Skills / Agent
```

也就是：

```text
基础常数和元素数据不引用业务项目
业务项目需要时再引用基础数据项目
```

不要反过来让 `FundamentalConstants` 引用：

```text
InputProcessor
Compute
Gaussian
Workflow
WinForms
```

基础数据项目必须保持简单，否则它会变成一个什么都放的“大杂烩”。

---

## 9. 以后增加新内容时放在哪里

### 新的自然常数

放入：

```text
Physics/PhysicalConstants.cs
```

例如未来的：

```text
ClassicalElectronRadius
ThomsonCrossSection
ComptonWavelength
```

### 新的单位换算

放入：

```text
Units/UnitConversions.cs
```

例如未来的：

```text
JouleToCalorie
ElectronVoltToWavenumbers
AngstromToBohr
```

### 新的元素属性

如果是所有元素共有的属性，先扩展：

```text
Chemistry/Elements/ChemicalElement
```

例如未来的：

```text
StandardAtomicWeight
Period
Group
ElectronConfiguration
VanDerWaalsRadius
```

### 新的同位素数据

不要继续塞进 `ChemicalElement`。

建议以后单独建立：

```text
Chemistry/Isotopes/Isotope
Chemistry/Isotopes/IsotopeCatalog
```

因为一个元素可能对应多个同位素，关系不是一对一。

### 新的元素资源数据

例如基组、赝势、相对论有效核势，不应放进当前项目。

它们属于计算模型或计算程序资源，不是元素基础常数。

---

## 10. 常见错误

不要做：

```text
把能量换算因子直接写进计算代码
在多个类里重复 27.21138
把元素质量当作所有同位素的精确质量
用 0 同时表示未知元素和虚拟元素
让基础常数项目依赖计算项目
把基组、赝势或工作流放进基础常数项目
```

推荐做：

```text
一个物理量只在一个规范性位置定义
单位换算尽量从物理常数推导
元素查询明确返回成功或失败
元素数据和业务计算分离
基础项目只保存低层数据
```

---

## 11. 当前测试

新增测试覆盖：

```text
摩尔气体常数由 AvogadroConstant × BoltzmannConstant 得到
Hartree 到 eV、cm^-1 的常用换算
Bohr 到 Angstrom 的换算
元素符号大小写忽略查询
按原子序数查询
未知元素返回失败
元素目录覆盖 0 到 54
元素数组下标与原子序数一致
```

当前验证结果：

```text
Release 构建：0 警告，0 错误
测试：42/42 通过
```

---

## 12. 总结

可以把它记成四句话：

```text
物理常数是自然界的定义值
单位换算是由物理常数推导的计算工具
元素数据是可查询的参考表
业务计算只引用这些基础数据，不能反过来污染它们
```

当前项目已经按照这个边界建立，后续可以在不改变主计算架构的情况下逐步扩展。
