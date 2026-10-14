using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.TddftCalculation;

namespace ChemSculptor.Core.Tests;

/// <summary>TDDFT 单点计算 Skill 测试。</summary>
public class TddftCalculationSkillTests
{
    /// <summary>验证 Skill 按原子能力顺序完成一次 TDDFT 计算。</summary>
    [Fact]
    public async Task RunsAtomicSkillsAndReturnsTddftResult()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-tddft-skill";
        job.WorkflowId = job.JobId;
        job.Spec =
            CalculationDefaults.CreateDefaultTddftSinglePoint();

        TddftCalculationResult tddftResult =
            new TddftCalculationResult();
        tddftResult.JobId = job.JobId;
        tddftResult.NormalTermination = true;
        TddftExcitedState state = new TddftExcitedState();
        state.StateIndex = 1;
        state.EnergyEv = 2.3061;
        tddftResult.ExcitedStates.Add(state);

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
        extraction.TddftResult = tddftResult;

        List<string> order = new List<string>();
        SkillRegistry registry = new SkillRegistry();
        Register(
            registry,
            CalculationSkillIds.CalculationInputPreparation,
            input,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationSubmission,
            submission,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationWait,
            wait,
            order);
        Register(
            registry,
            CalculationSkillIds.CalculationResultExtraction,
            extraction,
            order);

        TddftSinglePointCalculationSkill skill =
            new TddftSinglePointCalculationSkill(registry);
        CalculationInputGenerationRequest request =
            new CalculationInputGenerationRequest();
        request.Job = job;
        request.Spec = job.Spec;
        request.CoordinateText = "O 0.0 0.0 0.0";

        TaskRequest taskRequest = new TaskRequest();
        taskRequest.WorkflowId = job.WorkflowId;
        taskRequest.NodeId = "tddft-single-point";
        taskRequest.SkillId = skill.Name;
        taskRequest.Inputs["request"] =
            SkillJson.Serialize(request);

        TaskResult taskResult =
            await skill.ExecuteAsync(taskRequest);
        TddftSinglePointCalculationSkillResult result =
            SkillJson.Deserialize<
                TddftSinglePointCalculationSkillResult>(
                    taskResult.Output!);

        Assert.True(result.Succeeded);
        Assert.True(result.Passed);
        Assert.Single(result.Result.ExcitedStates);
        Assert.Equal(4, result.Steps.Count);
        Assert.Equal(
            new string[]
            {
                CalculationSkillIds.CalculationInputPreparation,
                CalculationSkillIds.CalculationSubmission,
                CalculationSkillIds.CalculationWait,
                CalculationSkillIds.CalculationResultExtraction
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
        registry.RegisterAsync(
            new RecordingSkill(skillId, output!, order))
            .GetAwaiter()
            .GetResult();
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
