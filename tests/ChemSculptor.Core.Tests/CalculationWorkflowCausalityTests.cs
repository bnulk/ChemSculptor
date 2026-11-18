using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>根工作流阶段与作业因果关系图测试。</summary>
public class CalculationWorkflowCausalityTests
{
    /// <summary>验证完整流程生成阶段、作业、包含、顺序、派生和恢复关系。</summary>
    [Fact]
    public async Task BuildsCompleteStageAndJobCausalityGraph()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob optimization = CreateJob(
                "job-cause-opt",
                "workflow-cause-all",
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.Optimization);
            CalculationJob recovery = CreateJob(
                "job-cause-recovery",
                optimization.RootWorkflowId,
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.OptimizationRecovery);
            recovery.ParentJobId = optimization.JobId;
            recovery.AttemptNumber = 1;
            recovery.CorrectionPlanId = "plan-cause-recovery";
            CalculationJob tddft = CreateJob(
                "job-cause-tddft",
                optimization.RootWorkflowId,
                CalculationTaskType.TdDft,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Tddft);
            CalculationJob nto = CreateJob(
                "job-cause-nto",
                optimization.RootWorkflowId,
                CalculationTaskType.Nto,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Nto);
            nto.ParentJobId = tddft.JobId;
            CalculationJob canceled = CreateJob(
                "job-cause-canceled",
                optimization.RootWorkflowId,
                CalculationTaskType.Frequency,
                CalculationJobState.Canceled,
                CalculationWorkflowStageIds.Frequency);

            await SaveJobAsync(workspace, calculations, optimization);
            await SaveJobAsync(workspace, calculations, recovery);
            await SaveJobAsync(workspace, calculations, tddft);
            await SaveJobAsync(workspace, calculations, nto);
            await SaveJobAsync(workspace, calculations, canceled);

            FileCalculationWorkflowCausalityRepository repository =
                new FileCalculationWorkflowCausalityRepository(workspace);
            CalculationWorkflowCausalityGraph graph =
                await repository.BuildAsync(optimization.RootWorkflowId);

            Assert.Equal(10, graph.Nodes.Count);
            Assert.Equal(
                5,
                graph.Nodes.Count(node => node.NodeKind
                    == CalculationWorkflowCausalityNodeKind.Stage));
            Assert.Equal(
                5,
                graph.Nodes.Count(node => node.NodeKind
                    == CalculationWorkflowCausalityNodeKind.JobAttempt));
            Assert.Contains(
                graph.Nodes,
                node => node.JobId == canceled.JobId
                    && node.Status == "Canceled");
            Assert.Equal(
                5,
                graph.Edges.Count(edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.StageContainsJob));
            Assert.Equal(
                4,
                graph.Edges.Count(edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.StagePrecedesStage));
            Assert.All(
                graph.Edges.Where(edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.StagePrecedesStage),
                edge => Assert.False(edge.IsRequired));
            Assert.All(
                graph.Edges.Where(edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.StageContainsJob),
                edge => Assert.True(edge.IsRequired));

            CalculationWorkflowCausalityEdge recoveryEdge =
                Assert.Single(
                    graph.Edges,
                    edge => edge.EdgeKind
                        == CalculationWorkflowCausalityEdgeKind.JobRecoveryOfJob);
            Assert.Equal(
                CalculationWorkflowCausalityEvidenceKind.CorrectionPlanId,
                recoveryEdge.EvidenceKind);
            Assert.True(recoveryEdge.IsRequired);
            Assert.Equal(
                "job:" + recovery.JobId,
                recoveryEdge.FromNodeId);
            Assert.Equal(
                "job:" + optimization.JobId,
                recoveryEdge.ToNodeId);

