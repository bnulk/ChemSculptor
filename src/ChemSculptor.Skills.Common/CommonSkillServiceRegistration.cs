using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using ChemSculptor.Anomaly.Planning;
using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Checks;
using ChemSculptor.Skills.Common.AnomalyWorkflow;
using ChemSculptor.Skills.Common.CalculationWorkflow;
using ChemSculptor.Skills.Common.CalculationResultValidation;
using ChemSculptor.Skills.Common.OptimizationCalculation;
using ChemSculptor.Skills.Common.OptimizationRecovery;
using ChemSculptor.Skills.Common.SinglePointCalculation;
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
            ICalculationResultValidator,
            OptimizationStepValidator>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ICalculationResultValidator,
            OptimizationResultValidator>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ICalculationResultValidator,
            OptimizationConvergenceValidator>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            CalculationValidationService>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            WavefunctionStabilityCorrectionPlanner>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            OptimizationConvergenceCheck>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IAnomalyCheck>(services);
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
            SinglePointCalculationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationCalculationSkill>(services);
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
            OptimizationCalculationValidationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationFailureDiagnosisSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationCorrectionPlanningSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationRecoveryJobCreationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationRecoveryJobExecutionSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationRecoveryValidationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationConvergenceCheckSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationRecoveryBranchSelectionSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationHardFailureReportSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            OptimizationProcessingPlanSkill>(services);
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
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            RecoveryJobExecutionSkill>(services);

        return services;
    }
}
