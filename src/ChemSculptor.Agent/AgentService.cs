using ChemSculptor.Compute;
using ChemSculptor.Conversation;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.Chemistry;

namespace ChemSculptor.Agent;

/// <summary>
/// 智能体编排服务。
/// 负责任务解释、参数问询、单点计算提交和结果组装。
/// </summary>
public sealed class AgentService : IAgentService
{
    private readonly IConversationService _conversationService;
    private readonly ISinglePointCalculationService _singlePointService;
    private readonly IOptimizationCalculationService
        _optimizationService;
    private readonly CalculationParameterConversationService
        _parameterConversationService;
    private readonly IGeometryTextParser _geometryParser;

    /// <summary>创建智能体服务。</summary>
    public AgentService(
        IConversationService conversationService,
        ISinglePointCalculationService singlePointService,
        IOptimizationCalculationService optimizationService,
        CalculationParameterConversationService parameterConversationService,
        IGeometryTextParser geometryParser)
    {
        if (conversationService == null)
        {
            throw new ArgumentNullException(
                nameof(conversationService));
        }

        if (singlePointService == null)
        {
            throw new ArgumentNullException(
                nameof(singlePointService));
        }

        if (optimizationService == null)
        {
            throw new ArgumentNullException(
                nameof(optimizationService));
        }

        if (parameterConversationService == null)
        {
            throw new ArgumentNullException(
                nameof(parameterConversationService));
        }

        if (geometryParser == null)
        {
            throw new ArgumentNullException(
                nameof(geometryParser));
        }

        _conversationService = conversationService;
        _singlePointService = singlePointService;
        _optimizationService = optimizationService;
        _parameterConversationService = parameterConversationService;
        _geometryParser = geometryParser;
    }

    /// <summary>处理客户端原始消息。</summary>
    public async Task<AgentResult> HandleMessageAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        PendingCalculationParameterRequest? pending =
            await _parameterConversationService.GetPendingAsync(
                request.SessionId,
                cancellationToken);

        if (pending != null)
        {
            if (pending.TaskType
                == CalculationTaskType.Optimization)
            {
                return await HandleOptimizationConversationAsync(
                    request,
                    null,
                    pending,
                    cancellationToken);
            }

            return await HandleParameterConversationAsync(
                request,
                null,
                pending,
                cancellationToken);
        }

        ConversationRequest conversationRequest =
            new ConversationRequest();
        conversationRequest.SessionId = request.SessionId;
        conversationRequest.Text = request.Text;

        ConversationReply conversationReply =
            await _conversationService.HandleMessageAsync(
                conversationRequest,
                cancellationToken);

        if (!conversationReply.Intent.IsSupported)
        {
            AgentResult unsupported = new AgentResult();
            unsupported.IsSupported = false;
            unsupported.Error = conversationReply.ReplyMessage;
            unsupported.Diagnostics =
                new List<string>(conversationReply.Diagnostics);
            return unsupported;
        }

        if (conversationReply.Intent.TaskType
            == CalculationTaskType.Optimization)
        {
            return await HandleOptimizationConversationAsync(
                request,
                conversationReply,
                null,
                cancellationToken);
        }

        if (conversationReply.Intent.TaskType
            != CalculationTaskType.SinglePoint)
        {
            AgentResult notImplemented = new AgentResult();
            notImplemented.IsSupported = false;
            notImplemented.Error = "该任务类型尚未实现。";
            return notImplemented;
        }

        if (conversationReply.Intent.ElectronicStateObjective
            == ElectronicStateObjective.TargetExcitedState)
        {
            AgentResult excitedStateNotImplemented =
                new AgentResult();
            excitedStateNotImplemented.IsSupported = false;
            excitedStateNotImplemented.Error =
                "已识别激发态目标，但当前阶段尚未实现激发态计算。";
            return excitedStateNotImplemented;
        }

