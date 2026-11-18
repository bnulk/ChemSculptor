namespace ChemSculptor.Reporting;

/// <summary>
/// 报告包的边界契约。
/// 同一对象同时服务科学点成果包和全过程计算档案包，
/// 但两类包通过 Kind 和收录开关保持不同语义。
/// </summary>
public sealed class ReportPackageScope
{
    /// <summary>科学点目录的固定逻辑名称。</summary>
    public const string DefaultScienceDirectory = "science";

    /// <summary>执行过程目录的固定逻辑名称。</summary>
    public const string DefaultProcessDirectory = "process";

    /// <summary>范围契约模式版本。</summary>
    public string SchemaVersion { get; set; } = ReportContractVersions.Current;

    /// <summary>包范围类别。</summary>
    public ReportPackageScopeKind Kind { get; set; } =
        ReportPackageScopeKind.ScientificPointArtifact;

    /// <summary>科学成果标识；科学点成果包必填。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>根工作流标识；全过程计算档案包必填。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>冻结证据的时间截面。</summary>
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>计算结论状态；不改变档案完整性定义。</summary>
    public ReportCalculationConclusionStatus CalculationConclusion { get; set; } =
        ReportCalculationConclusionStatus.Unknown;

    /// <summary>是否收录科学点集合。</summary>
    public bool IncludeSciencePointSet { get; set; } = true;

    /// <summary>是否收录全部阶段和作业尝试的执行证据。</summary>
    public bool IncludeExecutionEvidence { get; set; }

    /// <summary>是否收录排除项清单；全过程档案必须保留可审计的排除项。</summary>
    public bool IncludeExcludedArtifacts { get; set; }

    /// <summary>科学点集合在包内的逻辑目录。</summary>
    public string ScienceDirectory { get; set; } = DefaultScienceDirectory;

    /// <summary>执行证据在包内的逻辑目录。</summary>
    public string ProcessDirectory { get; set; } = DefaultProcessDirectory;

    /// <summary>创建一个科学点成果包范围。</summary>
    public static ReportPackageScope CreateScientificPointArtifact(
        string resultId)
    {
        return new ReportPackageScope
        {
            Kind = ReportPackageScopeKind.ScientificPointArtifact,
            ResultId = resultId ?? string.Empty,
            IncludeSciencePointSet = true,
            IncludeExecutionEvidence = false,
            IncludeExcludedArtifacts = false
        };
    }

    /// <summary>创建一个全过程计算档案包范围。</summary>
    public static ReportPackageScope CreateCalculationRunArchive(
        string rootWorkflowId,
        string resultId = "")
    {
        return new ReportPackageScope
        {
            Kind = ReportPackageScopeKind.CalculationRunArchive,
            RootWorkflowId = rootWorkflowId ?? string.Empty,
            ResultId = resultId ?? string.Empty,
            IncludeSciencePointSet = true,
            IncludeExecutionEvidence = true,
            IncludeExcludedArtifacts = true
        };
    }
}

/// <summary>报告包范围校验结果。</summary>
public sealed class ReportPackageScopeValidationResult
{
    /// <summary>校验发现的问题。</summary>
    public List<ReportEvidenceIssue> Issues { get; set; } =
        new List<ReportEvidenceIssue>();

    /// <summary>是否存在阻止范围生效的错误。</summary>
    public bool IsValid
    {
        get
        {
            return !Issues.Any(
                issue => issue.Severity
                    == ReportEvidenceIssueSeverity.Error);
        }
    }
}

/// <summary>校验报告包边界及其与外部标识的一致性。</summary>
public static class ReportPackageScopeValidator
{
    /// <summary>校验一个范围对象。</summary>
    public static ReportPackageScopeValidationResult Validate(
        ReportPackageScope? scope,
        ReportPackageStatus packageStatus = ReportPackageStatus.Draft,
        string resultId = "",
        string rootWorkflowId = "")
    {
        ReportPackageScopeValidationResult result =
            new ReportPackageScopeValidationResult();

        if (scope == null)
        {
            AddError(
                result,
                "report.scope.missing",
                "成果包缺少范围契约。",
                string.Empty);
            return result;
        }

        ValidateIdentity(
            result,
            scope,
            resultId,
            rootWorkflowId);

        if (scope.Kind
            == ReportPackageScopeKind.ScientificPointArtifact)
        {
            ValidateScientificPointArtifact(result, scope);
        }
        else if (scope.Kind
            == ReportPackageScopeKind.CalculationRunArchive)
        {
            ValidateCalculationRunArchive(
                result,
                scope,
                packageStatus);
        }

        return result;
    }

