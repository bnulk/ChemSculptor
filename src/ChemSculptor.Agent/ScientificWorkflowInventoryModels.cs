using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.Agent;

/// <summary>科学成果与根工作流作业之间关联问题的级别。</summary>
public enum ScientificWorkflowAssociationIssueSeverity
{
    /// <summary>信息性记录。</summary>
    Information,

    /// <summary>关联不完整，但仍保留已有证据。</summary>
    Warning,

    /// <summary>关联错误，不能作为完整的科学成果证据。</summary>
    Error
}

/// <summary>科学点引用作业的方式。</summary>
public enum ScientificWorkflowJobReferenceKind
{
    /// <summary>科学点直接计算作业。</summary>
    Calculation,

    /// <summary>科学点来源中的初始作业。</summary>
    Initial,

    /// <summary>科学点来源中的已接受作业。</summary>
    Accepted,

    /// <summary>科学点文件引用中的作业。</summary>
    Artifact,

    /// <summary>科学点验证记录中的作业。</summary>
    Validation,

    /// <summary>科学点元数据中声明的作业。</summary>
    Metadata
}

/// <summary>科学成果与根工作流作业关联审计的查询条件。</summary>
public sealed class ScientificWorkflowInventoryQuery
{
    /// <summary>必填的根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>可选科学成果筛选；为空表示扫描全部科学成果。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>
    /// 作业清单筛选条件。根工作流标识由外层查询统一提供，
    /// 该查询中的 RootWorkflowId 会被覆盖。
    /// </summary>
    public CalculationWorkflowJobInventoryQuery JobQuery { get; set; } =
        new CalculationWorkflowJobInventoryQuery();
}

/// <summary>科学成果与根工作流作业的关联审计结果。</summary>
public sealed class ScientificWorkflowInventory
{
    /// <summary>根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>生成结果的时间截面。</summary>
    public DateTimeOffset CapturedAt { get; set; } =
        DateTimeOffset.UtcNow;

    /// <summary>按调用方筛选条件得到的作业清单。</summary>
    public CalculationWorkflowJobInventory JobInventory { get; set; } =
        new CalculationWorkflowJobInventory();

    /// <summary>
    /// 关联审计使用的完整作业清单，不受调用方作业筛选影响。
    /// 该清单包含账本登记的作业以及发现的孤立作业。
    /// </summary>
    public CalculationWorkflowJobInventory AuditJobInventory { get; set; } =
        new CalculationWorkflowJobInventory();

    /// <summary>当前根工作流中关联的科学成果。</summary>
    public List<ScientificWorkflowResultAssociation> Results { get; set; } =
        new List<ScientificWorkflowResultAssociation>();

    /// <summary>科学成果、科学点与作业关联的问题。</summary>
    public List<ScientificWorkflowAssociationIssue> Issues { get; set; } =
        new List<ScientificWorkflowAssociationIssue>();

    /// <summary>是否存在阻止成果成为完整证据的错误。</summary>
    public bool HasErrors
    {
        get
        {
            return Issues.Any(
                issue => issue.Severity
                    == ScientificWorkflowAssociationIssueSeverity.Error);
        }
    }
}

/// <summary>一个科学成果及其计算点关联。</summary>
public sealed class ScientificWorkflowResultAssociation
{
    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>冻结的科学成果对象；来自科学数据仓储。</summary>
    public ScientificResult Result { get; set; } = new ScientificResult();

    /// <summary>科学点关联。</summary>
    public List<ScientificWorkflowPointAssociation> Points { get; set; } =
        new List<ScientificWorkflowPointAssociation>();

    /// <summary>科学成果是否至少有一个点可追溯到当前根工作流。</summary>
    public bool MatchesRootWorkflow { get; set; }
}

/// <summary>一个科学点及其全部作业引用。</summary>
public sealed class ScientificWorkflowPointAssociation
{
    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>所属科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>科学点对象。</summary>
    public CalculationPoint Point { get; set; } = new CalculationPoint();

    /// <summary>科学点明确记录的根工作流标识。</summary>
    public string DeclaredRootWorkflowId { get; set; } = string.Empty;

    /// <summary>由作业清单推断出的根工作流标识。</summary>
    public string EffectiveRootWorkflowId { get; set; } = string.Empty;

