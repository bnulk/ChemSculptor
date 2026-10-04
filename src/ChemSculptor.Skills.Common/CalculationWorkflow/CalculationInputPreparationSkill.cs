using ChemSculptor.Compute;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.Chemistry;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Skills.Common.CalculationWorkflow;

/// <summary>
/// 通用输入准备技能。
/// 具体输入格式由程序适配器负责。
/// </summary>
public sealed class CalculationInputPreparationSkill
    : JsonSkill<CalculationInputGenerationRequest, CalculationInputGenerationResult>
{
    private readonly IGeometryTextParser _geometryParser;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly List<string> _capabilities;

    /// <summary>创建通用输入准备技能。</summary>
    public CalculationInputPreparationSkill(
        IGeometryTextParser geometryParser,
        IQuantumProgramAdapterRegistry adapterRegistry)
    {
        if (geometryParser == null)
        {
            throw new ArgumentNullException(nameof(geometryParser));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        _geometryParser = geometryParser;
        _adapterRegistry = adapterRegistry;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.prepare-input");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return CalculationSkillIds.CalculationInputPreparation; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>准备程序输入并构建执行上下文。</summary>
    protected override async Task<CalculationInputGenerationResult> ExecuteAsync(
        CalculationInputGenerationRequest request,
        CancellationToken cancellationToken)
    {
        CalculationInputGenerationResult result =
            new CalculationInputGenerationResult();

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(request.Spec);

        if (adapter == null)
        {
            result.Succeeded = false;
            result.Error = "没有可处理 " + request.Spec.Program + " 的计算程序适配器。";
            return result;
        }

        MolecularGeometry molecularGeometry =
            await _geometryParser.ParseAsync(
                request.CoordinateText,
                cancellationToken);

        if (molecularGeometry.Atoms.Count == 0)
        {
            result.Succeeded = false;
            result.Error = "未能从坐标文本中解析出原子。";
            result.Diagnostics = new List<string>(molecularGeometry.Diagnostics);
            return result;
        }

        int electronCount;
        string electronCountError;

        if (!ElectronCountCalculator.TryCalculate(
            molecularGeometry.Atoms,
            request.Spec.Charge,
            out electronCount,
            out electronCountError))
        {
            result.Succeeded = false;
            result.Error = electronCountError;
            return result;
        }

        CalculationDefaults.ApplyDefaultMultiplicityFromElectronCount(
            request.Spec,
            electronCount);

        CanonicalGeometry canonicalGeometry =
            CanonicalGeometryMapper.FromMolecularGeometry(
                molecularGeometry,
                request.Job.JobId);

        EnsureParentDirectory(request.InputFilePath);
        EnsureParentDirectory(request.RunInputFilePath);

        await adapter.WriteInputAsync(
            request.Spec,
            canonicalGeometry,
            request.InputFilePath,
            cancellationToken);

        File.Copy(request.InputFilePath, request.RunInputFilePath, true);

        CalculationJob job = request.Job;
        job.Spec = request.Spec;
        job.InputFilePath = request.RunInputFilePath;
        job.OutputFilePath = request.OutputFilePath;
        job.State = CalculationJobState.InputGenerated;

        CalculationExecutionContext context =
            adapter.BuildExecutionContext(job, request.Spec);

        result.Succeeded = true;
        result.Job = job;
        result.ExecutionContext = context;
        return result;
    }

    /// <summary>检查技能可用性。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static void EnsureParentDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
