namespace ChemSculptor.ScientificSummary.Models;

/// <summary>面向客户端显示的科学研究摘要。</summary>
public sealed class ClientScientificSummary
{
    /// <summary>对应的科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>摘要标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>摘要状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>摘要正文。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>顺序化的客户端摘要段落。</summary>
    public List<ClientScientificSummarySection> Sections { get; set; } =
        new List<ClientScientificSummarySection>();

    /// <summary>附加显示信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>生成时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
