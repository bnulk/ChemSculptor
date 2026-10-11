# 对话式计算参数方案

> 目标：让用户可以在客户端对话窗口里指定计算参数，例如 `em=GD3BJ`、`nosymm`、
> SCF 迭代上限、方法、基组和溶剂，同时保持通用层、程序层和 Skill 层职责清晰。

---

## 1. 问题

当前链路只能处理有限参数：

```text
用户文本
  -> RuleBasedTaskInterpreter
  -> CalculationRequest
  -> SinglePointCalculationService
  -> GaussianInputWriter
```

现有缺口：

```text
ConversationIntent 没有结构化参数
AgentService 没有把文本参数写入 CalculationRequest.Overrides
CalculationSpec.ExtraOptions 只是字符串字典
GaussianInputWriter 只认识 scf-iteration-limit
客户端没有参数确认和审批闭环
```

用户输入例如：

```text
单点计算，CAM-B3LYP/6-31G*，加 GD3BJ 色散校正，nosymm，SCF 最多 300 次
```

最终应解析为：

```text
method = CAM-B3LYP
basis = 6-31G*
dispersion-correction = GD3BJ
symmetry = false
scf-iteration-limit = 300
```

然后由 Gaussian 层翻译为：

```text
#p CAM-B3LYP/6-31G* SP em=GD3BJ nosymm scfcyc=300
```

注意：

```text
em=GD3BJ 是用户输入别名
dispersion-correction=GD3BJ 是规范参数
Gaussian 路由关键词只是程序翻译结果
```

---

## 2. 设计原则

```text
1. 通用层只认识规范参数，不认识 Gaussian 关键词。
2. 程序层负责把规范参数翻译成程序输入。
3. 用户原始文本永远不直接拼接进输入文件。
4. 不支持、冲突、缺参数的请求必须停止并询问。
5. 高风险参数必须审批。
6. 每次计算必须保存参数来源和翻译结果。
7. Codex Skill 只指导如何提取、询问和调用，不复制 C# 实现。
```

---

## 3. 总体分层

```text
客户端对话文本
  -> 参数提取器
  -> 参数意图
  -> 参数解析器
  -> 规范 CalculationSpec
  -> 程序适配器
  -> Gaussian 路由关键词生成器
  -> 输入文件
```

对应代码职责：

```text
InputProcessor/Conversation
  提取用户意图和原始参数

Compute
  定义规范参数、默认值、风险和解析规则

Compute.Gaussian
  把规范参数翻译为 Gaussian 关键词

Skills.Common
  单点计算 Skill 使用解析后的 CalculationSpec

WinForms
  显示参数摘要、提问和审批
```

---

## 4. 参数分类

不是所有参数都属于同一类。建议分为五类：

| 类别 | 例子 | 规范键 | 处理层 |
|---|---|---|---|
| 科学意图 | 方法、基组、色散、溶剂 | `method`、`basis`、`dispersion-correction`、`solvent` | 通用 + 程序 |
| 算法控制 | SCF 迭代、收敛算法、初始猜测 | `scf-iteration-limit`、`scf-convergence` | 通用 + 程序 |
| 体系控制 | 对称性、电荷、多重度 | `symmetry`、`charge`、`multiplicity` | 通用 + 程序 |
| 资源控制 | 内存、核数、队列 | `memory`、`processor-count` | 执行后端 |
| 原始关键字 | 暂未建模的 route keyword | `program-extra-keywords` | 高风险允许列表 |

首期只实现：

```text
method
basis
dispersion-correction
symmetry
scf-iteration-limit
charge
multiplicity
solvent
```

---

## 5. 第一步：定义规范参数键

实现状态：已完成，代码位于 `CalculationOptionKeys.cs`。

扩展：

```text
src/ChemSculptor.Compute/CalculationOptionKeys.cs
```

新增：

```csharp
public const string Method = "method";
public const string Basis = "basis";
public const string DispersionCorrection = "dispersion-correction";
public const string Symmetry = "symmetry";
public const string ScfConvergence = "scf-convergence";
public const string Solvent = "solvent";
public const string Memory = "memory";
public const string ProcessorCount = "processor-count";
public const string ProgramExtraKeywords = "program-extra-keywords";
```

规则：

```text
规范键使用英文小写连字符
规范键不包含 Gaussian 语法
em、nosymm、scfcyc 都不能作为规范键
```

---

## 6. 第二步：定义参数值模型

实现状态：已完成。

新增参数模型：

```text
src/ChemSculptor.Compute/CalculationParameterModels.cs
```

核心类型：

