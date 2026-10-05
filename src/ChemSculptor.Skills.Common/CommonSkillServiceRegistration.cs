using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using ChemSculptor.Anomaly.Planning;
using ChemSculptor.Skills.Common.AnomalyWorkflow;
using ChemSculptor.Skills.Common.CalculationWorkflow;
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
            ICalculationResultValidator,
            SinglePointCalculationResultValidator>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            CalculationValidationService>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            WavefunctionStabilityCorrectionPlanner>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IQuantumProgramAdapterRegistry,
            QuantumProgramAdapterRegistry>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IGeometryTextParser,
            GeometryTextParser>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationResultValidationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationSubmissionSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationWaitSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationWorkflowValidationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationWorkflowProcessingPlanSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationInputPreparationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            CalculationResultExtractionWorkflowSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            WavefunctionStabilityCheckSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            WavefunctionStabilityCorrectionPlanningSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            RecoveryJobCreationSkill>(services);

        return services;
    }
}
