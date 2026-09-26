using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using ChemSculptor.Skills.Gaussian.GaussianFailureCorrectionProposal;
using ChemSculptor.Skills.Gaussian.GaussianFailureDiagnosis;
using ChemSculptor.Skills.Gaussian.GaussianInputGeneration;
using ChemSculptor.Skills.Gaussian.GaussianSinglePointResultExtraction;
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

        Gaussian16ProgramAdapterOptions options =
            Gaussian16ProgramAdapterOptions.CreateDefault();
        ServiceCollectionServiceExtensions.AddSingleton<Gaussian16ProgramAdapterOptions>(
            services,
            options);
        ServiceCollectionServiceExtensions.AddSingleton<
            IQuantumProgramAdapter,
            Gaussian16ProgramAdapter>(services);

        ServiceCollectionServiceExtensions.AddSingleton<IGeometryTextParser, GeometryTextParser>(
            services);

        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianInputGenerationSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianSinglePointResultExtractionSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianFailureDiagnosisSkill>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            GaussianFailureCorrectionProposalSkill>(services);

        return services;
    }
}
