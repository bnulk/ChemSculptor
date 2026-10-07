using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.AnomalyWorkflow;

namespace ChemSculptor.Core.Tests;

/// <summary>派生恢复作业执行测试。</summary>
public class RecoveryJobExecutionSkillTests
{
    /// <summary>验证已预授权的自旋多重度变更会直接执行单点计算。</summary>
    [Fact]
    public async Task ExecutesPreparedSpinMultiplicityRecovery()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;

            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileCalculationRepository calculationRepository =
                new FileCalculationRepository(workspace);
            FileAnomalyRepository anomalyRepository =
                new FileAnomalyRepository(workspace);
            RecordingComputeBackend computeBackend =
                new RecordingComputeBackend();

            AnomalyRecord anomalyRecord = new AnomalyRecord();
            anomalyRecord.Id = "record-1";
            anomalyRecord.JobId = "job-original";
            anomalyRecord.Status =
                AnomalyRecordStatus.RecoveryPrepared;
            await anomalyRepository.SaveAsync(anomalyRecord);

            CalculationJob recoveryJob = new CalculationJob();
            recoveryJob.JobId = "job-recovery";
            recoveryJob.Spec =
                CalculationDefaults.CreateDefaultSinglePoint();
            recoveryJob.WorkspaceDirectory =
                workspace.GetJobDirectory(recoveryJob.JobId);
            recoveryJob.RunDirectory =
                workspace.GetRunDirectory(recoveryJob.JobId);
            recoveryJob.InputFilePath =
                Path.Combine(
                    recoveryJob.RunDirectory,
                    recoveryJob.JobId + ".gjf");
            recoveryJob.OutputFilePath =
                workspace.GetJobOutputPath(recoveryJob.JobId);

            CorrectionOption option = new CorrectionOption();
            option.IntentCode =
                CorrectionIntentCodes.ChangeSpinMultiplicity;
            option.RequiresApproval = false;
            option.CanUseForRestart = true;

            CorrectionPlan plan = new CorrectionPlan();
            plan.Id = "plan-1";
            plan.SourceJobId = anomalyRecord.JobId;
            plan.Option = option;

            WavefunctionStabilityCorrectionPlanningResult planning =
                new WavefunctionStabilityCorrectionPlanningResult();
            planning.PlanCreated = true;
            planning.Plan = plan;
            planning.AnomalyRecordId = anomalyRecord.Id;

            RecoveryJobPreparationResult preparation =
                new RecoveryJobPreparationResult();
            preparation.Succeeded = true;
            preparation.RecoveryJob = recoveryJob;

            TaskRequest request = new TaskRequest();
            request.WorkflowId = "workflow-recovery";
            request.NodeId = "recovery-execution";
            request.Inputs["correctionPlan"] =
                SkillJson.Serialize(planning);
            request.Inputs["recoveryJob"] =
                SkillJson.Serialize(preparation);

            RecoveryJobExecutionSkill skill =
                new RecoveryJobExecutionSkill(
                    computeBackend,
                    new TestQuantumProgramAdapterRegistry(),
                    calculationRepository,
                    anomalyRepository);

            TaskResult taskResult =
                await skill.ExecuteAsync(request);
            RecoveryJobExecutionResult execution =
                SkillJson.Deserialize<RecoveryJobExecutionResult>(
                    taskResult.Output!);

            Assert.True(taskResult.Succeeded);
            Assert.True(execution.Attempted);
            Assert.True(execution.Succeeded);
            Assert.Equal(
                recoveryJob.JobId,
                computeBackend.SubmittedJobId);

            CalculationJob? savedJob =
                await calculationRepository.GetJobAsync(
                    recoveryJob.JobId);
            CalculationResult? savedResult =
                await calculationRepository.GetResultAsync(
                    recoveryJob.JobId);
            AnomalyRecord? savedRecord =
                await anomalyRepository.GetAsync(
                    anomalyRecord.JobId,
                    anomalyRecord.Id);

            Assert.NotNull(savedJob);
            Assert.Equal(
                CalculationJobState.Completed,
                savedJob.State);
            Assert.NotNull(savedResult);
            Assert.NotNull(savedRecord);
            Assert.Equal(
                AnomalyRecordStatus.Recovering,
                savedRecord.Status);
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

    private sealed class RecordingComputeBackend : IComputeBackend
    {
        public string SubmittedJobId { get; private set; } =
            string.Empty;

        public string Name
        {
            get { return "recording"; }
        }

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            SubmittedJobId = job.JobId;
            return Task.FromResult(job.JobId);
        }

        public Task<CalculationJobState> GetStatusAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                CalculationJobState.Completed);
        }

        public Task CancelAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
