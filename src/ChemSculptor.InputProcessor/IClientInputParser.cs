namespace ChemSculptor.InputProcessor;

/// <summary>
/// 客户输入解析器契约。
/// </summary>
public interface IClientInputParser
{
    /// <summary>把客户原始文本解析为结构化请求。</summary>
    Task<ProcessedClientRequest> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 客户输入解析后的结构化结果。
/// </summary>
public sealed class ProcessedClientRequest
{
    /// <summary>要使用的工作流模板标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>用户的科研目标描述。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>原始输入文本。</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>解析过程中的诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}
