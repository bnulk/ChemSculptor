using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Core.Tests;

/// <summary>吸收光谱工作流编排测试。</summary>
public class AbsorptionSpectrumWorkflowServiceTests
{
    /// <summary>验证“不优化”模式不会调用几何优化服务。</summary>
    [Fact]
    public async Task TddftOnlyModeSkipsOptimization()
    {
        RecordingOptimizationService optimization =
            new RecordingOptimizationService();
        RecordingTddftService tddft =
            new RecordingTddftService();
        AbsorptionSpectrumWorkflowService service =
            new AbsorptionSpectrumWorkflowService(
                optimization,
                tddft);

        AbsorptionSpectrumSubmissionResult result =
            await service.SubmitAsync(
                CreateRequest(
                    AbsorptionSpectrumMode.TddftOnly));

        Assert.True(result.Succeeded);
        Assert.Equal(0, optimization.CallCount);
        Assert.Equal(1, tddft.CallCount);
        Assert.Equal(
            "O 0.000000 0.000000 0.000000",
            tddft.LastRequest!.CoordinateText);
    }

    /// <summary>验证默认模式会把优化后的结构传给 TDDFT。</summary>
    [Fact]
    public async Task OptimizeModePassesFinalGeometryToTddft()
    {
        RecordingOptimizationService optimization =
            new RecordingOptimizationService();
        RecordingTddftService tddft =
            new RecordingTddftService();
        AbsorptionSpectrumWorkflowService service =
            new AbsorptionSpectrumWorkflowService(
                optimization,
                tddft);

        AbsorptionSpectrumSubmissionResult result =
            await service.SubmitAsync(
                CreateRequest(
                    AbsorptionSpectrumMode.OptimizeThenTddft));

        Assert.True(result.Succeeded);
        Assert.Equal(1, optimization.CallCount);
        Assert.Equal(1, tddft.CallCount);
        Assert.Contains(
            "O 0.000000 0.000000 1.000000",
            tddft.LastRequest!.CoordinateText);
    }

    /// <summary>验证冻结模式会把 HydrogensOnly 传给优化阶段。</summary>
    [Fact]
    public async Task ConstrainedModeSetsHydrogensOnlyConstraint()
    {
        RecordingOptimizationService optimization =
            new RecordingOptimizationService();
        RecordingTddftService tddft =
            new RecordingTddftService();
        AbsorptionSpectrumWorkflowService service =
            new AbsorptionSpectrumWorkflowService(
                optimization,
                tddft);

        AbsorptionSpectrumSubmissionResult result =
            await service.SubmitAsync(
                CreateRequest(
                    AbsorptionSpectrumMode
                        .ConstrainedOptimizationThenTddft));

        Assert.True(result.Succeeded);
        Assert.Equal(
            "HydrogensOnly",
            optimization.LastRequest!.ResolvedSpec!
                .ExtraOptions[
                    CalculationOptionKeys
                        .OptimizationConstraintMode]);
    }

    /// <summary>验证失败时会进入吸收光谱恢复扩展框架。</summary>
    [Fact]
    public async Task FailureCreatesRecoveryFrameworkPlan()
    {
        RecordingOptimizationService optimization =
            new RecordingOptimizationService();
        optimization.ShouldFail = true;
        RecordingTddftService tddft =
            new RecordingTddftService();
        AbsorptionSpectrumWorkflowService service =
            new AbsorptionSpectrumWorkflowService(
                optimization,
                tddft);

        AbsorptionSpectrumSubmissionResult result =
            await service.SubmitAsync(
                CreateRequest(
                    AbsorptionSpectrumMode.OptimizeThenTddft));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AbsorptionSpectrumFailureStage.Optimization,
            result.FailureStage);
        Assert.Equal(
            AbsorptionSpectrumAnomalyCodes.OptimizationFailed,
            result.FailureCode);
        Assert.NotNull(result.RecoveryPlan);
        Assert.False(result.RecoveryPlan!.PlanCreated);
    }

    private static AbsorptionSpectrumRequest CreateRequest(
        AbsorptionSpectrumMode mode)
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultTddftSinglePoint();
        spec.Charge = 0;
        spec.Multiplicity = 1;

        AbsorptionSpectrumRequest request =
            new AbsorptionSpectrumRequest();
        request.SessionId = "session-absorption";
        request.Goal = "吸收光谱";
        request.CoordinateText =
            "O 0.000000 0.000000 0.000000";
        request.Mode = mode;
        request.TddftSpec = spec;
        return request;
    }

    private sealed class RecordingOptimizationService
        : IOptimizationCalculationService
    {
        public int CallCount { get; private set; }

        public OptimizationRequest? LastRequest
        {
            get;
            private set;
        }

        public bool ShouldFail { get; set; }

        public Task<OptimizationSubmissionResult> SubmitAsync(
            OptimizationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            OptimizationSubmissionResult result =
                new OptimizationSubmissionResult();

            if (ShouldFail)
            {
                result.Error = "测试优化失败。";
                return Task.FromResult(result);
            }

            CalculationJob job = new CalculationJob();
            job.JobId = "job-optimization-absorption";
            job.State = CalculationJobState.Validated;

            OptimizationResult optimization =
                new OptimizationResult();
            optimization.JobId = job.JobId;
            optimization.NormalTermination = true;
            optimization.OptimizationConverged = true;
            optimization.StepCount = 1;
            optimization.FinalEnergy = -76.2;
            optimization.FinalGeometry =
                CreateOptimizedGeometry();

            result.Succeeded = true;
            result.Job = job;
            result.Result = optimization;
            result.FinalJobId = job.JobId;
            result.Message = "优化完成。";
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

    private sealed class RecordingTddftService
        : ITddftCalculationService
    {
        public int CallCount { get; private set; }

        public TddftCalculationRequest? LastRequest
        {
            get;
            private set;
        }

        public Task<TddftSubmissionResult> SubmitAsync(
            TddftCalculationRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            CalculationJob job = new CalculationJob();
            job.JobId = "job-tddft-absorption";
            job.State = CalculationJobState.Validated;

            TddftCalculationResult tddft =
                new TddftCalculationResult();
            tddft.JobId = job.JobId;
            tddft.NormalTermination = true;
            TddftExcitedState state = new TddftExcitedState();
            state.StateIndex = 1;
            state.WavelengthNm = 300.0;
            state.OscillatorStrength = 0.5;
            tddft.ExcitedStates.Add(state);

            TddftSubmissionResult result =
                new TddftSubmissionResult();
            result.Succeeded = true;
            result.Job = job;
            result.Result = tddft;
            result.Message = "TDDFT 完成。";
            return Task.FromResult(result);
        }

        public Task<CalculationJob?> GetJobAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<CalculationJob?>(null);
        }

        public Task<TddftCalculationResult?> GetResultAsync(
            string jobId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TddftCalculationResult?>(null);
        }
    }

    private static CanonicalGeometry CreateOptimizedGeometry()
    {
        CanonicalGeometry geometry = new CanonicalGeometry();
        CanonicalAtom atom = new CanonicalAtom();
        atom.Index = 1;
        atom.Element = "O";
        atom.X = 0.0;
        atom.Y = 0.0;
        atom.Z = 1.0;
        geometry.Atoms.Add(atom);
        return geometry;
    }
}
