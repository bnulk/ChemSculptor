using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor.GeometryIntake;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.OptimizationCalculation;

namespace ChemSculptor.Core.Tests;

/// <summary>几何优化复合 Skill 测试。</summary>
public class OptimizationCalculationSkillTests
{
    /// <summary>验证复合 Skill 完成一次优化并保留完整轨迹。</summary>
    [Fact]
    public async Task RunsAtomicSkillsAndReturnsOptimizationContract()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-optimization-skill";
        job.WorkflowId = job.JobId;
        job.Spec = CalculationDefaults.CreateDefaultOptimization();

        OptimizationResult optimizationResult =
            CreateOptimizationResult(job.JobId);
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
        extractionResult.OptimizationResult = optimizationResult;

        CalculationValidationReport report =
            new CalculationValidationReport();
        report.Passed = true;
        report.Status = CalculationValidationStatus.Passed;
        report.Summary = "优化验证通过。";

        OptimizationValidationSkillResult validationResult =
            new OptimizationValidationSkillResult();
        validationResult.Passed = true;
        validationResult.Job = job;
        validationResult.Result = optimizationResult;
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
            CalculationSkillIds.CalculationOptimizationValidation,
            validationResult,
            order);

        OptimizationCalculationSkill skill =
            new OptimizationCalculationSkill(registry);
        CalculationInputGenerationRequest inputRequest =
            new CalculationInputGenerationRequest();
        inputRequest.Job = job;
        inputRequest.Spec = job.Spec;
        inputRequest.CoordinateText = "O 0.0 0.0 0.0";

        TaskRequest taskRequest = new TaskRequest();
        taskRequest.WorkflowId = job.WorkflowId;
        taskRequest.NodeId = "optimization";
        taskRequest.SkillId = skill.Name;
        taskRequest.Inputs["request"] =
            SkillJson.Serialize(inputRequest);

        TaskResult taskResult =
            await skill.ExecuteAsync(taskRequest);
        OptimizationCalculationSkillResult result =
            SkillJson.Deserialize<
                OptimizationCalculationSkillResult>(
                    taskResult.Output!);

        Assert.True(taskResult.Succeeded);
        Assert.True(result.Succeeded);
        Assert.True(result.Passed);
        Assert.Equal(job.JobId, result.Job.JobId);
        Assert.Equal(2, result.Result.StepCount);
        Assert.Equal(2, result.Result.Trajectory.Count);
        Assert.Equal(
            -76.200000,
            result.Result.FinalEnergy);
        Assert.Equal(5, result.Steps.Count);
        Assert.Equal(
            CalculationSkillIds.CalculationOptimizationValidation,
            result.Steps[4].SkillId);
        Assert.Equal(
            new string[]
            {
                CalculationSkillIds.CalculationInputPreparation,
                CalculationSkillIds.CalculationSubmission,
                CalculationSkillIds.CalculationWait,
                CalculationSkillIds.CalculationResultExtraction,
                CalculationSkillIds.CalculationOptimizationValidation
            },
            order);
    }

    private static OptimizationResult CreateOptimizationResult(
        string jobId)
    {
        OptimizationResult result =
            new OptimizationResult();
        result.JobId = jobId;
        result.Program = CalculationDefaults.DefaultProgram;
        result.Method = CalculationDefaults.DefaultMethod;
        result.Basis = CalculationDefaults.DefaultBasis;
        result.NormalTermination = true;
        result.OptimizationConverged = true;
        result.StepCount = 2;
        result.FinalEnergy = -76.200000;

        OptimizationConvergence convergence =
            new OptimizationConvergence();
        convergence.Converged = true;
        convergence.MaximumForce = 0.000100;
        convergence.RmsForce = 0.000040;
        convergence.MaximumDisplacement = 0.000300;
        convergence.RmsDisplacement = 0.000100;
        result.FinalConvergence = convergence;

        CanonicalGeometry geometry =
            new CanonicalGeometry();
        CanonicalAtom atom = new CanonicalAtom();
        atom.Index = 1;
        atom.Element = "O";
        geometry.Atoms.Add(atom);
        result.FinalGeometry = geometry;

        OptimizationStep firstStep = new OptimizationStep();
        firstStep.StepIndex = 1;
        firstStep.Energy = -76.100000;
        firstStep.ScfConverged = true;
        firstStep.ScfIterations = 7;
        firstStep.Geometry = geometry;
        result.Trajectory.Add(firstStep);

        OptimizationStep secondStep = new OptimizationStep();
        secondStep.StepIndex = 2;
        secondStep.Energy = -76.200000;
        secondStep.ScfConverged = true;
        secondStep.ScfIterations = 5;
        secondStep.Geometry = geometry;
        secondStep.Convergence = convergence;
        result.Trajectory.Add(secondStep);

        return result;
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
