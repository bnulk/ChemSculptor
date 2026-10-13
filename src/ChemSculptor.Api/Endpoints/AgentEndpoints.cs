using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Conversation;
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

        AgentResult result = await agentService.HandleMessageAsync(agentRequest, cancellationToken);

        if (!result.IsSupported)
        {
            AgentMessageResponse unsupported = new AgentMessageResponse();
            unsupported.TaskType = GetTaskTypeText(result);
            unsupported.Status = "Unsupported";
            unsupported.Message = result.Error;
            unsupported.Diagnostics = new List<string>(result.Diagnostics);
            unsupported.RequiresUserAction = result.RequiresUserAction;
            unsupported.Questions = ConvertQuestions(result.Questions);
            unsupported.AppliedParameters =
                ConvertParameters(result.AppliedParameters);
            return Results.BadRequest(unsupported);
        }

        AgentMessageResponse response = new AgentMessageResponse();
        response.TaskType = GetTaskTypeText(result);
        response.JobId = result.JobId;
        response.Status = result.Status;
        response.InputFilePath = result.InputFilePath;
        response.OutputFilePath = result.OutputFilePath;
        response.Message = result.Message;
        response.Diagnostics = new List<string>(result.Diagnostics);
        response.RequiresUserAction = result.RequiresUserAction;
        response.Questions = ConvertQuestions(result.Questions);
        response.AppliedParameters =
            ConvertParameters(result.AppliedParameters);
        response.OptimizationConverged =
            result.OptimizationConverged;
        response.OptimizationStepCount =
            result.OptimizationStepCount;
        response.FinalEnergy = result.FinalEnergy;
        response.FinalStructure = result.FinalStructure;
        response.MaximumForce = result.MaximumForce;
        response.RmsForce = result.RmsForce;
        response.MaximumDisplacement =
            result.MaximumDisplacement;
        response.RmsDisplacement = result.RmsDisplacement;
        response.CorrectionCount = result.CorrectionCount;
        response.FinalJobId = result.FinalJobId;
        response.WavefunctionStabilityStatus =
            result.WavefunctionStabilityStatus;
        response.WavefunctionStabilitySummary =
            result.WavefunctionStabilitySummary;

        return Results.Ok(response);
    }

    private static List<AgentQuestionResponse> ConvertQuestions(
        List<ConversationQuestion> questions)
    {
        List<AgentQuestionResponse> responses =
            new List<AgentQuestionResponse>();

        for (int index = 0; index < questions.Count; index++)
        {
            ConversationQuestion question = questions[index];
            AgentQuestionResponse response =
                new AgentQuestionResponse();
            response.QuestionId = question.QuestionId;
            response.QuestionType = question.QuestionType;
            response.Text = question.Text;
            response.Options = new List<string>(question.Options);
            response.RiskLevel = question.RiskLevel.ToString();
            responses.Add(response);
        }

        return responses;
    }

    private static List<AgentAppliedParameterResponse> ConvertParameters(
        List<ResolvedCalculationParameter> parameters)
    {
        List<AgentAppliedParameterResponse> responses =
            new List<AgentAppliedParameterResponse>();

        for (int index = 0; index < parameters.Count; index++)
        {
            ResolvedCalculationParameter parameter = parameters[index];
            CalculationParameterDefinition? definition =
                CalculationParameterCatalog.Find(parameter.Key);
            AgentAppliedParameterResponse response =
                new AgentAppliedParameterResponse();
            response.Key = parameter.Key;
            response.DisplayName = definition == null
                ? parameter.Key
                : definition.DisplayName;
            response.Value = parameter.Value;
            response.RawText = parameter.RawText;
            response.Source = parameter.Source.ToString();
            response.Confidence = parameter.Confidence;
            response.RiskLevel = parameter.RiskLevel.ToString();
            response.RequiresApproval = parameter.RequiresApproval;
            response.TranslatedKeyword = parameter.TranslatedKeyword;
            responses.Add(response);
        }

        return responses;
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
