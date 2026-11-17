namespace ChemSculptor.Compute;

/// <summary>根工作流证据账本的整体状态。</summary>
public enum CalculationWorkflowEvidenceStatus
{
    /// <summary>账本已创建，尚未开始执行。</summary>
    Ready,

    /// <summary>至少一个阶段正在执行。</summary>
    Running,

    /// <summary>全部已登记阶段均已完成。</summary>
    Completed,

    /// <summary>至少一个阶段失败。</summary>
    Failed,

    /// <summary>至少一个阶段被取消，且没有失败阶段。</summary>
    Canceled
}

/// <summary>根工作流中单个阶段的证据状态。</summary>
public enum CalculationWorkflowStageStatus
{
    /// <summary>阶段已经登记，但尚未提交作业。</summary>
    Ready,

    /// <summary>阶段中存在待执行或执行中的作业。</summary>
    Running,

    /// <summary>阶段最近一次作业成功完成。</summary>
    Completed,

    /// <summary>阶段最近一次作业失败。</summary>
    Failed,

    /// <summary>阶段最近一次作业被取消。</summary>
    Canceled,

    /// <summary>阶段被明确跳过，原因必须记录。</summary>
    Skipped
}

/// <summary>
/// 一个根工作流的持久化证据账本。
/// 只保存阶段关系、作业索引和状态，不复制作业目录中的计算文件。
/// </summary>
public sealed class CalculationWorkflowEvidenceLedger
{
    /// <summary>账本模式版本。</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>根工作流标识，也是全过程档案的唯一范围。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>所属客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>用户或上层工作流的科研目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>根工作流整体状态。</summary>
    public CalculationWorkflowEvidenceStatus Status { get; set; } =
        CalculationWorkflowEvidenceStatus.Ready;

    /// <summary>账本创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>账本最后更新时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>根工作流中的全部阶段。</summary>
    public List<CalculationWorkflowStageEvidence> Stages { get; set; } =
        new List<CalculationWorkflowStageEvidence>();

    /// <summary>根工作流中的全部作业尝试索引。</summary>
    public List<CalculationWorkflowJobAttemptEvidence> JobAttempts
    {
        get;
        set;
    } = new List<CalculationWorkflowJobAttemptEvidence>();

    /// <summary>账本索引过程中发现的问题。</summary>
    public List<CalculationWorkflowEvidenceIssue> Issues { get; set; } =
        new List<CalculationWorkflowEvidenceIssue>();
}

/// <summary>根工作流中的一个阶段。</summary>
public sealed class CalculationWorkflowStageEvidence
{
    /// <summary>稳定阶段标识，不使用显示名称代替。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>阶段类型。</summary>
    public string StageType { get; set; } = string.Empty;

    /// <summary>稳定执行顺序，从 1 开始。</summary>
    public int Order { get; set; }

    /// <summary>阶段状态。</summary>
    public CalculationWorkflowStageStatus Status { get; set; } =
        CalculationWorkflowStageStatus.Ready;

    /// <summary>阶段最早开始时间。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>阶段结束时间；仍有活动作业时为空。</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>阶段内的作业尝试标识。恢复作业也保留在这里。</summary>
    public List<string> JobAttemptIds { get; set; } = new List<string>();

    /// <summary>被明确跳过时的原因。</summary>
    public string SkippedReason { get; set; } = string.Empty;

    /// <summary>阶段级诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } =
        new List<CalculationDiagnostic>();
}

/// <summary>账本中的一个计算作业尝试索引。</summary>
public sealed class CalculationWorkflowJobAttemptEvidence
{
    /// <summary>计算作业标识。</summary>
    public string CalculationJobId { get; set; } = string.Empty;

    /// <summary>作业所属阶段。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>父作业标识；原始作业为空。</summary>
    public string ParentJobId { get; set; } = string.Empty;

    /// <summary>恢复尝试序号。</summary>
    public int AttemptNumber { get; set; }

    /// <summary>作业最近保存时的状态。</summary>
    public CalculationJobState State { get; set; } =
        CalculationJobState.Created;

