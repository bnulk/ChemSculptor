using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Local;
using ChemSculptor.Conversation;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Agent;

/// <summary>
/// 智能体相关服务的依赖注册。
/// </summary>
public static class AgentServiceRegistration
{
    /// <summary>把智能体、会话、计算执行所需服务注册到容器。</summary>
    public static IServiceCollection AddAgentServices(IServiceCollection services)
    {
        CalculationWorkspaceOptions workspaceOptions = CalculationWorkspaceOptions.CreateDefault();

        ServiceCollectionServiceExtensions.AddSingleton<CalculationWorkspaceOptions>(
            services,
            workspaceOptions);
        ServiceCollectionServiceExtensions.AddSingleton<ICalculationWorkspace, WorkspaceManager>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<GaussianInputWriter>(services);
        ServiceCollectionServiceExtensions.AddSingleton<GaussianOutputParser>(services);
        Gaussian16ProgramAdapterOptions gaussianOptions =
            Gaussian16ProgramAdapterOptions.CreateDefault();
        ServiceCollectionServiceExtensions.AddSingleton<Gaussian16ProgramAdapterOptions>(
            services,
            gaussianOptions);
        ServiceCollectionServiceExtensions.AddSingleton<IQuantumProgramAdapter, Gaussian16ProgramAdapter>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<ITaskInterpreter, RuleBasedTaskInterpreter>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<IConversationRepository, InMemoryConversationRepository>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<IConversationService, ConversationService>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<ICalculationRepository, FileCalculationRepository>(
            services);
        CalculationJobMonitorOptions monitorOptions =
            CalculationJobMonitorOptions.CreateDefault();
        ServiceCollectionServiceExtensions.AddSingleton<CalculationJobMonitorOptions>(
            services,
            monitorOptions);
        ServiceCollectionServiceExtensions.AddSingleton<ICalculationJobMonitor, CalculationJobMonitor>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<SinglePointCalculationExecutor>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<ICalculationQueryService, CalculationQueryService>(
            services);
        LocalProcessBackendOptions localProcessOptions = LocalProcessBackendOptions.CreateDefault();
        ServiceCollectionServiceExtensions.AddSingleton<LocalProcessBackendOptions>(
            services,
            localProcessOptions);
        ServiceCollectionServiceExtensions.AddSingleton<IComputeBackend, LocalProcessBackend>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<IAgentService, AgentService>(
            services);

        return services;
    }
}
