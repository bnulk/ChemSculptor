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
        return ids;
    }
}
