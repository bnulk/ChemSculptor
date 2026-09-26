using ChemSculptor.Compute;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Skills.Gaussian.GaussianSinglePointResultExtraction;

/// <summary>
/// Gaussian 单点计算结果提取技能。
/// 技能内部完成 Gaussian 解析和通用结果翻译。
/// </summary>
public sealed class GaussianSinglePointResultExtractionSkill
    : JsonSkill<
        GaussianSinglePointResultExtractionSkillRequest,
        GaussianSinglePointResultExtractionSkillResult>
{
    private readonly IQuantumProgramAdapter _programAdapter;
    private readonly List<string> _capabilities;

    /// <summary>创建结果提取技能。</summary>
    public GaussianSinglePointResultExtractionSkill(
        IQuantumProgramAdapter programAdapter)
    {
        if (programAdapter == null)
        {
            throw new ArgumentNullException(nameof(programAdapter));
        }

        _programAdapter = programAdapter;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.result-extraction");
        _capabilities.Add("calculation.single-point");
        _capabilities.Add("gaussian.result");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return GaussianSinglePointResultExtractionSkillDescriptor.Id; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return GaussianSinglePointResultExtractionSkillDescriptor.VersionValue; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>执行结果提取。</summary>
    protected override async Task<GaussianSinglePointResultExtractionSkillResult> ExecuteAsync(
        GaussianSinglePointResultExtractionSkillRequest request,
        CancellationToken cancellationToken)
    {
        CalculationResult result =
            await _programAdapter.ParseOutputAsync(
                request.OutputFilePath,
                cancellationToken);

        result.JobId = request.JobId;
        result.Program = request.Program;
        result.Method = request.Spec.Method;
        result.Basis = request.Spec.Basis;
        result.Charge = request.Spec.Charge;
        result.Multiplicity = request.Spec.Multiplicity;
        result.OutputFilePath = request.OutputFilePath;

        GaussianSinglePointResultExtractionSkillResult skillResult =
            new GaussianSinglePointResultExtractionSkillResult();
        skillResult.Succeeded = true;
        skillResult.Result = result;
        return skillResult;
    }

    /// <summary>检查技能是否可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
