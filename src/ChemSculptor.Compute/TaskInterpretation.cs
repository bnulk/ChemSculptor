namespace ChemSculptor.Compute;

/// <summary>
/// 对用户自然语言文本的解释结果。
/// </summary>
public sealed class InterpretedTask
{
    /// <summary>用户原始文本。</summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>解释出的任务类型；无法识别时为空。</summary>
    public CalculationTaskType? TaskType { get; set; }

    /// <summary>建议使用的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>当前阶段是否支持该任务。</summary>
    public bool IsSupported { get; set; }

    /// <summary>解释置信度。</summary>
    public double Confidence { get; set; }

    /// <summary>电子态研究目标。</summary>
    public ElectronicStateObjective ElectronicStateObjective { get; set; } =
        ElectronicStateObjective.GroundState;

    /// <summary>指定自旋多重度；未指定时为空。</summary>
    public int? TargetMultiplicity { get; set; }

    /// <summary>指定激发态标签；未指定时为空。</summary>
    public string TargetStateLabel { get; set; } = string.Empty;

    /// <summary>解释诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>
/// 用户文本解释器。
/// 当前阶段使用规则解释，后续可替换为大语言模型网关。
/// </summary>
public interface ITaskInterpreter
{
    /// <summary>解释用户原始文本。</summary>
    Task<InterpretedTask> InterpretAsync(
        string text,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 基于规则的文本解释器。
/// 只识别当前阶段支持的任务类型。
/// </summary>
public sealed class RuleBasedTaskInterpreter : ITaskInterpreter
{
    /// <summary>单点计算的规则关键词。</summary>
    private const string SinglePointKeyword = "单点";

    /// <summary>单点计算的英文关键词。</summary>
    private const string SinglePointEnglishKeyword = "single point";

    /// <summary>单点计算的缩写关键词。</summary>
    private const string SinglePointAbbreviation = "sp";

    /// <summary>解释输入文本。</summary>
    public Task<InterpretedTask> InterpretAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        InterpretedTask interpretedTask = new InterpretedTask();
        interpretedTask.OriginalText = text;

        if (string.IsNullOrWhiteSpace(text))
        {
            interpretedTask.IsSupported = false;
            interpretedTask.Confidence = 0.0;
            interpretedTask.Diagnostics.Add("输入文本为空。");
            return Task.FromResult(interpretedTask);
        }

        string normalizedText = text.Trim();

        if (ContainsKeyword(normalizedText, SinglePointKeyword)
            || ContainsKeyword(normalizedText, SinglePointEnglishKeyword)
            || ContainsKeyword(normalizedText, SinglePointAbbreviation))
        {
            interpretedTask.TaskType = CalculationTaskType.SinglePoint;
            interpretedTask.WorkflowId = "single_point";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;
            InterpretElectronicState(
                normalizedText,
                interpretedTask);
            return Task.FromResult(interpretedTask);
        }

        interpretedTask.IsSupported = false;
        interpretedTask.Confidence = 0.0;
        interpretedTask.Diagnostics.Add("当前阶段只支持单点计算。");

        return Task.FromResult(interpretedTask);
    }

    private static bool ContainsKeyword(string text, string keyword)
    {
        int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        return index >= 0;
    }

    private static void InterpretElectronicState(
        string text,
        InterpretedTask interpretedTask)
    {
        bool excitedStateRequested =
            ContainsKeyword(text, "激发态")
            || ContainsKeyword(text, "excited state")
            || ContainsKeyword(text, "td-dft")
            || ContainsKeyword(text, "tddft");

        int multiplicity;
        bool multiplicityFound = TryReadMultiplicity(
            text,
            out multiplicity);

        if (excitedStateRequested)
        {
            interpretedTask.ElectronicStateObjective =
                ElectronicStateObjective.TargetExcitedState;
            interpretedTask.TargetMultiplicity =
                multiplicityFound
                    ? multiplicity
                    : null;
            interpretedTask.TargetStateLabel =
                ResolveTargetStateLabel(
                    text,
                    multiplicityFound,
                    multiplicity);
            return;
        }

        if (multiplicityFound)
        {
            interpretedTask.ElectronicStateObjective =
                ElectronicStateObjective.TargetSpinState;
            interpretedTask.TargetMultiplicity = multiplicity;
            return;
        }

        interpretedTask.ElectronicStateObjective =
            ElectronicStateObjective.GroundState;
    }

    private static bool TryReadMultiplicity(
        string text,
        out int multiplicity)
    {
        if (ContainsKeyword(text, "单重态")
            || ContainsKeyword(text, "singlet"))
        {
            multiplicity = 1;
            return true;
        }

        if (ContainsKeyword(text, "二重态")
            || ContainsKeyword(text, "doublet"))
        {
            multiplicity = 2;
            return true;
        }

        if (ContainsKeyword(text, "三重态")
            || ContainsKeyword(text, "triplet"))
        {
            multiplicity = 3;
            return true;
        }

        if (ContainsKeyword(text, "四重态")
            || ContainsKeyword(text, "quartet"))
        {
            multiplicity = 4;
            return true;
        }

        if (ContainsKeyword(text, "五重态")
            || ContainsKeyword(text, "quintet"))
        {
            multiplicity = 5;
            return true;
        }

        if (ContainsKeyword(text, "六重态")
            || ContainsKeyword(text, "sextet"))
        {
            multiplicity = 6;
            return true;
        }

        if (ContainsKeyword(text, "七重态")
            || ContainsKeyword(text, "septet"))
        {
            multiplicity = 7;
            return true;
        }

        if (ContainsKeyword(text, "八重态")
            || ContainsKeyword(text, "octet"))
        {
            multiplicity = 8;
            return true;
        }

        multiplicity = 0;
        return false;
    }

    private static string ResolveTargetStateLabel(
        string text,
        bool multiplicityFound,
        int multiplicity)
    {
        string upperText = text.ToUpperInvariant();

        if (upperText.IndexOf("T1", StringComparison.Ordinal) >= 0
            || (multiplicityFound && multiplicity == 3))
        {
            return "T1";
        }

        if (upperText.IndexOf("S1", StringComparison.Ordinal) >= 0
            || (multiplicityFound && multiplicity == 1))
        {
            return "S1";
        }

        return string.Empty;
    }
}
