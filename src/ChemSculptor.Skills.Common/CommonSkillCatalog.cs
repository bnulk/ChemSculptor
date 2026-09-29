using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common;

/// <summary>通用技能目录。</summary>
public static class CommonSkillCatalog
{
    /// <summary>列出本目录包含的技能标识。</summary>
    public static IReadOnlyList<string> ListSkillIds()
    {
        List<string> ids = new List<string>();
        ids.Add(
            CalculationResultValidation.CalculationResultValidationSkillDescriptor.Id);
        ids.Add(CalculationSkillIds.CalculationSubmission);
        ids.Add(CalculationSkillIds.CalculationWait);
        ids.Add(CalculationSkillIds.CalculationWorkflowValidation);
        ids.Add(CalculationSkillIds.CalculationWorkflowProcessingPlan);
        return ids;
    }
}
