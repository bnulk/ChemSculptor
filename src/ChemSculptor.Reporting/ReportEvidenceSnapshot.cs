using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.Reporting;

/// <summary>
/// 报告生成所依据的持久化证据快照。
/// 快照生成后不得从计算输出或实时作业状态重新填充字段。
/// </summary>
public sealed class ReportEvidenceSnapshot
{
    /// <summary>报告契约模式版本。</summary>
    public string SchemaVersion { get; set; } = ReportContractVersions.Current;

    /// <summary>快照标识。</summary>
    public string SnapshotId { get; set; } = string.Empty;

    /// <summary>触发快照的成果包请求标识。</summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>根工作流标识；无法确定时为空。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>冻结的报告请求。</summary>
    public ReportPackageRequest Request { get; set; } =
        new ReportPackageRequest();

    /// <summary>冻结的科学成果；缺失时为空。</summary>
    public ScientificResult? ScientificResult { get; set; }

    /// <summary>科学成果关联作业的冻结证据。</summary>
    public List<ReportJobEvidence> Jobs { get; set; } =
        new List<ReportJobEvidence>();

    /// <summary>证据来源指纹，用于判断快照基于哪些持久化记录。</summary>
    public List<ReportSourceFingerprint> SourceFingerprints { get; set; } =
        new List<ReportSourceFingerprint>();

    /// <summary>证据查找和完整性检查发现的问题。</summary>
    public List<ReportEvidenceIssue> Issues { get; set; } =
        new List<ReportEvidenceIssue>();

    /// <summary>证据整体完整性。</summary>
    public ReportEvidenceCompleteness Completeness { get; set; } =
        ReportEvidenceCompleteness.Insufficient;

    /// <summary>快照创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>规范快照 JSON 的 SHA-256；尚未计算时为空。</summary>
    public string SnapshotSha256 { get; set; } = string.Empty;
}

/// <summary>一个关联计算作业及其持久化证据。</summary>
public sealed class ReportJobEvidence
{
    /// <summary>该作业在报告中的角色，例如 original、recovery 或 source。</summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>计算作业记录；缺失时为空。</summary>
    public CalculationJob? Job { get; set; }

    /// <summary>规范化计算结果；缺失时为空。</summary>
    public CalculationResult? Result { get; set; }

    /// <summary>计算结果验证报告；缺失时为空。</summary>
    public CalculationValidationReport? Validation { get; set; }

    /// <summary>参数审计；缺失时为空。</summary>
    public CalculationParameterAudit? ParameterAudit { get; set; }
}

/// <summary>一项持久化证据来源及其摘要。</summary>
public sealed class ReportSourceFingerprint
{
    /// <summary>来源类别，例如 scientific-result、job 或 validation。</summary>
    public string SourceType { get; set; } = string.Empty;

    /// <summary>来源记录标识。</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>来源内容 SHA-256；无法计算时为空。</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>来源记录读取时间。</summary>
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>快照生成期间发现的一项证据问题。</summary>
public sealed class ReportEvidenceIssue
{
    /// <summary>稳定问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题级别。</summary>
    public ReportEvidenceIssueSeverity Severity { get; set; } =
        ReportEvidenceIssueSeverity.Warning;

    /// <summary>相关来源或记录标识。</summary>
    public string ReferenceId { get; set; } = string.Empty;

    /// <summary>问题说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>该问题是否阻止生成有意义的报告。</summary>
    public bool IsBlocking { get; set; }
}