```csharp
public enum CalculationParameterValueKind
{
    String,
    Integer,
    Double,
    Boolean,
    Enum,
    StringList
}

public sealed class CalculationParameterDefinition
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public CalculationParameterValueKind ValueKind { get; set; }
    public List<string> AllowedValues { get; set; } = new();
    public string DefaultValue { get; set; } = string.Empty;
    public CalculationRiskLevel RiskLevel { get; set; }
    public bool RequiresApproval { get; set; }
    public List<string> Aliases { get; set; } = new();
    public List<CalculationTaskType> TaskTypes { get; set; } = new();
    public List<string> Programs { get; set; } = new();
}

public sealed class ResolvedCalculationParameter
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public ParameterSource Source { get; set; }
    public double Confidence { get; set; }
    public CalculationRiskLevel RiskLevel { get; set; }
    public bool RequiresApproval { get; set; }
    public string TranslatedKeyword { get; set; } = string.Empty;
}
```

和现有模型的关系：

```text
CalculationParameter
  继续用于现有 UI 和兼容逻辑

ResolvedCalculationParameter
  作为新的严格参数模型
```

---

## 7. 第三步：建立参数目录

实现状态：通用参数目录已完成；程序专用目录留给程序翻译步骤。

新增：

```text
src/ChemSculptor.Compute/CalculationParameterCatalog.cs
```

示例定义：

```csharp
new CalculationParameterDefinition
{
    Key = CalculationOptionKeys.DispersionCorrection,
    DisplayName = "色散校正",
    ValueKind = CalculationParameterValueKind.Enum,
    AllowedValues = new List<string> { "GD3", "GD3BJ" },
    Aliases = new List<string>
    {
        "gd3",
        "gd3bj",
        "d3",
        "d3bj",
        "色散",
        "色散校正",
        "经验色散"
    },
    RiskLevel = CalculationRiskLevel.Info
}
```

```csharp
new CalculationParameterDefinition
{
    Key = CalculationOptionKeys.Symmetry,
    DisplayName = "使用对称性",
    ValueKind = CalculationParameterValueKind.Boolean,
    Aliases = new List<string>
    {
        "symm",
        "symmetry",
        "对称性",
        "nosymm",
        "no symmetry",
        "不使用对称性"
    },
    RiskLevel = CalculationRiskLevel.Warning
}
```

每个程序再有独立目录：

```text
src/ChemSculptor.Compute.Gaussian/GaussianParameterCatalog.cs
```

职责：

```text
通用目录定义参数含义
Gaussian 目录定义参数到 route keyword 的映射
```

---

## 8. 第四步：从对话文本提取参数

实现状态：规则版提取器已完成；尚未接入 AgentService。

新增接口：

```text
src/ChemSculptor.Compute/ICalculationParameterExtractor.cs
```

```csharp
public interface ICalculationParameterExtractor
{
    Task<CalculationParameterExtractionResult> ExtractAsync(
        string text,
        CalculationParameterExtractionContext context,
        CancellationToken cancellationToken = default);
}
```

首期实现：

```text
RuleBasedCalculationParameterExtractor
```

规则示例：

```text
em=GD3BJ
GD3BJ
gd3bj
d3bj
加色散
色散校正
  -> dispersion-correction = GD3BJ

nosymm
no symmetry
不使用对称性
关闭对称性
  -> symmetry = false

SCF 最多 300
SCF 迭代上限 300
scfcyc=300
  -> scf-iteration-limit = 300

CAM-B3LYP
B3LYP
PBE0
  -> method = <匹配值>

6-31G*
def2-TZVP
  -> basis = <匹配值>
```

输出：

```text
ParameterIntent:
  Key
  Value
  RawText
  Confidence
  StartIndex
  EndIndex
```

冲突处理：

```text
同一句话同时出现 nosymm 和 symm
  -> 不猜测
  -> 生成 Clarification 问题

同一句话同时出现 GD3 和 GD3BJ
  -> 不猜测
  -> 生成 Clarification 问题
```

---

## 9. 第五步：解析和合并参数

实现状态：纯逻辑 Resolver 已完成；尚未接入 AgentService。

新增：

```text
src/ChemSculptor.Compute/CalculationParameterResolver.cs
```

输入：

```text
默认 CalculationSpec
用户参数意图
程序目录
任务类型
```

输出：

```text
ParameterResolutionResult
  Spec
  AppliedParameters
  Questions
  RequiresApproval
  Diagnostics
```

解析顺序：

```text
1. 从 CalculationDefaults 创建默认 spec
2. 应用系统根据几何补充的参数，例如多重度
3. 应用用户显式参数
4. 检查程序是否支持
5. 检查值是否合法
6. 检查参数之间是否冲突
7. 标记需要审批的参数
8. 生成规范参数列表
```

