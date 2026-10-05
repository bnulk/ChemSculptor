using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Compute.Gaussian.Anomaly.Recovery;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Gaussian.Anomaly.Recovery;
using ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Skills.Gaussian.GaussianFailureCorrectionProposal;
using ChemSculptor.Skills.Gaussian.GaussianFailureDiagnosis;
using ChemSculptor.Skills.Gaussian.GaussianSinglePointResultExtraction;
using ChemSculptor.Skills.Gaussian.GaussianSinglePointResultValidation;
using ChemSculptor.Skills.Common.CalculationResultValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Skills.Gaussian;

/// <summary>Gaussian 技能与程序实现注册。</summary>
public static class GaussianSkillServiceRegistration
{
    /// <summary>注册 Gaussian 目录中的全部技能。</summary>
    public static IServiceCollection AddGaussianSkills(IServiceCollection services)
    {
        ServiceCollectionServiceExtensions.AddSingleton<GaussianInputWriter>(services);
        ServiceCollectionServiceExtensions.AddSingleton<GaussianOutputParser>(services);
        ServiceCollectionServiceExtensions.AddSingleton<GaussianResultTranslator>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<GaussianProcessingPlanTranslator>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<
            GaussianWavefunctionStabilityParser>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            GaussianWavefunctionStabilityInputWriter>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            GaussianRecoveryInputWriter>(services);

        Gaussian16ProgramAdapterOptions options =
            Gaussian16ProgramAdapterOptions.CreateDefault();
        ServiceCollectionServiceExtensions.AddSingleton<Gaussian16ProgramAdapterOptions>(
            services,
            options);
        ServiceCollectionServiceExtensions.AddSingleton<
            IQuantumProgramAdapter,
            Gaussian16ProgramAdapter>(services);

        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianSinglePointResultExtractionSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianFailureDiagnosisSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianFailureCorrectionProposalSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ICalculationResultValidator,
            GaussianSinglePointOutputValidator>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianWavefunctionStabilityCheckSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IAnomalyCheck,
            GaussianWavefunctionStabilityCheckSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IRecoveryJobProvider,
            GaussianRecoveryJobProvider>(services);
        return services;
    }
}
