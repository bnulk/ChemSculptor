using ChemSculptor.Compute;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.GeometryIntake;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Skills.Gaussian.GaussianInputGeneration;

/// <summary>
/// Gaussian 输入文件生成技能。
/// 负责解析坐标、生成输入副本并构建程序执行上下文。
/// </summary>
public sealed class GaussianInputGenerationSkill
    : JsonSkill<
        GaussianInputGenerationSkillRequest,
        GaussianInputGenerationSkillResult>
{
    private readonly IGeometryTextParser _geometryParser;
    private readonly IQuantumProgramAdapter _programAdapter;
    private readonly List<string> _capabilities;

    /// <summary>创建 Gaussian 输入文件生成技能。</summary>
    public GaussianInputGenerationSkill(
        IGeometryTextParser geometryParser,
        IQuantumProgramAdapter programAdapter)
    {
        if (geometryParser == null)
        {
            throw new ArgumentNullException(nameof(geometryParser));
        }

        if (programAdapter == null)
        {
            throw new ArgumentNullException(nameof(programAdapter));
        }

        _geometryParser = geometryParser;
        _programAdapter = programAdapter;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.input-generation");
        _capabilities.Add("gaussian.input");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return GaussianInputGenerationSkillDescriptor.Id; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return GaussianInputGenerationSkillDescriptor.VersionValue; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>生成输入文件并构建执行上下文。</summary>
    protected override async Task<GaussianInputGenerationSkillResult> ExecuteAsync(
        GaussianInputGenerationSkillRequest request,
        CancellationToken cancellationToken)
    {
        GaussianInputGenerationSkillResult result =
            new GaussianInputGenerationSkillResult();

        if (!_programAdapter.CanRun(request.Spec))
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

        CanonicalGeometry canonicalGeometry =
            CanonicalGeometryMapper.FromMolecularGeometry(
                molecularGeometry,
                request.Job.JobId);

        EnsureParentDirectory(request.InputFilePath);
        EnsureParentDirectory(request.RunInputFilePath);

        await _programAdapter.WriteInputAsync(
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
            _programAdapter.BuildExecutionContext(job, request.Spec);

        result.Succeeded = true;
        result.Job = job;
        result.ExecutionContext = context;
        return result;
    }

    /// <summary>检查技能是否可用。</summary>
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
