namespace ChemSculptor.Reporting;

/// <summary>
/// 与渲染格式无关的报告中间表示。
/// 同一文档可以渲染为 Markdown、JSON 或未来的其他格式。
/// </summary>
public sealed class ReportDocument
{
    /// <summary>报告契约模式版本。</summary>
    public string SchemaVersion { get; set; } = ReportContractVersions.Current;

    /// <summary>报告文档标识。</summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>触发文档的成果包请求标识。</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>文档所依据的证据快照标识。</summary>
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>文档所依据的证据快照 SHA-256。</summary>
    public string SnapshotSha256 { get; set; } = string.Empty;

    /// <summary>报告标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>报告用途剖面。</summary>
    public ReportPackageProfile Profile { get; set; } =
        ReportPackageProfile.Standard;

    /// <summary>文档摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>证据完整性。</summary>
    public ReportEvidenceCompleteness Completeness { get; set; } =
        ReportEvidenceCompleteness.Insufficient;

    /// <summary>报告章节。</summary>
    public List<ReportSection> Sections { get; set; } =
        new List<ReportSection>();

    /// <summary>文档生成时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>报告层附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>报告中的一个章节。</summary>
public sealed class ReportSection
{
    /// <summary>稳定的章节标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>章节标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>章节排序值；数值小的在前。</summary>
    public int Order { get; set; }

    /// <summary>章节摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>章节内容块。</summary>
    public List<ReportContentBlock> Blocks { get; set; } =
        new List<ReportContentBlock>();

    /// <summary>章节来源提供者标识。</summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>章节附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// 报告内容块。
/// 使用统一合同承载不同块类型，避免领域提供者直接输出 Markdown。
/// </summary>
public sealed class ReportContentBlock
{
    /// <summary>稳定的内容块标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>内容块类别。</summary>
    public ReportContentBlockKind Kind { get; set; } =
        ReportContentBlockKind.Paragraph;

    /// <summary>内容块标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>正文、公式、说明或附录文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>键值内容。</summary>
    public Dictionary<string, string> Values { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>表格列名，按显示顺序保存。</summary>
    public List<string> Columns { get; set; } = new List<string>();

    /// <summary>表格数据行。</summary>
    public List<ReportDataRow> Rows { get; set; } =
        new List<ReportDataRow>();

    /// <summary>与内容块关联的成果包文件。</summary>
    public List<ReportContentFileReference> Files { get; set; } =
        new List<ReportContentFileReference>();

    /// <summary>内容块附加信息，例如单位、图注和生成参数。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>报告表格中的一行。</summary>
public sealed class ReportDataRow
{
    /// <summary>按列顺序保存的单元格文本。</summary>
    public List<string> Cells { get; set; } = new List<string>();

    /// <summary>行附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>报告内容块引用的成果包文件。</summary>
public sealed class ReportContentFileReference
{
    /// <summary>成果包内相对路径。</summary>
    public string PackagePath { get; set; } = string.Empty;

    /// <summary>文件显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>文件媒体类型。</summary>
    public string MediaType { get; set; } = "application/octet-stream";

    /// <summary>文件说明或图注。</summary>
    public string Caption { get; set; } = string.Empty;
}