    /// <summary>科学点是否属于查询的根工作流。</summary>
    public bool MatchesRootWorkflow { get; set; }

    /// <summary>去重后的全部被引用作业标识。</summary>
    public List<string> ReferencedJobIds { get; set; } =
        new List<string>();

    /// <summary>每个作业引用的来源和清单映射。</summary>
    public List<ScientificWorkflowJobAssociation> Jobs { get; set; } =
        new List<ScientificWorkflowJobAssociation>();
}

/// <summary>科学点对某个作业的一次引用。</summary>
public sealed class ScientificWorkflowJobAssociation
{
    /// <summary>被引用的作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>引用来源类别。</summary>
    public ScientificWorkflowJobReferenceKind Kind { get; set; }

    /// <summary>引用来源字段、文件或验证记录标识。</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>匹配到的作业清单条目；未匹配时为空。</summary>
    public CalculationWorkflowJobInventoryEntry? InventoryEntry
    {
        get;
        set;
    }

    /// <summary>引用是否在当前作业清单中找到。</summary>
    public bool IsPresent
    {
        get { return InventoryEntry != null; }
    }
}

/// <summary>科学成果与作业关联审计问题。</summary>
public sealed class ScientificWorkflowAssociationIssue
{
    /// <summary>稳定问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>问题级别。</summary>
    public ScientificWorkflowAssociationIssueSeverity Severity { get; set; } =
        ScientificWorkflowAssociationIssueSeverity.Error;

    /// <summary>相关科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>相关科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>相关作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>引用来源字段、文件或验证记录标识。</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>问题发现时间。</summary>
    public DateTimeOffset RecordedAt { get; set; } =
        DateTimeOffset.UtcNow;
}

/// <summary>科学成果与根工作流作业关联审计的稳定问题码。</summary>
public static class ScientificWorkflowAssociationIssueCodes
{
    /// <summary>当前根工作流没有找到科学成果。</summary>
    public const string ResultNotFound =
        "science.result.not_found";

    /// <summary>显式指定的科学成果不存在。</summary>
    public const string RequestedResultNotFound =
        "science.result.requested_not_found";

    /// <summary>科学成果没有计算点。</summary>
    public const string ResultPointsEmpty =
        "science.result.points_empty";

    /// <summary>科学点没有记录根工作流标识。</summary>
    public const string PointRootMissing =
        "science.point.root_missing";

    /// <summary>科学点根工作流标识与查询范围不一致。</summary>
    public const string PointRootMismatch =
        "science.point.root_mismatch";

    /// <summary>科学点缺少直接计算作业标识。</summary>
    public const string PointCalculationJobMissing =
        "science.point.calculation_job_missing";

    /// <summary>科学点的直接计算作业不在当前作业清单中。</summary>
    public const string PointCalculationJobNotFound =
        "science.point.calculation_job_not_found";

    /// <summary>科学点缺少来源初始作业标识。</summary>
    public const string PointInitialJobMissing =
        "science.point.initial_job_missing";

    /// <summary>科学点的来源初始作业不在当前作业清单中。</summary>
    public const string PointInitialJobNotFound =
        "science.point.initial_job_not_found";

    /// <summary>科学点缺少已接受作业标识。</summary>
    public const string PointAcceptedJobMissing =
        "science.point.accepted_job_missing";

    /// <summary>科学点的已接受作业不在当前作业清单中。</summary>
    public const string PointAcceptedJobNotFound =
        "science.point.accepted_job_not_found";

    /// <summary>科学点文件没有记录来源作业。</summary>
    public const string ArtifactJobMissing =
        "science.artifact.job_missing";

    /// <summary>科学点文件的来源作业不在当前作业清单中。</summary>
    public const string ArtifactJobNotFound =
        "science.artifact.job_not_found";

    /// <summary>科学点验证记录没有记录作业。</summary>
    public const string ValidationJobMissing =
        "science.validation.job_missing";

    /// <summary>科学点验证记录的作业不在当前作业清单中。</summary>
    public const string ValidationJobNotFound =
        "science.validation.job_not_found";

    /// <summary>科学点元数据声明的作业不在当前作业清单中。</summary>
    public const string MetadataJobNotFound =
        "science.metadata.job_not_found";
}
