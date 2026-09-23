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
}
