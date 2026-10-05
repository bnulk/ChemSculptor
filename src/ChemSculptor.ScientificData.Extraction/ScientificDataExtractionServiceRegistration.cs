using ChemSculptor.ScientificData.Extraction.Abstractions;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.ScientificData.Extraction;

/// <summary>科学数据提取服务注册。</summary>
public static class ScientificDataExtractionServiceRegistration
{
    /// <summary>注册科学数据提取和记录服务。</summary>
    public static IServiceCollection AddScientificDataExtraction(
        IServiceCollection services,
        ScientificDataRepositoryOptions repositoryOptions)
    {
        if (repositoryOptions == null)
        {
            throw new ArgumentNullException(
                nameof(repositoryOptions));
        }

        ServiceCollectionServiceExtensions.AddSingleton(
            services,
            repositoryOptions);
        ServiceCollectionServiceExtensions.AddSingleton<
            IScientificDataRepository,
            FileScientificDataRepository>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IScientificResultExtractor,
            ScientificResultExtractor>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            IScientificDataRecorder,
            ScientificDataRecorder>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISkill,
            ScientificDataExtractionSkill>(services);
        return services;
    }
}
