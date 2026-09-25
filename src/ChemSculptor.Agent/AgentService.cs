using ChemSculptor.Compute;
using ChemSculptor.Conversation;

namespace ChemSculptor.Agent;

/// <summary>
/// 智能体编排服务。
/// 先调用会话层解释意图，再根据意图调用计算执行器。
/// </summary>
public sealed class AgentService : IAgentService
{
    private readonly IConversationService _conversationService;
    private readonly SinglePointCalculationExecutor _executor;

    /// <summary>创建智能体服务。</summary>
    public AgentService(
        IConversationService conversationService,
        SinglePointCalculationExecutor executor)
    {
        _conversationService = conversationService;
        _executor = executor;
    }

    /// <summary>处理客户端原始消息。</summary>
    public async Task<AgentResult> HandleMessageAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        ConversationRequest conversationRequest = new ConversationRequest();
        conversationRequest.SessionId = request.SessionId;
        conversationRequest.Text = request.Text;

        ConversationReply conversationReply =
            await _conversationService.HandleMessageAsync(conversationRequest, cancellationToken);

        if (!conversationReply.Intent.IsSupported)
        {
            AgentResult unsupported = new AgentResult();
            unsupported.IsSupported = false;
            unsupported.Error = conversationReply.ReplyMessage;
            unsupported.Diagnostics = new List<string>(conversationReply.Diagnostics);
            return unsupported;
        }

        if (conversationReply.Intent.TaskType != CalculationTaskType.SinglePoint)
        {
            AgentResult notImplemented = new AgentResult();
            notImplemented.IsSupported = false;
            notImplemented.Error = "该任务类型尚未实现。";
            return notImplemented;
        }

        SinglePointExecutionResult executionResult = await _executor.ExecuteAsync(
            request.CoordinateText,
            request.Charge,
            request.Multiplicity,
            cancellationToken);

        return BuildSinglePointResult(conversationReply, executionResult);
    }

    /// <summary>直接执行单点计算。</summary>
    public async Task<AgentResult> ExecuteSinglePointAsync(
        AgentSinglePointRequest request,
        CancellationToken cancellationToken = default)
    {
        SinglePointExecutionResult executionResult = await _executor.ExecuteAsync(
            request.CoordinateText,
            request.Charge,
            request.Multiplicity,
            cancellationToken);

        AgentResult result = new AgentResult();
        result.TaskType = CalculationTaskType.SinglePoint;

        if (!executionResult.Succeeded)
        {
            result.IsSupported = false;
            result.Error = executionResult.Error;
            result.Diagnostics = new List<string>(executionResult.Diagnostics);
            return result;
        }

        result.IsSupported = true;
        result.JobId = executionResult.JobId;
        result.Status = executionResult.Status;
        result.InputFilePath = executionResult.InputFilePath;
        result.Message = executionResult.Message;
        return result;
    }

    private static AgentResult BuildSinglePointResult(
        ConversationReply conversationReply,
        SinglePointExecutionResult executionResult)
    {
        AgentResult result = new AgentResult();
        result.TaskType = CalculationTaskType.SinglePoint;

        if (!executionResult.Succeeded)
        {
            result.IsSupported = false;
            result.Error = executionResult.Error;
            result.Diagnostics = new List<string>(executionResult.Diagnostics);
            return result;
        }

        result.IsSupported = true;
        result.JobId = executionResult.JobId;
        result.Status = executionResult.Status;
        result.InputFilePath = executionResult.InputFilePath;
        result.Message = conversationReply.ReplyMessage + " " + executionResult.Message;
        result.Diagnostics = new List<string>(conversationReply.Diagnostics);

        return result;
    }
}