            CalculationWorkflowCausalityEdge derivedEdge =
                Assert.Single(
                    graph.Edges,
                    edge => edge.EdgeKind
                        == CalculationWorkflowCausalityEdgeKind.JobDerivedFromJob);
            Assert.Equal(
                CalculationWorkflowCausalityEvidenceKind.ParentJobId,
                derivedEdge.EvidenceKind);
            Assert.DoesNotContain(
                graph.Issues,
                issue => issue.Severity
                    == CalculationWorkflowCausalityIssueSeverity.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证缺失父作业显式报错且不伪造关系边。</summary>
    [Fact]
    public async Task ReportsMissingParentJob()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob child = CreateJob(
                "job-cause-orphan",
                "workflow-cause-orphan",
                CalculationTaskType.Nto,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Nto);
            child.ParentJobId = "job-cause-parent-missing";
            await SaveJobAsync(workspace, calculations, child);

            CalculationWorkflowCausalityGraph graph =
                await new FileCalculationWorkflowCausalityRepository(workspace)
                    .BuildAsync(child.RootWorkflowId);

            Assert.Contains(
                graph.Issues,
                issue => issue.Code
                        == CalculationWorkflowCausalityIssueCodes.ParentJobMissing
                    && issue.JobId == child.JobId);
            Assert.DoesNotContain(
                graph.Edges,
                edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.JobDerivedFromJob);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证父作业位于另一个根工作流时显式报错。</summary>
    [Fact]
    public async Task ReportsParentFromAnotherRootWorkflow()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob parent = CreateJob(
                "job-cause-external-parent",
                "workflow-cause-root-a",
                CalculationTaskType.TdDft,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Tddft);
            CalculationJob child = CreateJob(
                "job-cause-cross-root-child",
                "workflow-cause-root-b",
                CalculationTaskType.Nto,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Nto);
            child.ParentJobId = parent.JobId;
            await SaveJobAsync(workspace, calculations, parent);
            await SaveJobAsync(workspace, calculations, child);

            CalculationWorkflowCausalityGraph graph =
                await new FileCalculationWorkflowCausalityRepository(workspace)
                    .BuildAsync(child.RootWorkflowId);

            Assert.Contains(
                graph.Issues,
                issue => issue.Code
                        == CalculationWorkflowCausalityIssueCodes.ParentJobRootMismatch
                    && issue.JobId == child.JobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证自引用被拒绝。</summary>
    [Fact]
    public async Task ReportsSelfReference()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-cause-self",
                "workflow-cause-self",
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.Optimization);
            job.ParentJobId = job.JobId;
            await SaveJobAsync(workspace, calculations, job);

            CalculationWorkflowCausalityGraph graph =
                await new FileCalculationWorkflowCausalityRepository(workspace)
                    .BuildAsync(job.RootWorkflowId);

            Assert.Contains(
                graph.Issues,
                issue => issue.Code
                    == CalculationWorkflowCausalityIssueCodes.SelfReference);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证互相引用形成循环时显式报错。</summary>
    [Fact]
    public async Task ReportsCausalCycle()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob first = CreateJob(
                "job-cause-cycle-a",
                "workflow-cause-cycle",
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.Optimization);
            CalculationJob second = CreateJob(
                "job-cause-cycle-b",
                first.RootWorkflowId,
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.OptimizationRecovery);
            first.ParentJobId = second.JobId;
            second.ParentJobId = first.JobId;
            await SaveJobAsync(workspace, calculations, first);
            await SaveJobAsync(workspace, calculations, second);

            CalculationWorkflowCausalityGraph graph =
                await new FileCalculationWorkflowCausalityRepository(workspace)
                    .BuildAsync(first.RootWorkflowId);

            Assert.Contains(
                graph.Issues,
                issue => issue.Code
                    == CalculationWorkflowCausalityIssueCodes.CausalCycle);
            Assert.Equal(
                2,
                graph.Edges.Count(edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.JobDerivedFromJob
                    || edge.EdgeKind
                        == CalculationWorkflowCausalityEdgeKind.JobRecoveryOfJob));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证阶段引用了不存在的作业尝试时显式报错。</summary>
    [Fact]
    public async Task ReportsStageReferenceToMissingJob()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository ledgerRepository =
                new FileCalculationWorkflowEvidenceLedgerRepository(workspace);
            CalculationWorkflowEvidenceLedger ledger =
                new CalculationWorkflowEvidenceLedger();
            ledger.RootWorkflowId = "workflow-cause-stage-reference";
            CalculationWorkflowStageEvidence stage =
                CreateStage(CalculationWorkflowStageIds.Optimization);
            stage.JobAttemptIds.Add("job-cause-not-in-ledger");
            ledger.Stages.Add(stage);
            await ledgerRepository.SaveAsync(ledger);

            CalculationWorkflowCausalityGraph graph =
                await new FileCalculationWorkflowCausalityRepository(workspace)
                    .BuildAsync(ledger.RootWorkflowId);

            Assert.Contains(
                graph.Issues,
                issue => issue.Code
                    == CalculationWorkflowCausalityIssueCodes.StageJobMissing);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证重复阶段作业引用不产生重复边。</summary>
    [Fact]
    public async Task DuplicateStageReferenceDoesNotDuplicateEdge()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            FileCalculationWorkflowEvidenceLedgerRepository ledgerRepository =
                new FileCalculationWorkflowEvidenceLedgerRepository(workspace);
            CalculationJob job = CreateJob(
                "job-cause-duplicate-edge",
                "workflow-cause-duplicate-edge",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Optimization);
            await SaveJobAsync(workspace, calculations, job);
            CalculationWorkflowEvidenceLedger ledger =
                (await ledgerRepository.GetAsync(job.RootWorkflowId))!;
            ledger.Stages[0].JobAttemptIds.Add(job.JobId);
            await ledgerRepository.SaveAsync(ledger);

            CalculationWorkflowCausalityGraph graph =
                await new FileCalculationWorkflowCausalityRepository(workspace)
                    .BuildAsync(job.RootWorkflowId);

            Assert.Single(
                graph.Edges,
                edge => edge.EdgeKind
                    == CalculationWorkflowCausalityEdgeKind.StageContainsJob);
            Assert.Contains(
                graph.Issues,
                issue => issue.Code
                    == CalculationWorkflowCausalityIssueCodes.DuplicateEdge);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证图写出后可由新仓储实例读取。</summary>
    [Fact]
    public async Task PersistsAndReloadsCausalityGraph()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-cause-persist",
                "workflow-cause-persist",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Optimization);
            await SaveJobAsync(workspace, calculations, job);
            FileCalculationWorkflowCausalityRepository first =
                new FileCalculationWorkflowCausalityRepository(workspace);
            CalculationWorkflowCausalityGraph built =
                await first.BuildAsync(job.RootWorkflowId);

