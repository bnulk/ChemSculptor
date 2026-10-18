using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor.GeometryIntake;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.CalculationResultValidation;
using ChemSculptor.Skills.Common.OptimizationFrequencyCalculation;

namespace ChemSculptor.Core.Tests;

/// <summary>联合优化频率 Skill 和验证测试。</summary>
public class OptimizationFrequencyCalculationSkillTests
{
    /// <summary>验证联合 Skill 在一个作业中返回优化和频率结果。</summary>
    [Fact]
    public async Task RunsAtomicSkillsAndReturnsCombinedResult()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-opt-freq";
        job.WorkflowId = job.JobId;
        job.Spec =
            CalculationDefaults
                .CreateDefaultOptimizationFrequency();

        OptimizationResult optimization =
            CreateOptimizationResult(job.JobId);
        FrequencyAnalysisResult frequency =
            CreateFrequencyResult(job.JobId);

        CalculationInputGenerationResult input =
            new CalculationInputGenerationResult();
        input.Succeeded = true;
        input.Job = job;

        CalculationSubmissionSkillResult submission =
            new CalculationSubmissionSkillResult();
        submission.Succeeded = true;
        submission.Job = job;

        CalculationWaitSkillResult wait =
            new CalculationWaitSkillResult();
        wait.State = CalculationJobState.Completed;
        wait.Job = job;

        CalculationResultExtractionResult extraction =
            new CalculationResultExtractionResult();
        extraction.Succeeded = true;
        extraction.OptimizationResult = optimization;
        extraction.FrequencyResult = frequency;

        OptimizationFrequencyValidationSkillResult validation =
            new OptimizationFrequencyValidationSkillResult();
        validation.Passed = true;
        validation.Job = job;
        validation.OptimizationResult = optimization;
        validation.FrequencyResult = frequency;
        validation.Report.Passed = true;
        validation.Report.Status =
            CalculationValidationStatus.Passed;

        List<string> order = new List<string>();
        SkillRegistry registry = new SkillRegistry();
        await RegisterAsync(
            registry,
            CalculationSkillIds.CalculationInputPreparation,
            input,
            order);
        await RegisterAsync(
            registry,
            CalculationSkillIds.CalculationSubmission,
            submission,
            order);
        await RegisterAsync(
            registry,
            CalculationSkillIds.CalculationWait,
            wait,
            order);
        await RegisterAsync(
            registry,
            CalculationSkillIds.CalculationResultExtraction,
            extraction,
            order);
        await RegisterAsync(
            registry,
            CalculationSkillIds
                .CalculationOptimizationFrequencyValidation,
            validation,
            order);

        OptimizationFrequencyCalculationSkill skill =
            new OptimizationFrequencyCalculationSkill(registry);
        CalculationInputGenerationRequest inputRequest =
            new CalculationInputGenerationRequest();
        inputRequest.Job = job;
        inputRequest.Spec = job.Spec;
        inputRequest.CoordinateText = "O 0.0 0.0 0.0";

        TaskRequest taskRequest = new TaskRequest();
        taskRequest.WorkflowId = job.WorkflowId;
        taskRequest.NodeId = "optimization-frequency";
        taskRequest.SkillId = skill.Name;
        taskRequest.Inputs["request"] =
            SkillJson.Serialize(inputRequest);

        TaskResult taskResult =
            await skill.ExecuteAsync(taskRequest);
        OptimizationFrequencyCalculationSkillResult result =
            SkillJson.Deserialize<
                OptimizationFrequencyCalculationSkillResult>(
                    taskResult.Output!);

