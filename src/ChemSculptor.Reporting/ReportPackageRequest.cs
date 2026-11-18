namespace ChemSculptor.Reporting;

/// <summary>
/// 请求生成一个计算成果包。
/// 该对象只表达报告生成意图和选项，不包含重新计算所需的业务逻辑。
/// </summary>
public sealed class ReportPackageRequest
{
    /// <summary>报告契约模式版本。</summary>
    public string SchemaVersion { get; set; } = ReportContractVersions.Current;

    /// <summary>本次报告请求标识，用于贯穿快照、文档和成果包。</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>要生成报告的已归档科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>
    /// 成果包范围契约；旧调用方可以暂不设置，
    /// 新成果包必须明确区分科学点成果包和全过程计算档案包。
    /// </summary>
    public ReportPackageScope? Scope { get; set; }

    /// <summary>成果包用途剖面。</summary>
    public ReportPackageProfile Profile { get; set; } =
        ReportPackageProfile.Standard;

    /// <summary>请求的报告语言代码。</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>可选的报告标题覆盖值；为空时采用科学成果标题。</summary>
    public string TitleOverride { get; set; } = string.Empty;

    /// <summary>调用方请求的章节标识；为空表示采用剖面默认章节。</summary>
    public List<string> RequestedSectionIds { get; set; } = new List<string>();

    /// <summary>是否包含 ScientificResult 明确引用的原始计算文件。</summary>
    public bool IncludeArtifacts { get; set; } = true;

    /// <summary>是否包含报告层确定性生成的数据表和图。</summary>
    public bool IncludeGeneratedData { get; set; } = true;

    /// <summary>是否把报告证据快照写入成果包。</summary>
    public bool IncludeEvidenceSnapshot { get; set; } = true;

    /// <summary>请求创建时间。</summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>不影响科学结论的客户端附加信息。</summary>
    public Dictionary<string, string> ClientMetadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
