using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>测试用程序适配器注册表。</summary>
internal sealed class TestQuantumProgramAdapterRegistry : IQuantumProgramAdapterRegistry
{
    private readonly List<IQuantumProgramAdapter> _adapters;

    public TestQuantumProgramAdapterRegistry()
    {
        _adapters = new List<IQuantumProgramAdapter>();
        _adapters.Add(new TestQuantumProgramAdapter());
    }

    public IQuantumProgramAdapter? Resolve(CalculationSpec spec)
    {
        return _adapters[0];
    }

    public IReadOnlyList<string> ListPrograms()
    {
        List<string> programs = new List<string>();
        programs.Add(_adapters[0].ProgramName);
        return programs;
    }
}

/// <summary>测试用程序适配器。</summary>
internal sealed class TestQuantumProgramAdapter : IQuantumProgramAdapter
{
    public string ProgramName
    {
        get { return "Test Program"; }
    }

    public bool CanRun(CalculationSpec spec)
    {
        return true;
    }

    public Task WriteInputAsync(
        CalculationSpec spec,
        ChemSculptor.InputProcessor.GeometryIntake.CanonicalGeometry geometry,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public string GetInputFileName(string jobId)
    {
        return jobId + ".inp";
    }

    public IReadOnlyList<CalculationArtifactPattern> GetArtifactPatterns(
        CalculationArtifactDiscoveryContext context)
    {
        List<CalculationArtifactPattern> patterns =
            new List<CalculationArtifactPattern>();

        CalculationArtifactPattern outputPattern =
            new CalculationArtifactPattern();
        outputPattern.FilePattern = "output.log";
        outputPattern.Kind = CalculationArtifactKind.PrimaryOutput;
        outputPattern.MediaType = "text/plain; charset=utf-8";
        outputPattern.CanUseForRestart = false;
        patterns.Add(outputPattern);

        CalculationArtifactPattern inputPattern =
            new CalculationArtifactPattern();
        inputPattern.FilePattern = "*.inp";
        inputPattern.Kind = CalculationArtifactKind.Input;
        inputPattern.MediaType = "text/plain; charset=utf-8";
        inputPattern.CanUseForRestart = false;
        patterns.Add(inputPattern);

        return patterns;
    }

    public Task PostProcessAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public CalculationExecutionContext BuildExecutionContext(
        CalculationJob job,
        CalculationSpec spec)
    {
        CalculationExecutionContext context = new CalculationExecutionContext();
        context.JobId = job.JobId;
        context.RunDirectory = job.RunDirectory;
        context.InputFilePath = job.InputFilePath;
        context.OutputFilePath = job.OutputFilePath;
        return context;
    }

    public Task<CalculationResult> ParseOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        CalculationResult result = new CalculationResult();
        result.NormalTermination = true;
        result.OutputFilePath = outputPath;
        return Task.FromResult(result);
    }

    public Task<ProgramProcessingPlan> TranslateProcessingPlanAsync(
        CalculationJob job,
        CalculationProcessingPlan processingPlan,
        CancellationToken cancellationToken = default)
    {
        ProgramProcessingPlan plan = new ProgramProcessingPlan();
        plan.JobId = job.JobId;
        plan.Program = ProgramName;
        return Task.FromResult(plan);
    }
}
