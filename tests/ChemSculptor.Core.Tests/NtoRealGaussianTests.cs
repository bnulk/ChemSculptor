using ChemSculptor.Agent;
using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Local;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Gaussian;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 真实 Gaussian 16 NTO 冒烟测试。
/// 默认不运行，设置 CHEMSCULPTOR_RUN_REAL_GAUSSIAN=1 后运行。
/// </summary>
public class NtoRealGaussianTests
{
    /// <summary>验证 TDDFT 检查点、NTO 派生作业和真实输出解析。</summary>
    [Fact]
    public async Task RunsRealGaussianNtoWorkflow()
    {
        if (!IsRealGaussianRunEnabled()
            || !IsGaussianAvailable())
        {
            return;
        }

        string root = CreateTemporaryRoot();
        Console.WriteLine("NTO real Gaussian root: " + root);

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;

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
            CommonSkillServiceRegistration.AddCommonSkills(services);
            GaussianSkillServiceRegistration.AddGaussianSkills(
                services);
            LocalComputeServiceRegistration.AddLocalComputeServices(
                services);
            AgentServiceRegistration.AddAgentServices(services);

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

            ITddftCalculationService tddftService =
                ServiceProviderServiceExtensions
                    .GetRequiredService<
                        ITddftCalculationService>(provider);
            CalculationSpec tddftSpec =
                CalculationDefaults
                    .CreateDefaultTddftSinglePoint();
            tddftSpec.Charge = 0;
            tddftSpec.Multiplicity = 1;
            tddftSpec.ExtraOptions[
                CalculationOptionKeys.TddftStateCount] = "5";

            TddftCalculationRequest tddftRequest =
                new TddftCalculationRequest();
            tddftRequest.SessionId =
                "session-real-gaussian-nto";
            tddftRequest.Goal =
                "真实 Gaussian NTO 源 TDDFT";
            tddftRequest.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";
            tddftRequest.ResolvedSpec = tddftSpec;

            TddftSubmissionResult tddft =
                await tddftService.SubmitAsync(
                    tddftRequest);

            Assert.True(
                tddft.Succeeded,
                string.IsNullOrWhiteSpace(tddft.Error)
                    ? "真实 TDDFT 源计算未成功。"
                    : tddft.Error);
            Assert.NotNull(tddft.Job);
            Assert.NotNull(tddft.Result);
            Assert.True(tddft.Result!.NormalTermination);
            Assert.NotEmpty(tddft.Result.ExcitedStates);
            Assert.Contains(
                tddft.Result.ExcitedStates,
                state =>
                    state.OscillatorStrength > 0.0
                    && state.WavelengthNm > 0.0);

            CalculationJob sourceJob = tddft.Job!;
            string[] checkpoints =
                Directory.GetFiles(
                    sourceJob.RunDirectory,
                    "*.chk",
                    SearchOption.TopDirectoryOnly);
            Assert.NotEmpty(checkpoints);

            INtoWorkflowService ntoService =
                ServiceProviderServiceExtensions
                    .GetRequiredService<
                        INtoWorkflowService>(provider);
            NtoRequest ntoRequest = new NtoRequest();
            ntoRequest.SessionId =
                "session-real-gaussian-nto";
            ntoRequest.Goal =
                "真实 Gaussian NTO 端到端验收";
            ntoRequest.SourceTddftJobId =
                sourceJob.JobId;
            ntoRequest.SelectionMode =
                NtoSelectionMode.Default;

            NtoWorkflowResult result =
                await ntoService.SubmitAsync(ntoRequest);

            string transitionErrors = BuildTransitionErrors(
                result);
            Assert.True(
                result.Succeeded,
                string.IsNullOrWhiteSpace(transitionErrors)
                    ? result.Message
                    : result.Message + " " + transitionErrors);
            Assert.Equal(
                NtoWorkflowStatus.Completed,
                result.Status);
            Assert.NotEmpty(result.SelectedStates);
            Assert.Equal(
                result.SelectedStates.Count,
                result.TransitionResults.Count);

            for (int index = 0;
                index < result.TransitionResults.Count;
                index++)
            {
                NtoTransitionResult transition =
                    result.TransitionResults[index];
                Assert.True(
                    transition.Succeeded,
                    transition.Error);
                Assert.True(transition.NormalTermination);
                Assert.True(transition.NtoFound);
                Assert.True(
                    File.Exists(transition.OutputFilePath));

                string outputText =
                    await File.ReadAllTextAsync(
                        transition.OutputFilePath);
                Assert.Contains(
                    "Normal termination",
                    outputText,
                    StringComparison.OrdinalIgnoreCase);
                Assert.Contains(
                    "NTOs for state",
                    outputText,
                    StringComparison.OrdinalIgnoreCase);
                Assert.Contains(
                    transition.Artifacts,
                    artifact => artifact.RelativePath.EndsWith(
                        ".chk",
                        StringComparison.OrdinalIgnoreCase));
                Assert.Contains(
                    transition.Artifacts,
                    artifact => artifact.RelativePath.EndsWith(
                        ".fchk",
                        StringComparison.OrdinalIgnoreCase));
            }
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string BuildTransitionErrors(
        NtoWorkflowResult result)
    {
        List<string> errors = new List<string>();

        for (int index = 0;
            index < result.TransitionResults.Count;
            index++)
        {
            NtoTransitionResult transition =
                result.TransitionResults[index];

            if (!string.IsNullOrWhiteSpace(transition.Error))
            {
                errors.Add(transition.Error);
            }
        }

        return string.Join(" ", errors);
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
        string? path =
            Environment.GetEnvironmentVariable("PATH");

        if (!string.IsNullOrWhiteSpace(path))
        {
            string[] directories =
                path.Split(Path.PathSeparator);

            for (int index = 0;
                index < directories.Length;
                index++)
            {
                string directory =
                    directories[index].Trim().Trim('"');

                if (directory.Length > 0
                    && File.Exists(
                        Path.Combine(directory, "g16.exe")))
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
