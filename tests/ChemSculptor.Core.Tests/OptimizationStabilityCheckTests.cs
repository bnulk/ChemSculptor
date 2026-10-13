using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.OptimizationRecovery;

namespace ChemSculptor.Core.Tests;

/// <summary>优化波函数稳定性检查策略测试。</summary>
public class OptimizationStabilityCheckTests
{
    /// <summary>验证用户明确跳过时不会执行稳定性检查。</summary>
    [Fact]
    public async Task SkipsStabilityCheckWhenRequestedByUser()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-stability-skip";
        job.Spec = CalculationDefaults.CreateDefaultOptimization();
        job.Spec.ExtraOptions[
            CalculationOptionKeys.WavefunctionStabilityCheck] =
            "Skip";

        OptimizationCalculationSkillResult optimization =
            new OptimizationCalculationSkillResult();
        optimization.Job = job;
        optimization.Result = new OptimizationResult();
        optimization.Result.JobId = job.JobId;

        OptimizationStabilityCheckSkill skill =
            new OptimizationStabilityCheckSkill(
                new AnomalyProviderRegistry(),
                new NoopAnomalyRepository());
        TaskRequest request = new TaskRequest();
        request.WorkflowId = "workflow-stability-skip";
        request.NodeId = "optimization-stability-check";
        request.SkillId = skill.Name;
        request.Inputs["optimization"] =
            SkillJson.Serialize(optimization);

        TaskResult taskResult =
            await skill.ExecuteAsync(request);
        AnomalyCheckResult result =
            SkillJson.Deserialize<AnomalyCheckResult>(
                taskResult.Output!);

        Assert.True(taskResult.Succeeded);
        Assert.Equal(
            AnomalyCheckStatus.Skipped,
            result.Status);
        Assert.Contains("用户", result.Summary);
    }

    private sealed class NoopAnomalyRepository
        : IAnomalyRepository
    {
        public Task SaveAsync(
            AnomalyRecord record,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<AnomalyRecord?> GetAsync(
            string jobId,
            string recordId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AnomalyRecord?>(null);
        }

        public Task<IReadOnlyList<AnomalyRecord>>
            ListByJobAsync(
                string jobId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<AnomalyRecord>>(
                    new List<AnomalyRecord>());
        }
    }
}
