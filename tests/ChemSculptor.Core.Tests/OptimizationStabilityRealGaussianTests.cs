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
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Gaussian;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 真实 Gaussian O2 波函数稳定性修正测试。
/// 默认不运行，设置 CHEMSCULPTOR_RUN_REAL_GAUSSIAN=1 后运行。
/// </summary>
public class OptimizationStabilityRealGaussianTests
{
    /// <summary>验证单重态 O2 会修正为三重态并完成稳定性复检。</summary>
    [Fact]
    public async Task CorrectsSingletOxygenToTriplet()
    {
        if (!IsRealGaussianRunEnabled()
            || !IsGaussianAvailable())
        {
            return;
        }

        string root = CreateTemporaryRoot();
        Console.WriteLine(
            "O2 stability real Gaussian root: " + root);

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
            GaussianSkillServiceRegistration.AddGaussianSkills(
                services);
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

            IAnomalyProviderRegistry anomalyRegistry =
                ServiceProviderServiceExtensions
                    .GetRequiredService<IAnomalyProviderRegistry>(
                        provider);
            IEnumerable<IAnomalyCheck> registeredChecks =
                ServiceProviderServiceExtensions
                    .GetServices<IAnomalyCheck>(provider);

            foreach (IAnomalyCheck check in registeredChecks)
            {
                anomalyRegistry.RegisterCheck(check);
            }

            IEnumerable<IRecoveryJobProvider>
                recoveryJobProviders =
                    ServiceProviderServiceExtensions
                        .GetServices<IRecoveryJobProvider>(
                            provider);

            foreach (IRecoveryJobProvider recoveryProvider
                in recoveryJobProviders)
            {
                anomalyRegistry.RegisterRecoveryJobProvider(
                    recoveryProvider);
            }

            IOptimizationCalculationService optimizationService =
                ServiceProviderServiceExtensions
                    .GetRequiredService<
                        IOptimizationCalculationService>(provider);
            CalculationSpec spec =
                CalculationDefaults.CreateDefaultOptimization();
            spec.Charge = 0;
            spec.Multiplicity = 1;
            spec.ExtraOptions[
                CalculationOptionKeys.OptimizationMaxCycles] = "30";
            spec.ExtraOptions[
                CalculationOptionKeys.WavefunctionStabilityCheck] =
                "Check";

            OptimizationRequest request =
                new OptimizationRequest();
            request.SessionId = "session-o2-stability";
            request.Goal = "单重态 O2 稳定性修正";
            request.CoordinateText =
                "O -1.768141 -0.414902 -0.017717\n" +
                "O -2.929741 -0.414902 -0.017717";
            request.Charge = 0;
            request.Multiplicity = 1;
            request.ResolvedSpec = spec;

            OptimizationSubmissionResult result =
                await optimizationService.SubmitAsync(request);

            Assert.True(
                result.Succeeded,
                string.IsNullOrWhiteSpace(result.Error)
                    ? result.Message
                    : result.Error);
            Assert.NotNull(result.Result);
            Assert.Equal(3, result.Result!.Multiplicity);
            Assert.True(result.CorrectionCount > 0);
            Assert.Equal("Stable", result.WavefunctionStabilityStatus);
            Assert.False(string.IsNullOrWhiteSpace(
                result.FinalJobId));
            Assert.NotEqual(
                result.Job!.JobId,
                result.FinalJobId);
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
        if (string.Equals(
            Environment.GetEnvironmentVariable(
                "CHEMSCULPTOR_KEEP_REAL_GAUSSIAN"),
            "1",
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
