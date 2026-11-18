namespace ChemSculptor.Compute;

/// <summary>全过程产物发现阶段记录的文件状态。</summary>
public enum CalculationWorkflowArtifactDiscoveryStatus
{
    /// <summary>文件存在，可在后续清单阶段读取并计算摘要。</summary>
    Present,

    /// <summary>按临时文件、缓存或零字节标准流规则排除。</summary>
    ExcludedByPolicy,

    /// <summary>当前作业状态不应产生该文件。</summary>
    NotExpected,

    /// <summary>当前作业状态要求该文件，但文件不存在。</summary>
    Missing,

    /// <summary>文件存在，但无法读取元数据或内容。</summary>
    Unreadable,

    /// <summary>路径越界、链接穿越或其它安全规则拒绝访问。</summary>
    SecurityRejected
}

/// <summary>发现项对档案完整性的要求等级。</summary>
public enum CalculationWorkflowArtifactRequirement
{
    /// <summary>缺失时档案不能视为完整。</summary>
    Required,

    /// <summary>缺失不影响档案完整性。</summary>
    Optional
}

/// <summary>全过程档案中的稳定文件角色。</summary>
public enum CalculationWorkflowArtifactRole
{
    /// <summary>作业清单。</summary>
    JobManifest,

    /// <summary>作业输入目录中的来源输入。</summary>
    SourceInput,

    /// <summary>几何或坐标输入。</summary>
    GeometryInput,

    /// <summary>实际提交给计算程序的输入。</summary>
    ProgramInput,

    /// <summary>计算程序主要输出。</summary>
    PrimaryOutput,

    /// <summary>标准输出或标准错误。</summary>
    ConsoleOutput,

    /// <summary>检查点或恢复状态。</summary>
    RestartState,

    /// <summary>规范化结果。</summary>
    NormalizedResult,

    /// <summary>验证证据。</summary>
    ValidationEvidence,

    /// <summary>参数审计。</summary>
    ParameterAudit,

    /// <summary>通用或程序专用处理方案。</summary>
    ProcessingPlan,

    /// <summary>后处理产物。</summary>
    PostProcessOutput,

    /// <summary>科学数据。</summary>
    ScientificData,

    /// <summary>科学叙述文件。</summary>
    Narrative,

    /// <summary>报告文件。</summary>
    Report,

    /// <summary>包清单。</summary>
    Manifest,

    /// <summary>校验和文件。</summary>
    Checksum,

    /// <summary>尚未分类的持久化文件。</summary>
    Other
}

/// <summary>发现项的来源类别。</summary>
public enum CalculationWorkflowArtifactSourceKind
{
    /// <summary>工作区固定证据。</summary>
    WorkspaceEvidence,

    /// <summary>程序适配器声明的候选规则。</summary>
    AdapterPattern,

    /// <summary>作业目录中实际存在的未分类文件。</summary>
    ExistingFile
}

/// <summary>产物发现审计问题级别。</summary>
public enum CalculationWorkflowArtifactDiscoveryIssueSeverity
{
    /// <summary>信息性记录。</summary>
    Info,

    /// <summary>需要关注，但不阻止继续发现。</summary>
    Warning,

    /// <summary>必要证据损坏或不可访问。</summary>
    Error
}

/// <summary>
/// 一个根工作流的全过程产物发现快照。
/// 只记录候选文件、实际状态和安全边界，不复制文件，也不计算 SHA-256。
/// </summary>
public sealed class CalculationWorkflowArtifactDiscoverySnapshot
{
    /// <summary>快照模式版本。</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>发现策略版本。</summary>
    public string PolicyVersion { get; set; } = "1.0";

    /// <summary>根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>生成快照的时间截面。</summary>
    public DateTimeOffset CapturedAt { get; set; } =
        DateTimeOffset.UtcNow;

    /// <summary>按阶段和尝试顺序排列的作业摘要。</summary>
    public List<CalculationWorkflowArtifactDiscoveryJobSummary> Jobs
    {
        get;
        set;
    } = new List<CalculationWorkflowArtifactDiscoveryJobSummary>();

    /// <summary>全部预期、实际和排除文件。</summary>
    public List<CalculationWorkflowArtifactDiscoveryEntry> Files
    {
        get;
        set;
    } = new List<CalculationWorkflowArtifactDiscoveryEntry>();

