# ChemSculptor 编码约定

> 状态：生效中
> 最后更新：2026-09-10

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

### 7.1 技能命名

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

### 7.2 总口径

```text
禁止：
  顶层语句
  扩展方法调用写法（必须显式写静态类和静态方法调用）
  技能相关命名中的 Container
  其余所有可选语法糖

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
