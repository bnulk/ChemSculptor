using ChemSculptor.Compute;
using ChemSculptor.Conversation;
using ChemSculptor.Domain;
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
        ServiceCollectionServiceExtensions.AddSingleton<ITaskInterpreter, RuleBasedTaskInterpreter>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<IConversationRepository, InMemoryConversationRepository>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<IConversationService, ConversationService>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<ICalculationRepository, FileCalculationRepository>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<ISkillInvoker, SkillJsonInvoker>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISinglePointWorkflowEngine,
            SinglePointWorkflowEngine>(services);
        ServiceCollectionServiceExtensions.AddSingleton<
            ISinglePointCalculationService,
            SinglePointCalculationService>(services);
        ServiceCollectionServiceExtensions.AddSingleton<ICalculationQueryService, CalculationQueryService>(
            services);
        ServiceCollectionServiceExtensions.AddSingleton<IAgentService, AgentService>(
            services);

        return services;
    }
}
