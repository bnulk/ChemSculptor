using ChemSculptor.Agent;
using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Local;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using ChemSculptor.ScientificData.Extraction;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Gaussian;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 真实 Gaussian 16 吸收光谱冒烟测试。
/// 默认不运行，设置 CHEMSCULPTOR_RUN_REAL_GAUSSIAN=1 后运行。
/// </summary>
public class AbsorptionSpectrumRealGaussianTests
{
    /// <summary>验证真实 Gaussian 16 三种吸收光谱模式和科学数据归档链。</summary>
    [Theory]
    [InlineData(AbsorptionSpectrumMode.TddftOnly)]
    [InlineData(AbsorptionSpectrumMode.OptimizeThenTddft)]
    [InlineData(AbsorptionSpectrumMode.ConstrainedOptimizationThenTddft)]
    public async Task RunsRealGaussianAbsorptionWorkflow(
        AbsorptionSpectrumMode mode)
    {
        if (!IsRealGaussianRunEnabled()
            || !IsGaussianAvailable())
        {
            return;
        }

        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;
            ScientificDataRepositoryOptions scientificOptions =
                new ScientificDataRepositoryOptions();
            scientificOptions.RootDirectory = Path.Combine(
                root,
                "scientific-data");

            ServiceCollection services =
                new ServiceCollection();
            ServiceCollectionServiceExtensions.AddSingleton<
                IEventBus,
                InMemoryEventBus>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                ISkillRegistry,
                SkillRegistry>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                IWorkflowRepository,
                InMemoryWorkflowRepository>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                IRuleEngine,
                AllowAllRuleEngine>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                IValidationGate,
                PassThroughValidationGate>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                ICaseMemory,
                InMemoryCaseMemory>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                IAnomalyRepository,
                FileAnomalyRepository>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                IAnomalyProviderRegistry,
                AnomalyProviderRegistry>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                WorkflowEngine>(services);
            ServiceCollectionServiceExtensions.AddSingleton<
                IGeometryTextParser,
                GeometryTextParser>(services);
            CommonSkillServiceRegistration.AddCommonSkills(services);
            GaussianSkillServiceRegistration.AddGaussianSkills(services);
            LocalComputeServiceRegistration.AddLocalComputeServices(
                services);
            AgentServiceRegistration.AddAgentServices(services);
            ScientificDataExtractionServiceRegistration
                .AddScientificDataExtraction(
                    services,
                    scientificOptions);

            for (int index = services.Count - 1;
                index >= 0;
                index--)
            {
                ServiceDescriptor descriptor = services[index];

                if (descriptor.ServiceType.IsInterface
                    && descriptor.ImplementationType
                        == descriptor.ServiceType)
                {
                    services.RemoveAt(index);
                }
            }

            // 覆盖默认工作区，确保真实测试文件全部落在临时目录。
            ServiceCollectionServiceExtensions.AddSingleton(
                services,
                workspaceOptions);
            ServiceCollectionServiceExtensions.AddSingleton<
                ICalculationWorkspace,
                WorkspaceManager>(services);

            ServiceProviderOptions providerOptions =
                new ServiceProviderOptions();
            providerOptions.ValidateOnBuild = false;
            providerOptions.ValidateScopes = false;
            ServiceProvider provider =
                services.BuildServiceProvider(
                    providerOptions);
            ISkillRegistry skillRegistry =
                ServiceProviderServiceExtensions
                    .GetRequiredService<ISkillRegistry>(provider);
            IEnumerable<ISkill> registeredSkills =
                ServiceProviderServiceExtensions
                    .GetServices<ISkill>(provider);

            foreach (ISkill skill in registeredSkills)
            {
                await skillRegistry.RegisterAsync(skill);
            }

            IAbsorptionSpectrumWorkflowService workflow =
                ServiceProviderServiceExtensions
                    .GetRequiredService<
                        IAbsorptionSpectrumWorkflowService>(
                            provider);
            AbsorptionSpectrumRequest request =
                new AbsorptionSpectrumRequest();
            request.SessionId = "session-real-gaussian";
            request.Goal = "真实 Gaussian 吸收光谱冒烟测试";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";
            request.Mode = mode;
            request.TddftSpec =
                CalculationDefaults.CreateDefaultTddftSinglePoint();
            request.TddftSpec.Charge = 0;
            request.TddftSpec.Multiplicity = 1;
            request.TddftSpec.ExtraOptions[
                CalculationOptionKeys.TddftStateCount] = "5";

            AbsorptionSpectrumSubmissionResult result =
                await workflow.SubmitAsync(request);

            Assert.True(
                result.Succeeded,
                string.IsNullOrWhiteSpace(result.Error)
                    ? "真实 Gaussian 吸收光谱工作流未成功。"
                    : result.Error);
            Assert.NotNull(result.Tddft);
            Assert.NotNull(result.Tddft!.Result);
            Assert.NotEmpty(
                result.Tddft.Result!.ExcitedStates);
            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.ScientificResultId));

            IScientificDataRepository repository =
                ServiceProviderServiceExtensions
                    .GetRequiredService<
                        IScientificDataRepository>(provider);
            ScientificResult? scientificResult =
                await repository.GetAsync(
                    result.ScientificResultId);
            Assert.NotNull(scientificResult);
            Assert.Equal(
                ScientificResultStatus.Complete,
                scientificResult!.Status);
            Assert.Single(
                scientificResult.PointSet.Points);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static bool IsRealGaussianRunEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable(
                "CHEMSCULPTOR_RUN_REAL_GAUSSIAN"),
            "1",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGaussianAvailable()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");

        if (!string.IsNullOrWhiteSpace(path))
        {
            string[] directories = path.Split(
                Path.PathSeparator);

            for (int index = 0;
                index < directories.Length;
                index++)
            {
                string directory =
                    directories[index].Trim().Trim('"');

                if (directory.Length > 0
                    && File.Exists(
                        Path.Combine(
                            directory,
                            "g16.exe")))
                {
                    return true;
                }
            }
        }

        return File.Exists(@"C:\G16W\g16.exe");
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