    /// <summary>计算任务类型。</summary>
    public CalculationTaskType TaskType { get; set; } =
        CalculationTaskType.SinglePoint;

    /// <summary>计算程序。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>产生该作业的修正计划标识。</summary>
    public string CorrectionPlanId { get; set; } = string.Empty;

    /// <summary>作业创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>作业开始时间。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>作业结束时间。</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>该索引最后一次更新时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>账本索引问题。</summary>
public sealed class CalculationWorkflowEvidenceIssue
{
    /// <summary>稳定问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>相关阶段标识；没有时为空。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>相关作业标识；没有时为空。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>问题发现时间。</summary>
    public DateTimeOffset RecordedAt { get; set; } =
        DateTimeOffset.UtcNow;
}

/// <summary>根工作流证据账本使用的稳定阶段标识。</summary>
public static class CalculationWorkflowStageIds
{
    /// <summary>单点计算阶段。</summary>
    public const string SinglePoint = "single-point";

    /// <summary>几何优化阶段。</summary>
    public const string Optimization = "optimization";

    /// <summary>振动分析阶段。</summary>
    public const string Frequency = "frequency";

    /// <summary>联合优化频率阶段。</summary>
    public const string OptimizationFrequency = "optimization-frequency";

    /// <summary>TDDFT 单点阶段。</summary>
    public const string Tddft = "tddft";

    /// <summary>波函数稳定性检查阶段。</summary>
    public const string WavefunctionStability =
        "wavefunction-stability";

    /// <summary>波函数稳定性异常修正阶段。</summary>
    public const string StabilityCorrection =
        "stability-correction";

    /// <summary>优化异常诊断和恢复阶段。</summary>
    public const string OptimizationRecovery =
        "optimization-recovery";

    /// <summary>NTO 阶段。</summary>
    public const string Nto = "nto";

    /// <summary>自旋轨道耦合阶段。</summary>
    public const string Soc = "soc";

    /// <summary>MECP 搜索阶段。</summary>
    public const string Mecp = "mecp";

    /// <summary>科学数据归档阶段。</summary>
    public const string Archive = "archive";

    /// <summary>按任务类型解析默认阶段标识。</summary>
    public static string ResolveDefaultStageId(
        CalculationTaskType taskType)
    {
        switch (taskType)
        {
            case CalculationTaskType.Optimization:
                return Optimization;
            case CalculationTaskType.Frequency:
                return Frequency;
            case CalculationTaskType.OptimizationFrequency:
                return OptimizationFrequency;
            case CalculationTaskType.TdDft:
                return Tddft;
            case CalculationTaskType.Nto:
                return Nto;
            case CalculationTaskType.Soc:
                return Soc;
            case CalculationTaskType.Mecp:
                return Mecp;
            default:
                return SinglePoint;
        }
    }

    /// <summary>按稳定阶段标识给出默认显示顺序。</summary>
    public static int ResolveDefaultOrder(string stageId)
    {
        if (string.Equals(
                stageId,
                SinglePoint,
                StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (string.Equals(
                stageId,
                Optimization,
                StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (string.Equals(
                stageId,
                OptimizationFrequency,
                StringComparison.OrdinalIgnoreCase))
        {
            return 10;
        }

        if (string.Equals(
                stageId,
                WavefunctionStability,
                StringComparison.OrdinalIgnoreCase))
        {
            return 20;
        }

        if (string.Equals(
                stageId,
                StabilityCorrection,
                StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (string.Equals(
                stageId,
                OptimizationRecovery,
                StringComparison.OrdinalIgnoreCase))
        {
            return 30;
        }

        if (string.Equals(
                stageId,
                Frequency,
                StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        if (string.Equals(
                stageId,
                Tddft,
                StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

        if (string.Equals(
                stageId,
                Nto,
                StringComparison.OrdinalIgnoreCase))
        {
            return 60;
        }

        if (string.Equals(
                stageId,
                Archive,
                StringComparison.OrdinalIgnoreCase))
        {
            return 90;
        }

        return 100;
    }
}