重要规则：

```text
用户显式参数优先级高于默认值
系统推断值不能覆盖用户值
程序适配器不能修改规范参数含义
```

---

## 10. 第六步：建立对话问询闭环

实现状态：参数问询状态机已完成；尚未接入 AgentService。

当前会话模型已经有：

```text
ConversationReply.Questions
ConversationReply.RequiresUserAction
ConversationQuestion
```

需要增加：

```text
PendingCalculationParameterRequest
```

建议放在：

```text
src/ChemSculptor.Conversation/ConversationModels.cs
```

内容：

```text
SessionId
OriginalText
CoordinateText
CurrentSpec
MissingParameters
ConflictParameters
Questions
CreatedAt
```

交互流程：

```text
用户第一句话包含需要确认的参数
  -> 服务器保存 PendingCalculationParameterRequest
  -> 返回 Clarification 或 ApprovalRequest
  -> 客户端显示问题

用户下一句话
  -> 先作为 pending 的答案解释
  -> 合并到 CurrentSpec
  -> 如果仍有问题，继续提问
  -> 全部解决后再提交计算
```

示例：

```text
用户：
单点计算，nosymm，但我也想保持对称性

服务器：
检测到对称性设置冲突。请选择：
1. 不使用对称性，对应 Gaussian nosymm
2. 使用对称性

用户：
选择 1
```

---

## 11. 第七步：接入 AgentService

实现状态：AgentService 已接入参数提取、Resolver 和问询闭环；API 响应字段尚未扩展。

在：

```text
src/ChemSculptor.Agent/AgentService.cs
```

增加：

```text
1. 调用 ICalculationParameterExtractor
2. 调用 CalculationParameterResolver
3. 如果存在 Questions，返回 Clarification，不提交计算
4. 如果 RequiresApproval，返回 ApprovalRequest，不提交计算
5. 全部通过后，把 ResolvedParameters 写入 CalculationRequest
```

扩展：

```text
src/ChemSculptor.Agent/AgentModels.cs
```

`AgentResult` 增加：

```text
RequiresUserAction
Questions
AppliedParameters
```

扩展：

```text
src/ChemSculptor.Conversation/ConversationModels.cs
```

`ConversationIntent` 增加：

```text
ParameterIntents
```

---

## 12. 第八步：接入 SinglePointCalculationService

实现状态：ResolvedSpec 和 ResolvedParameters 已接入服务层；Gaussian 输入翻译尚未实现。

当前：

```text
SinglePointCalculationService.ApplyOverrides
```

只处理：

```text
program
method
basis
charge
multiplicity
```

重构后：

```text
SinglePointCalculationService
  -> CalculationParameterResolver.Apply
  -> ResolvedCalculationSpec
  -> CalculationInputGenerationRequest
```

`CalculationRequest` 增加：

```text
ResolvedParameters
```

如果解析结果需要用户操作：

```text
不要创建作业
不要创建工作区
不要启动工作流
返回 NeedsUserInput
```

---

## 13. 第九步：Gaussian 路有关键词翻译

实现状态：Gaussian route 关键词翻译已完成；API 与客户端尚未接入结构化参数摘要。

新增：

```text
src/ChemSculptor.Compute.Gaussian/GaussianRouteKeywordBuilder.cs
```

职责：

```text
输入：CalculationSpec + ResolvedParameters
输出：稳定排序的 Gaussian route keywords
```

映射表：

```text
dispersion-correction = GD3BJ  -> em=GD3BJ
dispersion-correction = GD3    -> em=GD3
symmetry = false               -> nosymm
scf-iteration-limit = 300      -> scfcyc=300
solvent = Ethanol              -> SCRF=(Solvent=Ethanol)
```

输出示例：

```text
#p CAM-B3LYP/6-31G* SP em=GD3BJ nosymm scfcyc=300
```

规则：

```text
关键词顺序由 builder 决定
不允许在 Skill 或 Conversation 层拼接 route line
不允许把用户原始 em=GD3BJ 直接拼进输入文件
```

改造：

```text
GaussianInputWriter.BuildRouteLine
  -> GaussianRouteKeywordBuilder.Build
```

---

## 14. 第十步：保存参数来源和翻译结果

实现状态：parameters.json 与输入文件 SHA-256 已完成；API 和客户端读取尚未实现。

每次作业至少保存：

```text
原始用户文本片段
规范参数键
规范参数值
参数来源
置信度
程序翻译关键词
翻译器版本
风险等级
审批标识
```

可以新增：

```text
jobs/<jobId>/parameters.json
```

内容示例：