            WorkspaceManager restarted = CreateWorkspace(root);
            FileCalculationWorkflowCausalityRepository second =
                new FileCalculationWorkflowCausalityRepository(restarted);
            CalculationWorkflowCausalityGraph? reloaded =
                await second.GetAsync(job.RootWorkflowId);

            Assert.NotNull(reloaded);
            Assert.Equal(built.RootWorkflowId, reloaded!.RootWorkflowId);
            Assert.Equal(built.CapturedAt, reloaded.CapturedAt);
            Assert.Equal(built.Nodes.Count, reloaded.Nodes.Count);
            Assert.Equal(built.Edges.Count, reloaded.Edges.Count);
            Assert.True(File.Exists(Path.Combine(
                restarted.GetWorkflowRunDirectory(job.RootWorkflowId),
                FileCalculationWorkflowCausalityRepository
                    .CausalityGraphFileName)));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证相同输入生成稳定节点、边和标识顺序。</summary>
    [Fact]
    public async Task BuildsDeterministicGraph()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob first = CreateJob(
                "job-cause-stable-b",
                "workflow-cause-stable",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Optimization);
            CalculationJob second = CreateJob(
                "job-cause-stable-a",
                first.RootWorkflowId,
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.OptimizationRecovery);
            second.ParentJobId = first.JobId;
            await SaveJobAsync(workspace, calculations, first);
            await SaveJobAsync(workspace, calculations, second);
            FileCalculationWorkflowCausalityRepository repository =
                new FileCalculationWorkflowCausalityRepository(workspace);

            CalculationWorkflowCausalityGraph firstGraph =
                await repository.BuildAsync(first.RootWorkflowId);
            CalculationWorkflowCausalityGraph secondGraph =
                await repository.BuildAsync(first.RootWorkflowId);

