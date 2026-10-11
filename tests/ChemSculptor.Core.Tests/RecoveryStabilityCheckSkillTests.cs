using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.AnomalyWorkflow;

namespace ChemSculptor.Core.Tests;

/// <summary>派生作业稳定性复检测试。</summary>
public class RecoveryStabilityCheckSkillTests
{
    /// <summary>验证没有派生作业时复检会安全跳过。</summary>
    [Fact]
    public async Task SkipsWhenNoRecoveryJobWasExecuted()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;
            WorkspaceManager workspace =
                new WorkspaceManager(options);
            FileAnomalyRepository repository =
                new FileAnomalyRepository(workspace);
            AnomalyProviderRegistry registry =
                new AnomalyProviderRegistry();
            RecoveryJobExecutionResult execution =
                new RecoveryJobExecutionResult();

            TaskRequest taskRequest = new TaskRequest();
            taskRequest.WorkflowId = "workflow-no-recovery";
            taskRequest.NodeId = "recovery-stability-check";
            taskRequest.Inputs["recoveryExecution"] =
                SkillJson.Serialize(execution);

            WavefunctionStabilityCheckSkill skill =
                new WavefunctionStabilityCheckSkill(
                    registry,
                    repository);

            TaskResult taskResult =
                await skill.ExecuteAsync(taskRequest);
            AnomalyCheckResult checkResult =
                SkillJson.Deserialize<AnomalyCheckResult>(
                    taskResult.Output!);

            Assert.True(taskResult.Succeeded);
            Assert.Equal(
                AnomalyCheckStatus.Skipped,
                checkResult.Status);
            Assert.Contains(
                "没有派生恢复作业",
                checkResult.SkippedReason);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证通用稳定性 Skill 可以检查派生作业。</summary>
    [Fact]
    public async Task ChecksRecoveryJobFromExecutionResult()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;
            WorkspaceManager workspace =
                new WorkspaceManager(options);
            FileAnomalyRepository repository =
                new FileAnomalyRepository(workspace);
            AnomalyProviderRegistry registry =
                new AnomalyProviderRegistry();
            RecordingAnomalyCheck check =
                new RecordingAnomalyCheck();
            registry.RegisterCheck(check);

            CalculationJob recoveryJob = new CalculationJob();
            recoveryJob.JobId = "job-recovery";
            recoveryJob.WorkflowId = "workflow-recovery";
            recoveryJob.Spec =
                CalculationDefaults.CreateDefaultSinglePoint();

            RecoveryJobExecutionResult execution =
                new RecoveryJobExecutionResult();
            execution.RecoveryJob = recoveryJob;

            TaskRequest taskRequest = new TaskRequest();
            taskRequest.WorkflowId = recoveryJob.WorkflowId;
            taskRequest.NodeId = "recovery-stability-check";
            taskRequest.Inputs["recoveryExecution"] =
                SkillJson.Serialize(execution);

            WavefunctionStabilityCheckSkill skill =
                new WavefunctionStabilityCheckSkill(
                    registry,
                    repository);

            TaskResult taskResult =
                await skill.ExecuteAsync(taskRequest);
            AnomalyCheckResult checkResult =
                SkillJson.Deserialize<AnomalyCheckResult>(
                    taskResult.Output!);

            Assert.True(taskResult.Succeeded);
            Assert.Equal(
                AnomalyCheckStatus.Passed,
                checkResult.Status);
            Assert.Equal(
                recoveryJob.JobId,
                checkResult.JobId);
            Assert.Equal(
                recoveryJob.JobId,
                check.LastContext!.Job!.JobId);

            AnomalyRecord? record =
                await repository.GetAsync(
                    recoveryJob.JobId,
                    checkResult.AnomalyRecordId);

            Assert.NotNull(record);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
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

    private sealed class RecordingAnomalyCheck : IAnomalyCheck
    {
        public AnomalyContext? LastContext { get; private set; }

        public AnomalyCheckDescriptor Descriptor
        {
            get
            {
                AnomalyCheckDescriptor descriptor =
                    new AnomalyCheckDescriptor();
                descriptor.Code =
                    CommonAnomalyCheckCodes.WavefunctionStability;
                descriptor.ImplementationId =
                    "test.recovery-stability-check";
                descriptor.DisplayName = "测试稳定性复检";
                descriptor.Category = AnomalyCategory.Scientific;
                descriptor.Mechanism =
                    AnomalyCheckMechanism.OutputArtifact;
                return descriptor;
            }
        }

        public bool CanCheck(AnomalyContext context)
        {
            return context.Job != null;
        }

        public Task<AnomalyCheckResult> CheckAsync(
            AnomalyContext context,
            CancellationToken cancellationToken = default)
        {
            LastContext = context;
            AnomalyCheckResult result =
                new AnomalyCheckResult();
            result.Code =
                CommonAnomalyCheckCodes.WavefunctionStability;
            result.JobId = context.Job!.JobId;
            result.Status = AnomalyCheckStatus.Passed;
            result.Summary = "测试用稳定性复检通过。";
            return Task.FromResult(result);
        }
    }
}
