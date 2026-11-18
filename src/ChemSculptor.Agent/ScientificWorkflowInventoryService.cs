using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Agent;

/// <summary>
/// 把科学成果中的科学点映射到根工作流作业清单。
/// 该服务只读取持久化数据，不复制文件、不执行计算。
/// </summary>
public sealed class ScientificWorkflowInventoryService
    : IScientificWorkflowInventoryService
{
    private readonly ICalculationWorkflowInventoryRepository
        _calculationInventory;
    private readonly IScientificDataRepository _scientificData;

    /// <summary>创建科学成果关联审计服务。</summary>
    public ScientificWorkflowInventoryService(
        ICalculationWorkflowInventoryRepository calculationInventory,
        IScientificDataRepository scientificData)
    {
        if (calculationInventory == null)
        {
            throw new ArgumentNullException(
                nameof(calculationInventory));
        }

        if (scientificData == null)
        {
            throw new ArgumentNullException(nameof(scientificData));
        }

        _calculationInventory = calculationInventory;
        _scientificData = scientificData;
    }

    /// <summary>构建科学成果、科学点和作业引用的关联审计结果。</summary>
    public async Task<ScientificWorkflowInventory> QueryAsync(
        ScientificWorkflowInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        string rootWorkflowId = query.RootWorkflowId.Trim();

        if (string.IsNullOrWhiteSpace(rootWorkflowId))
        {
            throw new ArgumentException(
                "根工作流标识不能为空。",
                nameof(query));
        }

        CalculationWorkflowJobInventory auditJobInventory =
            await _calculationInventory.QueryAsync(
                CreateAuditJobQuery(rootWorkflowId),
                cancellationToken);
        CalculationWorkflowJobInventory jobInventory = auditJobInventory;

        if (RequiresSeparateJobQuery(query))
        {
            jobInventory = await _calculationInventory.QueryAsync(
                CreateJobQuery(query, rootWorkflowId),
                cancellationToken);
        }

        ScientificWorkflowInventory inventory =
            new ScientificWorkflowInventory();
        inventory.RootWorkflowId = rootWorkflowId;
        inventory.JobInventory = jobInventory;
        inventory.AuditJobInventory = auditJobInventory;

        Dictionary<string, CalculationWorkflowJobInventoryEntry> jobs =
            BuildJobIndex(auditJobInventory);
        List<ScientificResult> results = await SelectResultsAsync(
            query,
            rootWorkflowId,
            jobs,
            inventory.Issues,
            cancellationToken);

        for (int index = 0; index < results.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            inventory.Results.Add(BuildResultAssociation(
                results[index],
                rootWorkflowId,
                jobs,
                inventory.Issues));
        }

        SortResults(inventory.Results);
        SortIssues(inventory.Issues);
        return inventory;
    }

    private async Task<List<ScientificResult>> SelectResultsAsync(
        ScientificWorkflowInventoryQuery query,
        string rootWorkflowId,
        Dictionary<string, CalculationWorkflowJobInventoryEntry> jobs,
        List<ScientificWorkflowAssociationIssue> issues,
        CancellationToken cancellationToken)
    {
        List<ScientificResult> selected = new List<ScientificResult>();

        if (!string.IsNullOrWhiteSpace(query.ResultId))
        {
            ScientificResult? requested = await _scientificData.GetAsync(
                query.ResultId.Trim(),
                cancellationToken);

            if (requested != null)
            {
                selected.Add(requested);
            }
            else
            {
                AddIssue(
                    issues,
                    ScientificWorkflowAssociationIssueCodes
                        .RequestedResultNotFound,
                    "指定的科学成果不存在，不能建立作业关联。",
                    ScientificWorkflowAssociationIssueSeverity.Error,
                    query.ResultId,
                    string.Empty,
                    string.Empty,
                    string.Empty);
            }

            return selected;
        }

        IReadOnlyList<ScientificResult> allResults =
            _scientificData.List();

        for (int index = 0; index < allResults.Count; index++)
        {
            ScientificResult result = allResults[index];

            if (result == null)
            {
                continue;
            }

            if (HasPointInRoot(result, rootWorkflowId, jobs))
            {
                selected.Add(result);
            }
        }

        if (selected.Count == 0)
        {
            AddIssue(
                issues,
                ScientificWorkflowAssociationIssueCodes.ResultNotFound,
                "当前根工作流没有可关联的科学成果。",
                ScientificWorkflowAssociationIssueSeverity.Error,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        return selected;
    }

    private static ScientificWorkflowResultAssociation
        BuildResultAssociation(
            ScientificResult result,
            string rootWorkflowId,
            Dictionary<string, CalculationWorkflowJobInventoryEntry> jobs,
            List<ScientificWorkflowAssociationIssue> issues)
    {
        ScientificWorkflowResultAssociation association =
            new ScientificWorkflowResultAssociation();
        association.Result = result;
        association.ResultId = result.Id;

        CalculationPointSet pointSet = result.PointSet
            ?? new CalculationPointSet();

        for (int index = 0; index < pointSet.Points.Count; index++)
        {
            CalculationPoint point = pointSet.Points[index];

            if (point == null)
            {
                continue;
            }

            ScientificWorkflowPointAssociation pointAssociation =
                BuildPointAssociation(
                    result.Id,
                    point,
                    rootWorkflowId,
                    jobs,
                    issues);
            association.Points.Add(pointAssociation);

            if (pointAssociation.MatchesRootWorkflow)
            {
                association.MatchesRootWorkflow = true;
            }
        }

        if (association.Points.Count == 0)
        {
            AddIssue(
                issues,
                ScientificWorkflowAssociationIssueCodes.ResultPointsEmpty,
                "科学成果没有计算点，不能建立科学点作业关联。",
                ScientificWorkflowAssociationIssueSeverity.Error,
                result.Id,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        return association;
    }

    private static ScientificWorkflowPointAssociation
        BuildPointAssociation(
            string resultId,
            CalculationPoint point,
            string rootWorkflowId,
            Dictionary<string, CalculationWorkflowJobInventoryEntry> jobs,
            List<ScientificWorkflowAssociationIssue> issues)
    {
        ScientificWorkflowPointAssociation association =
            new ScientificWorkflowPointAssociation();
        association.ResultId = resultId;
        association.PointId = point.Id;
        association.Point = point;

        PointProvenance provenance = point.Provenance
            ?? new PointProvenance();
        association.DeclaredRootWorkflowId =
            provenance.RootWorkflowId ?? string.Empty;

        List<JobReferenceCandidate> candidates =
            EnumerateJobReferences(point);
        Dictionary<string, ScientificWorkflowJobAssociation> associations =
            new Dictionary<string, ScientificWorkflowJobAssociation>(
                StringComparer.OrdinalIgnoreCase);
        HashSet<string> referencedJobIds = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        bool anyJobFound = false;

        for (int index = 0; index < candidates.Count; index++)
        {
            JobReferenceCandidate candidate = candidates[index];
            string jobId = candidate.JobId.Trim();

            if (string.IsNullOrWhiteSpace(jobId))
            {
                if (!string.IsNullOrWhiteSpace(
                        candidate.EmptyIssueCode))
                {
                    AddIssue(
                        issues,
                        candidate.EmptyIssueCode,
                        "科学点引用缺少作业标识。",
                        IsRequiredCalculationReference(candidate.Kind)
                            ? ScientificWorkflowAssociationIssueSeverity
                                .Error
                            : ScientificWorkflowAssociationIssueSeverity
                                .Warning,
                        resultId,
                        point.Id,
                        string.Empty,
                        candidate.SourceId);
                }

                continue;
            }

            referencedJobIds.Add(jobId);
            CalculationWorkflowJobInventoryEntry? entry;
            jobs.TryGetValue(jobId, out entry);

            ScientificWorkflowJobAssociation? jobAssociation;
            string key = candidate.Kind.ToString()
                + "|"
                + jobId
                + "|"
                + candidate.SourceId;

            if (!associations.TryGetValue(key, out jobAssociation))
            {
                jobAssociation = new ScientificWorkflowJobAssociation();
                jobAssociation.JobId = jobId;
                jobAssociation.Kind = candidate.Kind;
                jobAssociation.SourceId = candidate.SourceId;
                jobAssociation.InventoryEntry = entry;
                associations[key] = jobAssociation;
            }

            if (entry == null)
            {
                AddIssue(
                    issues,
                    candidate.NotFoundIssueCode,
                    "科学点引用的作业不在当前根工作流作业清单中。",
                    ScientificWorkflowAssociationIssueSeverity.Error,
                    resultId,
                    point.Id,
                    jobId,
                    candidate.SourceId);
            }
            else
            {
                anyJobFound = true;
            }
        }

        if (string.IsNullOrWhiteSpace(
                association.DeclaredRootWorkflowId))
        {
            AddIssue(
                issues,
                ScientificWorkflowAssociationIssueCodes.PointRootMissing,
                "科学点没有记录根工作流标识，已尝试从作业清单推断。",
                ScientificWorkflowAssociationIssueSeverity.Warning,
                resultId,
                point.Id,
                string.Empty,
                "Provenance.RootWorkflowId");
            association.EffectiveRootWorkflowId =
                anyJobFound ? rootWorkflowId : string.Empty;
            association.MatchesRootWorkflow = anyJobFound;
        }
        else if (string.Equals(
                association.DeclaredRootWorkflowId,
                rootWorkflowId,
                StringComparison.OrdinalIgnoreCase))
        {
            association.EffectiveRootWorkflowId =
                association.DeclaredRootWorkflowId;
            association.MatchesRootWorkflow = true;
        }
        else
        {
            AddIssue(
                issues,
                ScientificWorkflowAssociationIssueCodes.PointRootMismatch,
                "科学点根工作流标识与查询范围不一致。",
                ScientificWorkflowAssociationIssueSeverity.Error,
                resultId,
                point.Id,
                string.Empty,
                "Provenance.RootWorkflowId");
            association.EffectiveRootWorkflowId =
                association.DeclaredRootWorkflowId;
            association.MatchesRootWorkflow = false;
        }

        association.ReferencedJobIds.AddRange(referencedJobIds);
        association.ReferencedJobIds.Sort(
            StringComparer.OrdinalIgnoreCase);
        association.Jobs.AddRange(associations.Values);
        SortJobAssociations(association.Jobs);
        return association;
    }

    private static bool HasPointInRoot(
        ScientificResult result,
        string rootWorkflowId,
        Dictionary<string, CalculationWorkflowJobInventoryEntry> jobs)
    {
        CalculationPointSet pointSet = result.PointSet
            ?? new CalculationPointSet();

        for (int index = 0; index < pointSet.Points.Count; index++)
        {
            CalculationPoint point = pointSet.Points[index];

            if (point == null)
            {
                continue;
            }

            PointProvenance provenance = point.Provenance
                ?? new PointProvenance();
            string declaredRoot = provenance.RootWorkflowId
                ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(declaredRoot))
            {
                if (string.Equals(
                        declaredRoot,
                        rootWorkflowId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                continue;
            }

            List<JobReferenceCandidate> candidates =
                EnumerateJobReferences(point);

            for (int candidateIndex = 0;
                candidateIndex < candidates.Count;
                candidateIndex++)
            {
                string jobId = candidates[candidateIndex].JobId.Trim();

                if (!string.IsNullOrWhiteSpace(jobId)
                    && jobs.ContainsKey(jobId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static List<JobReferenceCandidate>
        EnumerateJobReferences(CalculationPoint point)
    {
        List<JobReferenceCandidate> candidates =
            new List<JobReferenceCandidate>();
        PointProvenance provenance = point.Provenance
            ?? new PointProvenance();

        candidates.Add(new JobReferenceCandidate
        {
            JobId = point.CalculationJobId,
            Kind = ScientificWorkflowJobReferenceKind.Calculation,
            SourceId = "CalculationJobId",
            EmptyIssueCode = ScientificWorkflowAssociationIssueCodes
                .PointCalculationJobMissing,
            NotFoundIssueCode = ScientificWorkflowAssociationIssueCodes
                .PointCalculationJobNotFound
        });
        candidates.Add(new JobReferenceCandidate
        {
            JobId = provenance.InitialJobId,
            Kind = ScientificWorkflowJobReferenceKind.Initial,
            SourceId = "Provenance.InitialJobId",
            EmptyIssueCode = ScientificWorkflowAssociationIssueCodes
                .PointInitialJobMissing,
            NotFoundIssueCode = ScientificWorkflowAssociationIssueCodes
                .PointInitialJobNotFound
        });
        candidates.Add(new JobReferenceCandidate
        {
            JobId = provenance.AcceptedJobId,
            Kind = ScientificWorkflowJobReferenceKind.Accepted,
            SourceId = "Provenance.AcceptedJobId",
            EmptyIssueCode = ScientificWorkflowAssociationIssueCodes
                .PointAcceptedJobMissing,
            NotFoundIssueCode = ScientificWorkflowAssociationIssueCodes
                .PointAcceptedJobNotFound
        });

        if (point.Artifacts != null)
        {
            for (int index = 0; index < point.Artifacts.Count; index++)
            {
                PointArtifactReference artifact = point.Artifacts[index];

                if (artifact == null)
                {
                    continue;
                }

                candidates.Add(new JobReferenceCandidate
                {
                    JobId = artifact.CalculationJobId,
                    Kind = ScientificWorkflowJobReferenceKind.Artifact,
                    SourceId = string.IsNullOrWhiteSpace(
                            artifact.ArtifactId)
                        ? "artifact-" + index
                        : artifact.ArtifactId,
                    EmptyIssueCode = ScientificWorkflowAssociationIssueCodes
                        .ArtifactJobMissing,
                    NotFoundIssueCode = ScientificWorkflowAssociationIssueCodes
                        .ArtifactJobNotFound
                });
            }
        }

        if (point.Validations != null)
        {
            for (int index = 0; index < point.Validations.Count; index++)
            {
                PointValidationRecord validation = point.Validations[index];

                if (validation == null)
                {
                    continue;
                }

                candidates.Add(new JobReferenceCandidate
                {
                    JobId = validation.JobId,
                    Kind = ScientificWorkflowJobReferenceKind.Validation,
                    SourceId = string.IsNullOrWhiteSpace(validation.Id)
                        ? "validation-" + index
                        : validation.Id,
                    EmptyIssueCode = ScientificWorkflowAssociationIssueCodes
                        .ValidationJobMissing,
                    NotFoundIssueCode = ScientificWorkflowAssociationIssueCodes
                        .ValidationJobNotFound
                });
            }
        }

        if (point.Metadata != null)
        {
            List<string> keys = point.Metadata.Keys.ToList();
            keys.Sort(StringComparer.OrdinalIgnoreCase);

            for (int index = 0; index < keys.Count; index++)
            {
                string key = keys[index];

                if (!key.EndsWith(
                        "JobId",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = point.Metadata[key];

                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                candidates.Add(new JobReferenceCandidate
                {
                    JobId = value,
                    Kind = ScientificWorkflowJobReferenceKind.Metadata,
                    SourceId = "Metadata." + key,
                    EmptyIssueCode = string.Empty,
                    NotFoundIssueCode = ScientificWorkflowAssociationIssueCodes
                        .MetadataJobNotFound
                });
            }
        }

        return candidates;
    }

    private static Dictionary<string, CalculationWorkflowJobInventoryEntry>
        BuildJobIndex(CalculationWorkflowJobInventory jobInventory)
    {
        Dictionary<string, CalculationWorkflowJobInventoryEntry> jobs =
            new Dictionary<string, CalculationWorkflowJobInventoryEntry>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < jobInventory.Jobs.Count; index++)
        {
            CalculationWorkflowJobInventoryEntry entry =
                jobInventory.Jobs[index];

            if (entry == null
                || string.IsNullOrWhiteSpace(entry.JobId)
                || jobs.ContainsKey(entry.JobId))
            {
                continue;
            }

            jobs[entry.JobId] = entry;
        }

        return jobs;
    }

    private static CalculationWorkflowJobInventoryQuery CreateJobQuery(
        ScientificWorkflowInventoryQuery query,
        string rootWorkflowId)
    {
        CalculationWorkflowJobInventoryQuery source = query.JobQuery
            ?? new CalculationWorkflowJobInventoryQuery();
        CalculationWorkflowJobInventoryQuery target =
            new CalculationWorkflowJobInventoryQuery();
        target.RootWorkflowId = rootWorkflowId;
        target.StageIds = source.StageIds == null
            ? new List<string>()
            : new List<string>(source.StageIds);
        target.States = source.States == null
            ? new List<CalculationJobState>()
            : new List<CalculationJobState>(source.States);
        target.TaskTypes = source.TaskTypes == null
            ? new List<CalculationTaskType>()
            : new List<CalculationTaskType>(source.TaskTypes);
        target.Programs = source.Programs == null
            ? new List<string>()
            : new List<string>(source.Programs);
        target.ParentJobId = source.ParentJobId;
        target.RecoveryAttempt = source.RecoveryAttempt;
        target.IncludeUnindexedJobs = source.IncludeUnindexedJobs;
        return target;
    }

    private static CalculationWorkflowJobInventoryQuery CreateAuditJobQuery(
        string rootWorkflowId)
    {
        CalculationWorkflowJobInventoryQuery query =
            new CalculationWorkflowJobInventoryQuery();
        query.RootWorkflowId = rootWorkflowId;
        query.IncludeUnindexedJobs = true;
        return query;
    }

    private static bool RequiresSeparateJobQuery(
        ScientificWorkflowInventoryQuery query)
    {
        CalculationWorkflowJobInventoryQuery source = query.JobQuery
            ?? new CalculationWorkflowJobInventoryQuery();

        return !source.IncludeUnindexedJobs
            || (source.StageIds != null && source.StageIds.Count > 0)
            || (source.States != null && source.States.Count > 0)
            || (source.TaskTypes != null && source.TaskTypes.Count > 0)
            || (source.Programs != null && source.Programs.Count > 0)
            || !string.IsNullOrWhiteSpace(source.ParentJobId)
            || source.RecoveryAttempt.HasValue;
    }

    private static bool IsRequiredCalculationReference(
        ScientificWorkflowJobReferenceKind kind)
    {
        return kind == ScientificWorkflowJobReferenceKind.Calculation;
    }

    private static void AddIssue(
        List<ScientificWorkflowAssociationIssue> issues,
        string code,
        string message,
        ScientificWorkflowAssociationIssueSeverity severity,
        string resultId,
        string pointId,
        string jobId,
        string sourceId)
    {
        ScientificWorkflowAssociationIssue issue =
            new ScientificWorkflowAssociationIssue();
        issue.Code = code;
        issue.Message = message;
        issue.Severity = severity;
        issue.ResultId = resultId ?? string.Empty;
        issue.PointId = pointId ?? string.Empty;
        issue.JobId = jobId ?? string.Empty;
        issue.SourceId = sourceId ?? string.Empty;
        issues.Add(issue);
    }

    private static void SortResults(
        List<ScientificWorkflowResultAssociation> results)
    {
        results.Sort(
            delegate(
                ScientificWorkflowResultAssociation left,
                ScientificWorkflowResultAssociation right)
            {
                return string.Compare(
                    left.ResultId,
                    right.ResultId,
                    StringComparison.OrdinalIgnoreCase);
            });

        for (int index = 0; index < results.Count; index++)
        {
            results[index].Points.Sort(
                delegate(
                    ScientificWorkflowPointAssociation left,
                    ScientificWorkflowPointAssociation right)
                {
                    return string.Compare(
                        left.PointId,
                        right.PointId,
                        StringComparison.OrdinalIgnoreCase);
                });
        }
    }

    private static void SortJobAssociations(
        List<ScientificWorkflowJobAssociation> jobs)
    {
        jobs.Sort(
            delegate(
                ScientificWorkflowJobAssociation left,
                ScientificWorkflowJobAssociation right)
            {
                int kind = left.Kind.CompareTo(right.Kind);

                if (kind != 0)
                {
                    return kind;
                }

                int job = string.Compare(
                    left.JobId,
                    right.JobId,
                    StringComparison.OrdinalIgnoreCase);

                if (job != 0)
                {
                    return job;
                }

                return string.Compare(
                    left.SourceId,
                    right.SourceId,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private static void SortIssues(
        List<ScientificWorkflowAssociationIssue> issues)
    {
        issues.Sort(
            delegate(
                ScientificWorkflowAssociationIssue left,
                ScientificWorkflowAssociationIssue right)
            {
                int severity = right.Severity.CompareTo(left.Severity);

                if (severity != 0)
                {
                    return severity;
                }

                int code = string.Compare(
                    left.Code,
                    right.Code,
                    StringComparison.OrdinalIgnoreCase);

                if (code != 0)
                {
                    return code;
                }

                int result = string.Compare(
                    left.ResultId,
                    right.ResultId,
                    StringComparison.OrdinalIgnoreCase);

                if (result != 0)
                {
                    return result;
                }

                int point = string.Compare(
                    left.PointId,
                    right.PointId,
                    StringComparison.OrdinalIgnoreCase);

                if (point != 0)
                {
                    return point;
                }

                return string.Compare(
                    left.JobId,
                    right.JobId,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private sealed class JobReferenceCandidate
    {
        public string JobId { get; set; } = string.Empty;
        public ScientificWorkflowJobReferenceKind Kind { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string EmptyIssueCode { get; set; } = string.Empty;
        public string NotFoundIssueCode { get; set; } = string.Empty;
    }
}