        return await HandleParameterConversationAsync(
            request,
            conversationReply,
            null,
            cancellationToken);
    }

    /// <summary>直接执行单点计算。</summary>
    public async Task<AgentResult> ExecuteSinglePointAsync(
        AgentSinglePointRequest request,
        CancellationToken cancellationToken = default)
    {
        CalculationRequest calculationRequest =
            new CalculationRequest();
        calculationRequest.Goal = "单点计算";
        calculationRequest.CoordinateText = request.CoordinateText;

        SinglePointCalculationSubmissionResult submission =
            await _singlePointService.SubmitAsync(
                calculationRequest,
                cancellationToken);

        return BuildSubmissionResult(null, submission);
    }

    private async Task<AgentResult> HandleParameterConversationAsync(
        AgentRequest request,
        ConversationReply? conversationReply,
        PendingCalculationParameterRequest? pending,
        CancellationToken cancellationToken)
    {
        string coordinateText = string.IsNullOrWhiteSpace(
            request.CoordinateText)
            ? pending == null
                ? string.Empty
                : pending.CoordinateText
            : request.CoordinateText;

        int? neutralElectronCount =
            await TryGetNeutralElectronCountAsync(
                coordinateText,
                cancellationToken);

        CalculationParameterResolutionContext resolutionContext =
            new CalculationParameterResolutionContext();
        resolutionContext.Engine =
            CalculationDefaults.DefaultProgram;
        resolutionContext.TaskType =
            CalculationTaskType.SinglePoint;
        resolutionContext.ElectronCount = neutralElectronCount;

        ParameterConversationTurn turn =
            await _parameterConversationService.HandleAsync(
                request.SessionId,
                request.Text,
                coordinateText,
                CalculationDefaults.CreateDefaultSinglePoint(),
                resolutionContext,
                cancellationToken);

        if (turn.Status
            == ParameterConversationTurnStatus.ReadyToSubmit)
        {
            CalculationRequest calculationRequest =
                BuildCalculationRequest(
                    request,
                    conversationReply,
                    turn);

            SinglePointCalculationSubmissionResult submission =
                await _singlePointService.SubmitAsync(
                    calculationRequest,
                    cancellationToken);

            AgentResult submitted =
                BuildSubmissionResult(
                    conversationReply,
                    submission);
            submitted.RequiresUserAction = false;
            submitted.AppliedParameters =
                turn.Resolution == null
                    ? new List<ResolvedCalculationParameter>()
                    : new List<ResolvedCalculationParameter>(
                        turn.Resolution.AppliedParameters);
            return submitted;
        }

        if (turn.Status
            == ParameterConversationTurnStatus.NeedsClarification)
        {
            return BuildClarificationResult(turn);
        }

        if (turn.Status
            == ParameterConversationTurnStatus.Rejected)
        {
            return BuildRejectedResult(turn);
        }

        AgentResult stateResult = new AgentResult();
        stateResult.IsSupported = true;
        stateResult.TaskType = CalculationTaskType.SinglePoint;
        stateResult.Status = turn.Status.ToString();
        stateResult.Message = turn.Message;
        return stateResult;
    }

    private static CalculationRequest BuildCalculationRequest(
        AgentRequest request,
        ConversationReply? conversationReply,
        ParameterConversationTurn turn)
    {
        CalculationRequest calculationRequest =
            new CalculationRequest();
        calculationRequest.SessionId = request.SessionId;
        calculationRequest.Goal = string.IsNullOrWhiteSpace(
            turn.OriginalText)
            ? request.Text
            : turn.OriginalText;
        calculationRequest.CoordinateText =
            turn.CoordinateText;
        calculationRequest.ResolvedSpec = turn.Resolution == null
            ? CalculationDefaults.CreateDefaultSinglePoint()
            : turn.Resolution.Spec;
        calculationRequest.ResolvedParameters =
            turn.Resolution == null
                ? new List<ResolvedCalculationParameter>()
                : new List<ResolvedCalculationParameter>(
                    turn.Resolution.AppliedParameters);

        if (conversationReply != null)
        {
            calculationRequest.ElectronicStateObjective =
                conversationReply.Intent.ElectronicStateObjective;
            calculationRequest.TargetMultiplicity =
                conversationReply.Intent.TargetMultiplicity;
            calculationRequest.TargetStateLabel =
                conversationReply.Intent.TargetStateLabel;
        }

        return calculationRequest;
    }

    private async Task<AgentResult>
        HandleOptimizationConversationAsync(
            AgentRequest request,
            ConversationReply? conversationReply,
            PendingCalculationParameterRequest? pending,
            CancellationToken cancellationToken)
    {
        string coordinateText = string.IsNullOrWhiteSpace(
            request.CoordinateText)
            ? pending == null
                ? string.Empty
                : pending.CoordinateText
            : request.CoordinateText;

        int? neutralElectronCount =
            await TryGetNeutralElectronCountAsync(
                coordinateText,
                cancellationToken);

        CalculationParameterResolutionContext resolutionContext =
            new CalculationParameterResolutionContext();
        resolutionContext.Engine =
            CalculationDefaults.DefaultProgram;
        resolutionContext.TaskType =
            CalculationTaskType.Optimization;
        resolutionContext.ElectronCount = neutralElectronCount;

        ParameterConversationTurn turn =
            await _parameterConversationService.HandleAsync(
                request.SessionId,
                request.Text,
                coordinateText,
                CalculationDefaults.CreateDefaultOptimization(),
                resolutionContext,
                cancellationToken);

        if (turn.Status
            == ParameterConversationTurnStatus.ReadyToSubmit)
        {
            OptimizationRequest optimizationRequest =
                BuildOptimizationRequest(
                    request,
                    turn);
            OptimizationSubmissionResult submission =
                await _optimizationService.SubmitAsync(
                    optimizationRequest,
                    cancellationToken);
            AgentResult submitted =
                BuildOptimizationResult(submission);
            submitted.RequiresUserAction = false;
            submitted.AppliedParameters =
                turn.Resolution == null
                    ? new List<ResolvedCalculationParameter>()
                    : new List<ResolvedCalculationParameter>(
                        turn.Resolution.AppliedParameters);
            return submitted;
        }

        if (turn.Status
            == ParameterConversationTurnStatus.NeedsClarification)
        {
            AgentResult clarification =
                BuildClarificationResult(turn);
            clarification.TaskType =
                CalculationTaskType.Optimization;
            return clarification;
        }

        if (turn.Status
            == ParameterConversationTurnStatus.Rejected)
        {
            AgentResult rejected =
                BuildRejectedResult(turn);
            rejected.TaskType =
                CalculationTaskType.Optimization;
            return rejected;
        }

        AgentResult stateResult = new AgentResult();
        stateResult.IsSupported = true;
        stateResult.TaskType =
            CalculationTaskType.Optimization;
        stateResult.Status = turn.Status.ToString();
        stateResult.Message = turn.Message;
        return stateResult;
    }

    private static OptimizationRequest BuildOptimizationRequest(
        AgentRequest request,
        ParameterConversationTurn turn)
    {
        OptimizationRequest optimizationRequest =
            new OptimizationRequest();
        optimizationRequest.SessionId = request.SessionId;
        optimizationRequest.Goal = string.IsNullOrWhiteSpace(
            turn.OriginalText)
            ? request.Text
            : turn.OriginalText;
        optimizationRequest.CoordinateText =
            turn.CoordinateText;
        optimizationRequest.ResolvedSpec =
            turn.Resolution == null
                ? CalculationDefaults.CreateDefaultOptimization()
                : turn.Resolution.Spec;
        optimizationRequest.ResolvedParameters =
            turn.Resolution == null
                ? new List<ResolvedCalculationParameter>()
                : new List<ResolvedCalculationParameter>(
                    turn.Resolution.AppliedParameters);

        if (optimizationRequest.ResolvedSpec != null)
        {
            optimizationRequest.Charge =
                optimizationRequest.ResolvedSpec.Charge;

            if (optimizationRequest.ResolvedSpec.Multiplicity > 0)
            {
                optimizationRequest.Multiplicity =
                    optimizationRequest.ResolvedSpec.Multiplicity;
            }
        }

        return optimizationRequest;
    }

    private static AgentResult BuildOptimizationResult(
        OptimizationSubmissionResult submission)
    {
        AgentResult result = new AgentResult();
        result.TaskType = CalculationTaskType.Optimization;

        if (!submission.Succeeded || submission.Job == null)
        {
            result.IsSupported = false;
            result.Error = submission.Error;
            result.Diagnostics =
                new List<string>(submission.Diagnostics);
            return result;
        }

        CalculationJob job = submission.Job;
        result.IsSupported = true;
        result.JobId = job.JobId;
        result.Status = job.State.ToString();
        result.InputFilePath = job.SourceInputFilePath;
        result.OutputFilePath = job.OutputFilePath;
        result.Message = submission.Message;
        result.CorrectionCount = submission.CorrectionCount;
        result.FinalJobId = submission.FinalJobId.Length == 0
            ? job.JobId
            : submission.FinalJobId;

        if (submission.Result != null)
        {
            OptimizationResult optimization = submission.Result;
            result.OptimizationConverged =
                optimization.OptimizationConverged;
            result.OptimizationStepCount =
                optimization.StepCount;
            result.FinalEnergy = optimization.FinalEnergy;
            result.FinalStructure =
                FormatOptimizationGeometry(
                    optimization.FinalGeometry);

            if (optimization.FinalConvergence != null)
            {
                result.MaximumForce =
                    optimization.FinalConvergence.MaximumForce;
                result.RmsForce =
                    optimization.FinalConvergence.RmsForce;
                result.MaximumDisplacement =
                    optimization.FinalConvergence
                        .MaximumDisplacement;
                result.RmsDisplacement =
                    optimization.FinalConvergence
                        .RmsDisplacement;
            }
        }

        return result;
    }

    private static string FormatOptimizationGeometry(
        ChemSculptor.InputProcessor.GeometryIntake.CanonicalGeometry?
            geometry)
    {
        if (geometry == null || geometry.Atoms.Count == 0)
        {
            return "缺失";
        }

        string text = string.Empty;

        for (int index = 0;
            index < geometry.Atoms.Count;
            index++)
        {
            ChemSculptor.InputProcessor.GeometryIntake.CanonicalAtom
                atom = geometry.Atoms[index];

            if (index > 0)
            {
                text = text + "; ";
            }

            text = text +
                atom.Element +
                " " +
                atom.X.ToString("F6") +
                " " +
                atom.Y.ToString("F6") +
                " " +
                atom.Z.ToString("F6");
        }

        return text;
    }

    private static AgentResult BuildClarificationResult(
        ParameterConversationTurn turn)
    {
        AgentResult result = new AgentResult();
        result.IsSupported = true;
        result.RequiresUserAction = true;
        result.TaskType = CalculationTaskType.SinglePoint;
        result.Status = "NeedsClarification";
        result.Message = turn.Message;
        result.Questions = MapQuestions(turn);
        result.AppliedParameters =
            turn.Resolution == null
                ? new List<ResolvedCalculationParameter>()
                : new List<ResolvedCalculationParameter>(
                    turn.Resolution.AppliedParameters);
        AddResolutionDiagnostics(result, turn.Resolution);
        return result;
    }

    private static AgentResult BuildRejectedResult(
        ParameterConversationTurn turn)
    {
        AgentResult result = new AgentResult();
        result.IsSupported = false;
        result.TaskType = CalculationTaskType.SinglePoint;
        result.Status = "Rejected";
        result.Error = turn.Message;
        result.AppliedParameters =
            turn.Resolution == null
                ? new List<ResolvedCalculationParameter>()
                : new List<ResolvedCalculationParameter>(
                    turn.Resolution.AppliedParameters);
        AddResolutionDiagnostics(result, turn.Resolution);

        if (result.Diagnostics.Count > 0)
        {
            result.Error = result.Error + " " + result.Diagnostics[0];
        }

        return result;
    }

    private static AgentResult BuildSubmissionResult(
        ConversationReply? conversationReply,
        SinglePointCalculationSubmissionResult submission)
    {
        AgentResult result = new AgentResult();
        result.TaskType = CalculationTaskType.SinglePoint;

        if (!submission.Succeeded || submission.Job == null)
        {
            result.IsSupported = false;
            result.Error = submission.Error;
            result.Diagnostics =
                new List<string>(submission.Diagnostics);
            return result;
        }

        CalculationJob job = submission.Job;
        result.IsSupported = true;
        result.JobId = job.JobId;
        result.Status = job.State.ToString();
        result.InputFilePath = job.SourceInputFilePath;
        result.OutputFilePath = job.OutputFilePath;
        result.Message = conversationReply == null
            ? submission.Message
            : conversationReply.ReplyMessage +
                " " +
                submission.Message;
        result.Diagnostics = conversationReply == null
            ? new List<string>()
            : new List<string>(conversationReply.Diagnostics);
        return result;
    }

    private static List<ConversationQuestion> MapQuestions(
        ParameterConversationTurn turn)
    {
        List<ConversationQuestion> questions =
            new List<ConversationQuestion>();

        if (turn.PendingRequest == null)
        {
            return questions;
        }

        for (int index = 0;
            index < turn.PendingRequest.Questions.Count;
            index++)
        {
            CalculationParameterQuestion source =
                turn.PendingRequest.Questions[index];
            ConversationQuestion question =
                new ConversationQuestion();
            question.QuestionId = source.QuestionId;
            question.QuestionType =
                source.QuestionType.ToString();
            question.Text = source.Prompt;
            question.Options =
                new List<string>(source.Options);
            question.RiskLevel = CalculationRiskLevel.Info;
            questions.Add(question);
        }

        return questions;
    }

    private static void AddResolutionDiagnostics(
        AgentResult result,
        CalculationParameterResolutionResult? resolution)
    {
        if (resolution == null)
        {
            return;
        }

        for (int index = 0;
            index < resolution.Diagnostics.Count;
            index++)
        {
            CalculationParameterDiagnostic diagnostic =
                resolution.Diagnostics[index];
            result.Diagnostics.Add(
                diagnostic.Code + ": " + diagnostic.Message);
        }
    }

    private async Task<int?> TryGetNeutralElectronCountAsync(
        string coordinateText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(coordinateText))
        {
            return null;
        }

        MolecularGeometry geometry =
            await _geometryParser.ParseAsync(
                coordinateText,
                cancellationToken);

        if (geometry.Atoms.Count == 0)
        {
            return null;
        }

        int electronCount;
        string error;

        if (!ElectronCountCalculator.TryCalculate(
            geometry.Atoms,
            0,
            out electronCount,
            out error))
        {
            return null;
        }

        return electronCount;
    }
}
