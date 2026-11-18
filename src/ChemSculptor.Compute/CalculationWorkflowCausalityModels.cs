namespace ChemSculptor.Compute;

/// <summary>根工作流因果关系图中的节点类型。</summary>
public enum CalculationWorkflowCausalityNodeKind
{
    /// <summary>工作流阶段。</summary>
    Stage,

    /// <summary>一次计算作业尝试。</summary>
    JobAttempt
}

/// <summary>根工作流因果关系图中的关系类型。</summary>
public enum CalculationWorkflowCausalityEdgeKind
{
    /// <summary>阶段包含作业尝试。</summary>
    StageContainsJob,

    /// <summary>阶段按声明顺序先于另一个阶段。</summary>
    StagePrecedesStage,

    /// <summary>作业由另一个作业派生。</summary>
    JobDerivedFromJob,

    /// <summary>作业用于修正另一个作业的异常。</summary>
    JobRecoveryOfJob
}

/// <summary>因果关系边的证据类别。</summary>
public enum CalculationWorkflowCausalityEvidenceKind
{
    /// <summary>阶段清单中的作业引用。</summary>
    StageJobReference,

    /// <summary>阶段声明的执行顺序。</summary>
    StageOrder,

    /// <summary>作业清单中的父作业标识。</summary>
    ParentJobId,

    /// <summary>作业清单中的恢复尝试序号。</summary>
    AttemptNumber,

    /// <summary>作业清单中的修正计划标识。</summary>
    CorrectionPlanId
}

/// <summary>因果关系审计问题级别。</summary>
public enum CalculationWorkflowCausalityIssueSeverity
{
    /// <summary>信息性记录。</summary>
    Info,

    /// <summary>需要关注，但图仍可用于部分审计。</summary>
    Warning,

    /// <summary>证据冲突或关系断裂，不能静默忽略。</summary>
    Error
}

/// <summary>
/// 根工作流 causation graph 的持久化快照。
/// 只从账本和作业清单取证，不读取计算输出，也不推断时间因果关系。
/// </summary>
public sealed class CalculationWorkflowCausalityGraph
{
    /// <summary>图模式版本。</summary>
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>唯一根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>构建该图的时间截面。</summary>
    public DateTimeOffset CapturedAt { get; set; } =
        DateTimeOffset.UtcNow;

    /// <summary>阶段和作业尝试节点。</summary>
    public List<CalculationWorkflowCausalityNode> Nodes { get; set; } =
        new List<CalculationWorkflowCausalityNode>();

    /// <summary>显式证据支持的因果关系边。</summary>
    public List<CalculationWorkflowCausalityEdge> Edges { get; set; } =
        new List<CalculationWorkflowCausalityEdge>();

    /// <summary>构建或校验关系时发现的问题。</summary>
    public List<CalculationWorkflowCausalityIssue> Issues { get; set; } =
        new List<CalculationWorkflowCausalityIssue>();
}

/// <summary>因果关系图中的一个阶段或作业尝试节点。</summary>
public sealed class CalculationWorkflowCausalityNode
{
    /// <summary>稳定节点标识。</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>节点类型。</summary>
    public CalculationWorkflowCausalityNodeKind NodeKind { get; set; }

    /// <summary>阶段标识；作业节点为空。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>作业标识；阶段节点为空。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>阶段或作业的可审计状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>阶段声明顺序；作业节点为 0。</summary>
    public int StageOrder { get; set; }

    /// <summary>任务类型；阶段节点为空。</summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>计算程序；阶段节点为空。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>父作业标识；没有时为空。</summary>
    public string ParentJobId { get; set; } = string.Empty;

    /// <summary>修正计划标识；没有时为空。</summary>
    public string CorrectionPlanId { get; set; } = string.Empty;

    /// <summary>原始作业清单是否可用。</summary>
    public bool HasJobManifest { get; set; }

    /// <summary>节点取证来源说明。</summary>
    public List<string> Evidence { get; set; } = new List<string>();
}

/// <summary>因果关系图中的一条有证据支持的关系边。</summary>
public sealed class CalculationWorkflowCausalityEdge
{
    /// <summary>稳定边标识。</summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>关系类型。</summary>
    public CalculationWorkflowCausalityEdgeKind EdgeKind { get; set; }

    /// <summary>起点节点标识。</summary>
    public string FromNodeId { get; set; } = string.Empty;

    /// <summary>终点节点标识。</summary>
    public string ToNodeId { get; set; } = string.Empty;

    /// <summary>关系所依据的证据类别。</summary>
    public CalculationWorkflowCausalityEvidenceKind EvidenceKind
    {
        get;
        set;
    }

    /// <summary>来源字段名。</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>该关系是否是完整审计的必要条件。</summary>
    public bool IsRequired { get; set; }
}

/// <summary>因果关系构建或校验问题。</summary>
public sealed class CalculationWorkflowCausalityIssue
{
    /// <summary>稳定问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题摘要。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>问题严重级别。</summary>
    public CalculationWorkflowCausalityIssueSeverity Severity { get; set; } =
        CalculationWorkflowCausalityIssueSeverity.Error;

    /// <summary>相关阶段标识。</summary>
    public string StageId { get; set; } = string.Empty;

    /// <summary>相关作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>相关节点标识。</summary>
    public string NodeId { get; set; } = string.Empty;
}

/// <summary>因果关系图的稳定问题码。</summary>
public static class CalculationWorkflowCausalityIssueCodes
{
    /// <summary>账本不存在。</summary>
    public const string LedgerMissing =
        "causality.ledger.missing";

    /// <summary>同一个阶段或作业节点重复出现。</summary>
    public const string DuplicateNode =
        "causality.node.duplicate";

    /// <summary>作业尝试没有可归属的阶段。</summary>
    public const string JobStageMissing =
        "causality.job.stage_missing";

    /// <summary>阶段引用了不存在的作业尝试。</summary>
    public const string StageJobMissing =
        "causality.stage.job_missing";

    /// <summary>作业同时被多个阶段引用。</summary>
    public const string JobStageConflict =
        "causality.job.stage_conflict";

    /// <summary>父作业不存在。</summary>
    public const string ParentJobMissing =
        "causality.parent.missing";

    /// <summary>父作业属于另一个根工作流。</summary>
    public const string ParentJobRootMismatch =
        "causality.parent.root_mismatch";

    /// <summary>作业引用自身为父作业。</summary>
    public const string SelfReference =
        "causality.job.self_reference";

    /// <summary>派生或恢复关系形成循环。</summary>
    public const string CausalCycle =
        "causality.job.cycle";

    /// <summary>同一有向关系被重复声明。</summary>
    public const string DuplicateEdge =
        "causality.edge.duplicate";

    /// <summary>作业清单缺失或无效。</summary>
    public const string JobManifestUnavailable =
        "causality.job.manifest_unavailable";

    /// <summary>作业可归属当前根工作流，但账本未登记。</summary>
    public const string JobUnindexed =
        "causality.job.unindexed";

    /// <summary>作业清单与账本关系字段冲突。</summary>
    public const string JobEvidenceConflict =
        "causality.job.evidence_conflict";
}
