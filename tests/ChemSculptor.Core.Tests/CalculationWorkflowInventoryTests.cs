using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>根工作流作业完整枚举和筛选测试。</summary>
public class CalculationWorkflowInventoryTests
{
    /// <summary>验证多阶段、失败和取消作业均被完整枚举。</summary>
    [Fact]
    public async Task EnumeratesAllStageJobsAndPreservesFailures()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob optimization = CreateJob(
                "job-inventory-opt",
                "workflow-inventory-all",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Optimization);
            CalculationJob failed = CreateJob(
                "job-inventory-failed",
                optimization.RootWorkflowId,
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.OptimizationRecovery);
            failed.ParentJobId = optimization.JobId;
            failed.AttemptNumber = 1;
            CalculationJob canceled = CreateJob(
                "job-inventory-canceled",
                optimization.RootWorkflowId,
                CalculationTaskType.Frequency,
                CalculationJobState.Canceled,
                CalculationWorkflowStageIds.Frequency);

            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                optimization);
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                failed);
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                canceled);

            CalculationWorkflowJobInventory inventory =
                await new FileCalculationWorkflowInventoryRepository(workspace)
                    .QueryAsync(new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = optimization.RootWorkflowId
                    });

            Assert.Equal(3, inventory.Jobs.Count);
            Assert.All(
                inventory.Jobs,
                entry => Assert.Equal(
                    CalculationWorkflowJobManifestStatus.Present,
                    entry.ManifestStatus));
            Assert.Contains(
                inventory.Jobs,
                entry => entry.JobId == failed.JobId
                    && entry.State == CalculationJobState.Failed);
            Assert.Contains(
                inventory.Jobs,
                entry => entry.JobId == canceled.JobId
                    && entry.State == CalculationJobState.Canceled);
            Assert.DoesNotContain(
                inventory.Issues,
                issue => issue.Severity
                    == CalculationWorkflowInventoryIssueSeverity.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证阶段、状态、任务类型、程序和恢复筛选可以组合。</summary>
    [Fact]
    public async Task FiltersByStageStateTaskProgramAndRecovery()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob original = CreateJob(
                "job-filter-original",
                "workflow-filter",
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.Optimization);
            original.Spec.Program = "Gaussian";
            CalculationJob recovery = CreateJob(
                "job-filter-recovery",
                original.RootWorkflowId,
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.OptimizationRecovery);
            recovery.Spec.Program = "Gaussian";
            recovery.ParentJobId = original.JobId;
            recovery.AttemptNumber = 1;
            CalculationJob unrelated = CreateJob(
                "job-filter-unrelated",
                original.RootWorkflowId,
                CalculationTaskType.TdDft,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Tddft);
            unrelated.Spec.Program = "ORCA";
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                original);
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                recovery);
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                unrelated);

            FileCalculationWorkflowInventoryRepository repository =
                new FileCalculationWorkflowInventoryRepository(workspace);
            CalculationWorkflowJobInventory inventory =
                await repository.QueryAsync(
                    new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = original.RootWorkflowId,
                        StageIds = new List<string>
                        {
                            CalculationWorkflowStageIds.OptimizationRecovery
                        },
                        States = new List<CalculationJobState>
                        {
                            CalculationJobState.Validated
                        },
                        TaskTypes = new List<CalculationTaskType>
                        {
                            CalculationTaskType.Optimization
                        },
                        Programs = new List<string> { "gaussian" },
                        ParentJobId = original.JobId,
                        RecoveryAttempt = true
                    });

            CalculationWorkflowJobInventoryEntry entry =
                Assert.Single(inventory.Jobs);
            Assert.Equal(recovery.JobId, entry.JobId);
            Assert.True(entry.IsIndexed);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证账本引用的清单缺失时仍保留作业条目并报告错误。</summary>
    [Fact]
    public async Task MissingManifestIsReportedAndNotSilentlyDropped()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository ledgers =
                new FileCalculationWorkflowEvidenceLedgerRepository(workspace);
            CalculationJob job = CreateJob(
                "job-inventory-missing-manifest",
                "workflow-inventory-missing-manifest",
                CalculationTaskType.SinglePoint,
                CalculationJobState.Completed,
                CalculationWorkflowStageIds.SinglePoint);
            await workspace.EnsureJobWorkspaceAsync(job.JobId);
            await ledgers.AttachJobAsync(
                job.RootWorkflowId,
                job.EvidenceStageId,
                job);

            CalculationWorkflowJobInventory inventory =
                await new FileCalculationWorkflowInventoryRepository(workspace)
                    .QueryAsync(new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = job.RootWorkflowId
                    });

            CalculationWorkflowJobInventoryEntry entry =
                Assert.Single(inventory.Jobs);
            Assert.Equal(
                CalculationWorkflowJobManifestStatus.Missing,
                entry.ManifestStatus);
            Assert.Contains(
                inventory.Issues,
                issue => issue.Code
                    == CalculationWorkflowInventoryIssueCodes.ManifestMissing
                    && issue.JobId == job.JobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证目录存在但账本未登记的作业会被发现。</summary>
    [Fact]
    public async Task UnindexedJobIsDiscoveredAndReported()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob indexed = CreateJob(
                "job-inventory-indexed",
                "workflow-inventory-unindexed",
                CalculationTaskType.SinglePoint,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.SinglePoint);
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                indexed);
            CalculationJob unindexed = CreateJob(
                "job-inventory-unindexed",
                indexed.RootWorkflowId,
                CalculationTaskType.TdDft,
                CalculationJobState.Completed,
                CalculationWorkflowStageIds.Tddft);
            await workspace.EnsureJobWorkspaceAsync(unindexed.JobId);
            unindexed.RunDirectory =
                workspace.GetRunDirectory(unindexed.JobId);
            await WriteManifestAsync(workspace, unindexed);

            CalculationWorkflowJobInventory inventory =
                await new FileCalculationWorkflowInventoryRepository(workspace)
                    .QueryAsync(new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = indexed.RootWorkflowId
                    });

            CalculationWorkflowJobInventoryEntry entry =
                Assert.Single(
                    inventory.Jobs,
                    candidate => candidate.JobId == unindexed.JobId);
            Assert.False(entry.IsIndexed);
            Assert.Contains(
                inventory.Issues,
                issue => issue.Code
                    == CalculationWorkflowInventoryIssueCodes.JobUnindexed
                    && issue.JobId == unindexed.JobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证阶段和作业尝试的双向悬空引用都会被报告。</summary>
    [Fact]
    public async Task LedgerRelationAuditReportsBothDanglingDirections()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationWorkflowEvidenceLedgerRepository ledgers =
                new FileCalculationWorkflowEvidenceLedgerRepository(workspace);
            CalculationWorkflowEvidenceLedger ledger =
                new CalculationWorkflowEvidenceLedger();
            ledger.RootWorkflowId = "workflow-inventory-dangling";
            CalculationWorkflowStageEvidence stage =
                new CalculationWorkflowStageEvidence();
            stage.StageId = CalculationWorkflowStageIds.Optimization;
            stage.StageType = stage.StageId;
            stage.Order = 10;
            stage.JobAttemptIds.Add("job-stage-only");
            ledger.Stages.Add(stage);
            CalculationWorkflowJobAttemptEvidence attempt =
                new CalculationWorkflowJobAttemptEvidence();
            attempt.CalculationJobId = "job-attempt-only";
            attempt.StageId = CalculationWorkflowStageIds.Optimization;
            attempt.TaskType = CalculationTaskType.TdDft;
            ledger.JobAttempts.Add(attempt);
            CalculationWorkflowJobAttemptEvidence orphanStage =
                new CalculationWorkflowJobAttemptEvidence();
            orphanStage.CalculationJobId = "job-orphan-stage";
            orphanStage.StageId = "stage-does-not-exist";
            orphanStage.TaskType = CalculationTaskType.SinglePoint;
            ledger.JobAttempts.Add(orphanStage);
            await ledgers.SaveAsync(ledger);

            CalculationWorkflowJobInventory inventory =
                await new FileCalculationWorkflowInventoryRepository(workspace)
                    .QueryAsync(new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = ledger.RootWorkflowId,
                        IncludeUnindexedJobs = false
                    });

            Assert.Contains(
                inventory.Issues,
                issue => issue.Code
                    == CalculationWorkflowInventoryIssueCodes.StageJobReferenceMissing
                    && issue.JobId == "job-stage-only");
            Assert.Contains(
                inventory.Issues,
                issue => issue.Code
                    == CalculationWorkflowInventoryIssueCodes.AttemptStageMissing
                    && issue.JobId == "job-orphan-stage");
            Assert.Contains(
                inventory.Issues,
                issue => issue.Code
                    == CalculationWorkflowInventoryIssueCodes.StageJobReferenceNotListed
                    && issue.JobId == "job-attempt-only");
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证新仓储实例可以在服务重启后读取同一账本和清单。</summary>
    [Fact]
    public async Task NewRepositoryInstanceReadsPersistedInventory()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-inventory-restart",
                "workflow-inventory-restart",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Optimization);
            await SaveAndCreateRunDirectoryAsync(
                workspace,
                calculations,
                job);

            WorkspaceManager restartedWorkspace =
                CreateWorkspace(root);
            CalculationWorkflowJobInventory inventory =
                await new FileCalculationWorkflowInventoryRepository(
                        restartedWorkspace)
                    .QueryAsync(new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = job.RootWorkflowId
                    });

            CalculationWorkflowJobInventoryEntry entry =
                Assert.Single(inventory.Jobs);
            Assert.Equal(job.JobId, entry.JobId);
            Assert.Equal(
                CalculationWorkflowJobManifestStatus.Present,
                entry.ManifestStatus);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证缺失账本会明确报错，而不是返回空成功清单。</summary>
    [Fact]
    public async Task MissingLedgerIsReported()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            CalculationWorkflowJobInventory inventory =
                await new FileCalculationWorkflowInventoryRepository(workspace)
                    .QueryAsync(new CalculationWorkflowJobInventoryQuery
                    {
                        RootWorkflowId = "workflow-inventory-no-ledger"
                    });

            Assert.Empty(inventory.Jobs);
            Assert.Contains(
                inventory.Issues,
                issue => issue.Code
                    == CalculationWorkflowInventoryIssueCodes.LedgerMissing);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证无效根工作流标识被拒绝。</summary>
    [Fact]
    public async Task EmptyRootWorkflowIdIsRejected()
    {
        WorkspaceManager workspace = CreateWorkspace(
            Path.Combine(
                Path.GetTempPath(),
                "ChemSculptorTests"));
        FileCalculationWorkflowInventoryRepository repository =
            new FileCalculationWorkflowInventoryRepository(workspace);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.QueryAsync(
                new CalculationWorkflowJobInventoryQuery()));
    }

    private static async Task WriteManifestAsync(
        WorkspaceManager workspace,
        CalculationJob job)
    {
        System.Text.Json.JsonSerializerOptions options =
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web);
        options.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        string json = System.Text.Json.JsonSerializer.Serialize(
            job,
            options);
        await File.WriteAllTextAsync(
            workspace.GetJobManifestPath(job.JobId),
            json);
    }

    private static async Task SaveAndCreateRunDirectoryAsync(
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
        job.SessionId = "session-inventory";
        job.Goal = "作业清单测试";
        job.Spec = CalculationDefaults.CreateDefaultSinglePoint();
        job.Spec.TaskType = taskType;
        job.State = state;
        job.EvidenceStageId = stageId;
        job.CreatedAt = DateTimeOffset.UtcNow;
        return job;
    }

    private static WorkspaceManager CreateWorkspace(string root)
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

