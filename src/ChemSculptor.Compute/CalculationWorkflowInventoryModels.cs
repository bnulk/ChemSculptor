namespace ChemSculptor.Compute;

/// <summary>根工作流作业清单中 manifest.json 的状态。</summary>
public enum CalculationWorkflowJobManifestStatus
{
    /// <summary>作业清单存在且可读取。</summary>
    Present,

    /// <summary>账本引用了作业，但作业清单不存在。</summary>
    Missing,

    /// <summary>作业清单存在，但无法读取或反序列化。</summary>
    Invalid
}

/// <summary>作业清单审计问题的严重级别。</summary>
public enum CalculationWorkflowInventoryIssueSeverity
{
    /// <summary>信息性记录。</summary>
    Info,

    /// <summary>需要关注，但作业仍可用于部分审计。</summary>
    Warning,

    /// <summary>证据不完整，不能静默忽略。</summary>
    Error
}

/// <summary>根工作流作业完整枚举和筛选条件。</summary>
public sealed class CalculationWorkflowJobInventoryQuery
{
    /// <summary>必填的根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>可选阶段筛选；空集合表示全部阶段。</summary>
    public List<string> StageIds { get; set; } = new List<string>();

    /// <summary>可选作业状态筛选；空集合表示全部状态。</summary>
    public List<CalculationJobState> States { get; set; } =
        new List<CalculationJobState>();

    /// <summary>可选任务类型筛选；空集合表示全部任务类型。</summary>
    public List<CalculationTaskType> TaskTypes { get; set; } =
        new List<CalculationTaskType>();

    /// <summary>可选程序筛选；不区分大小写，空集合表示全部程序。</summary>
    public List<string> Programs { get; set; } = new List<string>();

    /// <summary>可选父作业筛选；空字符串表示不筛选。</summary>
    public string ParentJobId { get; set; } = string.Empty;

    /// <summary>
    /// 可选恢复尝试筛选。
    /// true 表示 AttemptNumber 大于 0 或存在 ParentJobId；
    /// false 表示普通原始作业；null 表示不筛选。
    /// </summary>
    public bool? RecoveryAttempt { get; set; }

    /// <summary>是否扫描并包含清单存在但账本未登记的作业。</summary>
    public bool IncludeUnindexedJobs { get; set; } = true;
}

/// <summary>一个根工作流作业清单的查询结果。</summary>
public sealed class CalculationWorkflowJobInventory
{
    /// <summary>根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>生成清单的时间截面。</summary>
    public DateTimeOffset CapturedAt { get; set; } =
        DateTimeOffset.UtcNow;

    /// <summary>读取到的账本；账本缺失时为空。</summary>
    public CalculationWorkflowEvidenceLedger? Ledger { get; set; }

    /// <summary>经过筛选的作业条目，按阶段和创建时间稳定排序。</summary>
    public List<CalculationWorkflowJobInventoryEntry> Jobs { get; set; } =
        new List<CalculationWorkflowJobInventoryEntry>();

    /// <summary>枚举和关联审计发现的问题；不受作业筛选影响。</summary>
    public List<CalculationWorkflowInventoryIssue> Issues { get; set; } =
        new List<CalculationWorkflowInventoryIssue>();
}

/// <summary>根工作流中的一个作业条目。</summary>
public sealed class CalculationWorkflowJobInventoryEntry
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>是否已出现在根工作流账本中。</summary>
    public bool IsIndexed { get; set; }

    /// <summary>所属阶段标识。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>父作业标识；原始作业为空。</summary>
    public string ParentJobId { get; set; } = string.Empty;

    /// <summary>恢复尝试序号。</summary>
    public int AttemptNumber { get; set; }

    /// <summary>作业状态；清单可用时采用清单中的最新值。</summary>
    public CalculationJobState State { get; set; } =
        CalculationJobState.Created;

    /// <summary>任务类型。</summary>
    public CalculationTaskType TaskType { get; set; } =
        CalculationTaskType.SinglePoint;

    /// <summary>计算程序；空字符串表示未记录。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>作业清单状态。</summary>
    public CalculationWorkflowJobManifestStatus ManifestStatus { get; set; } =
        CalculationWorkflowJobManifestStatus.Missing;

    /// <summary>作业清单；缺失或无效时为空。</summary>
    public CalculationJob? Job { get; set; }

    /// <summary>账本中的原始作业尝试索引；未登记时为空。</summary>
    public CalculationWorkflowJobAttemptEvidence? LedgerAttempt { get; set; }

    /// <summary>预期作业目录。</summary>
    public string JobDirectory { get; set; } = string.Empty;

    /// <summary>作业目录是否存在。</summary>
    public bool JobDirectoryExists { get; set; }

    /// <summary>本次运行目录；来自清单或工作区约定。</summary>
    public string RunDirectory { get; set; } = string.Empty;

    /// <summary>本次运行目录是否存在。</summary>
    public bool RunDirectoryExists { get; set; }

    /// <summary>作业创建时间；优先采用清单中的值。</summary>
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}

/// <summary>作业枚举和关联审计中的稳定问题。</summary>
public sealed class CalculationWorkflowInventoryIssue
{
    /// <summary>稳定问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题摘要。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>问题严重级别。</summary>
    public CalculationWorkflowInventoryIssueSeverity Severity { get; set; } =
        CalculationWorkflowInventoryIssueSeverity.Error;

    /// <summary>相关作业标识；没有时为空。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>相关阶段标识；没有时为空。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>相关文件或目录路径；没有时为空。</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>问题发现时间。</summary>
    public DateTimeOffset RecordedAt { get; set; } =
        DateTimeOffset.UtcNow;
}

/// <summary>根工作流作业清单使用的稳定问题码。</summary>
public static class CalculationWorkflowInventoryIssueCodes
{
    /// <summary>根工作流账本不存在。</summary>
    public const string LedgerMissing = "inventory.ledger.missing";

    /// <summary>账本引用的作业目录不存在。</summary>
    public const string JobDirectoryMissing =
        "inventory.job_directory.missing";

    /// <summary>账本引用的作业清单不存在。</summary>
    public const string ManifestMissing =
        "inventory.manifest.missing";

    /// <summary>作业清单无法读取或反序列化。</summary>
    public const string ManifestInvalid =
        "inventory.manifest.invalid";

    /// <summary>清单中的作业标识与账本或目录不一致。</summary>
    public const string ManifestJobMismatch =
        "inventory.manifest.job_mismatch";

    /// <summary>清单中的根工作流标识与查询范围不一致。</summary>
    public const string ManifestRootMismatch =
        "inventory.manifest.root_mismatch";

    /// <summary>清单中的运行目录缺失。</summary>
    public const string RunDirectoryMissing =
        "inventory.run_directory.missing";

    /// <summary>作业目录存在且可归属根工作流，但账本未登记。</summary>
    public const string JobUnindexed =
        "inventory.job.unindexed";

    /// <summary>阶段引用了不存在的作业尝试。</summary>
    public const string StageJobReferenceMissing =
        "inventory.stage.reference_missing";

    /// <summary>账本作业尝试引用不存在的阶段。</summary>
    public const string AttemptStageMissing =
        "inventory.attempt.stage_missing";

    /// <summary>阶段列表没有包含账本作业尝试。</summary>
    public const string StageJobReferenceNotListed =
        "inventory.stage.reference_not_listed";

    /// <summary>账本中同一个作业被登记了多次。</summary>
    public const string DuplicateAttempt =
        "inventory.attempt.duplicate";

    /// <summary>账本中同一个阶段被登记了多次。</summary>
    public const string DuplicateStage =
        "inventory.stage.duplicate";
}


