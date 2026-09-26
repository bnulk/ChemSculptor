using ChemSculptor.Compute;
using ChemSculptor.Skills.Gaussian.GaussianFailureCorrectionProposal;
using ChemSculptor.Skills.Gaussian.GaussianFailureDiagnosis;
using ChemSculptor.Skills.Gaussian.GaussianInputGeneration;
using ChemSculptor.Skills.Gaussian.GaussianSinglePointResultExtraction;

namespace ChemSculptor.Skills.Gaussian;

/// <summary>Gaussian 技能目录。</summary>
public static class GaussianSkillCatalog
{
    /// <summary>列出本目录包含的技能标识。</summary>
    public static IReadOnlyList<string> ListSkillIds()
    {
        List<string> ids = new List<string>();
        ids.Add(GaussianInputGenerationSkillDescriptor.Id);
        ids.Add(GaussianSinglePointResultExtractionSkillDescriptor.Id);
        ids.Add(GaussianFailureDiagnosisSkill.NameValue);
        ids.Add(GaussianFailureCorrectionProposalSkillDescriptor.Id);
        return ids;
    }
}
