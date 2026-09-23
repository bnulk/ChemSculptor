using ChemSculptor.Compute;
using ChemSculptor.Conversation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 智能体消息端点。
/// 客户端只发送原始自然语言，服务器负责解释任务类型。
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

    /// <summary>
    /// 先把原始消息交给会话层，再根据意图决定是否执行计算。
    /// </summary>
    private static async Task<IResult> SubmitMessageAsync(
        AgentMessageRequest request,
        IConversationService conversationService,
        SinglePointCalculationExecutor executor,
        CancellationToken cancellationToken)
    {
        ConversationRequest conversationRequest = new ConversationRequest();
        conversationRequest.SessionId = request.SessionId;
        conversationRequest.Text = request.Text;

        ConversationReply conversationReply =
            await conversationService.HandleMessageAsync(conversationRequest, cancellationToken);

        if (!conversationReply.Intent.IsSupported)
        {
            AgentMessageResponse unsupported = new AgentMessageResponse();
            unsupported.TaskType = string.Empty;
            unsupported.Status = "Unsupported";
            unsupported.Message = conversationReply.ReplyMessage;
            unsupported.Diagnostics = new List<string>(conversationReply.Diagnostics);
            return Results.BadRequest(unsupported);
        }

        if (conversationReply.Intent.TaskType != CalculationTaskType.SinglePoint)
        {
            AgentMessageResponse notImplemented = new AgentMessageResponse();
            notImplemented.TaskType = string.Empty;
            notImplemented.Status = "NotImplemented";
            notImplemented.Message = "该任务类型尚未实现。";
            return Results.BadRequest(notImplemented);
        }

        SinglePointExecutionResult executionResult = await executor.ExecuteAsync(
            request.CoordinateText,
            request.Charge,
            request.Multiplicity,
            cancellationToken);

        if (!executionResult.Succeeded)
        {
            CalculationErrorResponse error = new CalculationErrorResponse();
            error.Error = executionResult.Error;
            error.Diagnostics = new List<string>(executionResult.Diagnostics);
            return Results.BadRequest(error);
        }

        AgentMessageResponse response = new AgentMessageResponse();
        response.TaskType = CalculationTaskType.SinglePoint.ToString();
        response.JobId = executionResult.JobId;
        response.Status = executionResult.Status;
        response.InputFilePath = executionResult.InputFilePath;
        response.Message = conversationReply.ReplyMessage + " " + executionResult.Message;
        response.Diagnostics = new List<string>(conversationReply.Diagnostics);

        return Results.Ok(response);
    }
}
