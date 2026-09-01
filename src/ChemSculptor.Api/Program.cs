using System.Text.Json;
using ChemSculptor.Api;
using ChemSculptor.Core;
using ChemSculptor.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IContainerRegistry, ContainerRegistry>();
builder.Services.AddSingleton<IWorkflowRepository, InMemoryWorkflowRepository>();
builder.Services.AddSingleton<IRuleEngine, AllowAllRuleEngine>();
builder.Services.AddSingleton<IValidationGate, PassThroughValidationGate>();
builder.Services.AddSingleton<ICaseMemory, InMemoryCaseMemory>();
builder.Services.AddSingleton<EchoSkillContainer>();
builder.Services.AddSingleton<WorkflowEngine>();

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
        "POST /containers/register"
    }
}));

app.MapWorkflowEndpoints();
app.MapContainerEndpoints();

app.Run();