```json
{
  "parameters": [
    {
      "key": "dispersion-correction",
      "value": "GD3BJ",
      "rawText": "em=GD3BJ",
      "source": "User",
      "confidence": 1.0,
      "translatedKeyword": "em=GD3BJ"
    },
    {
      "key": "symmetry",
      "value": "false",
      "rawText": "nosymm",
      "source": "User",
      "confidence": 1.0,
      "translatedKeyword": "nosymm"
    }
  ]
}
```

同时写入：

```text
manifest.json
result.json
科学数据点元数据
```

---

## 15. 第十一步：客户端显示

实现状态：API 响应和 WinForms 对话显示已完成。

`AgentMessageResponse` 增加：

```text
RequiresUserAction
Questions
AppliedParameters
```

`MainForm` 显示：

```text
已应用参数：
方法：CAM-B3LYP
基组：6-31G*
色散校正：GD3BJ（em=GD3BJ）
对称性：关闭（nosymm）
SCF 迭代上限：300（scfcyc=300）
```

需要用户操作时显示：

```text
请补充或确认：
1. 是否确认关闭对称性？
2. 色散校正使用 GD3BJ 还是 GD3？
```

首期可以直接用对话文本回复。
后续可以将 `ConversationQuestion` 渲染成按钮或下拉选择。

---

## 16. 第十二步：测试

必须增加的测试：

```text
ParameterExtractorTests
  ExtractsDispersionFromGd3bjAlias
  ExtractsNosymmAsSymmetryFalse
  ExtractsScfIterationLimit
  DetectsConflictingSymmetry
  RejectsUnknownRawRouteKeyword

ParameterResolverTests
  AppliesUserOverrideOverDefault
  RejectsUnsupportedParameter
  RequiresApprovalForCharge
  PreservesParameterSource

GaussianRouteKeywordBuilderTests
  EmitsEmGd3bj
  EmitsNosymm
  EmitsScfcyc
  OrdersKeywordsDeterministically

AgentServiceTests
  ChatMessageWithParametersProducesResolvedSpec

SinglePointCalculationServiceTests
  JobStoresResolvedParameters
```

端到端测试输入：

```text
单点计算，CAM-B3LYP/6-31G*，加 GD3BJ 色散校正，nosymm，SCF 最多 300 次
```

期望路由：

```text
#p CAM-B3LYP/6-31G* SP em=GD3BJ nosymm scfcyc=300
```

---

## 17. 第十三步：同步 Codex Skill

Codex Skill `single-point-calculation` 增加：

```text
references/parameters.md
references/gaussian.md
references/validation-rules.md
```

其中：

```text
parameters.md
  规范参数、别名、含义、风险、示例

gaussian.md
  规范参数到 Gaussian route keyword 的映射

validation-rules.md
  参数冲突、缺参数、需要审批时的停止条件
```

Skill 必须告诉 Codex：

```text
先提取规范参数
不要直接拼接 Gaussian 关键词
冲突时提问
高风险参数请求审批
不支持的参数不要猜测
```

---

## 18. 实施顺序

建议按下面顺序实现：

```text
阶段 1：规范参数和 Gaussian 翻译
  CalculationOptionKeys
  ResolvedCalculationParameter
  CalculationParameterCatalog
  GaussianRouteKeywordBuilder
  测试 em=GD3BJ 和 nosymm

阶段 2：对话文本提取
  ICalculationParameterExtractor
  RuleBasedCalculationParameterExtractor
  AgentService 接入
  测试中文和英文别名

阶段 3：参数解析和冲突处理
  CalculationParameterResolver
  Questions 和 Approval
  测试冲突和风险

阶段 4：客户端交互
  AgentMessageResponse 扩展
  MainForm 显示参数摘要和问题
  测试多轮对话

阶段 5：持久化
  parameters.json
  manifest/result 参数记录
  测试可追溯性

阶段 6：Codex Skill 文档同步
  references/parameters.md
  references/gaussian.md
  结构校验和行为测试
```

---

## 19. 验收标准

当用户输入：

```text
单点计算，CAM-B3LYP/6-31G*，加 GD3BJ 色散校正，nosymm，SCF 最多 300 次
```

系统必须：

```text
提取 method=CAM-B3LYP
提取 basis=6-31G*
提取 dispersion-correction=GD3BJ
提取 symmetry=false
提取 scf-iteration-limit=300
生成路由 #p CAM-B3LYP/6-31G* SP em=GD3BJ nosymm scfcyc=300
保存每个参数来源
客户端显示参数摘要
```

如果输入：

```text
单点计算，nosymm，但使用对称性
```

系统必须：

```text
不提交计算
返回冲突问题
等待用户选择
只有用户确认后才生成输入
```
