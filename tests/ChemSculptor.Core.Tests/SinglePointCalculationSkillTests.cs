using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.SinglePointCalculation;

namespace ChemSculptor.Core.Tests;

/// <summary>单点计算复合 Skill 测试。</summary>
public class SinglePointCalculationSkillTests
{
    /// <summary>验证复合结果可以同时供验证和提取节点读取。</summary>
    [Fact]
    public void ResultContractIsCompatibleWithWorkflowNodes()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-contract";

        CalculationResult calculationResult =
            new CalculationResult();
        calculationResult.JobId = job.JobId;
        calculationResult.Energy = -76.0;
        calculationResult.NormalTermination = true;
        calculationResult.ScfConverged = true;
        calculationResult.ScfIterations = 8;

        CalculationValidationReport report =
            new CalculationValidationReport();
        report.Passed = true;

        SinglePointCalculationSkillResult compositeResult =
            new SinglePointCalculationSkillResult();
        compositeResult.Succeeded = true;
        compositeResult.Passed = true;
        compositeResult.Job = job;
        compositeResult.Result = calculationResult;
        compositeResult.Report = report;

        string json = SkillJson.Serialize(compositeResult);
        CalculationWorkflowValidationSkillResult validation =
            SkillJson.Deserialize<
                CalculationWorkflowValidationSkillResult>(json);
        CalculationResultExtractionResult extraction =
            SkillJson.Deserialize<
                CalculationResultExtractionResult>(json);

        Assert.True(validation.Passed);
        Assert.Equal(job.JobId, validation.Job.JobId);
        Assert.True(extraction.Succeeded);
        Assert.Equal(-76.0, extraction.Result.Energy);
        Assert.True(extraction.Result.ScfConverged);
    }

    /// <summary>验证复合 Skill 按固定能力顺序完成一次单点计算。</summary>
    [Fact]
    public async Task RunsAtomicSkillsAndReturnsSinglePointContract()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-single-point-skill";
        job.WorkflowId = job.JobId;
        job.Spec = CalculationDefaults.CreateDefaultSinglePoint();

        CalculationResult calculationResult =
            new CalculationResult();
        calculationResult.JobId = job.JobId;
        calculationResult.Energy = -76.3801014;
        calculationResult.EnergyUnit = "Hartree";
        calculationResult.NormalTermination = true;
        calculationResult.ScfConverged = true;
        calculationResult.ScfIterations = 8;
        calculationResult.FailureKind =
            CalculationFailureKind.None;

        CalculationInputGenerationResult inputResult =
            new CalculationInputGenerationResult();
        inputResult.Succeeded = true;
        inputResult.Job = job;

        CalculationSubmissionSkillResult submissionResult =
            new CalculationSubmissionSkillResult();
        submissionResult.Succeeded = true;
        submissionResult.Job = job;

        CalculationWaitSkillResult waitResult =
            new CalculationWaitSkillResult();
        waitResult.State = CalculationJobState.Completed;
        waitResult.Job = job;

        CalculationResultExtractionResult extractionResult =
            new CalculationResultExtractionResult();
        extractionResult.Succeeded = true;
        extractionResult.Result = calculationResult;

        CalculationValidationReport report =
            new CalculationValidationReport();
        report.Passed = true;
        report.Status = CalculationValidationStatus.Passed;
        report.Summary = "验证通过。";

        CalculationWorkflowValidationSkillResult validationResult =
            new CalculationWorkflowValidationSkillResult();
        validationResult.Passed = true;
        validationResult.Job = job;
        validationResult.Report = report;

        List<string> order = new List<string>();
        SkillRegistry registry = new SkillRegistry();
        Register(
            registry,
            CalculationSkillIds.CalculationInputPreparation,
            inputResult,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationSubmission,
            submissionResult,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationWait,
            waitResult,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationResultExtraction,
            extractionResult,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationWorkflowValidation,
            validationResult,
            order);

        SinglePointCalculationSkill skill =
            new SinglePointCalculationSkill(registry);
        CalculationInputGenerationRequest inputRequest =
            new CalculationInputGenerationRequest();
        inputRequest.Job = job;
        inputRequest.Spec = job.Spec;
        inputRequest.CoordinateText = "O 0.0 0.0 0.0";

        TaskRequest taskRequest = new TaskRequest();
        taskRequest.WorkflowId = job.WorkflowId;
        taskRequest.NodeId = "single-point";
        taskRequest.SkillId = skill.Name;
        taskRequest.Inputs["request"] =
            SkillJson.Serialize(inputRequest);

        TaskResult taskResult =
            await skill.ExecuteAsync(taskRequest);
        SinglePointCalculationSkillResult result =
            SkillJson.Deserialize<
                SinglePointCalculationSkillResult>(
                taskResult.Output!);

        Assert.True(taskResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.Passed);
        Assert.Equal(job.JobId, result.Job.JobId);
        Assert.Equal(-76.3801014, result.Result.Energy);
        Assert.True(result.Result.ScfConverged);
        Assert.Equal(8, result.Result.ScfIterations);
        Assert.Equal(5, result.Steps.Count);
        Assert.Equal(
            CalculationSkillIds.CalculationInputPreparation,
            result.Steps[0].SkillId);
        Assert.Equal(
            CalculationSkillIds.CalculationWorkflowValidation,
            result.Steps[4].SkillId);
        Assert.Equal(
            new string[]
            {
                CalculationSkillIds.CalculationInputPreparation,
                CalculationSkillIds.CalculationSubmission,
                CalculationSkillIds.CalculationWait,
                CalculationSkillIds.CalculationResultExtraction,
                CalculationSkillIds.CalculationWorkflowValidation
            },
            order);
    }

    private static void Register<T>(
        SkillRegistry registry,
        string skillId,
        T output,
        List<string> order)
        where T : class
    {
        RecordingSkill skill =
            new RecordingSkill(skillId, output!, order);
        registry.RegisterAsync(skill).GetAwaiter().GetResult();
    }

    private sealed class RecordingSkill : ISkill
    {
        private readonly object _output;
        private readonly List<string> _order;
        private readonly List<string> _capabilities;

        public RecordingSkill(
            string name,
            object output,
            List<string> order)
        {
            NameValue = name;
            _output = output;
            _order = order;
            _capabilities = new List<string>();
            _capabilities.Add("test.atomic-skill");
        }

        public string NameValue { get; private set; }

        public string Name
        {
            get { return NameValue; }
        }

        public string Version
        {
            get { return "1.0.0"; }
        }

        public IReadOnlyList<string> Capabilities
        {
            get { return _capabilities; }
        }

        public Task<TaskResult> ExecuteAsync(
            TaskRequest request,
            CancellationToken cancellationToken = default)
        {
            _order.Add(Name);

            TaskResult result = new TaskResult();
            result.WorkflowId = request.WorkflowId;
            result.NodeId = request.NodeId;
            result.Succeeded = true;
            result.Output = SkillJson.Serialize(_output);
            return Task.FromResult(result);
        }

        public Task<bool> HealthAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
