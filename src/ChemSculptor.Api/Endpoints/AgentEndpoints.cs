using ChemSculptor.Agent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 智能体消息端点。
/// 只负责把 HTTP 请求转换为智能体请求并返回响应。
/// </summary>
public static class AgentEndpoints
{
    /// <summary>登记智能体消息路由。</summary>
    public static IEndpointRouteBuilder MapAgentEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder agent = EndpointRouteBuilderExtensions.MapGroup(app, "/agent");

        EndpointRouteBuilderExtensions.MapPost(agent, "/messages", SubmitMessageAsync);

        return app;
    }

    /// <summary>把客户端原始消息交给智能体处理。</summary>
    private static async Task<IResult> SubmitMessageAsync(
        AgentMessageRequest request,
        IAgentService agentService,
        CancellationToken cancellationToken)
    {
        AgentRequest agentRequest = new AgentRequest();
        agentRequest.SessionId = request.SessionId;
        agentRequest.Text = request.Text;
        agentRequest.CoordinateText = request.CoordinateText;
        agentRequest.Charge = request.Charge;
        agentRequest.Multiplicity = request.Multiplicity;

        AgentResult result = await agentService.HandleMessageAsync(agentRequest, cancellationToken);

        if (!result.IsSupported)
        {
            AgentMessageResponse unsupported = new AgentMessageResponse();
            unsupported.TaskType = GetTaskTypeText(result);
            unsupported.Status = "Unsupported";
            unsupported.Message = result.Error;
            unsupported.Diagnostics = new List<string>(result.Diagnostics);
            return Results.BadRequest(unsupported);
        }

        AgentMessageResponse response = new AgentMessageResponse();
        response.TaskType = GetTaskTypeText(result);
        response.JobId = result.JobId;
        response.Status = result.Status;
        response.InputFilePath = result.InputFilePath;
        response.Message = result.Message;
        response.Diagnostics = new List<string>(result.Diagnostics);

        return Results.Ok(response);
    }

    private static string GetTaskTypeText(AgentResult result)
    {
        if (result.TaskType == null)
        {
            return string.Empty;
        }

        return result.TaskType.Value.ToString();
    }
}
