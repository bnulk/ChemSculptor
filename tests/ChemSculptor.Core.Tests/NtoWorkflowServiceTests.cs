using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Core.Tests;

/// <summary>NTO Workflow 编排测试。</summary>
public class NtoWorkflowServiceTests
{
    /// <summary>验证默认选择会为最多五个状态调用单状态 NTO Skill。</summary>
    [Fact]
    public async Task RunsSelectedNtoTransitions()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationJob sourceJob = CreateSourceJob(root);
            TddftCalculationResult sourceResult =
                new TddftCalculationResult();
            sourceResult.NormalTermination = true;
            sourceResult.ExcitedStates.AddRange(
                new TddftExcitedState[]
                {
                    CreateState(1, 0.10),
                    CreateState(2, 0.20),
                    CreateState(3, 0.30),
                    CreateState(4, 0.40),
                    CreateState(5, 0.50),
                    CreateState(6, 0.60)
                });

            RecordingTddftService tddftService =
                new RecordingTddftService(
                    sourceJob,
                    sourceResult);
            RecordingNtoSkill ntoSkill =
                new RecordingNtoSkill();
            SkillRegistry registry = new SkillRegistry();
            await registry.RegisterAsync(ntoSkill);
            NtoWorkflowService service =
                new NtoWorkflowService(
                    tddftService,
                    registry);

            NtoRequest request = new NtoRequest();
            request.SessionId = "session-nto";
            request.Goal = "NTO";
            request.SourceTddftJobId = sourceJob.JobId;
            request.SelectionMode =
                NtoSelectionMode.Default;

            NtoWorkflowResult result =
                await service.SubmitAsync(request);

            Assert.True(result.Succeeded);
            Assert.Equal(
                NtoWorkflowStatus.Completed,
                result.Status);
            Assert.Equal(5, result.SelectedStates.Count);
            Assert.Equal(5, result.TransitionResults.Count);
            Assert.Equal(5, ntoSkill.CallCount);
            Assert.All(
                result.TransitionResults,
                transition => Assert.True(transition.Succeeded));
            Assert.All(
                result.SelectedStates,
                state => Assert.False(
                    string.IsNullOrWhiteSpace(state.StateId)));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证没有正 f 状态时返回正常空结果且不调用 NTO Skill。</summary>
    [Fact]
    public async Task NoPositiveStateDoesNotCallNtoSkill()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationJob sourceJob = CreateSourceJob(root);
            TddftCalculationResult sourceResult =
                new TddftCalculationResult();
            sourceResult.NormalTermination = true;
            sourceResult.ExcitedStates.Add(
                CreateState(1, 0.0));
            RecordingTddftService tddftService =
                new RecordingTddftService(
                    sourceJob,
                    sourceResult);
            RecordingNtoSkill ntoSkill =
                new RecordingNtoSkill();
            SkillRegistry registry = new SkillRegistry();
            await registry.RegisterAsync(ntoSkill);
            NtoWorkflowService service =
                new NtoWorkflowService(
                    tddftService,
                    registry);

            NtoRequest request = new NtoRequest();
            request.SourceTddftJobId = sourceJob.JobId;
            NtoWorkflowResult result =
                await service.SubmitAsync(request);

            Assert.True(result.Succeeded);
            Assert.Equal(
                NtoWorkflowStatus
                    .NoPositiveOscillatorStrength,
                result.Status);
            Assert.Empty(result.SelectedStates);
            Assert.Empty(result.TransitionResults);
            Assert.Equal(0, ntoSkill.CallCount);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static CalculationJob CreateSourceJob(
        string root)
    {
        string inputPath = Path.Combine(
            root,
            "tddft.gjf");
        string checkpointPath = Path.Combine(
            root,
            "tddft.chk");
        File.WriteAllText(
            inputPath,
            "%chk=tddft.chk\n\n" +
            "#p CAM-B3LYP/6-31G* td(nstates=50)\n\n" +
            "tddft\n\n0 1\nO 0.0 0.0 0.0\n");
        File.WriteAllText(
            checkpointPath,
            "checkpoint");

        CalculationJob job = new CalculationJob();
        job.JobId = "job-nto-source";
        job.Spec =
            CalculationDefaults.CreateDefaultTddftSinglePoint();
        job.Spec.Multiplicity = 1;
        job.RunDirectory = root;
        job.InputFilePath = inputPath;
        job.SourceInputFilePath = inputPath;
        return job;
    }

    private static TddftExcitedState CreateState(
        int index,
        double oscillatorStrength)
    {
        TddftExcitedState state = new TddftExcitedState();
        state.StateIndex = index;
        state.StateLabel = "Singlet-" + index.ToString();
        state.WavelengthNm = 600.0 - index * 20.0;
        state.OscillatorStrength = oscillatorStrength;
        return state;
    }

    private sealed class RecordingTddftService
        : ITddftCalculationService
    {
        private readonly CalculationJob _job;
        private readonly TddftCalculationResult _result;

        public RecordingTddftService(
            CalculationJob job,
            TddftCalculationResult result)
        {
            _job = job;
            _result = result;
        }

        public Task<TddftSubmissionResult> SubmitAsync(
            TddftCalculationRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<CalculationJob?> GetJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CalculationJob?>(
                _job.JobId == jobId ? _job : null);
        }

        public Task<TddftCalculationResult?> GetResultAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TddftCalculationResult?>(
                _job.JobId == jobId ? _result : null);
        }
    }

    private sealed class RecordingNtoSkill : ISkill
    {
        private readonly List<string> _capabilities;

        public RecordingNtoSkill()
        {
            _capabilities = new List<string>();
            _capabilities.Add("test.nto");
        }

        public int CallCount { get; private set; }

        public string Name
        {
            get
            {
                return CalculationSkillIds
                    .CalculationNtoTransition;
            }
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
            CallCount++;
            NtoTransitionRequest input =
                SkillJson.Deserialize<NtoTransitionRequest>(
                    request.Inputs["request"]);
            NtoTransitionResult result =
                new NtoTransitionResult();
            result.Succeeded = true;
            result.Message = "测试 NTO 完成。";
            result.JobId =
                "job-nto-" +
                input.SelectedState.StateIndex.ToString();
            result.SourceJobId = input.SourceJob.JobId;
            result.StateIndex =
                input.SelectedState.StateIndex;
            result.StateLabel =
                input.SelectedState.StateLabel;
            result.NormalTermination = true;
            result.NtoFound = true;

            TaskResult taskResult = new TaskResult();
            taskResult.WorkflowId = request.WorkflowId;
            taskResult.NodeId = request.NodeId;
            taskResult.Succeeded = true;
            taskResult.Output = SkillJson.Serialize(result);
            return Task.FromResult(taskResult);
        }

        public Task<bool> HealthAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
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
}
