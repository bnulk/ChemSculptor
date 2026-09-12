using ChemSculptor.Core;
using ChemSculptor.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 工作流相关端点。
/// </summary>
public static class WorkflowEndpoints
{
    /// <summary>登记工作流、任务日志与审批相关路由。</summary>
    public static IEndpointRouteBuilder MapWorkflowEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder workflows = EndpointRouteBuilderExtensions.MapGroup(app, "/workflows");

        EndpointRouteBuilderExtensions.MapPost(workflows, "/", SubmitWorkflowAsync);
        EndpointRouteBuilderExtensions.MapGet(workflows, "/", ListWorkflows);
        EndpointRouteBuilderExtensions.MapGet(workflows, "/{id}", GetWorkflowAsync);
        EndpointRouteBuilderExtensions.MapPost(workflows, "/{id}/run", RunWorkflowAsync);
        EndpointRouteBuilderExtensions.MapPost(workflows, "/{id}/intervene", Intervene);
        EndpointRouteBuilderExtensions.MapGet(app, "/tasks/{workflowId}/log", GetTaskLogAsync);
        EndpointRouteBuilderExtensions.MapPost(app, "/approvals/{id}", Approve);

        return app;
    }

    /// <summary>提交工作流定义。</summary>
    private static async Task<IResult> SubmitWorkflowAsync(
        WorkflowDefinition definition,
        WorkflowEngine engine,
        CancellationToken cancellationToken)
    {
        try
        {
            WorkflowRun run = await engine.SubmitAsync(definition, cancellationToken);
            string location = "/workflows/" + run.Id;
            return Results.Created(location, run);
        }
        catch (InvalidOperationException ex)
        {
            Dictionary<string, string[]> errors = new Dictionary<string, string[]>();
            string[] messages = new string[1];
            messages[0] = ex.Message;
            errors.Add("definition", messages);
            return Results.ValidationProblem(errors);
        }
    }

    /// <summary>列出所有工作流运行记录。</summary>
    private static IResult ListWorkflows(IWorkflowRepository repository)
    {
        return Results.Ok(repository.List());
    }

    /// <summary>按标识查询工作流。</summary>
    private static async Task<IResult> GetWorkflowAsync(
        string id,
        IWorkflowRepository repository,
        CancellationToken cancellationToken)
    {
        WorkflowRun? run = await repository.GetAsync(id, cancellationToken);

        if (run == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(run);
    }

    /// <summary>执行指定工作流。</summary>
    private static async Task<IResult> RunWorkflowAsync(
        string id,
        WorkflowEngine engine,
        CancellationToken cancellationToken)
    {
        try
        {
            WorkflowRun run = await engine.RunAsync(id, cancellationToken);
            return Results.Ok(run);
        }
        catch (KeyNotFoundException ex)
        {
            ApiError error = new ApiError();
            error.Error = ex.Message;
            return Results.NotFound(error);
        }
    }

    /// <summary>人工干预占位端点。</summary>
    private static IResult Intervene(string id, InterveneRequest request)
    {
        InterventionResponse response = new InterventionResponse();
        response.Id = id;
        response.Operation = request.Operation;
        response.NodeId = request.NodeId;
        response.Status = "queued";
        response.Note = "intervention hook is a framework stub";

        return Results.Ok(response);
    }

    /// <summary>读取指定工作流的事件日志。</summary>
    private static async Task<IResult> GetTaskLogAsync(
        string workflowId,
        IWorkflowRepository repository,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkflowEvent> events = await repository.GetEventsAsync(workflowId, cancellationToken);
        return Results.Ok(events);
    }

    /// <summary>人工审批占位端点。</summary>
    private static IResult Approve(string id, ApprovalRequest request)
    {
        ApprovalResponse response = new ApprovalResponse();
        response.Id = id;
        response.Approved = request.Approved;
        response.Status = "recorded";
        response.Note = "approval hook is a framework stub";

        return Results.Ok(response);
    }
}
