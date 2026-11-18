namespace ChemSculptor.Reporting;

/// <summary>报告契约的当前模式版本。</summary>
public static class ReportContractVersions
{
    /// <summary>当前报告模式版本。</summary>
    public const string Current = "1.0";
}

/// <summary>成果包用途剖面。</summary>
public enum ReportPackageProfile
{
    /// <summary>标准科学成果包。</summary>
    Standard,

    /// <summary>面向客户端的精简成果包。</summary>
    ClientSummary,

    /// <summary>面向长期归档的完整成果包。</summary>
    Archive
}

/// <summary>成果包的边界类别。</summary>
public enum ReportPackageScopeKind
{
    /// <summary>以科学成果和科学点集合为边界的成果包。</summary>
    ScientificPointArtifact,

    /// <summary>以根工作流为边界、同时包含科学点和全部执行证据的档案包。</summary>
    CalculationRunArchive
}

/// <summary>计算结论状态；与档案包完整性相互独立。</summary>
public enum ReportCalculationConclusionStatus
{
    /// <summary>计算工作流成功完成。</summary>
    Succeeded,

    /// <summary>计算工作流完成并保留部分有效结果。</summary>
    PartiallySucceeded,

    /// <summary>计算工作流失败。</summary>
    Failed,

    /// <summary>计算工作流被取消。</summary>
    Canceled,

    /// <summary>计算工作流仍在运行。</summary>
    InProgress,

    /// <summary>无法确定计算结论。</summary>
    Unknown
}

/// <summary>报告证据完整性。</summary>
public enum ReportEvidenceCompleteness
{
    /// <summary>所需证据完整。</summary>
    Complete,

    /// <summary>部分证据缺失，但报告仍可生成。</summary>
    Partial,

    /// <summary>缺少生成有意义报告所需的关键证据。</summary>
    Insufficient
}

/// <summary>报告证据问题级别。</summary>
public enum ReportEvidenceIssueSeverity
{
    /// <summary>信息。</summary>
    Information,

    /// <summary>警告；可能降低报告完整性。</summary>
    Warning,

    /// <summary>错误；可能导致证据不足。</summary>
    Error
}

/// <summary>报告内容块类别。</summary>
public enum ReportContentBlockKind
{
    /// <summary>标题。</summary>
    Heading,

    /// <summary>普通段落。</summary>
    Paragraph,

    /// <summary>键值表。</summary>
    KeyValueGrid,

    /// <summary>数据表。</summary>
    Table,

    /// <summary>图片。</summary>
    Figure,

    /// <summary>公式。</summary>
    Formula,

    /// <summary>提示或警告。</summary>
    Callout,

    /// <summary>缺失数据说明。</summary>
    MissingData,

    /// <summary>原始文件清单。</summary>
    ArtifactTable,

    /// <summary>附录原文。</summary>
    RawAppendix
}

/// <summary>成果包生成状态。</summary>
public enum ReportPackageStatus
{
    /// <summary>尚未开始。</summary>
    Draft,

    /// <summary>正在组装。</summary>
    Building,

    /// <summary>已完整生成。</summary>
    Complete,

    /// <summary>生成失败。</summary>
    Failed
}

/// <summary>成果包中的文件角色。</summary>
public enum ReportPackageFileRole
{
    /// <summary>Markdown 报告。</summary>
    ReportMarkdown,

    /// <summary>机器可读报告文档。</summary>
    ReportJson,

    /// <summary>证据快照。</summary>
    EvidenceSnapshot,

    /// <summary>数据表。</summary>
    DataTable,

    /// <summary>图形。</summary>
    Figure,

    /// <summary>计算输入文件。</summary>
    InputArtifact,

    /// <summary>计算主要输出文件。</summary>
    OutputArtifact,

    /// <summary>恢复或继续计算状态文件。</summary>
    RestartArtifact,

    /// <summary>校验清单。</summary>
    ChecksumManifest,

    /// <summary>其他文件。</summary>
    Other
}
