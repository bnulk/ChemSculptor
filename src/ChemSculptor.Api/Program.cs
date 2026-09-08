using System.Text.Json;
using ChemSculptor.Api;
using ChemSculptor.Api.Client;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;

// 语法糖说明：下面是“顶层语句”。
// 传统写法是把它包进 Main 方法里：
// public class Program {
//     public static void Main(string[] args) {
//         var builder = WebApplication.CreateBuilder(args);
//         ...
//     }
// }
var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

var registry = app.Services.GetRequiredService<IContainerRegistry>();
await registry.RegisterAsync(app.Services.GetRequiredService<EchoSkillContainer>());

var samplePath = Path.Combine(Directory.GetCurrentDirectory(), "workflows", "tadf-mechanism.json");
if (File.Exists(samplePath))
{
    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    var definition = JsonSerializer.Deserialize<WorkflowDefinition>(
        await File.ReadAllTextAsync(samplePath),
        jsonOptions);

    if (definition is not null)
    {
        var repository = app.Services.GetRequiredService<IWorkflowRepository>();
        if (await repository.GetAsync(definition.Id) is null)
        {
            await app.Services.GetRequiredService<WorkflowEngine>().SubmitAsync(definition);
        }
    }
}

app.MapGet("/", () => Results.Ok(new
{
    service = "ChemSculptor minimal core",
    endpoints = new[]
    {
        "POST /workflows",
        "GET /workflows",
        "GET /workflows/{id}",
        "POST /workflows/{id}/run",
        "POST /workflows/{id}/intervene",
        "GET /tasks/{workflowId}/log",
        "POST /approvals/{id}",
        "GET /containers",
        "POST /containers/register",
        "POST /client/jobs",
        "GET /client/jobs/{id}/status",
        "GET /client/jobs/{id}/result",
        "POST /geometries"
    }
}));

// 语法糖说明：app.MapXxx() 是 C# 扩展方法写法，等价于：
// WorkflowEndpoints.MapWorkflowEndpoints(app);
// ContainerEndpoints.MapContainerEndpoints(app);
// ClientJobEndpoints.MapClientJobEndpoints(app);
// GeometryEndpoints.MapGeometryEndpoints(app);
app.MapWorkflowEndpoints();
app.MapContainerEndpoints();
app.MapClientJobEndpoints();
app.MapGeometryEndpoints();

app.Run();
