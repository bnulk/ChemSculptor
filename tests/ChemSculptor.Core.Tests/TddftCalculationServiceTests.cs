using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Core.Tests;

/// <summary>TDDFT 单点工作流服务测试。</summary>
public class TddftCalculationServiceTests
{
    /// <summary>验证服务执行 TDDFT 单点工作流并保留结果。</summary>
    [Fact]
    public async Task RunsTddftWorkflowAndStoresResult()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);

            TddftCalculationResult tddftResult =
                new TddftCalculationResult();
            tddftResult.NormalTermination = true;
            TddftExcitedState state = new TddftExcitedState();
            state.StateIndex = 1;
            state.WavelengthNm = 300.0;
            state.OscillatorStrength = 0.5;
            tddftResult.ExcitedStates.Add(state);

            SkillRegistry skills = new SkillRegistry();
            await skills.RegisterAsync(
                new RecordingTddftSkill(tddftResult));
            WorkflowEngine workflowEngine = new WorkflowEngine(
                skills,
                new InMemoryEventBus(),
                new InMemoryWorkflowRepository(),
                new AllowAllRuleEngine(),
                new PassThroughValidationGate(),
                new InMemoryCaseMemory());

            TddftCalculationService service =
                new TddftCalculationService(
                    workspace,
                    new TestQuantumProgramAdapterRegistry(),
                    repository,
                    workflowEngine);
            TddftCalculationRequest request =
                new TddftCalculationRequest();
            request.SessionId = "session-tddft-service";
            request.Goal = "吸收光谱 TDDFT";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";
            request.ResolvedSpec =
                CalculationDefaults.CreateDefaultTddftSinglePoint();
            request.ResolvedSpec.Multiplicity = 1;

            TddftSubmissionResult result =
                await service.SubmitAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Job);
            Assert.NotNull(result.Result);
            Assert.Equal(
                CalculationJobState.Validated,
                result.Job.State);
            Assert.Single(result.Result.ExcitedStates);

            CalculationJob? savedJob =
                await service.GetJobAsync(result.Job.JobId);
            TddftCalculationResult? savedResult =
                await service.GetResultAsync(result.Job.JobId);
            Assert.NotNull(savedJob);
            Assert.NotNull(savedResult);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private sealed class RecordingTddftSkill : ISkill
    {
        private readonly TddftCalculationResult _result;
        private readonly List<string> _capabilities;

        public RecordingTddftSkill(
            TddftCalculationResult result)
        {
            _result = result;
            _capabilities = new List<string>();
            _capabilities.Add("test.tddft");
        }

        public string Name
        {
            get
            {
                return CalculationSkillIds
                    .CalculationTddftSinglePoint;
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
            TddftSinglePointCalculationSkillResult skillResult =
                new TddftSinglePointCalculationSkillResult();
            skillResult.Succeeded = true;
            skillResult.Passed = true;
            skillResult.Result = _result;

            TaskResult result = new TaskResult();
            result.WorkflowId = request.WorkflowId;
            result.NodeId = request.NodeId;
            result.Succeeded = true;
            result.Output = SkillJson.Serialize(skillResult);
            return Task.FromResult(result);
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
