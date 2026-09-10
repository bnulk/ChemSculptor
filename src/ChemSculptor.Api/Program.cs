using System.Text.Json;
using ChemSculptor.Api.Client;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

public static class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
        builder.Services.AddSingleton<IContainerRegistry, ContainerRegistry>();
        builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
        builder.Services.AddSingleton<IRuleEngine, AllowAllRuleEngine>();
        builder.Services.AddSingleton<IValidationGate, PassThroughValidationGate>();
        builder.Services.AddSingleton<ICaseMemory, InMemoryCaseMemory>();
        builder.Services.AddSingleton<EchoSkillContainer>();
        builder.Services.AddSingleton<WorkflowEngine>();
        builder.Services.AddSingleton<IClientInputParser, TextClientInputParser>();
        builder.Services.AddSingleton<IGeometryTextParser, GeometryTextParser>();
        builder.Services.AddSingleton<ClientJobService>();

        WebApplication app = builder.Build();

        IContainerRegistry registry = GetRequiredService<IContainerRegistry>(app.Services);
        EchoSkillContainer echoContainer = GetRequiredService<EchoSkillContainer>(app.Services);
        await registry.RegisterAsync(echoContainer);

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

        EndpointRouteBuilderExtensions.MapGet(app, "/", GetServiceInfo);

        WorkflowEndpoints.MapWorkflowEndpoints(app);
        ContainerEndpoints.MapContainerEndpoints(app);
        ClientJobEndpoints.MapClientJobEndpoints(app);
        GeometryEndpoints.MapGeometryEndpoints(app);

        app.Run();
    }

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
        response.Endpoints.Add("GET /containers");
        response.Endpoints.Add("POST /containers/register");
        response.Endpoints.Add("POST /client/jobs");
        response.Endpoints.Add("GET /client/jobs/{id}/status");
        response.Endpoints.Add("GET /client/jobs/{id}/result");
        response.Endpoints.Add("POST /geometries");

        return Results.Ok(response);
    }

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
