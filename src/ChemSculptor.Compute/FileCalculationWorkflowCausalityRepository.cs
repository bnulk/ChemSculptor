using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChemSculptor.Compute;

/// <summary>
/// 基于根工作流账本和作业清单的因果关系图仓储。
/// 只使用持久化证据，不读取计算文件，也不按时间戳猜测因果关系。
/// </summary>
public sealed class FileCalculationWorkflowCausalityRepository
    : ICalculationWorkflowCausalityRepository
{
    /// <summary>因果关系图相对于根工作流运行目录的路径。</summary>
    public const string CausalityGraphFileName =
        CalculationWorkspacePaths.WorkflowCausalityFileName;

    private readonly ICalculationWorkspace _workspace;
    private readonly ICalculationWorkflowInventoryRepository _inventory;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>创建使用默认清单仓储的因果关系图仓储。</summary>
    public FileCalculationWorkflowCausalityRepository(
        ICalculationWorkspace workspace)
        : this(
            workspace,
            new FileCalculationWorkflowInventoryRepository(workspace))
    {
    }

    /// <summary>创建使用指定清单仓储的因果关系图仓储。</summary>
    public FileCalculationWorkflowCausalityRepository(
        ICalculationWorkspace workspace,
        ICalculationWorkflowInventoryRepository inventory)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        _workspace = workspace;
        _inventory = inventory;
        _jsonOptions = new JsonSerializerOptions(
            JsonSerializerDefaults.Web);
        _jsonOptions.WriteIndented = true;
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>按根工作流构建并持久化完整因果关系图。</summary>
    public async Task<CalculationWorkflowCausalityGraph> BuildAsync(
        string rootWorkflowId,
        CancellationToken cancellationToken = default)
    {
        string root = NormalizeRootWorkflowId(rootWorkflowId);
        CalculationWorkflowJobInventory inventory =
            await _inventory.QueryAsync(
                new CalculationWorkflowJobInventoryQuery
                {
                    RootWorkflowId = root,
                    IncludeUnindexedJobs = true
                },
                cancellationToken);
        CalculationWorkflowCausalityGraph graph =
            BuildGraph(root, inventory);
        await SaveAsync(graph, cancellationToken);
        return graph;
    }

    /// <summary>读取最近一次持久化的因果关系图。</summary>
    public async Task<CalculationWorkflowCausalityGraph?> GetAsync(
        string rootWorkflowId,
        CancellationToken cancellationToken = default)
    {
        string root = NormalizeRootWorkflowId(rootWorkflowId);
        string path = GetGraphPath(root);

        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(
            path,
            cancellationToken);
        return JsonSerializer.Deserialize<
            CalculationWorkflowCausalityGraph>(
                json,
                _jsonOptions);
    }

    private async Task SaveAsync(
        CalculationWorkflowCausalityGraph graph,
        CancellationToken cancellationToken)
    {
        string path = GetGraphPath(graph.RootWorkflowId);
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(graph, _jsonOptions);
        string temporaryPath =
            path +
            "." +
            Guid.NewGuid().ToString("N") +
            CalculationWorkspacePaths.JsonTemporaryFileSuffix;

        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                json,
                cancellationToken);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private CalculationWorkflowCausalityGraph BuildGraph(
        string rootWorkflowId,
        CalculationWorkflowJobInventory inventory)
    {
        CalculationWorkflowCausalityGraph graph =
            new CalculationWorkflowCausalityGraph();
        graph.RootWorkflowId = rootWorkflowId;
        graph.CapturedAt = inventory.CapturedAt;
        CopyInventoryIssues(inventory, graph);

        if (inventory.Ledger == null)
        {
            NormalizeGraph(graph);
            return graph;
        }

        Dictionary<string, CalculationWorkflowStageEvidence> stages =
            BuildStageIndex(inventory.Ledger, graph);
        Dictionary<string, CalculationWorkflowJobInventoryEntry> entries =
            BuildEntryIndex(inventory.Jobs);
        List<CalculationWorkflowJobAttemptEvidence> attempts =
            BuildOrderedAttempts(inventory.Ledger, entries, graph);
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attemptsById =
            BuildAttemptIndex(attempts);
        BuildStageNodes(stages, graph);
        BuildJobNodes(
            attempts,
            entries,
            graph);
        BuildStageContainsJobEdges(
            stages,
            attemptsById,
            graph);
        BuildStagePrecedenceEdges(stages, graph);
        BuildJobRelationshipEdges(
            attempts,
            attemptsById,
            entries,
            graph);
        NormalizeGraph(graph);
        return graph;
    }

    private static void ValidateManifestEvidence(
        CalculationWorkflowJobAttemptEvidence attempt,
        CalculationWorkflowJobInventoryEntry entry,
        CalculationWorkflowCausalityGraph graph)
    {
        CalculationJob job = entry.Job!;
        bool mismatch =
            !string.Equals(
                attempt.ParentJobId ?? string.Empty,
                job.ParentJobId ?? string.Empty,
                StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                attempt.CorrectionPlanId ?? string.Empty,
                job.CorrectionPlanId ?? string.Empty,
                StringComparison.OrdinalIgnoreCase)
            || attempt.AttemptNumber != job.AttemptNumber;

        if (mismatch)
        {
            AddIssue(
                graph,
                CalculationWorkflowCausalityIssueCodes.JobEvidenceConflict,
                "账本与作业清单的父作业、恢复序号或修正计划字段不一致。",
                CalculationWorkflowCausalityIssueSeverity.Error,
                attempt.StageId,
                attempt.CalculationJobId,
                JobNodeId(attempt.CalculationJobId));
        }
    }

    private static void BuildStageContainsJobEdges(
        Dictionary<string, CalculationWorkflowStageEvidence> stages,
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attempts,
        CalculationWorkflowCausalityGraph graph)
    {
        Dictionary<string, HashSet<string>> memberships =
            new Dictionary<string, HashSet<string>>(
                StringComparer.OrdinalIgnoreCase);

        foreach (CalculationWorkflowStageEvidence stage in stages.Values)
        {
            for (int index = 0; index < stage.JobAttemptIds.Count; index++)
            {
                string jobId = stage.JobAttemptIds[index];

                if (string.IsNullOrWhiteSpace(jobId))
                {
                    continue;
                }

                if (!memberships.TryGetValue(
                        jobId,
                        out HashSet<string>? stageIds))
                {
                    stageIds = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);
                    memberships[jobId] = stageIds;
                }

                stageIds.Add(stage.StageId);

                CalculationWorkflowJobAttemptEvidence? attempt;

                if (!attempts.TryGetValue(jobId, out attempt))
                {
                    AddIssue(
                        graph,
                        CalculationWorkflowCausalityIssueCodes.StageJobMissing,
                        "阶段引用了因果关系图中不存在的作业尝试。",
                        CalculationWorkflowCausalityIssueSeverity.Error,
                        stage.StageId,
                        jobId,
                        StageNodeId(stage.StageId));
                    continue;
                }

                if (!string.Equals(
                        attempt.StageId,
                        stage.StageId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    AddIssue(
                        graph,
                        CalculationWorkflowCausalityIssueCodes.JobStageConflict,
                        "作业的账本阶段归属与阶段清单引用不一致。",
                        CalculationWorkflowCausalityIssueSeverity.Error,
                        stage.StageId,
                        jobId,
                        JobNodeId(jobId));
                }

                AddEdge(
                    graph,
                    CalculationWorkflowCausalityEdgeKind.StageContainsJob,
                    StageNodeId(stage.StageId),
                    JobNodeId(jobId),
                    CalculationWorkflowCausalityEvidenceKind.StageJobReference,
                    "Stages[].JobAttemptIds",
                    true);
            }
        }

        foreach (CalculationWorkflowJobAttemptEvidence attempt in attempts.Values)
        {
            if (!memberships.TryGetValue(
                    attempt.CalculationJobId,
                    out HashSet<string>? stageIds)
                || stageIds.Count == 0)
            {
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.JobStageMissing,
                    "作业尝试没有出现在任何阶段清单中。",
                    CalculationWorkflowCausalityIssueSeverity.Error,
                    attempt.StageId,
                    attempt.CalculationJobId,
                    JobNodeId(attempt.CalculationJobId));
            }
            else if (stageIds.Count > 1)
            {
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.JobStageConflict,
                    "同一个作业尝试被多个阶段同时引用。",
                    CalculationWorkflowCausalityIssueSeverity.Error,
                    attempt.StageId,
                    attempt.CalculationJobId,
                    JobNodeId(attempt.CalculationJobId));
            }
        }
    }

    private static void BuildStagePrecedenceEdges(
        Dictionary<string, CalculationWorkflowStageEvidence> stages,
        CalculationWorkflowCausalityGraph graph)
    {
        List<CalculationWorkflowStageEvidence> ordered =
            stages.Values.ToList();
        ordered.Sort(
            delegate(
                CalculationWorkflowStageEvidence left,
                CalculationWorkflowStageEvidence right)
            {
                int order = left.Order.CompareTo(right.Order);

                if (order != 0)
                {
                    return order;
                }

                return string.Compare(
                    left.StageId,
                    right.StageId,
                    StringComparison.OrdinalIgnoreCase);
            });

        for (int index = 0; index < ordered.Count - 1; index++)
        {
            CalculationWorkflowStageEvidence current = ordered[index];
            CalculationWorkflowStageEvidence next = ordered[index + 1];

            if (current.Order <= 0
                || next.Order <= 0
                || current.Order >= next.Order)
            {
                continue;
            }

            AddEdge(
                graph,
                CalculationWorkflowCausalityEdgeKind.StagePrecedesStage,
                StageNodeId(current.StageId),
                StageNodeId(next.StageId),
                CalculationWorkflowCausalityEvidenceKind.StageOrder,
                "Stages[].Order",
                false);
        }
    }

    private void BuildJobRelationshipEdges(
        List<CalculationWorkflowJobAttemptEvidence> attempts,
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attemptsById,
        Dictionary<string, CalculationWorkflowJobInventoryEntry> entries,
        CalculationWorkflowCausalityGraph graph)
    {
        Dictionary<string, List<string>> dependencies =
            new Dictionary<string, List<string>>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < attempts.Count; index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt = attempts[index];
            string parentId = attempt.ParentJobId ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(parentId)
                && string.Equals(
                    parentId,
                    attempt.CalculationJobId,
                    StringComparison.OrdinalIgnoreCase))
            {
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.SelfReference,
                    "作业引用了自身作为父作业。",
                    CalculationWorkflowCausalityIssueSeverity.Error,
                    attempt.StageId,
                    attempt.CalculationJobId,
                    JobNodeId(attempt.CalculationJobId));
                continue;
            }

            if (string.IsNullOrWhiteSpace(parentId))
            {
                if (!string.IsNullOrWhiteSpace(
                        attempt.CorrectionPlanId))
                {
                    AddIssue(
                        graph,
                        CalculationWorkflowCausalityIssueCodes.ParentJobMissing,
                        "作业记录了修正计划，但没有父作业标识。",
                        CalculationWorkflowCausalityIssueSeverity.Error,
                        attempt.StageId,
                        attempt.CalculationJobId,
                        JobNodeId(attempt.CalculationJobId));
                }

                continue;
            }

            if (!attemptsById.ContainsKey(parentId))
            {
                CalculationWorkflowJobInventoryEntry? external;
                entries.TryGetValue(parentId, out external);

                string externalRoot = external?.Job?.RootWorkflowId
                    ?? TryReadManifestRootWorkflowId(parentId)
                    ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(externalRoot)
                    && !string.Equals(
                        externalRoot,
                        graph.RootWorkflowId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    AddIssue(
                        graph,
                        CalculationWorkflowCausalityIssueCodes.ParentJobRootMismatch,
                        "父作业属于另一个根工作流。",
                        CalculationWorkflowCausalityIssueSeverity.Error,
                        attempt.StageId,
                        attempt.CalculationJobId,
                        JobNodeId(attempt.CalculationJobId));
                }
                else
                {
                    AddIssue(
                        graph,
                        CalculationWorkflowCausalityIssueCodes.ParentJobMissing,
                        "父作业不存在于当前根工作流的完整作业清单中。",
                        CalculationWorkflowCausalityIssueSeverity.Error,
                        attempt.StageId,
                        attempt.CalculationJobId,
                        JobNodeId(attempt.CalculationJobId));
                }

                continue;
            }

            bool isRecovery =
                !string.IsNullOrWhiteSpace(attempt.CorrectionPlanId)
                || attempt.AttemptNumber > 0;
            CalculationWorkflowCausalityEdgeKind kind = isRecovery
                ? CalculationWorkflowCausalityEdgeKind.JobRecoveryOfJob
                : CalculationWorkflowCausalityEdgeKind.JobDerivedFromJob;
            CalculationWorkflowCausalityEvidenceKind evidenceKind;
            string sourceField;

            if (!string.IsNullOrWhiteSpace(attempt.CorrectionPlanId))
            {
                evidenceKind =
                    CalculationWorkflowCausalityEvidenceKind.CorrectionPlanId;
                sourceField = "JobAttempts[].CorrectionPlanId";
            }
            else if (attempt.AttemptNumber > 0)
            {
                evidenceKind =
                    CalculationWorkflowCausalityEvidenceKind.AttemptNumber;
                sourceField = "JobAttempts[].AttemptNumber";
            }
            else
            {
                evidenceKind =
                    CalculationWorkflowCausalityEvidenceKind.ParentJobId;
                sourceField = "JobAttempts[].ParentJobId";
            }

            AddEdge(
                graph,
                kind,
                JobNodeId(attempt.CalculationJobId),
                JobNodeId(parentId),
                evidenceKind,
                sourceField,
                true);

            if (!dependencies.TryGetValue(
                    attempt.CalculationJobId,
                    out List<string>? parents))
            {
                parents = new List<string>();
                dependencies[attempt.CalculationJobId] = parents;
            }

            parents.Add(parentId);
        }

        DetectDependencyCycles(dependencies, graph);
    }

    private string? TryReadManifestRootWorkflowId(string jobId)
    {
        try
        {
            string path = _workspace.GetJobManifestPath(jobId);

            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            CalculationJob? job = JsonSerializer.Deserialize<CalculationJob>(
                json,
                _jsonOptions);
            return job?.RootWorkflowId;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static void DetectDependencyCycles(
        Dictionary<string, List<string>> dependencies,
        CalculationWorkflowCausalityGraph graph)
    {
        Dictionary<string, int> visitState =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> reported = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string nodeId in dependencies.Keys)
        {
            VisitDependency(
                nodeId,
                dependencies,
                visitState,
                reported,
                graph);
        }
    }

    private static void VisitDependency(
        string nodeId,
        Dictionary<string, List<string>> dependencies,
        Dictionary<string, int> visitState,
        HashSet<string> reported,
        CalculationWorkflowCausalityGraph graph)
    {
        if (reported.Contains(nodeId))
        {
            return;
        }

        int state;

        if (visitState.TryGetValue(nodeId, out state))
        {
            if (state == 1)
            {
                reported.Add(nodeId);
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.CausalCycle,
                    "作业派生或恢复关系形成循环。",
                    CalculationWorkflowCausalityIssueSeverity.Error,
                    string.Empty,
                    nodeId.StartsWith(
                        "job:",
                        StringComparison.OrdinalIgnoreCase)
                        ? nodeId.Substring(4)
                        : nodeId,
                    JobNodeId(nodeId.StartsWith(
                        "job:",
                        StringComparison.OrdinalIgnoreCase)
                        ? nodeId.Substring(4)
                        : nodeId));
            }

            return;
        }

        visitState[nodeId] = 1;
        List<string>? parents;

        if (dependencies.TryGetValue(nodeId, out parents))
        {
            for (int index = 0; index < parents.Count; index++)
            {
                VisitDependency(
                    parents[index],
                    dependencies,
                    visitState,
                    reported,
                    graph);
            }
        }

        visitState[nodeId] = 2;
    }

    private static Dictionary<string, CalculationWorkflowStageEvidence>
        BuildStageIndex(
            CalculationWorkflowEvidenceLedger ledger,
            CalculationWorkflowCausalityGraph graph)
    {
        Dictionary<string, CalculationWorkflowStageEvidence> result =
            new Dictionary<string, CalculationWorkflowStageEvidence>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < ledger.Stages.Count; index++)
        {
            CalculationWorkflowStageEvidence stage = ledger.Stages[index];

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                continue;
            }

            if (!result.TryAdd(stage.StageId, stage))
            {
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.DuplicateNode,
                    "账本中同一个阶段重复出现。",
                    CalculationWorkflowCausalityIssueSeverity.Error,
                    stage.StageId,
                    string.Empty,
                    StageNodeId(stage.StageId));
            }
        }

        return result;
    }

    private static Dictionary<string, CalculationWorkflowJobInventoryEntry>
        BuildEntryIndex(
            List<CalculationWorkflowJobInventoryEntry> jobs)
    {
        Dictionary<string, CalculationWorkflowJobInventoryEntry> result =
            new Dictionary<string, CalculationWorkflowJobInventoryEntry>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < jobs.Count; index++)
        {
            CalculationWorkflowJobInventoryEntry entry = jobs[index];

            if (!string.IsNullOrWhiteSpace(entry.JobId))
            {
                result.TryAdd(entry.JobId, entry);
            }
        }

        return result;
    }

    private static List<CalculationWorkflowJobAttemptEvidence>
        BuildOrderedAttempts(
            CalculationWorkflowEvidenceLedger ledger,
            Dictionary<string, CalculationWorkflowJobInventoryEntry> entries,
            CalculationWorkflowCausalityGraph graph)
    {
        List<CalculationWorkflowJobAttemptEvidence> attempts =
            new List<CalculationWorkflowJobAttemptEvidence>();
        HashSet<string> seen = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        AddAttempts(
            attempts,
            seen,
            ledger.JobAttempts,
            graph);

        foreach (KeyValuePair<
            string,
            CalculationWorkflowJobInventoryEntry> pair in entries)
        {
            if (seen.Contains(pair.Key))
            {
                continue;
            }

            CalculationWorkflowJobInventoryEntry entry = pair.Value;
            CalculationWorkflowJobAttemptEvidence attempt =
                new CalculationWorkflowJobAttemptEvidence();
            attempt.CalculationJobId = entry.JobId;
            attempt.StageId = entry.StageId;
            attempt.ParentJobId = entry.ParentJobId;
            attempt.AttemptNumber = entry.AttemptNumber;
            attempt.State = entry.State;
            attempt.TaskType = entry.TaskType;
            attempt.Program = entry.Program;
            attempt.CorrectionPlanId = entry.Job?.CorrectionPlanId
                ?? string.Empty;
            attempt.CreatedAt = entry.CreatedAt;
            attempt.StartedAt = entry.Job?.StartedAt;
            attempt.CompletedAt = entry.Job?.CompletedAt;
            attempts.Add(attempt);
            seen.Add(attempt.CalculationJobId);

            AddIssue(
                graph,
                CalculationWorkflowCausalityIssueCodes.JobUnindexed,
                "作业不在账本中，已根据可归属的作业清单补入审计图。",
                CalculationWorkflowCausalityIssueSeverity.Warning,
                attempt.StageId,
                attempt.CalculationJobId,
                JobNodeId(attempt.CalculationJobId));
        }

        attempts.Sort(
            delegate(
                CalculationWorkflowJobAttemptEvidence left,
                CalculationWorkflowJobAttemptEvidence right)
            {
                int stage = string.Compare(
                    left.StageId,
                    right.StageId,
                    StringComparison.OrdinalIgnoreCase);

                if (stage != 0)
                {
                    return stage;
                }

                int created = left.CreatedAt.CompareTo(right.CreatedAt);

                if (created != 0)
                {
                    return created;
                }

                return string.Compare(
                    left.CalculationJobId,
                    right.CalculationJobId,
                    StringComparison.OrdinalIgnoreCase);
            });
        return attempts;
    }

    private static void AddAttempts(
        List<CalculationWorkflowJobAttemptEvidence> target,
        HashSet<string> seen,
        List<CalculationWorkflowJobAttemptEvidence> source,
        CalculationWorkflowCausalityGraph graph)
    {
        for (int index = 0; index < source.Count; index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt = source[index];

            if (string.IsNullOrWhiteSpace(attempt.CalculationJobId))
            {
                continue;
            }

            if (!seen.Add(attempt.CalculationJobId))
            {
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.DuplicateNode,
                    "账本中同一个作业尝试重复出现，只保留第一次记录。",
                    CalculationWorkflowCausalityIssueSeverity.Error,
                    attempt.StageId,
                    attempt.CalculationJobId,
                    JobNodeId(attempt.CalculationJobId));
                continue;
            }

            target.Add(attempt);
        }
    }

    private static Dictionary<string, CalculationWorkflowJobAttemptEvidence>
        BuildAttemptIndex(
            List<CalculationWorkflowJobAttemptEvidence> attempts)
    {
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> result =
            new Dictionary<string, CalculationWorkflowJobAttemptEvidence>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < attempts.Count; index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt = attempts[index];

            if (!string.IsNullOrWhiteSpace(attempt.CalculationJobId))
            {
                result[attempt.CalculationJobId] = attempt;
            }
        }

        return result;
    }

    private static void BuildStageNodes(
        Dictionary<string, CalculationWorkflowStageEvidence> stages,
        CalculationWorkflowCausalityGraph graph)
    {
        foreach (CalculationWorkflowStageEvidence stage in stages.Values)
        {
            CalculationWorkflowCausalityNode node =
                new CalculationWorkflowCausalityNode();
            node.NodeId = StageNodeId(stage.StageId);
            node.NodeKind = CalculationWorkflowCausalityNodeKind.Stage;
            node.StageId = stage.StageId;
            node.Status = stage.Status.ToString();
            node.StageOrder = stage.Order;
            node.Evidence.Add("ledger.stages");
            graph.Nodes.Add(node);
        }
    }

    private static void BuildJobNodes(
        List<CalculationWorkflowJobAttemptEvidence> attempts,
        Dictionary<string, CalculationWorkflowJobInventoryEntry> entries,
        CalculationWorkflowCausalityGraph graph)
    {
        for (int index = 0; index < attempts.Count; index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt = attempts[index];
            CalculationWorkflowJobInventoryEntry? entry;
            entries.TryGetValue(attempt.CalculationJobId, out entry);
            CalculationWorkflowCausalityNode node =
                new CalculationWorkflowCausalityNode();
            node.NodeId = JobNodeId(attempt.CalculationJobId);
            node.NodeKind =
                CalculationWorkflowCausalityNodeKind.JobAttempt;
            node.JobId = attempt.CalculationJobId;
            node.Status = attempt.State.ToString();
            node.TaskType = attempt.TaskType.ToString();
            node.Program = attempt.Program;
            node.ParentJobId = attempt.ParentJobId;
            node.CorrectionPlanId = attempt.CorrectionPlanId;
            node.HasJobManifest = entry != null
                && entry.ManifestStatus
                    == CalculationWorkflowJobManifestStatus.Present
                && entry.Job != null;
            node.Evidence.Add("ledger.jobAttempts");

            if (node.HasJobManifest)
            {
                node.Evidence.Add("job.manifest");
                ValidateManifestEvidence(
                    attempt,
                    entry!,
                    graph);
            }
            else
            {
                AddIssue(
                    graph,
                    CalculationWorkflowCausalityIssueCodes.JobManifestUnavailable,
                    "作业清单不可用，因果关系只能使用账本字段。",
                    CalculationWorkflowCausalityIssueSeverity.Warning,
                    attempt.StageId,
                    attempt.CalculationJobId,
                    node.NodeId);
            }

            graph.Nodes.Add(node);
        }
    }

    private static void CopyInventoryIssues(
        CalculationWorkflowJobInventory inventory,
        CalculationWorkflowCausalityGraph graph)
    {
        for (int index = 0; index < inventory.Issues.Count; index++)
        {
            CalculationWorkflowInventoryIssue issue = inventory.Issues[index];
            AddIssue(
                graph,
                issue.Code,
                issue.Message,
                ToCausalitySeverity(issue.Severity),
                issue.StageId,
                issue.JobId,
                string.Empty);
        }
    }

    private static CalculationWorkflowCausalityIssueSeverity
        ToCausalitySeverity(
            CalculationWorkflowInventoryIssueSeverity severity)
    {
        switch (severity)
        {
            case CalculationWorkflowInventoryIssueSeverity.Info:
                return CalculationWorkflowCausalityIssueSeverity.Info;
            case CalculationWorkflowInventoryIssueSeverity.Warning:
                return CalculationWorkflowCausalityIssueSeverity.Warning;
            default:
                return CalculationWorkflowCausalityIssueSeverity.Error;
        }
    }

    private static void AddEdge(
        CalculationWorkflowCausalityGraph graph,
        CalculationWorkflowCausalityEdgeKind kind,
        string fromNodeId,
        string toNodeId,
        CalculationWorkflowCausalityEvidenceKind evidenceKind,
        string sourceField,
        bool isRequired)
    {
        string edgeId =
            kind +
            "|" +
            fromNodeId +
            "|" +
            toNodeId;

        if (graph.Edges.Any(edge => string.Equals(
                edge.EdgeId,
                edgeId,
                StringComparison.OrdinalIgnoreCase)))
        {
            AddIssue(
                graph,
                CalculationWorkflowCausalityIssueCodes.DuplicateEdge,
                "同一有向关系被重复声明，已保留第一条证据。",
                CalculationWorkflowCausalityIssueSeverity.Warning,
                string.Empty,
                string.Empty,
                fromNodeId);
            return;
        }

        CalculationWorkflowCausalityEdge relation =
            new CalculationWorkflowCausalityEdge();
        relation.EdgeId = edgeId;
        relation.EdgeKind = kind;
        relation.FromNodeId = fromNodeId;
        relation.ToNodeId = toNodeId;
        relation.EvidenceKind = evidenceKind;
        relation.SourceField = sourceField;
        relation.IsRequired = isRequired;
        graph.Edges.Add(relation);
    }

    private static void NormalizeGraph(
        CalculationWorkflowCausalityGraph graph)
    {
        graph.Nodes.Sort(
            delegate(
                CalculationWorkflowCausalityNode left,
                CalculationWorkflowCausalityNode right)
            {
                int kind = left.NodeKind.CompareTo(right.NodeKind);

                if (kind != 0)
                {
                    return kind;
                }

                int order = left.StageOrder.CompareTo(right.StageOrder);

                if (order != 0)
                {
                    return order;
                }

                return string.Compare(
                    left.NodeId,
                    right.NodeId,
                    StringComparison.OrdinalIgnoreCase);
            });
        graph.Edges.Sort(
            delegate(
                CalculationWorkflowCausalityEdge left,
                CalculationWorkflowCausalityEdge right)
            {
                int kind = left.EdgeKind.CompareTo(right.EdgeKind);

                if (kind != 0)
                {
                    return kind;
                }

                return string.Compare(
                    left.EdgeId,
                    right.EdgeId,
                    StringComparison.OrdinalIgnoreCase);
            });
        graph.Issues.Sort(
            delegate(
                CalculationWorkflowCausalityIssue left,
                CalculationWorkflowCausalityIssue right)
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

                int job = string.Compare(
                    left.JobId,
                    right.JobId,
                    StringComparison.OrdinalIgnoreCase);

                if (job != 0)
                {
                    return job;
                }

                return string.Compare(
                    left.NodeId,
                    right.NodeId,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private static void AddIssue(
        CalculationWorkflowCausalityGraph graph,
        string code,
        string message,
        CalculationWorkflowCausalityIssueSeverity severity,
        string stageId,
        string jobId,
        string nodeId)
    {
        CalculationWorkflowCausalityIssue issue =
            new CalculationWorkflowCausalityIssue();
        issue.Code = code;
        issue.Message = message;
        issue.Severity = severity;
        issue.StageId = stageId;
        issue.JobId = jobId;
        issue.NodeId = nodeId;
        graph.Issues.Add(issue);
    }

    private string GetGraphPath(string rootWorkflowId)
    {
        return Path.Combine(
            _workspace.GetWorkflowRunDirectory(rootWorkflowId),
            CausalityGraphFileName);
    }

    private static string NormalizeRootWorkflowId(
        string rootWorkflowId)
    {
        if (string.IsNullOrWhiteSpace(rootWorkflowId))
        {
            throw new ArgumentException(
                "根工作流标识不能为空。",
                nameof(rootWorkflowId));
        }

        return rootWorkflowId.Trim();
    }

    private static string StageNodeId(string stageId)
    {
        return "stage:" + stageId.Trim();
    }

    private static string JobNodeId(string jobId)
    {
        return "job:" + jobId.Trim();
    }
}
