using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Skills.Gaussian.GaussianFailureCorrectionProposal;

/// <summary>
/// Gaussian 异常修正提案技能框架。
/// 当前只实现通用处理方案到 Gaussian 方案的翻译，不执行修改。
/// </summary>
public sealed class GaussianFailureCorrectionProposalSkill
    : JsonSkill<
        GaussianFailureCorrectionProposalSkillRequest,
        GaussianFailureCorrectionProposalSkillResult>
{
    private readonly GaussianProcessingPlanTranslator _translator;
    private readonly List<string> _capabilities;

    /// <summary>创建修正提案技能。</summary>
    public GaussianFailureCorrectionProposalSkill(
        GaussianProcessingPlanTranslator translator)
    {
        if (translator == null)
        {
            throw new ArgumentNullException(nameof(translator));
        }

        _translator = translator;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.failure-correction-proposal");
        _capabilities.Add("gaussian.failure-correction");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return GaussianFailureCorrectionProposalSkillDescriptor.Id; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return GaussianFailureCorrectionProposalSkillDescriptor.VersionValue; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>翻译处理方案，不执行修改。</summary>
    protected override Task<GaussianFailureCorrectionProposalSkillResult> ExecuteAsync(
        GaussianFailureCorrectionProposalSkillRequest request,
        CancellationToken cancellationToken)
    {
        GaussianProcessingPlan gaussianPlan =
            _translator.Translate(request.ProcessingPlan);

        gaussianPlan.JobId = request.Job.JobId;
        ProgramProcessingPlan programPlan =
            _translator.ToProgramPlan(gaussianPlan);

        GaussianFailureCorrectionProposalSkillResult result =
            new GaussianFailureCorrectionProposalSkillResult();
        result.Succeeded = true;
        result.ProgramPlan = programPlan;
        return Task.FromResult(result);
    }

    /// <summary>检查技能是否可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
