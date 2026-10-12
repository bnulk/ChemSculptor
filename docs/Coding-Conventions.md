# ChemSculptor 编码约定

> 状态：生效中
> 最后更新：2026-10-13

## 1. 总则

本项目优先采用**传统、显式、容易逐行阅读**的写法，避免可选语法糖。

目标不是“写得短”，而是：

- 初学者能逐行看懂
- 维护时不需要依赖隐式规则
- 行为与代码字面意思一致
- 所有代码必须使用传统入口写法，禁止顶层语句
- 所有语法糖默认禁止，但 `async / await` 不属于要避免的语法糖
- 代码注释统一使用标准 C# 注释风格，并使用中文说明
- 技能相关类型、文件名、端点和字段命名不得使用 `Container`

## 2. 禁止使用的可选语法糖

以下写法在本项目中禁止使用，除非确有必要并经过确认：

```text
顶层语句
三元运算符 ? :
空合并运算符 ?? / ??=
集合表达式 []
目标类型 new()
末尾索引 ^1
范围切片 [..n]
switch 表达式
字符串插值 $"..."
LINQ 链式调用（Where / Select / ToDictionary 等）
Lambda 表达式（=>）
匿名类型 new { }
record 类型
对象初始化器 new T { X = ... }
using 声明式写法（不放花括号的 using）
await using
```

遇到上述写法时，应改写为对应的传统形式，例如：

```text
三元          → if / else
?? / ??=      → if 判断
集合表达式     → new List<T>()
LINQ          → foreach 循环
Lambda        → 命名方法
匿名类型       → 显式定义的类
record        → 普通 class + 构造函数 + 属性
字符串插值     → string.Format 或字符串拼接
```

## 3. 平台必需机制的例外

有一部分不是“可选糖”，而是 C# / .NET 平台本身的工作方式。完全去掉会让代码不可用或明显更差，需作为例外保留：

```text
async / await
  用于避免阻塞线程，是 HttpClient、文件读取和服务器处理的基础机制。
  如需不使用，必须同步阻塞线程，通常是不推荐的做法。

Task / Task<T>
  是异步 API 的标准返回类型。

属性（Attribute）
  例如 [FromForm]，是框架绑定请求的必需标记。

接口与依赖注入
  不是语法糖，是架构边界。

框架提供的扩展方法
  不保留扩展方法调用写法。
  例如 app.MapGet(...) 必须写成：
  EndpointRouteBuilderExtensions.MapGet(app, ...)
  自己的扩展方法同样改为普通静态方法，调用时显式传入 app。
```

上述例外在代码中必须保持最小使用，并在首次出现处加注释，说明它等价于什么写法、为什么必须保留。

## 4. 当前状态

```text
ChemSculptor.Api/Program.cs
  已改为传统 public static async Task Main 写法，不再使用顶层语句。

其他项目
  仍包含部分语法糖，后续按“一个文件一次改动”的方式逐步重构。
```

## 5. 重构流程

每次重构一个文件时：

1. 只改变写法，不改变行为
2. 保留原有注释的含义，必要时补上传统写法的等价说明
3. 编译并运行测试
4. 更新 `docs/CHANGES.md`
5. 单独提交，不与功能改动混在一起

## 6. 注释规范

### 6.1 公共成员必须使用 XML 注释

公共类、公共接口、公共方法和公共属性使用 `/// <summary>` 形式：

```csharp
/// <summary>
/// 描述这个类型或方法的职责。
/// </summary>
/// <param name="value">参数含义。</param>
/// <returns>返回值含义。</returns>
public ...
```

### 6.2 非公共逻辑使用行注释

私有方法内部的关键步骤使用 `//` 中文注释，例如：

```csharp
// 在锁内复制订阅者列表，避免遍历时集合被修改。
```

### 6.3 注释内容要求

```text
说明“为什么这样做”，而不是重复“代码表面在做什么”
说明输入、输出、关键边界和失败情况
对必须保留的平台机制（如 async/await）说明原因
避免空话式注释，例如“给变量赋值”
```

## 7. 最终口径

### 7.1 通用流程与具体程序分离

通用流程层包括：

```text
ChemSculptor.Api
ChemSculptor.Agent
ChemSculptor.Core
ChemSculptor.Domain
ChemSculptor.Compute
ChemSculptor.Skills.Common
工作流定义
通用 Skill ID
```

这些位置中禁止出现：

```text
具体计算程序名称
具体可执行文件名
具体输入文件扩展名
具体输入关键词
具体输出关键词
具体程序异常名称
具体程序环境变量
```

例如禁止出现：

```text
Gaussian
g16
.gjf
%chk
SCF Done
Normal termination
GAUSS_EXEDIR
ORCA
```

具体计算程序内容只允许出现在：

```text
ChemSculptor.Compute.<Program>
ChemSculptor.Skills.<Program>
对应程序适配器、Parser、Translator 和具体实现
```

允许的例外：

```text
默认计算程序配置值
例如 CalculationDefaults.DefaultProgram = "Gaussian 16"
```

通用工作流必须通过：

```text
IQuantumProgramAdapterRegistry
```

根据 `CalculationSpec.Program` 选择具体实现。

通用 Skill ID 必须使用能力语义，例如：