    /// <summary>目录遍历、路径安全和规则解析问题。</summary>
    public List<CalculationWorkflowArtifactDiscoveryIssue> Issues
    {
        get;
        set;
    } = new List<CalculationWorkflowArtifactDiscoveryIssue>();

    /// <summary>用于快速判断当前发现结果的汇总。</summary>
    public CalculationWorkflowArtifactDiscoverySummary Summary
    {
        get;
        set;
    } = new CalculationWorkflowArtifactDiscoverySummary();
}

/// <summary>一个作业尝试的产物发现摘要。</summary>
public sealed class CalculationWorkflowArtifactDiscoveryJobSummary
{
    /// <summary>阶段标识。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>阶段声明顺序。</summary>
    public int StageOrder { get; set; }

    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>恢复尝试序号。</summary>
    public int AttemptNumber { get; set; }

    /// <summary>作业状态。</summary>
    public CalculationJobState State { get; set; } =
        CalculationJobState.Created;

    /// <summary>任务类型。</summary>
    public CalculationTaskType TaskType { get; set; } =
        CalculationTaskType.SinglePoint;

    /// <summary>计算程序。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>作业清单可用状态。</summary>
    public CalculationWorkflowJobManifestStatus ManifestStatus
    {
        get;
        set;
    } = CalculationWorkflowJobManifestStatus.Missing;

    /// <summary>作业目录是否存在。</summary>
    public bool JobDirectoryExists { get; set; }

    /// <summary>运行目录是否存在。</summary>
    public bool RunDirectoryExists { get; set; }

    /// <summary>该作业发现的文件总数。</summary>
    public int FileCount { get; set; }

    /// <summary>存在且可进入后续清单阶段的文件数。</summary>
    public int PresentCount { get; set; }

    /// <summary>缺失的必要文件数。</summary>
    public int MissingCount { get; set; }

    /// <summary>排除文件数。</summary>
    public int ExcludedCount { get; set; }

    /// <summary>不可读文件数。</summary>
    public int UnreadableCount { get; set; }

    /// <summary>安全拒绝的文件数。</summary>
    public int SecurityRejectedCount { get; set; }
}

/// <summary>一个文件的发现结果。</summary>
public sealed class CalculationWorkflowArtifactDiscoveryEntry
{
    /// <summary>稳定发现项标识。</summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>阶段标识。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>阶段声明顺序。</summary>
    public int StageOrder { get; set; }

    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>恢复尝试序号。</summary>
    public int AttemptNumber { get; set; }

    /// <summary>相对于作业目录的逻辑路径，统一使用正斜杠。</summary>
    public string LogicalPath { get; set; } = string.Empty;

    /// <summary>文件角色。</summary>
    public CalculationWorkflowArtifactRole Role { get; set; } =
        CalculationWorkflowArtifactRole.Other;

    /// <summary>该发现项是否属于必要条件。</summary>
    public CalculationWorkflowArtifactRequirement Requirement { get; set; } =
        CalculationWorkflowArtifactRequirement.Optional;

    /// <summary>发现状态。</summary>
    public CalculationWorkflowArtifactDiscoveryStatus Status { get; set; } =
        CalculationWorkflowArtifactDiscoveryStatus.NotExpected;

    /// <summary>发现来源。</summary>
    public CalculationWorkflowArtifactSourceKind SourceKind { get; set; } =
        CalculationWorkflowArtifactSourceKind.ExistingFile;

    /// <summary>传输媒体类型。</summary>
    public string MediaType { get; set; } = "application/octet-stream";

    /// <summary>文件长度；不存在或不可读时为空。</summary>
    public long? Length { get; set; }

    /// <summary>是否可以用于恢复或继续计算。</summary>
    public bool CanUseForRestart { get; set; }

    /// <summary>排除类别；未排除时为空。</summary>
    public string ExclusionCategory { get; set; } = string.Empty;

    /// <summary>排除、缺失或访问失败的原因。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>是否可以重新生成。</summary>
    public bool CanRegenerate { get; set; }

    /// <summary>该记录是否汇总一个被排除的目录，而不是单个文件。</summary>
    public bool IsAggregate { get; set; }

    /// <summary>文件是否存在；目录汇总项固定为 false。</summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 一个排除记录代表的文件数。
    /// 普通文件为 1，整目录排除时记录该目录下的文件总数。
    /// </summary>
    public int ExcludedFileCount { get; set; }
}

