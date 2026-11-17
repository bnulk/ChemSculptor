using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>根工作流持久化证据账本测试。</summary>
public class CalculationWorkflowEvidenceLedgerTests
{
    /// <summary>验证单个作业写入账本并保留文件和阶段索引。</summary>
    [Fact]
    public async Task AttachesSingleJobToLedger()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationJob job = CreateJob(
                "job-ledger-single",
                "workflow-ledger-single",
                CalculationTaskType.SinglePoint,
                CalculationJobState.Running);
            job.StartedAt = DateTimeOffset.UtcNow;

            await repository.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.SinglePoint,
                job);

            CalculationWorkflowEvidenceLedger? ledger =
                await repository.GetAsync(
                    job.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.True(File.Exists(
                workspace.GetWorkflowRunLedgerPath(
                    job.RootWorkflowId)));
            Assert.Single(ledger!.Stages);
            Assert.Single(ledger.JobAttempts);
            Assert.Equal(
                CalculationWorkflowStageStatus.Running,
                ledger.Stages[0].Status);
            Assert.Contains(
                job.JobId,
                ledger.Stages[0].JobAttemptIds);
            Assert.Equal(
                job.SessionId,
                ledger.SessionId);
            Assert.Equal(
                job.Goal,
                ledger.Goal);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证同一作业重复保存只更新，不产生重复尝试。</summary>
    [Fact]
    public async Task SavingSameJobTwiceUpdatesAttempt()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationJob job = CreateJob(
                "job-ledger-update",
                "workflow-ledger-update",
                CalculationTaskType.Optimization,
                CalculationJobState.Running);
            job.StartedAt = DateTimeOffset.UtcNow;

            await repository.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.Optimization,
                job);

            job.State = CalculationJobState.Validated;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await repository.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.Optimization,
                job);

            CalculationWorkflowEvidenceLedger? ledger =
                await repository.GetAsync(
                    job.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Single(ledger!.JobAttempts);
            Assert.Single(ledger.Stages);
            Assert.Single(ledger.Stages[0].JobAttemptIds);
            Assert.Equal(
                CalculationJobState.Validated,
                ledger.JobAttempts[0].State);
            Assert.Equal(
                CalculationWorkflowStageStatus.Completed,
                ledger.Stages[0].Status);
            Assert.Equal(
                CalculationWorkflowEvidenceStatus.Completed,
                ledger.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证父作业和恢复作业进入同一根工作流，失败证据不丢失。</summary>
    [Fact]
    public async Task ParentAndRecoveryJobsShareRootWorkflow()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationJob parent = CreateJob(
                "job-ledger-parent",
                "workflow-ledger-recovery",
                CalculationTaskType.Optimization,
                CalculationJobState.Failed);
            parent.StartedAt =
                DateTimeOffset.UtcNow.AddMinutes(-10);
            parent.CompletedAt =
                DateTimeOffset.UtcNow.AddMinutes(-5);
            await repository.AttachJobAsync(
                parent.RootWorkflowId,
                CalculationWorkflowStageIds.Optimization,
                parent);

            CalculationJob recovery = CreateJob(
                "job-ledger-recovery",
                "workflow-ledger-recovery",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated);
            recovery.ParentJobId = parent.JobId;
            recovery.AttemptNumber = 1;
            recovery.CorrectionPlanId = "plan-ledger-recovery";
            recovery.StartedAt = DateTimeOffset.UtcNow;
            recovery.CompletedAt =
                DateTimeOffset.UtcNow.AddMinutes(1);
            await repository.AttachJobAsync(
                recovery.RootWorkflowId,
                CalculationWorkflowStageIds.Optimization,
                recovery);

            CalculationWorkflowEvidenceLedger? ledger =
                await repository.GetAsync(
                    parent.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Equal(2, ledger!.JobAttempts.Count);
            Assert.Equal(2, ledger.Stages[0].JobAttemptIds.Count);
            Assert.Contains(
                ledger.JobAttempts,
                attempt => attempt.CalculationJobId
                    == parent.JobId
                    && attempt.State
                        == CalculationJobState.Failed);
            Assert.Contains(
                ledger.JobAttempts,
                attempt => attempt.CalculationJobId
                    == recovery.JobId
                    && attempt.ParentJobId
                        == parent.JobId
                    && attempt.CorrectionPlanId
                        == "plan-ledger-recovery");
            Assert.Equal(
                CalculationWorkflowStageStatus.Completed,
                ledger.Stages[0].Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证失败和取消作业都保留，失败优先决定根状态。</summary>
    [Fact]
    public async Task FailedAndCanceledJobsRemainVisible()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationJob failed = CreateJob(
                "job-ledger-failed",
                "workflow-ledger-failure",
                CalculationTaskType.Optimization,
                CalculationJobState.Failed);
            CalculationJob canceled = CreateJob(
                "job-ledger-canceled",
                "workflow-ledger-failure",
                CalculationTaskType.TdDft,
                CalculationJobState.Canceled);

            await repository.AttachJobAsync(
                failed.RootWorkflowId,
                CalculationWorkflowStageIds.Optimization,
                failed);
            await repository.AttachJobAsync(
                canceled.RootWorkflowId,
                CalculationWorkflowStageIds.Tddft,
                canceled);

            CalculationWorkflowEvidenceLedger? ledger =
                await repository.GetAsync(
                    failed.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Equal(2, ledger!.JobAttempts.Count);
            Assert.Contains(
                ledger.Stages,
                stage => stage.StageId
                    == CalculationWorkflowStageIds.Optimization
                    && stage.Status
                        == CalculationWorkflowStageStatus.Failed);
            Assert.Contains(
                ledger.Stages,
                stage => stage.StageId
                    == CalculationWorkflowStageIds.Tddft
                    && stage.Status
                        == CalculationWorkflowStageStatus.Canceled);
            Assert.Equal(
                CalculationWorkflowEvidenceStatus.Failed,
                ledger.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证复合工作流阶段按稳定顺序写入并读取。</summary>
    [Fact]
    public async Task StagesHaveStableOrder()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            string rootWorkflowId = "workflow-ledger-order";

            await repository.EnsureStageAsync(
                rootWorkflowId,
                CreateStage(
                    CalculationWorkflowStageIds.Tddft));
            await repository.EnsureStageAsync(
                rootWorkflowId,
                CreateStage(
                    CalculationWorkflowStageIds.Optimization));
            await repository.EnsureStageAsync(
                rootWorkflowId,
                CreateStage(
                    CalculationWorkflowStageIds.Nto));

            CalculationWorkflowEvidenceLedger? ledger =
                await repository.GetAsync(rootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Equal(3, ledger!.Stages.Count);
            Assert.Equal(
                CalculationWorkflowStageIds.Optimization,
                ledger.Stages[0].StageId);
            Assert.Equal(
                CalculationWorkflowStageIds.Tddft,
                ledger.Stages[1].StageId);
            Assert.Equal(
                CalculationWorkflowStageIds.Nto,
                ledger.Stages[2].StageId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证仓储实例重建后只凭根标识仍可读取账本。</summary>
    [Fact]
    public async Task RecreatedRepositoryCanReadLedger()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository first =
                new FileCalculationWorkflowEvidenceLedgerRepository(
                    workspace);
            CalculationJob job = CreateJob(
                "job-ledger-restart",
                "workflow-ledger-restart",
                CalculationTaskType.TdDft,
                CalculationJobState.Validated);
            await first.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.Tddft,
                job);

            WorkspaceManager rebuiltWorkspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository second =
                new FileCalculationWorkflowEvidenceLedgerRepository(
                    rebuiltWorkspace);
            CalculationWorkflowEvidenceLedger? ledger =
                await second.GetAsync(job.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Single(ledger!.JobAttempts);
            Assert.Equal(
                job.JobId,
                ledger.JobAttempts[0].CalculationJobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证空根标识被明确拒绝。</summary>
    [Fact]
    public async Task EmptyRootWorkflowIdIsRejected()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);

            await Assert.ThrowsAsync<ArgumentException>(
                delegate
                {
                    return repository.GetAsync(string.Empty);
                });
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证账本 JSON 使用字符串枚举且无临时文件残留。</summary>
    [Fact]
    public async Task LedgerJsonIsAtomicAndDeserializable()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationJob job = CreateJob(
                "job-ledger-json",
                "workflow-ledger-json",
                CalculationTaskType.TdDft,
                CalculationJobState.Validated);
            await repository.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.Tddft,
                job);

            string ledgerPath =
                workspace.GetWorkflowRunLedgerPath(
                    job.RootWorkflowId);
            string json = await File.ReadAllTextAsync(
                ledgerPath);
            JsonSerializerOptions options =
                new JsonSerializerOptions(
                    JsonSerializerDefaults.Web);
            options.Converters.Add(
                new JsonStringEnumConverter());
            CalculationWorkflowEvidenceLedger? restored =
                JsonSerializer.Deserialize<
                    CalculationWorkflowEvidenceLedger>(
                        json,
                        options);

            Assert.NotNull(restored);
            Assert.Contains(
                "\"status\": \"Completed\"",
                json);
            Assert.DoesNotContain(
                Directory.GetFiles(
                    Path.GetDirectoryName(ledgerPath)!),
                path => path.EndsWith(
                    CalculationWorkspacePaths
                        .JsonTemporaryFileSuffix,
                    StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证文件作业仓储会自动把作业索引写入账本。</summary>
    [Fact]
    public async Task FileCalculationRepositoryRegistersLedger()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-ledger-auto",
                "workflow-ledger-auto",
                CalculationTaskType.SinglePoint,
                CalculationJobState.InputGenerated);

            await calculations.SaveJobAsync(job);

            FileCalculationWorkflowEvidenceLedgerRepository
                ledgers =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationWorkflowEvidenceLedger? ledger =
                await ledgers.GetAsync(job.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Single(ledger!.JobAttempts);
            Assert.Equal(
                job.JobId,
                ledger.JobAttempts[0].CalculationJobId);
            Assert.Equal(
                CalculationWorkflowStageIds.SinglePoint,
                ledger.JobAttempts[0].StageId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证显式阶段优先于任务类型推断。</summary>
    [Fact]
    public async Task FileCalculationRepositoryUsesExplicitEvidenceStage()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-ledger-explicit-stage",
                "workflow-ledger-explicit-stage",
                CalculationTaskType.Optimization,
                CalculationJobState.Completed);
            job.EvidenceStageId =
                CalculationWorkflowStageIds.WavefunctionStability;

            await calculations.SaveJobAsync(job);

            CalculationWorkflowEvidenceLedger? ledger =
                await new FileCalculationWorkflowEvidenceLedgerRepository(
                    workspace).GetAsync(job.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Single(ledger!.Stages);
            Assert.Equal(
                CalculationWorkflowStageIds.WavefunctionStability,
                ledger.Stages[0].StageId);
            Assert.Equal(
                CalculationWorkflowStageIds.WavefunctionStability,
                ledger.JobAttempts[0].StageId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证作业改归阶段时不会在旧阶段留下悬空引用。</summary>
    [Fact]
    public async Task MovingJobBetweenStagesRemovesStaleReference()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace =
                CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository
                repository =
                    new FileCalculationWorkflowEvidenceLedgerRepository(
                        workspace);
            CalculationJob job = CreateJob(
                "job-ledger-move",
                "workflow-ledger-move",
                CalculationTaskType.Optimization,
                CalculationJobState.Running);

            await repository.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.Optimization,
                job);
            await repository.AttachJobAsync(
                job.RootWorkflowId,
                CalculationWorkflowStageIds.WavefunctionStability,
                job);

            CalculationWorkflowEvidenceLedger? ledger =
                await repository.GetAsync(job.RootWorkflowId);

            Assert.NotNull(ledger);
            Assert.Equal(2, ledger!.Stages.Count);
            Assert.DoesNotContain(
                job.JobId,
                ledger.Stages[0].JobAttemptIds);
            Assert.Contains(
                job.JobId,
                ledger.Stages[1].JobAttemptIds);
            Assert.Single(ledger.JobAttempts);
            Assert.Equal(
                CalculationWorkflowStageIds.WavefunctionStability,
                ledger.JobAttempts[0].StageId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static CalculationJob CreateJob(
        string jobId,
        string rootWorkflowId,
        CalculationTaskType taskType,
        CalculationJobState state)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = jobId;
        job.WorkflowId = "workflow-stage-" + jobId;
        job.RootWorkflowId = rootWorkflowId;
        job.SessionId = "session-ledger";
        job.Goal = "根工作流账本测试";
        job.Spec = CreateSpec(taskType);
        job.State = state;
        job.CreatedAt = DateTimeOffset.UtcNow;
        return job;
    }

    private static CalculationSpec CreateSpec(
        CalculationTaskType taskType)
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.TaskType = taskType;
        return spec;
    }

    private static CalculationWorkflowStageEvidence
        CreateStage(string stageId)
    {
        CalculationWorkflowStageEvidence stage =
            new CalculationWorkflowStageEvidence();
        stage.StageId = stageId;
        stage.StageType = stageId;
        stage.Order =
            CalculationWorkflowStageIds
                .ResolveDefaultOrder(stageId);
        return stage;
    }

    private static WorkspaceManager CreateWorkspace(
        string root)
    {
        CalculationWorkspaceOptions options =
            new CalculationWorkspaceOptions();
        options.RootDirectory = root;
        return new WorkspaceManager(options);
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
}
