namespace ChemSculptor.ScientificSummary.Models;

/// <summary>客户端摘要中的一个显示段落。</summary>
public sealed class ClientScientificSummarySection
{
    /// <summary>段落标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>段落标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>段落正文。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>显示顺序。</summary>
    public int Order { get; set; }

    /// <summary>附加显示信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