/// <summary>产物发现过程中发现的问题。</summary>
public sealed class CalculationWorkflowArtifactDiscoveryIssue
{
    /// <summary>稳定问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题摘要。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>问题严重级别。</summary>
    public CalculationWorkflowArtifactDiscoveryIssueSeverity Severity
    {
        get;
        set;
    } = CalculationWorkflowArtifactDiscoveryIssueSeverity.Error;

    /// <summary>阶段标识。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>相对作业目录的逻辑路径。</summary>
    public string LogicalPath { get; set; } = string.Empty;
}

/// <summary>产物发现快照的汇总。</summary>
public sealed class CalculationWorkflowArtifactDiscoverySummary
{
    /// <summary>发现的作业尝试数。</summary>
    public int JobCount { get; set; }

    /// <summary>全部文件记录数。</summary>
    public int FileCount { get; set; }

    /// <summary>存在且可进入后续清单阶段的文件数。</summary>
    public int PresentCount { get; set; }

    /// <summary>缺失的必要文件数。</summary>
    public int MissingCount { get; set; }

    /// <summary>排除文件数。</summary>
    public int ExcludedCount { get; set; }

    /// <summary>不可读文件数。</summary>
    public int UnreadableCount { get; set; }

    /// <summary>安全拒绝的文件数。</summary>
    public int SecurityRejectedCount { get; set; }

    /// <summary>现有文件总字节数。</summary>
    public long TotalBytes { get; set; }

    /// <summary>错误级问题数。</summary>
    public int ErrorCount { get; set; }

    /// <summary>当前发现结果是否覆盖了全部必要条件。</summary>
    public bool IsComplete { get; set; }

    /// <summary>按排除类别汇总的目录或文件数量。</summary>
    public List<CalculationWorkflowArtifactExclusionSummary>
        Exclusions { get; set; } =
            new List<CalculationWorkflowArtifactExclusionSummary>();
}

/// <summary>一种排除策略命中的文件数、字节数和原因。</summary>
public sealed class CalculationWorkflowArtifactExclusionSummary
{
    /// <summary>排除类别，例如 scratch-cache、temporary-file。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>命中该类别的文件数量。</summary>
    public int FileCount { get; set; }

    /// <summary>可确定时的总字节数。</summary>
    public long TotalBytes { get; set; }

    /// <summary>排除原因。</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>是否可以重新生成。</summary>
    public bool CanRegenerate { get; set; }

    /// <summary>是否影响科学复现。</summary>
    public bool AffectsReproduction { get; set; }
}

/// <summary>全过程产物发现使用的稳定问题码。</summary>
public static class CalculationWorkflowArtifactDiscoveryIssueCodes
{
    /// <summary>根工作流账本不存在。</summary>
    public const string LedgerMissing =
        "artifacts.ledger.missing";

    /// <summary>作业清单不可用，无法完整解析程序规则。</summary>
    public const string ManifestUnavailable =
        "artifacts.manifest.unavailable";

    /// <summary>没有可处理该作业的程序适配器。</summary>
    public const string AdapterMissing =
        "artifacts.adapter.missing";

    /// <summary>作业目录不存在。</summary>
    public const string JobDirectoryMissing =
        "artifacts.job_directory.missing";

    /// <summary>作业中的固定目录不存在。</summary>
    public const string DirectoryMissing =
        "artifacts.directory.missing";

    /// <summary>工作区和作业清单给出了冲突的运行目录。</summary>
    public const string RunDirectoryConflict =
        "artifacts.run_directory.conflict";

    /// <summary>目录无法枚举。</summary>
    public const string DirectoryUnreadable =
        "artifacts.directory.unreadable";

    /// <summary>目录或文件是重解析点，拒绝跟随。</summary>
    public const string ReparsePointRejected =
        "artifacts.path.reparse_point";

    /// <summary>路径位于作业目录之外。</summary>
    public const string PathOutsideJob =
        "artifacts.path.outside_job";

    /// <summary>文件元数据无法读取。</summary>
    public const string FileUnreadable =
        "artifacts.file.unreadable";

    /// <summary>必要条件被排除或缺失。</summary>
    public const string RequiredEvidenceMissing =
        "artifacts.required.missing";

    /// <summary>必要条件存在，但被策略排除。</summary>
    public const string RequiredEvidenceExcluded =
        "artifacts.required.excluded";

    /// <summary>程序适配器候选规则无法解析。</summary>
    public const string AdapterPatternInvalid =
        "artifacts.adapter.pattern_invalid";
}
