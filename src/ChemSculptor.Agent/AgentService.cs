using ChemSculptor.Compute;
using ChemSculptor.Conversation;

namespace ChemSculptor.Agent;

/// <summary>
/// 智能体编排服务。
/// 先调用会话层解释意图，再根据意图调用单点计算服务。
/// </summary>
public sealed class AgentService : IAgentService
{
    private readonly IConversationService _conversationService;
    private readonly ISinglePointCalculationService _singlePointService;

    /// <summary>创建智能体服务。</summary>
    public AgentService(
        IConversationService conversationService,
        ISinglePointCalculationService singlePointService)
    {
        _conversationService = conversationService;
        _singlePointService = singlePointService;
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

        CalculationRequest calculationRequest = new CalculationRequest();
        calculationRequest.SessionId = request.SessionId;
        calculationRequest.Goal = request.Text;
        calculationRequest.CoordinateText = request.CoordinateText;

        SinglePointCalculationSubmissionResult submission =
            await _singlePointService.SubmitAsync(
                calculationRequest,
                cancellationToken);

        return BuildSinglePointResult(conversationReply, submission);
    }

    /// <summary>直接执行单点计算。</summary>
    public async Task<AgentResult> ExecuteSinglePointAsync(
        AgentSinglePointRequest request,
        CancellationToken cancellationToken = default)
    {
        CalculationRequest calculationRequest = new CalculationRequest();
        calculationRequest.Goal = "单点计算";
        calculationRequest.CoordinateText = request.CoordinateText;

        SinglePointCalculationSubmissionResult submission =
            await _singlePointService.SubmitAsync(
                calculationRequest,
                cancellationToken);

        AgentResult result = new AgentResult();
        result.TaskType = CalculationTaskType.SinglePoint;

        if (!submission.Succeeded || submission.Job == null)
        {
            result.IsSupported = false;
            result.Error = submission.Error;
            result.Diagnostics = new List<string>(submission.Diagnostics);
            return result;
        }

        result.IsSupported = true;
        result.JobId = submission.Job.JobId;
        result.Status = submission.Job.State.ToString();
        result.InputFilePath = submission.Job.SourceInputFilePath;
        result.OutputFilePath = submission.Job.OutputFilePath;
        result.Message = submission.Message;
        return result;
    }

    private static AgentResult BuildSinglePointResult(
        ConversationReply conversationReply,
        SinglePointCalculationSubmissionResult submission)
    {
        AgentResult result = new AgentResult();
        result.TaskType = CalculationTaskType.SinglePoint;

        if (!submission.Succeeded || submission.Job == null)
        {
            result.IsSupported = false;
            result.Error = submission.Error;
            result.Diagnostics = new List<string>(submission.Diagnostics);
            return result;
        }

        CalculationJob job = submission.Job;
        result.IsSupported = true;
        result.JobId = job.JobId;
        result.Status = job.State.ToString();
        result.InputFilePath = job.SourceInputFilePath;
        result.OutputFilePath = job.OutputFilePath;
        result.Message = conversationReply.ReplyMessage + " " + submission.Message;
        result.Diagnostics = new List<string>(conversationReply.Diagnostics);

        return result;
    }
}
