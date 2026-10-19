using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Conversation;
using ChemSculptor.InputProcessor;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 智能体编排服务测试。
/// </summary>
public class AgentServiceTests
{
    /// <summary>验证不支持的自然语言会返回错误。</summary>
    [Fact]
    public async Task UnsupportedMessageReturnsError()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service = CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-1";
            request.Text = "频率计算";
            request.CoordinateText = "O 0.0 0.0 0.0";

            AgentResult result = await service.HandleMessageAsync(request);

            Assert.False(result.IsSupported);
            Assert.Contains("只支持", result.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证“单点计算”会生成 Gaussian 输入文件。</summary>
    [Fact]
    public async Task SinglePointMessageGeneratesInputFile()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service = CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-2";
            request.Text = "单点计算";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result = await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal("Running", result.Status);
            Assert.Contains("output.log", result.OutputFilePath);
            Assert.Equal(result.JobId, workflowEngine.StartedJobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证优化对话会路由到优化服务。</summary>
    [Fact]
    public async Task OptimizationMessageRoutesToOptimizationService()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-optimization-agent";
            request.Text =
                "优化水分子使用 opt，maxcycles=200";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal(
                CalculationTaskType.Optimization,
                result.TaskType);
            Assert.Equal("200", optimizationService.LastRequest
                .ResolvedSpec!.ExtraOptions[
                    CalculationOptionKeys.OptimizationMaxCycles]);
            Assert.Equal(2, result.OptimizationStepCount);
            Assert.True(result.OptimizationConverged);
            Assert.Equal("job-optimization-agent-test", result.FinalJobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证“冻结非氢原子”会路由到优化并写入约束参数。</summary>
    [Fact]
    public async Task HydrogensOnlyMessageRoutesToOptimizationService()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-hydrogens-only-agent";
            request.Text = "冻结非氢原子";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal(
                CalculationTaskType.Optimization,
                result.TaskType);
            Assert.Equal(
                "HydrogensOnly",
                optimizationService.LastRequest.ResolvedSpec!
                    .ExtraOptions[
                        CalculationOptionKeys
                            .OptimizationConstraintMode]);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证联合优化频率对话会路由到联合计算服务。</summary>
    [Fact]
    public async Task OptimizationFrequencyMessageRoutesToService()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            RecordingOptimizationFrequencyService
                optimizationFrequencyService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService,
                out optimizationFrequencyService);

            AgentRequest request = new AgentRequest();
            request.SessionId =
                "session-optimization-frequency-agent";
            request.Text = "优化并做振动分析";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal(
                CalculationTaskType.OptimizationFrequency,
                result.TaskType);
            Assert.Equal(
                "job-optimization-frequency-agent",
                result.JobId);
            Assert.Equal(1, result.FrequencyCount);
            Assert.True(result.OptimizationConverged);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证“吸收光谱”会路由到吸收光谱工作流。</summary>
    [Fact]
    public async Task AbsorptionSpectrumMessageRoutesToWorkflow()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            RecordingAbsorptionSpectrumWorkflowService
                absorptionService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService,
                out absorptionService);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-absorption-agent";
            request.Text = "吸收光谱，不优化";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal(CalculationTaskType.TdDft, result.TaskType);
            Assert.Equal(
                "absorption-spectrum",
                result.WorkflowId);
            Assert.Equal(
                AbsorptionSpectrumMode.TddftOnly,
                absorptionService.LastRequest!.Mode);
            Assert.Equal(
                "job-tddft-absorption-agent",
                result.JobId);
            Assert.Equal(1, result.ExcitedStateCount);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证没有 TDDFT 源时 NTO 请求返回可执行提示。</summary>
    [Fact]
    public async Task NtoWithoutTddftSourceReturnsGuidance()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            RecordingAbsorptionSpectrumWorkflowService
                absorptionService;
            RecordingNtoWorkflowService ntoService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService,
                out absorptionService,
                out ntoService);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-nto-no-source";
            request.Text = "做 NTO";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal(CalculationTaskType.Nto, result.TaskType);
            Assert.Equal("nto", result.WorkflowId);
            Assert.Equal("NoTddftSource", result.Status);
            Assert.Contains("先完成 TDDFT", result.Message);
            Assert.Null(ntoService.LastRequest);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 NTO 使用当前会话最近一次 TDDFT 作业和波长范围。</summary>
    [Fact]
    public async Task NtoUsesLatestTddftJobAndWavelengthRange()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            RecordingAbsorptionSpectrumWorkflowService
                absorptionService;
            RecordingNtoWorkflowService ntoService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService,
                out absorptionService,
                out ntoService);
            string coordinates =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentRequest tddftRequest = new AgentRequest();
            tddftRequest.SessionId = "session-nto-language";
            tddftRequest.Text = "吸收光谱，不优化";
            tddftRequest.CoordinateText = coordinates;
            AgentResult tddftResult =
                await service.HandleMessageAsync(tddftRequest);

            AgentRequest ntoRequest = new AgentRequest();
            ntoRequest.SessionId = "session-nto-language";
            ntoRequest.Text = "NTO 300-500";
            ntoRequest.CoordinateText = coordinates;
            AgentResult ntoResult =
                await service.HandleMessageAsync(ntoRequest);

            Assert.True(tddftResult.IsSupported);
            Assert.True(ntoResult.IsSupported);
            Assert.NotNull(ntoService.LastRequest);
            Assert.Equal(
                "job-tddft-absorption-agent",
                ntoService.LastRequest!.SourceTddftJobId);
            Assert.Equal(
                NtoSelectionMode.WavelengthRange,
                ntoService.LastRequest.SelectionMode);
            Assert.Equal(
                300.0,
                ntoService.LastRequest.WavelengthMinNm);
            Assert.Equal(
                500.0,
                ntoService.LastRequest.WavelengthMaxNm);
            Assert.Equal("Completed", ntoResult.Status);
            Assert.Equal(1, ntoResult.NtoSelectedStateCount);
            Assert.Single(ntoResult.NtoJobIds);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证全部 NTO 无正 f 状态时返回正常空结果。</summary>
    [Fact]
    public async Task AllNtoWithoutPositiveStateReturnsEmptyResult()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            RecordingOptimizationService optimizationService;
            RecordingAbsorptionSpectrumWorkflowService
                absorptionService;
            RecordingNtoWorkflowService ntoService;
            AgentService service = CreateAgentService(
                root,
                out workflowEngine,
                out optimizationService,
                out absorptionService,
                out ntoService);
            ntoService.ReturnNoPositiveOscillatorStrength = true;
            string coordinates =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentRequest tddftRequest = new AgentRequest();
            tddftRequest.SessionId = "session-nto-all-empty";
            tddftRequest.Text = "吸收光谱，不优化";
            tddftRequest.CoordinateText = coordinates;
            await service.HandleMessageAsync(tddftRequest);

            AgentRequest ntoRequest = new AgentRequest();
            ntoRequest.SessionId = "session-nto-all-empty";
            ntoRequest.Text = "全部吸收的 NTO";
            ntoRequest.CoordinateText = coordinates;
            AgentResult ntoResult =
                await service.HandleMessageAsync(ntoRequest);

            Assert.True(ntoResult.IsSupported);
            Assert.Equal(
                "NoPositiveOscillatorStrength",
                ntoResult.Status);
            Assert.Equal(
                NtoSelectionMode.All,
                ntoService.LastRequest!.SelectionMode);
            Assert.Equal(0, ntoResult.NtoSelectedStateCount);
            Assert.Empty(ntoResult.NtoJobIds);
            Assert.Contains("没有", ntoResult.Message);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证激发态目标在当前阶段被明确拒绝。</summary>
    [Fact]
    public async Task ExcitedStateMessageIsNotImplemented()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service =
                CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-excited";
            request.Text = "S1 激发态单点计算";
            request.CoordinateText = "O 0.0 0.0 0.0";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.False(result.IsSupported);
            Assert.Contains("尚未实现激发态计算", result.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证指定自旋态会写入计算方案。</summary>
    [Fact]
    public async Task TargetSpinStateIsAppliedToSpec()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service =
                CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-triplet";
            request.Text = "三重态单点计算";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal(
                ElectronicStateObjective.TargetSpinState,
                workflowEngine.LastJob.Spec.ElectronicStateObjective);
            Assert.Equal(
                3,
                workflowEngine.LastJob.Spec.Multiplicity);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证参数问询会阻断提交，并在回答后继续提交。</summary>
    [Fact]
    public async Task ParameterClarificationContinuesInNextMessage()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service =
                CreateAgentService(root, out workflowEngine);
            string coordinates =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentRequest firstRequest = new AgentRequest();
            firstRequest.SessionId = "session-parameters";
            firstRequest.Text = "单点计算，对称性";
            firstRequest.CoordinateText = coordinates;

            AgentResult first =
                await service.HandleMessageAsync(firstRequest);

            Assert.True(first.IsSupported);
            Assert.True(first.RequiresUserAction);
            Assert.Equal("NeedsClarification", first.Status);
            Assert.Empty(first.JobId);
            Assert.Single(first.Questions);
            Assert.Contains(
                CalculationOptionKeys.Symmetry,
                first.Questions[0].QuestionId);

            AgentRequest secondRequest = new AgentRequest();
            secondRequest.SessionId = "session-parameters";
            secondRequest.Text = "关闭对称性";
            secondRequest.CoordinateText = coordinates;

            AgentResult second =
                await service.HandleMessageAsync(secondRequest);

            Assert.True(second.IsSupported);
            Assert.False(second.RequiresUserAction);
            Assert.Equal("Running", second.Status);
            Assert.NotEmpty(second.JobId);
            Assert.Equal(
                "Ignore",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.Symmetry]);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证一条带参数的消息会生成已解析计算方案。</summary>
    [Fact]
    public async Task ChatMessageWithParametersProducesResolvedSpec()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service =
                CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-parameter-message";
            request.Text =
                "单点计算，CAM-B3LYP/6-31G*，加 GD3BJ 色散校正，" +
                "nosymm，SCF 最多 300 次";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.False(result.RequiresUserAction);
            Assert.Equal("Running", result.Status);
            Assert.Equal(
                "GD3BJ",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.DispersionCorrection]);
            Assert.Equal(
                "Ignore",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.Symmetry]);
            Assert.Equal(
                "300",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.ScfIterationLimit]);
            Assert.Contains(
                result.AppliedParameters,
                parameter =>
                    parameter.Key
                        == CalculationOptionKeys.DispersionCorrection
                    && parameter.Value == "GD3BJ");
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证客户端直接输入 Gaussian route 行会被接受。</summary>
    [Fact]
    public async Task GaussianRouteLineMessageIsAccepted()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service =
                CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-route-line";
            request.Text =
                "#p cam-b3lyp/6-31G* em=GD3BJ scfcyc=200 nosymm";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result =
                await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.False(result.RequiresUserAction);
            Assert.Equal("Running", result.Status);
            Assert.Equal(
                "GD3BJ",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.DispersionCorrection]);
            Assert.Equal(
                "Ignore",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.Symmetry]);
            Assert.Equal(
                "200",
                workflowEngine.LastJob.Spec.ExtraOptions[
                    CalculationOptionKeys.ScfIterationLimit]);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine)
    {
        RecordingOptimizationService optimizationService;
        return CreateAgentService(
            root,
            out workflowEngine,
            out optimizationService);
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine,
        out RecordingOptimizationService optimizationService)
    {
        RecordingAbsorptionSpectrumWorkflowService
            absorptionService;
        return CreateAgentService(
            root,
            out workflowEngine,
            out optimizationService,
            out absorptionService);
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine,
        out RecordingOptimizationService optimizationService,
        out RecordingOptimizationFrequencyService
            optimizationFrequencyService)
    {
        RecordingAbsorptionSpectrumWorkflowService
            absorptionService;
        return CreateAgentService(
            root,
            out workflowEngine,
            out optimizationService,
            out absorptionService,
            out optimizationFrequencyService);
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine,
        out RecordingOptimizationService optimizationService,
        out RecordingAbsorptionSpectrumWorkflowService
            absorptionService)
    {
        RecordingOptimizationFrequencyService
            optimizationFrequencyService;
        return CreateAgentService(
            root,
            out workflowEngine,
            out optimizationService,
            out absorptionService,
            out optimizationFrequencyService);
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine,
        out RecordingOptimizationService optimizationService,
        out RecordingAbsorptionSpectrumWorkflowService
            absorptionService,
        out RecordingOptimizationFrequencyService
            optimizationFrequencyService)
    {
        RecordingNtoWorkflowService ntoService;
        return CreateAgentService(
            root,
            out workflowEngine,
            out optimizationService,
            out absorptionService,
            out optimizationFrequencyService,
            out ntoService);
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine,
        out RecordingOptimizationService optimizationService,
        out RecordingAbsorptionSpectrumWorkflowService
            absorptionService,
        out RecordingNtoWorkflowService ntoService)
    {
        RecordingOptimizationFrequencyService
            optimizationFrequencyService;
        return CreateAgentService(
            root,
            out workflowEngine,
            out optimizationService,
            out absorptionService,
            out optimizationFrequencyService,
            out ntoService);
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine,
        out RecordingOptimizationService optimizationService,
        out RecordingAbsorptionSpectrumWorkflowService
            absorptionService,
        out RecordingOptimizationFrequencyService
            optimizationFrequencyService,
        out RecordingNtoWorkflowService ntoService)
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = root;

        WorkspaceManager workspace = new WorkspaceManager(options);
        TestQuantumProgramAdapterRegistry adapterRegistry =
            new TestQuantumProgramAdapterRegistry();
        FileCalculationRepository calculationRepository =
            new FileCalculationRepository(workspace);
        RecordingComputeBackend backend = new RecordingComputeBackend();
        workflowEngine = new RecordingWorkflowEngine();

        SinglePointCalculationService singlePointService =
            new SinglePointCalculationService(
                workspace,
                backend,
                adapterRegistry,
                calculationRepository,
                workflowEngine);
        RuleBasedTaskInterpreter interpreter = new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationService conversationService = new ConversationService(interpreter, repository);
        CalculationParameterConversationService parameterService =
            new CalculationParameterConversationService(
                repository,
                new RuleBasedCalculationParameterExtractor(),
                new CalculationParameterResolver());

        optimizationService = new RecordingOptimizationService();
        absorptionService =
            new RecordingAbsorptionSpectrumWorkflowService();
        optimizationFrequencyService =
            new RecordingOptimizationFrequencyService();
        ntoService =
            new RecordingNtoWorkflowService();
        return new AgentService(
            conversationService,
            singlePointService,
            optimizationService,
            parameterService,
            new GeometryTextParser(),
            absorptionService,
            optimizationFrequencyService,
            ntoService);
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
        public string Name
        {
            get { return "recording"; }
        }

        public CalculationJob LastJob { get; private set; } = new CalculationJob();

        public CalculationExecutionContext LastContext { get; private set; } =
            new CalculationExecutionContext();

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastJob = job;
            LastContext = context;
            return Task.FromResult(job.JobId);
        }

        public Task<CalculationJobState> GetStatusAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CalculationJobState.Running);
        }

        public Task CancelAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingWorkflowEngine : ISinglePointWorkflowEngine
    {
        public string StartedJobId { get; private set; } = string.Empty;

        public CalculationJob LastJob { get; private set; } =
            new CalculationJob();

        public Task<WorkflowRun> StartAsync(
            WorkflowDefinition definition,
            IReadOnlyDictionary<string, string> inputs,
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            StartedJobId = job.JobId;
            LastJob = job;

            WorkflowRun run = new WorkflowRun();
            run.Id = definition.Id;
            run.Definition = definition;
            run.State = WorkflowState.Running;
            return Task.FromResult(run);
        }
    }

    private sealed class RecordingOptimizationService
        : IOptimizationCalculationService
    {
        public OptimizationRequest LastRequest { get; private set; } =
            new OptimizationRequest();

        public Task<OptimizationSubmissionResult> SubmitAsync(
            OptimizationRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            OptimizationSubmissionResult result =
                new OptimizationSubmissionResult();
            CalculationJob job = new CalculationJob();
            job.JobId = "job-optimization-agent-test";
            job.State = CalculationJobState.Validated;
            job.SourceInputFilePath = Path.Combine(
                Path.GetTempPath(),
                "optimization.gjf");
            job.OutputFilePath = Path.Combine(
                Path.GetTempPath(),
                "optimization.log");

            OptimizationResult optimizationResult =
                new OptimizationResult();
            optimizationResult.JobId = job.JobId;
            optimizationResult.OptimizationConverged = true;
            optimizationResult.StepCount = 2;
            optimizationResult.FinalEnergy = -76.2;

            OptimizationConvergence convergence =
                new OptimizationConvergence();
            convergence.MaximumForce = 0.0001;
            convergence.RmsForce = 0.00004;
            convergence.MaximumDisplacement = 0.0003;
            convergence.RmsDisplacement = 0.0001;
            optimizationResult.FinalConvergence = convergence;

            result.Succeeded = true;
            result.Job = job;
            result.Result = optimizationResult;
            result.FinalJobId = job.JobId;
            result.Message = "测试优化已完成。";
            return Task.FromResult(result);
        }

        public Task<CalculationJob?> GetJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CalculationJob?>(null);
        }

        public Task<OptimizationResult?> GetResultAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<OptimizationResult?>(null);
        }

        public Task<CalculationValidationReport?> GetValidationAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CalculationValidationReport?>(null);
        }

        public Task<bool> CancelAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class RecordingAbsorptionSpectrumWorkflowService
        : IAbsorptionSpectrumWorkflowService
    {
        public AbsorptionSpectrumRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<AbsorptionSpectrumSubmissionResult> SubmitAsync(
            AbsorptionSpectrumRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            AbsorptionSpectrumSubmissionResult result =
                new AbsorptionSpectrumSubmissionResult();
            CalculationJob job = new CalculationJob();
            job.JobId = "job-tddft-absorption-agent";
            job.State = CalculationJobState.Validated;
            job.SourceInputFilePath = Path.Combine(
                Path.GetTempPath(),
                "absorption.gjf");
            job.OutputFilePath = Path.Combine(
                Path.GetTempPath(),
                "absorption.log");

            TddftCalculationResult tddft =
                new TddftCalculationResult();
            tddft.JobId = job.JobId;
            tddft.NormalTermination = true;
            TddftExcitedState state = new TddftExcitedState();
            state.StateIndex = 1;
            state.WavelengthNm = 300.0;
            state.OscillatorStrength = 0.5;
            tddft.ExcitedStates.Add(state);

            TddftSubmissionResult tddftSubmission =
                new TddftSubmissionResult();
            tddftSubmission.Succeeded = true;
            tddftSubmission.Job = job;
            tddftSubmission.Result = tddft;

            result.Succeeded = true;
            result.Mode = request.Mode;
            result.Tddft = tddftSubmission;
            result.Message = "吸收光谱测试完成。";
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingOptimizationFrequencyService
        : IOptimizationFrequencyCalculationService
    {
        public OptimizationFrequencyRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<OptimizationFrequencySubmissionResult> SubmitAsync(
            OptimizationFrequencyRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            CalculationJob job = new CalculationJob();
            job.JobId = "job-optimization-frequency-agent";
            job.State = CalculationJobState.Validated;
            job.SourceInputFilePath = Path.Combine(
                Path.GetTempPath(),
                "optimization-frequency.gjf");
            job.OutputFilePath = Path.Combine(
                Path.GetTempPath(),
                "optimization-frequency.log");

            OptimizationResult optimization =
                new OptimizationResult();
            optimization.JobId = job.JobId;
            optimization.NormalTermination = true;
            optimization.OptimizationConverged = true;
            optimization.StepCount = 2;
            optimization.FinalEnergy = -76.2;
            OptimizationConvergence convergence =
                new OptimizationConvergence();
            convergence.MaximumForce = 0.0001;
            convergence.RmsForce = 0.00004;
            convergence.MaximumDisplacement = 0.0003;
            convergence.RmsDisplacement = 0.0001;
            optimization.FinalConvergence = convergence;

            FrequencyAnalysisResult frequency =
                new FrequencyAnalysisResult();
            frequency.JobId = job.JobId;
            frequency.NormalTermination = true;
            frequency.Frequencies.Add(100.0);
            frequency.MinimumFrequency = 100.0;
            frequency.IsMinimum = true;

            OptimizationFrequencyCalculationSkillResult combined =
                new OptimizationFrequencyCalculationSkillResult();
            combined.Succeeded = true;
            combined.Passed = true;
            combined.Job = job;
            combined.OptimizationResult = optimization;
            combined.FrequencyResult = frequency;

            OptimizationFrequencySubmissionResult result =
                new OptimizationFrequencySubmissionResult();
            result.Succeeded = true;
            result.Job = job;
            result.Result = combined;
            result.Message = "联合优化频率已完成。";
            return Task.FromResult(result);
        }

        public Task<CalculationJob?> GetJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CalculationJob?>(null);
        }

        public Task<OptimizationFrequencyCalculationSkillResult?>
            GetResultAsync(
                string jobId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                OptimizationFrequencyCalculationSkillResult?>(null);
        }
    }

    private sealed class RecordingNtoWorkflowService
        : INtoWorkflowService
    {
        public NtoRequest? LastRequest { get; private set; }

        public bool ReturnNoPositiveOscillatorStrength
        {
            get;
            set;
        }

        public Task<NtoWorkflowResult> SubmitAsync(
            NtoRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            NtoWorkflowResult result =
                new NtoWorkflowResult();
            result.Succeeded = true;
            result.Status = NtoWorkflowStatus.Completed;
            result.SourceTddftJobId =
                request.SourceTddftJobId;
            result.SelectionMode = request.SelectionMode;

            if (ReturnNoPositiveOscillatorStrength)
            {
                result.Status =
                    NtoWorkflowStatus
                        .NoPositiveOscillatorStrength;
                result.Message =
                    "没有可计算的激发态。";
                return Task.FromResult(result);
            }

            result.Message = "NTO 测试完成。";

            NtoSelectedState state =
                new NtoSelectedState();
            state.StateId = "tddft:test:state:1";
            state.StateIndex = 1;
            state.StateLabel = "Singlet-1";
            state.WavelengthNm = 400.0;
            state.OscillatorStrength = 0.5;
            result.SelectedStates.Add(state);

            NtoTransitionResult transition =
                new NtoTransitionResult();
            transition.Succeeded = true;
            transition.JobId = "job-nto-language-test";
            transition.SourceJobId =
                request.SourceTddftJobId;
            transition.StateIndex = state.StateIndex;
            transition.StateLabel = state.StateLabel;
            transition.NormalTermination = true;
            transition.NtoFound = true;
            result.TransitionResults.Add(transition);
            return Task.FromResult(result);
        }
    }
}