        Assert.True(taskResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.Passed);
        Assert.Equal(2, result.OptimizationResult.StepCount);
        Assert.Single(result.FrequencyResult.Frequencies);
        Assert.Equal(5, result.Steps.Count);
        Assert.Equal(
            CalculationSkillIds
                .CalculationOptimizationFrequencyValidation,
            result.Steps[4].SkillId);
        Assert.Equal(
            new string[]
            {
                CalculationSkillIds.CalculationInputPreparation,
                CalculationSkillIds.CalculationSubmission,
                CalculationSkillIds.CalculationWait,
                CalculationSkillIds.CalculationResultExtraction,
                CalculationSkillIds
                    .CalculationOptimizationFrequencyValidation
            },
            order);
    }

    /// <summary>验证联合结果缺少频率时验证器会失败。</summary>
    [Fact]
    public void FrequencyValidatorRejectsMissingFrequencies()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-opt-freq-validation";
        job.Spec =
            CalculationDefaults
                .CreateDefaultOptimizationFrequency();

        OptimizationResult optimization =
            CreateOptimizationResult(job.JobId);
        FrequencyAnalysisResult frequency =
            CreateFrequencyResult(job.JobId);
        frequency.Frequencies.Clear();
        frequency.MinimumFrequency = null;

        CalculationResultValidationRequest request =
            new CalculationResultValidationRequest();
        request.Job = job;
        request.OptimizationResult = optimization;
        request.FrequencyResult = frequency;

        CalculationValidationService service =
            new CalculationValidationService(
                new ICalculationResultValidator[]
                {
                    new OptimizationStepValidator(),
                    new OptimizationResultValidator(),
                    new OptimizationConvergenceValidator(),
                    new FrequencyAnalysisResultValidator()
                });
        CalculationResultValidationResult result =
            service.Validate(request);

        Assert.False(result.Passed);
        Assert.Contains(
            result.Report.Issues,
            issue =>
                issue.Code == "frequency.frequencies_present");
    }

    private static OptimizationResult CreateOptimizationResult(
        string jobId)
    {
        OptimizationResult result = new OptimizationResult();
        result.JobId = jobId;
        result.Program = CalculationDefaults.DefaultProgram;
        result.Method = CalculationDefaults.DefaultMethod;
        result.Basis = CalculationDefaults.DefaultBasis;
        result.NormalTermination = true;
        result.OptimizationConverged = true;
        result.StepCount = 2;
        result.FinalEnergy = -76.2;

        CanonicalGeometry geometry = new CanonicalGeometry();
        CanonicalAtom atom = new CanonicalAtom();
        atom.Index = 1;
        atom.Element = "O";
        geometry.Atoms.Add(atom);
        result.FinalGeometry = geometry;

        OptimizationConvergence convergence =
            new OptimizationConvergence();
        convergence.Converged = true;
        convergence.MaximumForce = 0.0001;
        convergence.MaximumForceThreshold = 0.00045;
        convergence.MaximumForceConverged = true;
        convergence.RmsForce = 0.00004;
        convergence.RmsForceThreshold = 0.0003;
        convergence.RmsForceConverged = true;
        convergence.MaximumDisplacement = 0.0003;
        convergence.MaximumDisplacementThreshold = 0.0018;
        convergence.MaximumDisplacementConverged = true;
        convergence.RmsDisplacement = 0.0001;
        convergence.RmsDisplacementThreshold = 0.0012;
        convergence.RmsDisplacementConverged = true;
        result.FinalConvergence = convergence;

        OptimizationStep first = new OptimizationStep();
        first.StepIndex = 1;
        first.Geometry = geometry;
        first.Energy = -76.1;
        first.ScfConverged = true;
        first.ScfIterations = 5;
        first.Convergence = convergence;
        result.Trajectory.Add(first);

        OptimizationStep second = new OptimizationStep();
        second.StepIndex = 2;
        second.Geometry = geometry;
        second.Energy = -76.2;
        second.ScfConverged = true;
        second.ScfIterations = 5;
        second.Convergence = convergence;
        result.Trajectory.Add(second);
        return result;
    }

    private static FrequencyAnalysisResult CreateFrequencyResult(
        string jobId)
    {
        FrequencyAnalysisResult result =
            new FrequencyAnalysisResult();
        result.JobId = jobId;
        result.Program = CalculationDefaults.DefaultProgram;
        result.Method = CalculationDefaults.DefaultMethod;
        result.Basis = CalculationDefaults.DefaultBasis;
        result.NormalTermination = true;
        result.Frequencies.Add(100.0);
        result.MinimumFrequency = 100.0;
        result.IsMinimum = true;
        return result;
    }

    private static async Task RegisterAsync<T>(
        SkillRegistry registry,
        string skillId,
        T output,
        List<string> order)
        where T : class
    {
        await registry.RegisterAsync(
            new RecordingSkill(skillId, output, order));
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