    private static void ValidateIdentity(
        ReportPackageScopeValidationResult result,
        ReportPackageScope scope,
        string resultId,
        string rootWorkflowId)
    {
        if (!string.IsNullOrWhiteSpace(resultId)
            && !string.Equals(
                resultId.Trim(),
                scope.ResultId.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            AddError(
                result,
                "report.scope.result_id.mismatch",
                "外部科学成果标识与范围中的科学成果标识不一致。",
                scope.ResultId);
        }

        if (!string.IsNullOrWhiteSpace(rootWorkflowId)
            && !string.Equals(
                rootWorkflowId.Trim(),
                scope.RootWorkflowId.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            AddError(
                result,
                "report.scope.root_workflow_id.mismatch",
                "外部根工作流标识与范围中的根工作流标识不一致。",
                scope.RootWorkflowId);
        }
    }

    private static void ValidateScientificPointArtifact(
        ReportPackageScopeValidationResult result,
        ReportPackageScope scope)
    {
        if (string.IsNullOrWhiteSpace(scope.ResultId))
        {
            AddError(
                result,
                "report.scope.result_id.missing",
                "科学点成果包必须指定科学成果标识。",
                string.Empty);
        }

        if (!scope.IncludeSciencePointSet)
        {
            AddError(
                result,
                "report.scope.science_point_set.not_included",
                "科学点成果包必须包含科学点集合。",
                string.Empty);
        }

        if (scope.IncludeExecutionEvidence)
        {
            AddError(
                result,
                "report.scope.process_evidence.included",
                "科学点成果包不得收录全过程执行证据；请使用全过程计算档案包范围。",
                string.Empty);
        }
    }

    private static void ValidateCalculationRunArchive(
        ReportPackageScopeValidationResult result,
        ReportPackageScope scope,
        ReportPackageStatus packageStatus)
    {
        if (string.IsNullOrWhiteSpace(scope.RootWorkflowId))
        {
            AddError(
                result,
                "report.scope.root_workflow_id.missing",
                "全过程计算档案包必须指定根工作流标识。",
                string.Empty);
        }

        if (!scope.IncludeSciencePointSet)
        {
            AddError(
                result,
                "report.scope.science_point_set.not_included",
                "全过程计算档案包必须保留科学点集合。",
                string.Empty);
        }

        if (!scope.IncludeExecutionEvidence)
        {
            AddError(
                result,
                "report.scope.execution_evidence.not_included",
                "全过程计算档案包必须包含全部阶段和作业尝试的执行证据。",
                string.Empty);
        }

        if (!scope.IncludeExcludedArtifacts)
        {
            AddError(
                result,
                "report.scope.excluded_artifacts.not_included",
                "全过程计算档案包必须保留排除项清单。",
                string.Empty);
        }

        if (!IsSafeLogicalDirectory(scope.ScienceDirectory))
        {
            AddError(
                result,
                "report.scope.science_directory.invalid",
                "科学点目录必须是非根路径的安全相对目录。",
                scope.ScienceDirectory);
        }

        if (!IsSafeLogicalDirectory(scope.ProcessDirectory))
        {
            AddError(
                result,
                "report.scope.process_directory.invalid",
                "执行证据目录必须是非根路径的安全相对目录。",
                scope.ProcessDirectory);
        }

        if (IsSafeLogicalDirectory(scope.ScienceDirectory)
            && IsSafeLogicalDirectory(scope.ProcessDirectory)
            && string.Equals(
                scope.ScienceDirectory,
                scope.ProcessDirectory,
                StringComparison.OrdinalIgnoreCase))
        {
            AddError(
                result,
                "report.scope.directories.not_distinct",
                "科学点目录和执行证据目录必须彼此不同。",
                scope.ScienceDirectory);
        }

        if (packageStatus == ReportPackageStatus.Complete
            && scope.CalculationConclusion
                == ReportCalculationConclusionStatus.InProgress)
        {
            AddError(
                result,
                "report.scope.in_progress.cannot_be_complete",
                "仍在运行的工作流只能生成临时快照，不能标记成果包为 Complete。",
                string.Empty);
        }

        if (packageStatus == ReportPackageStatus.Complete
            && scope.CalculationConclusion
                == ReportCalculationConclusionStatus.Unknown)
        {
            AddError(
                result,
                "report.scope.unknown_conclusion.cannot_be_complete",
                "无法确定计算结论时不能把全过程档案标记为 Complete。",
                string.Empty);
        }

    }

    private static bool IsSafeLogicalDirectory(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Path.IsPathRooted(value)
            || value.Contains(':', StringComparison.Ordinal)
            || value.Contains('\\', StringComparison.Ordinal)
            || value.Contains("../", StringComparison.Ordinal)
            || value.Contains("./", StringComparison.Ordinal)
            || value.StartsWith("/", StringComparison.Ordinal)
            || value.EndsWith("/", StringComparison.Ordinal)
            || value.Contains("//", StringComparison.Ordinal))
        {
            return false;
        }

        string[] segments = value.Split(
            new[] { '/' },
            StringSplitOptions.RemoveEmptyEntries);

        for (int index = 0; index < segments.Length; index++)
        {
            string segment = segments[index];

            if (string.IsNullOrWhiteSpace(segment)
                || segment == "."
                || segment == ".."
                || segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }
        }

        return true;
    }

    private static void AddError(
        ReportPackageScopeValidationResult result,
        string code,
        string message,
        string referenceId)
    {
        ReportEvidenceIssue issue = new ReportEvidenceIssue();
        issue.Code = code;
        issue.Severity = ReportEvidenceIssueSeverity.Error;
        issue.ReferenceId = referenceId;
        issue.Message = message;
        issue.IsBlocking = true;
        result.Issues.Add(issue);
    }
}
