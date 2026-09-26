using ChemSculptor.Domain;
using ChemSculptor.Skills.Common.CalculationResultValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Skills.Common;

/// <summary>通用技能注册。</summary>
public static class CommonSkillServiceRegistration
{
    /// <summary>注册通用技能目录中的全部技能。</summary>
    public static IServiceCollection AddCommonSkills(IServiceCollection services)
    {
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationResultValidationSkill>(services);

        return services;
    }
}
