using ChemSculptor.Compute;

namespace ChemSculptor.Conversation;

/// <summary>
/// 管理参数收集的多轮对话状态。
/// 本服务只决定继续提问、取消或准备提交，不创建计算作业。
/// </summary>
public sealed class CalculationParameterConversationService
{
    private readonly IConversationRepository _repository;
    private readonly ICalculationParameterExtractor _extractor;
    private readonly CalculationParameterResolver _resolver;

    /// <summary>创建参数问询闭环服务。</summary>
    public CalculationParameterConversationService(
        IConversationRepository repository,
        ICalculationParameterExtractor extractor,
        CalculationParameterResolver resolver)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (extractor == null)
        {
            throw new ArgumentNullException(nameof(extractor));
        }

        if (resolver == null)
        {
            throw new ArgumentNullException(nameof(resolver));
        }

        _repository = repository;
        _extractor = extractor;
        _resolver = resolver;
    }

    /// <summary>处理一条可能属于参数收集流程的用户消息。</summary>
    public async Task<ParameterConversationTurn> HandleAsync(
        string sessionId,
        string text,
        string coordinateText,
        CalculationSpec baseSpec,
        CalculationParameterResolutionContext resolutionContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException(
                "会话标识不能为空。",
                nameof(sessionId));
        }

        if (baseSpec == null)
        {
            throw new ArgumentNullException(nameof(baseSpec));
        }

        if (resolutionContext == null)
        {
            throw new ArgumentNullException(
                nameof(resolutionContext));
        }

        ConversationSession session =
            await GetOrCreateSessionAsync(
                sessionId,
                cancellationToken);
        PendingCalculationParameterRequest? pending =
            session.PendingParameterRequest;
        string originalText = pending == null
            ? text
            : pending.OriginalText;
        string originalCoordinateText =
            string.IsNullOrWhiteSpace(coordinateText)
                ? pending == null
                    ? string.Empty
                    : pending.CoordinateText
                : coordinateText;

        if (IsCancelText(text))
        {
            session.PendingParameterRequest = null;
            await _repository.SaveSessionAsync(
                session,
                cancellationToken);
            ParameterConversationTurn canceled = CreateTurn(
                ParameterConversationTurnStatus.Canceled,
                "已取消当前参数收集。");
            canceled.OriginalText = originalText;
            canceled.CoordinateText = originalCoordinateText;
            return canceled;
        }

        if (IsRestartText(text))
        {
            session.PendingParameterRequest = null;
            await _repository.SaveSessionAsync(
                session,
                cancellationToken);
            ParameterConversationTurn restarted = CreateTurn(
                ParameterConversationTurnStatus.Restarted,
                "已清除当前参数收集，请重新描述任务。");
            restarted.OriginalText = originalText;
            restarted.CoordinateText = originalCoordinateText;
            return restarted;
        }

        CalculationSpec workingSpec = pending == null
            ? baseSpec
            : pending.CurrentSpec;
        CalculationParameterExtractionContext extractionContext =
            new CalculationParameterExtractionContext();
        extractionContext.Engine = resolutionContext.Engine;
        extractionContext.TaskType = resolutionContext.TaskType;

        CalculationParameterExtractionResult extracted =
            await _extractor.ExtractAsync(
                text,
                extractionContext,
                cancellationToken);
        List<CalculationParameterIntent> intents =
            MergeParameterIntents(
                pending == null
                    ? new List<CalculationParameterIntent>()
                    : pending.ParameterIntents,
                extracted.Intents);

        CalculationParameterResolutionResult resolution =
            _resolver.Resolve(
                workingSpec,
                intents,
                resolutionContext);

        if (resolution.Status
            == CalculationParameterResolutionStatus.Resolved)
        {
            session.PendingParameterRequest = null;
            await _repository.SaveSessionAsync(
                session,
                cancellationToken);

            ParameterConversationTurn resolvedTurn =
                CreateTurn(
                    ParameterConversationTurnStatus.ReadyToSubmit,
                    "参数解析完成，可以提交计算。");
            resolvedTurn.Resolution = resolution;
            resolvedTurn.OriginalText = originalText;
            resolvedTurn.CoordinateText = originalCoordinateText;
            return resolvedTurn;
        }

        if (resolution.Status
            == CalculationParameterResolutionStatus.Rejected)
        {
            session.PendingParameterRequest = null;
            await _repository.SaveSessionAsync(
                session,
                cancellationToken);

            ParameterConversationTurn rejectedTurn =
                CreateTurn(
                    ParameterConversationTurnStatus.Rejected,
                    "参数解析失败，请修正后重新提交。");
            rejectedTurn.Resolution = resolution;
            rejectedTurn.OriginalText = originalText;
            rejectedTurn.CoordinateText = originalCoordinateText;
            return rejectedTurn;
        }

        PendingCalculationParameterRequest nextPending =
            CreatePending(
                sessionId,
                pending,
                text,
                coordinateText,
                resolution,
                intents);
        session.PendingParameterRequest = nextPending;
        await _repository.SaveSessionAsync(
            session,
            cancellationToken);

        ParameterConversationTurn turn =
            CreateTurn(
                ParameterConversationTurnStatus.NeedsClarification,
                "需要继续确认计算参数。");
        turn.Resolution = resolution;
        turn.PendingRequest = nextPending;
        turn.OriginalText = nextPending.OriginalText;
        turn.CoordinateText = nextPending.CoordinateText;
        return turn;
    }

    /// <summary>读取当前会话尚未完成的参数请求。</summary>
    public async Task<PendingCalculationParameterRequest?> GetPendingAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        ConversationSession? session =
            await _repository.GetSessionAsync(
                sessionId,
                cancellationToken);

        if (session == null)
        {
            return null;
        }

        return session.PendingParameterRequest;
    }

    private static List<CalculationParameterIntent>
        MergeParameterIntents(
            IReadOnlyList<CalculationParameterIntent> existing,
            IReadOnlyList<CalculationParameterIntent> incoming)
    {
        HashSet<string> overwrittenKeys =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < incoming.Count; index++)
        {
            if (!string.IsNullOrWhiteSpace(incoming[index].Key))
            {
                overwrittenKeys.Add(incoming[index].Key);
            }
        }

        List<CalculationParameterIntent> merged =
            new List<CalculationParameterIntent>();

        for (int index = 0; index < existing.Count; index++)
        {
            if (!overwrittenKeys.Contains(existing[index].Key))
            {
                merged.Add(existing[index]);
            }
        }

        merged.AddRange(incoming);
        return merged;
    }

    private async Task<ConversationSession> GetOrCreateSessionAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        ConversationSession? session =
            await _repository.GetSessionAsync(
                sessionId,
                cancellationToken);

        if (session != null)
        {
            return session;
        }

        session = new ConversationSession();
        session.SessionId = sessionId;
        session.Title = "新会话";
        session.WorkflowId =
            "workflow-" + Guid.NewGuid().ToString("N");
        await _repository.SaveSessionAsync(
            session,
            cancellationToken);
        return session;
    }

    private static PendingCalculationParameterRequest CreatePending(
        string sessionId,
        PendingCalculationParameterRequest? previous,
        string text,
        string coordinateText,
        CalculationParameterResolutionResult resolution,
        List<CalculationParameterIntent> intents)
    {
        PendingCalculationParameterRequest pending =
            new PendingCalculationParameterRequest();
        pending.SessionId = sessionId;
        pending.OriginalText = previous == null
            ? text
            : previous.OriginalText;
        pending.CoordinateText = string.IsNullOrWhiteSpace(
            coordinateText)
            ? previous == null
                ? string.Empty
                : previous.CoordinateText
            : coordinateText;
        pending.CurrentSpec = resolution.Spec;
        pending.ParameterIntents =
            new List<CalculationParameterIntent>(intents);
        pending.Questions =
            new List<CalculationParameterQuestion>(
                resolution.Questions);
        pending.CreatedAt = previous == null
            ? DateTimeOffset.UtcNow
            : previous.CreatedAt;
        pending.UpdatedAt = DateTimeOffset.UtcNow;

        for (int index = 0;
            index < resolution.Questions.Count;
            index++)
        {
            CalculationParameterQuestion question =
                resolution.Questions[index];

            if (question.QuestionId.IndexOf(
                "conflict",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                pending.ConflictParameters.Add(
                    question.ParameterKey);
            }
            else
            {
                pending.MissingParameters.Add(
                    question.ParameterKey);
            }
        }

        return pending;
    }

    private static ParameterConversationTurn CreateTurn(
        ParameterConversationTurnStatus status,
        string message)
    {
        ParameterConversationTurn turn =
            new ParameterConversationTurn();
        turn.Status = status;
        turn.Message = message;
        return turn;
    }

    private static bool IsCancelText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = text.Trim();
        return string.Equals(
            normalized,
            "取消",
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                normalized,
                "取消计算",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRestartText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string normalized = text.Trim();
        return string.Equals(
            normalized,
            "重新开始",
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                normalized,
                "重来",
                StringComparison.OrdinalIgnoreCase);
    }
}