```text
calculation.single-point
calculation.prepare-input
calculation.submit
calculation.wait
calculation.extract-result
calculation.workflow-validation
calculation.workflow-processing-plan
```

禁止使用：

```text
gaussian.input-generation
gaussian.single-point-workflow-extraction
orca.input-generation
```

本规则永久生效，后续代码不得违反。

### 7.1.1 通用 Skill 与专用 Skill 必须成对

凡是需要调用具体计算程序的能力，必须同时设计：

```text
通用 Skill
专用 Skill
```

通用 Skill：

```text
由工作流直接引用
名称不包含具体程序
使用稳定的通用请求和结果
通过注册表选择具体程序实现
负责统一参数校验、结果翻译和错误归一化
```

专用 Skill：

```text
名称可以包含具体程序
服从通用 Skill 的能力契约
负责具体程序的输入生成、执行调用、输出解析和参数翻译
返回通用结果模型
```

示例：

```text
通用 Skill
  anomaly.check-wavefunction-stability

专用 Skill
  gaussian.wavefunction-stability.check
```

工作流定义只能引用：

```text
anomaly.check-wavefunction-stability
```

不能引用：

```text
gaussian.wavefunction-stability.check
```

通用 Skill 与专用 Skill 通过能力代码和程序标识关联。

禁止：

```text
只在工作流中写专用程序 Skill
把具体程序名称写入通用工作流定义
让通用 Skill 直接包含具体程序关键词
让专用 Skill 返回只适用于当前程序的私有结果模型
```

程序适配器、Parser 和 Translator 可以继续作为普通组件存在。只有当它们形成独立的工作流能力时，才需要包装为专用 Skill。

本规则永久生效，后续所有需要调用具体计算程序的能力都按此执行。

### 7.2 技能命名

```text
正确：
  ISkill
  ISkillRegistry
  SkillRegistry
  EchoSkill
  SkillDescriptor
  WorkflowNode.Skill
  TaskRequest.SkillId
  /skills

禁止用于技能命名：
  ISkillContainer
  IContainerRegistry
  ContainerRegistry
  EchoSkillContainer
  ContainerDescriptor
  WorkflowNode.Container
  TaskRequest.ContainerId
  /containers
```

例外：

```text
依赖注入容器（DI Container）是基础设施术语，可以继续使用。
Docker 容器是运行环境术语，可以继续使用。
WinForms 的 SplitContainer 是框架界面控件，不属于技能命名。
```

### 7.3 Skill / Workflow 强制建设模板

适用范围：

```text
任务级 Skill
完整 Workflow
涉及执行、验证、异常和恢复的功能模块
```

原子能力 Skill 可以只使用以下子集：

```text
契约 -> 执行 -> 验证 -> 测试
```

任何任务级 Skill 和完整 Workflow，默认必须严格按以下 11 步建设：

```text
1. 契约
2. 参数
3. 翻译
4. 结果
5. 执行
6. 验证
7. 异常
8. 恢复
9. 工作流
10. 接入
11. 测试
```

各步骤的最低要求：

```text
1. 契约
   冻结任务目标、输入、输出、失败语义和状态边界。

2. 参数
   定义规范参数键、默认值、别名、允许值和来源。

3. 翻译
   把规范参数翻译为程序关键词；程序关键词不得反向污染通用层。

4. 结果
   定义结构化结果模型，包含正常结果、失败分类和诊断。

5. 执行
   定义任务级 Skill 或执行链路，只负责一次正常任务执行。

6. 验证
   定义必需、推荐和信息性检查，并给出可复现的验证报告。

7. 异常
   定义通用异常和任务专用异常，包含稳定错误码、严重级别和证据。

8. 恢复
   定义诊断、修正规划、派生恢复作业、执行和复检；不得覆盖原作业。

9. 工作流
   定义 DAG、条件分支、重试上限、停止条件和人工确认位置。

10. 接入
    接入 Agent、API、客户端和 Codex Skill，并保持职责边界清晰。

11. 测试
    增加单元测试、集成测试、端到端测试和审计验证。
```

强制规则：

```text
每一步都必须有明确代码落点或文档落点。
每一步都必须有对应测试或验收证据。
任务级 Skill 不得擅自承担恢复、审批和重试。
完整 Workflow 不得绕过通用参数层直接拼接程序输入。
程序专用规则只能出现在程序适配器、Parser、Translator 和专用 Skill 中。
```

特殊情况处理：

```text
如果某个 Skill 或 Workflow 确实不能按 11 步模板建设：
1. 先在设计文档中说明原因。
2. 说明影响范围和替代方案。
3. 说明如何保证安全、可测试和可追溯。
4. 必须由用户确认后再开始实现。
5. 在 CHANGES.md 中记录该例外。

禁止静默省略步骤。
```

### 7.4 总口径

```text
禁止：
  顶层语句
  扩展方法调用写法（必须显式写静态类和静态方法调用）
  技能相关命名中的 Container
  其余所有可选语法糖
  Skill / Workflow 未按 11 步模板建设且未获确认

允许保留：
  async / await
  属性（Attribute），例如 [FromForm]
  接口与依赖注入
  Task / Task<T>

注释：
  标准 C# 注释风格
  公共成员使用 XML 注释
  说明文字使用中文
```

本约定立即生效，后续新增代码和重构代码都按此执行。