            Assert.Equal(
                firstGraph.Nodes.Select(node => node.NodeId),
                secondGraph.Nodes.Select(node => node.NodeId));
            Assert.Equal(
                firstGraph.Edges.Select(edge => edge.EdgeId),
                secondGraph.Edges.Select(edge => edge.EdgeId));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证图模型使用 camelCase 和字符串枚举 JSON 往返。</summary>
    [Fact]
    public void GraphRoundTripsJson()
    {
        CalculationWorkflowCausalityGraph graph =
            new CalculationWorkflowCausalityGraph();
        graph.RootWorkflowId = "workflow-json";
        CalculationWorkflowCausalityNode node =
            new CalculationWorkflowCausalityNode();
        node.NodeId = "stage:optimization";
        node.NodeKind = CalculationWorkflowCausalityNodeKind.Stage;
        node.StageId = CalculationWorkflowStageIds.Optimization;
        node.Status = "Completed";
        node.StageOrder = 10;
        graph.Nodes.Add(node);
        JsonSerializerOptions options = CreateJsonOptions();

        string json = JsonSerializer.Serialize(graph, options);
        CalculationWorkflowCausalityGraph? restored =
            JsonSerializer.Deserialize<
                CalculationWorkflowCausalityGraph>(json, options);

        Assert.Contains("\"rootWorkflowId\"", json);
        Assert.Contains("\"Stage\"", json);
        Assert.NotNull(restored);
        Assert.Equal(
            CalculationWorkflowCausalityNodeKind.Stage,
            restored!.Nodes[0].NodeKind);
    }

    /// <summary>验证根工作流标识不能为空。</summary>
    [Fact]
    public async Task EmptyRootWorkflowIdIsRejected()
    {
        WorkspaceManager workspace = CreateWorkspace(
            Path.Combine(Path.GetTempPath(), "ChemSculptorTests"));
        FileCalculationWorkflowCausalityRepository repository =
            new FileCalculationWorkflowCausalityRepository(workspace);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.BuildAsync(" "));
        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetAsync(string.Empty));
    }

    /// <summary>验证构建查询不使用展示筛选条件。</summary>
    [Fact]
    public async Task UsesUnfilteredInventoryQuery()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            RecordingInventoryRepository inventory =
                new RecordingInventoryRepository();
            FileCalculationWorkflowCausalityRepository repository =
                new FileCalculationWorkflowCausalityRepository(
                    workspace,
                    inventory);

            await repository.BuildAsync("workflow-cause-query");

            Assert.NotNull(inventory.Query);
            Assert.Empty(inventory.Query!.StageIds);
            Assert.Empty(inventory.Query.States);
            Assert.Empty(inventory.Query.TaskTypes);
            Assert.Empty(inventory.Query.Programs);
            Assert.True(inventory.Query.IncludeUnindexedJobs);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static async Task SaveJobAsync(
        WorkspaceManager workspace,
        FileCalculationRepository repository,
        CalculationJob job)
    {
        await workspace.EnsureJobWorkspaceAsync(job.JobId);
        job.RunDirectory = workspace.GetRunDirectory(job.JobId);
        await repository.SaveJobAsync(job);
    }

    private static CalculationJob CreateJob(
        string jobId,
        string rootWorkflowId,
        CalculationTaskType taskType,
        CalculationJobState state,
        string stageId)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = jobId;
        job.WorkflowId = "workflow-" + jobId;
        job.RootWorkflowId = rootWorkflowId;
        job.SessionId = "session-causality";
        job.Goal = "因果关系图测试";
        job.Spec = CalculationDefaults.CreateDefaultSinglePoint();
        job.Spec.TaskType = taskType;
        job.Spec.Program = "Gaussian";
        job.State = state;
        job.EvidenceStageId = stageId;
        job.CreatedAt = DateTimeOffset.UtcNow;
        return job;
    }

    private static CalculationWorkflowStageEvidence CreateStage(
        string stageId)
    {
        CalculationWorkflowStageEvidence stage =
            new CalculationWorkflowStageEvidence();
        stage.StageId = stageId;
        stage.StageType = stageId;
        stage.Order =
            CalculationWorkflowStageIds.ResolveDefaultOrder(stageId);
        return stage;
    }

    private static WorkspaceManager CreateWorkspace(string root)
    {
        CalculationWorkspaceOptions options =
            new CalculationWorkspaceOptions();
        options.RootDirectory = root;
        return new WorkspaceManager(options);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class RecordingInventoryRepository
        : ICalculationWorkflowInventoryRepository
    {
        public CalculationWorkflowJobInventoryQuery? Query { get; private set; }

        public Task<CalculationWorkflowJobInventory> QueryAsync(
            CalculationWorkflowJobInventoryQuery query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            CalculationWorkflowJobInventory inventory =
                new CalculationWorkflowJobInventory();
            inventory.RootWorkflowId = query.RootWorkflowId;
            return Task.FromResult(inventory);
        }
    }
}
