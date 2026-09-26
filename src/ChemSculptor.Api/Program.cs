using System.Text.Json;
using ChemSculptor.Api.Client;
using ChemSculptor.Agent;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ChemSculptor.Api;

/// <summary>
/// ChemSculptor.Api 的启动入口。
/// 负责组装依赖、载入示例工作流、登记路由并启动 Kestrel。
/// </summary>
public static class Program
{
    /// <summary>
    /// 应用程序入口。
    /// 启动阶段只做装配与登记，不处理具体业务请求。
    /// </summary>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // 注册服务到依赖注入容器；单例表示整个进程共用一个实例。
        ServiceCollectionServiceExtensions.AddSingleton<IEventBus, InMemoryEventBus>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<ISkillRegistry, SkillRegistry>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<IRuleEngine, AllowAllRuleEngine>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<IValidationGate, PassThroughValidationGate>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<ICaseMemory, InMemoryCaseMemory>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<EchoSkill>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<WorkflowEngine>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<IClientInputParser, TextClientInputParser>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<IGeometryTextParser, GeometryTextParser>(builder.Services);
        ServiceCollectionServiceExtensions.AddSingleton<ClientJobService>(builder.Services);

        AgentServiceRegistration.AddAgentServices(builder.Services);

        // 构建可运行的 Web 应用，此时还未开始监听端口。
        WebApplication app = builder.Build();

        // 手动把演示技能容器登记到注册表中。
        ISkillRegistry skillRegistry = GetRequiredService<ISkillRegistry>(app.Services);
        EchoSkill echoSkill = GetRequiredService<EchoSkill>(app.Services);
        await skillRegistry.RegisterAsync(echoSkill);

        // 若示例工作流文件存在，则载入并登记为 Ready 状态（不执行）。
        string samplePath = Path.Combine(Directory.GetCurrentDirectory(), "workflows", "tadf-mechanism.json");
        if (File.Exists(samplePath))
        {
            JsonSerializerOptions jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            string json = await File.ReadAllTextAsync(samplePath);
            WorkflowDefinition? definition = JsonSerializer.Deserialize<WorkflowDefinition>(json, jsonOptions);

            if (definition != null)
            {
                IWorkflowRepository repository = GetRequiredService<IWorkflowRepository>(app.Services);
                WorkflowRun? existingRun = await repository.GetAsync(definition.Id);

                if (existingRun == null)
                {
                    WorkflowEngine engine = GetRequiredService<WorkflowEngine>(app.Services);
                    await engine.SubmitAsync(definition);
                }
            }
        }

        // 使用显式静态调用登记路由，不使用扩展方法写法。
        EndpointRouteBuilderExtensions.MapGet(app, "/", GetServiceInfo);

        WorkflowEndpoints.MapWorkflowEndpoints(app);
        SkillEndpoints.MapSkillEndpoints(app);
        ClientJobEndpoints.MapClientJobEndpoints(app);
        GeometryEndpoints.MapGeometryEndpoints(app);
        CalculationEndpoints.MapCalculationEndpoints(app);
        AgentEndpoints.MapAgentEndpoints(app);

        // 启动 Kestrel 并进入请求监听循环，直到进程关闭。
        app.Run();
    }

    /// <summary>返回服务说明与可用端点列表。</summary>
    private static IResult GetServiceInfo()
    {
        ServiceInfoResponse response = new ServiceInfoResponse();
        response.Service = "ChemSculptor minimal core";
        response.Endpoints = new List<string>();
        response.Endpoints.Add("POST /workflows");
        response.Endpoints.Add("GET /workflows");
        response.Endpoints.Add("GET /workflows/{id}");
        response.Endpoints.Add("POST /workflows/{id}/run");
        response.Endpoints.Add("POST /workflows/{id}/intervene");
        response.Endpoints.Add("GET /tasks/{workflowId}/log");
        response.Endpoints.Add("POST /approvals/{id}");
        response.Endpoints.Add("GET /skills");
        response.Endpoints.Add("POST /skills/register");
        response.Endpoints.Add("POST /client/jobs");
        response.Endpoints.Add("GET /client/jobs/{id}/status");
        response.Endpoints.Add("GET /client/jobs/{id}/result");
        response.Endpoints.Add("POST /geometries");
        response.Endpoints.Add("POST /calculations/single-point");
        response.Endpoints.Add("GET /calculations/{jobId}/status");
        response.Endpoints.Add("GET /calculations/{jobId}/result");
        response.Endpoints.Add("POST /agent/messages");

        return Results.Ok(response);
    }

    /// <summary>
    /// 按类型从服务容器中获取必需的服务。
    /// 找不到时抛出异常，避免后续出现空引用。
    /// </summary>
    /// <typeparam name="T">要获取的服务类型。</typeparam>
    /// <param name="services">当前应用的服务容器。</param>
    /// <returns>已注册的服务实例。</returns>
    private static T GetRequiredService<T>(IServiceProvider services)
    {
        object? service = services.GetService(typeof(T));

        if (service == null)
        {
            throw new InvalidOperationException("Required service was not registered: " + typeof(T).FullName);
        }

        return (T)service;
    }
}
