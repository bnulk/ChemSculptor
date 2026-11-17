using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChemSculptor.Compute;

/// <summary>
/// 基于文件的根工作流证据账本仓储。
/// 每个 RootWorkflowId 对应一个 ledger.json，采用临时文件原子替换写入。
/// </summary>
public sealed class FileCalculationWorkflowEvidenceLedgerRepository
    : ICalculationWorkflowEvidenceLedgerRepository
{
    private static readonly ConcurrentDictionary<
        string,
        SemaphoreSlim> PathLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>(
                StringComparer.OrdinalIgnoreCase);

    private readonly ICalculationWorkspace _workspace;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>创建文件账本仓储。</summary>
    public FileCalculationWorkflowEvidenceLedgerRepository(
        ICalculationWorkspace workspace)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        _workspace = workspace;
        _jsonOptions = new JsonSerializerOptions(
            JsonSerializerDefaults.Web);
        _jsonOptions.WriteIndented = true;
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>读取账本；不存在时返回 null。</summary>
    public async Task<CalculationWorkflowEvidenceLedger?> GetAsync(
        string rootWorkflowId,
        CancellationToken cancellationToken = default)
    {
        string path = _workspace.GetWorkflowRunLedgerPath(
            rootWorkflowId);
        SemaphoreSlim gate = GetPathGate(path);
        await gate.WaitAsync(cancellationToken);

        try
        {
            return await ReadWithoutLockAsync(
                path,
                cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>保存完整账本。</summary>
    public async Task SaveAsync(
        CalculationWorkflowEvidenceLedger ledger,
        CancellationToken cancellationToken = default)
    {
        if (ledger == null)
        {
            throw new ArgumentNullException(nameof(ledger));
        }

        string path = _workspace.GetWorkflowRunLedgerPath(
            ledger.RootWorkflowId);
        SemaphoreSlim gate = GetPathGate(path);
        await gate.WaitAsync(cancellationToken);

        try
        {
            NormalizeLedger(ledger);
            await WriteWithoutLockAsync(
                path,
                ledger,
                cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>登记或更新一个阶段。</summary>
    public async Task<CalculationWorkflowStageEvidence> EnsureStageAsync(
        string rootWorkflowId,
        CalculationWorkflowStageEvidence stage,
        CancellationToken cancellationToken = default)
    {
        if (stage == null)
        {
            throw new ArgumentNullException(nameof(stage));
        }

        if (string.IsNullOrWhiteSpace(stage.StageId))
        {
            throw new ArgumentException(
                "阶段标识不能为空。",
                nameof(stage));
        }

        string path = _workspace.GetWorkflowRunLedgerPath(
            rootWorkflowId);
        SemaphoreSlim gate = GetPathGate(path);
        await gate.WaitAsync(cancellationToken);

        try
        {
            CalculationWorkflowEvidenceLedger ledger =
                await GetOrCreateWithoutLockAsync(
                    rootWorkflowId,
                    path,
                    cancellationToken);
            CalculationWorkflowStageEvidence existing =
                GetOrCreateStage(
                    ledger,
                    stage.StageId,
                    stage.StageType,
                    stage.Order);
            MergeStage(existing, stage);
            RecalculateFromJobs(ledger);
            NormalizeLedger(ledger);
            await WriteWithoutLockAsync(
                path,
                ledger,
                cancellationToken);
            return existing;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>把作业尝试附加到指定阶段。</summary>
    public async Task AttachJobAsync(
        string rootWorkflowId,
        string stageId,
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.JobId))
        {
            throw new ArgumentException(
                "作业标识不能为空。",
                nameof(job));
        }

        if (string.IsNullOrWhiteSpace(stageId))
        {
            throw new ArgumentException(
                "阶段标识不能为空。",
                nameof(stageId));
        }

        string path = _workspace.GetWorkflowRunLedgerPath(
            rootWorkflowId);
        SemaphoreSlim gate = GetPathGate(path);
        await gate.WaitAsync(cancellationToken);

        try
        {
            CalculationWorkflowEvidenceLedger ledger =
                await GetOrCreateWithoutLockAsync(
                    rootWorkflowId,
                    path,
                    cancellationToken);
            ApplyJobIdentity(ledger, job);

            CalculationWorkflowStageEvidence stage =
                GetOrCreateStage(
                    ledger,
                    stageId,
                    stageId,
                    CalculationWorkflowStageIds
                        .ResolveDefaultOrder(stageId));

            if (!stage.JobAttemptIds.Contains(
                    job.JobId,
                    StringComparer.OrdinalIgnoreCase))
            {
                stage.JobAttemptIds.Add(job.JobId);
            }

            CalculationWorkflowJobAttemptEvidence? attempt =
                FindAttempt(ledger, job.JobId);

            if (attempt == null)
            {
                attempt =
                    new CalculationWorkflowJobAttemptEvidence();
                ledger.JobAttempts.Add(attempt);
            }
            else if (!string.Equals(
                         attempt.StageId,
                         stageId,
                         StringComparison.OrdinalIgnoreCase))
            {
                RemoveJobFromStage(
                    ledger,
                    attempt.StageId,
                    job.JobId);
            }

            UpdateAttempt(attempt, stageId, job);
            RecalculateFromJobs(ledger);
            NormalizeLedger(ledger);
            await WriteWithoutLockAsync(
                path,
                ledger,
                cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<CalculationWorkflowEvidenceLedger>
        GetOrCreateWithoutLockAsync(
            string rootWorkflowId,
            string path,
            CancellationToken cancellationToken)
    {
        CalculationWorkflowEvidenceLedger? existing =
            await ReadWithoutLockAsync(path, cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        CalculationWorkflowEvidenceLedger ledger =
            new CalculationWorkflowEvidenceLedger();
        ledger.RootWorkflowId = rootWorkflowId;
        return ledger;
    }

    private async Task<CalculationWorkflowEvidenceLedger?>
        ReadWithoutLockAsync(
            string path,
            CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(
            path,
            cancellationToken);
        return JsonSerializer.Deserialize<
            CalculationWorkflowEvidenceLedger>(
                json,
                _jsonOptions);
    }

    private async Task WriteWithoutLockAsync(
        string path,
        CalculationWorkflowEvidenceLedger ledger,
        CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ledger.UpdatedAt = DateTimeOffset.UtcNow;
        string json = JsonSerializer.Serialize(
            ledger,
            _jsonOptions);
        string temporaryPath =
            path +
            "." +
            Guid.NewGuid().ToString("N") +
            CalculationWorkspacePaths.JsonTemporaryFileSuffix;

        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                json,
                cancellationToken);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static CalculationWorkflowStageEvidence
        GetOrCreateStage(
            CalculationWorkflowEvidenceLedger ledger,
            string stageId,
            string stageType,
            int order)
    {
        CalculationWorkflowStageEvidence? existing =
            ledger.Stages.Find(
                candidate => string.Equals(
                    candidate.StageId,
                    stageId,
                    StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            if (string.IsNullOrWhiteSpace(existing.StageType))
            {
                existing.StageType = string.IsNullOrWhiteSpace(
                        stageType)
                    ? stageId
                    : stageType;
            }

            if (existing.Order <= 0)
            {
                existing.Order = order > 0
                    ? order
                    : CalculationWorkflowStageIds
                        .ResolveDefaultOrder(stageId);
            }

            return existing;
        }

        CalculationWorkflowStageEvidence stage =
            new CalculationWorkflowStageEvidence();
        stage.StageId = stageId;
        stage.StageType = string.IsNullOrWhiteSpace(stageType)
            ? stageId
            : stageType;
        stage.Order = order > 0
            ? order
            : CalculationWorkflowStageIds
                .ResolveDefaultOrder(stageId);
        ledger.Stages.Add(stage);
        return stage;
    }

    private static void MergeStage(
        CalculationWorkflowStageEvidence target,
        CalculationWorkflowStageEvidence source)
    {
        if (source.Order > 0)
        {
            target.Order = source.Order;
        }

        if (!string.IsNullOrWhiteSpace(source.StageType))
        {
            target.StageType = source.StageType;
        }

        if (source.Status != CalculationWorkflowStageStatus.Ready)
        {
            target.Status = source.Status;
        }

        if (source.StartedAt.HasValue
            && (!target.StartedAt.HasValue
                || source.StartedAt.Value
                    < target.StartedAt.Value))
        {
            target.StartedAt = source.StartedAt;
        }

        if (source.CompletedAt.HasValue
            && (!target.CompletedAt.HasValue
                || source.CompletedAt.Value
                    > target.CompletedAt.Value))
        {
            target.CompletedAt = source.CompletedAt;
        }

        if (!string.IsNullOrWhiteSpace(source.SkippedReason))
        {
            target.SkippedReason = source.SkippedReason;
        }

        for (int index = 0;
            index < source.JobAttemptIds.Count;
            index++)
        {
            string jobId = source.JobAttemptIds[index];

            if (!target.JobAttemptIds.Contains(
                    jobId,
                    StringComparer.OrdinalIgnoreCase))
            {
                target.JobAttemptIds.Add(jobId);
            }
        }

        for (int index = 0;
            index < source.Diagnostics.Count;
            index++)
        {
            target.Diagnostics.Add(
                source.Diagnostics[index]);
        }
    }

    private static CalculationWorkflowJobAttemptEvidence?
        FindAttempt(
            CalculationWorkflowEvidenceLedger ledger,
            string jobId)
    {
        return ledger.JobAttempts.Find(
            candidate => string.Equals(
                candidate.CalculationJobId,
                jobId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void ApplyJobIdentity(
        CalculationWorkflowEvidenceLedger ledger,
        CalculationJob job)
    {
        if (string.IsNullOrWhiteSpace(ledger.SessionId)
            && !string.IsNullOrWhiteSpace(job.SessionId))
        {
            ledger.SessionId = job.SessionId;
        }

        if (string.IsNullOrWhiteSpace(ledger.Goal)
            && !string.IsNullOrWhiteSpace(job.Goal))
        {
            ledger.Goal = job.Goal;
        }
    }

    private static void RemoveJobFromStage(
        CalculationWorkflowEvidenceLedger ledger,
        string stageId,
        string jobId)
    {
        CalculationWorkflowStageEvidence? oldStage =
            ledger.Stages.Find(
                candidate => string.Equals(
                    candidate.StageId,
                    stageId,
                    StringComparison.OrdinalIgnoreCase));

        if (oldStage != null)
        {
            oldStage.JobAttemptIds.RemoveAll(
                candidate => string.Equals(
                    candidate,
                    jobId,
                    StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void UpdateAttempt(
        CalculationWorkflowJobAttemptEvidence attempt,
        string stageId,
        CalculationJob job)
    {
        attempt.CalculationJobId = job.JobId;
        attempt.StageId = stageId;
        attempt.ParentJobId = job.ParentJobId ?? string.Empty;
        attempt.AttemptNumber = job.AttemptNumber;
        attempt.State = job.State;
        attempt.TaskType = job.Spec?.TaskType
            ?? CalculationTaskType.SinglePoint;
        attempt.Program = job.Spec?.Program ?? string.Empty;
        attempt.CorrectionPlanId =
            job.CorrectionPlanId ?? string.Empty;
        attempt.CreatedAt = job.CreatedAt;
        attempt.StartedAt = job.StartedAt;
        attempt.CompletedAt = job.CompletedAt;
        attempt.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void RecalculateFromJobs(
        CalculationWorkflowEvidenceLedger ledger)
    {
        for (int stageIndex = 0;
            stageIndex < ledger.Stages.Count;
            stageIndex++)
        {
            CalculationWorkflowStageEvidence stage =
                ledger.Stages[stageIndex];

            if (stage.Status
                == CalculationWorkflowStageStatus.Skipped)
            {
                continue;
            }

            CalculationWorkflowJobAttemptEvidence? latest = null;

            for (int attemptIndex = 0;
                attemptIndex < ledger.JobAttempts.Count;
                attemptIndex++)
            {
                CalculationWorkflowJobAttemptEvidence candidate =
                    ledger.JobAttempts[attemptIndex];

                if (!string.Equals(
                        candidate.StageId,
                        stage.StageId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (latest == null
                    || candidate.UpdatedAt >= latest.UpdatedAt)
                {
                    latest = candidate;
                }
            }

            if (latest == null)
            {
                stage.Status = CalculationWorkflowStageStatus.Ready;
                continue;
            }

            stage.Status = MapStageStatus(latest.State);
            stage.StartedAt = ResolveStageStartedAt(
                ledger,
                stage);
            stage.CompletedAt = IsTerminal(stage.Status)
                ? ResolveStageCompletedAt(ledger, stage)
                : null;
        }

        ledger.Status = ResolveLedgerStatus(ledger);
    }

    private static CalculationWorkflowStageStatus MapStageStatus(
        CalculationJobState state)
    {
        switch (state)
        {
            case CalculationJobState.Created:
            case CalculationJobState.InputGenerated:
            case CalculationJobState.Queued:
            case CalculationJobState.Running:
                return CalculationWorkflowStageStatus.Running;
            case CalculationJobState.Completed:
            case CalculationJobState.Parsed:
            case CalculationJobState.Validated:
                return CalculationWorkflowStageStatus.Completed;
            case CalculationJobState.Canceled:
                return CalculationWorkflowStageStatus.Canceled;
            case CalculationJobState.Failed:
                return CalculationWorkflowStageStatus.Failed;
            default:
                return CalculationWorkflowStageStatus.Ready;
        }
    }

    private static bool IsTerminal(
        CalculationWorkflowStageStatus status)
    {
        return status == CalculationWorkflowStageStatus.Completed
            || status == CalculationWorkflowStageStatus.Failed
            || status == CalculationWorkflowStageStatus.Canceled
            || status == CalculationWorkflowStageStatus.Skipped;
    }

    private static DateTimeOffset? ResolveStageStartedAt(
        CalculationWorkflowEvidenceLedger ledger,
        CalculationWorkflowStageEvidence stage)
    {
        DateTimeOffset? startedAt = null;

        for (int index = 0;
            index < ledger.JobAttempts.Count;
            index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt =
                ledger.JobAttempts[index];

            if (!string.Equals(
                    attempt.StageId,
                    stage.StageId,
                    StringComparison.OrdinalIgnoreCase)
                || !attempt.StartedAt.HasValue)
            {
                continue;
            }

            if (!startedAt.HasValue
                || attempt.StartedAt.Value < startedAt.Value)
            {
                startedAt = attempt.StartedAt;
            }
        }

        return startedAt;
    }

    private static DateTimeOffset? ResolveStageCompletedAt(
        CalculationWorkflowEvidenceLedger ledger,
        CalculationWorkflowStageEvidence stage)
    {
        DateTimeOffset? completedAt = null;

        for (int index = 0;
            index < ledger.JobAttempts.Count;
            index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt =
                ledger.JobAttempts[index];

            if (!string.Equals(
                    attempt.StageId,
                    stage.StageId,
                    StringComparison.OrdinalIgnoreCase)
                || !attempt.CompletedAt.HasValue)
            {
                continue;
            }

            if (!completedAt.HasValue
                || attempt.CompletedAt.Value > completedAt.Value)
            {
                completedAt = attempt.CompletedAt;
            }
        }

        return completedAt;
    }

    private static CalculationWorkflowEvidenceStatus
        ResolveLedgerStatus(
            CalculationWorkflowEvidenceLedger ledger)
    {
        bool hasFailure = false;
        bool hasCancellation = false;
        bool hasActive = false;
        bool hasIncomplete = false;

        for (int index = 0;
            index < ledger.Stages.Count;
            index++)
        {
            CalculationWorkflowStageStatus status =
                ledger.Stages[index].Status;

            if (status == CalculationWorkflowStageStatus.Failed)
            {
                hasFailure = true;
            }
            else if (status
                == CalculationWorkflowStageStatus.Canceled)
            {
                hasCancellation = true;
            }
            else if (status
                == CalculationWorkflowStageStatus.Running)
            {
                hasActive = true;
            }
            else if (status != CalculationWorkflowStageStatus.Completed
                && status != CalculationWorkflowStageStatus.Skipped)
            {
                hasIncomplete = true;
            }
        }

        if (hasFailure)
        {
            return CalculationWorkflowEvidenceStatus.Failed;
        }

        if (hasCancellation)
        {
            return CalculationWorkflowEvidenceStatus.Canceled;
        }

        if (hasActive)
        {
            return CalculationWorkflowEvidenceStatus.Running;
        }

        if (ledger.Stages.Count > 0
            && !hasIncomplete)
        {
            return CalculationWorkflowEvidenceStatus.Completed;
        }

        return CalculationWorkflowEvidenceStatus.Ready;
    }

    private static void NormalizeLedger(
        CalculationWorkflowEvidenceLedger ledger)
    {
        if (string.IsNullOrWhiteSpace(ledger.SchemaVersion))
        {
            ledger.SchemaVersion = "1.0";
        }

        if (string.IsNullOrWhiteSpace(ledger.RootWorkflowId))
        {
            throw new ArgumentException(
                "根工作流标识不能为空。",
                nameof(ledger));
        }

        ledger.Stages.Sort(
            delegate(
                CalculationWorkflowStageEvidence left,
                CalculationWorkflowStageEvidence right)
            {
                int order = left.Order.CompareTo(right.Order);

                if (order != 0)
                {
                    return order;
                }

                return string.Compare(
                    left.StageId,
                    right.StageId,
                    StringComparison.OrdinalIgnoreCase);
            });

        ledger.JobAttempts.Sort(
            delegate(
                CalculationWorkflowJobAttemptEvidence left,
                CalculationWorkflowJobAttemptEvidence right)
            {
                int created = left.CreatedAt.CompareTo(
                    right.CreatedAt);

                if (created != 0)
                {
                    return created;
                }

                return string.Compare(
                    left.CalculationJobId,
                    right.CalculationJobId,
                    StringComparison.OrdinalIgnoreCase);
            });

        ledger.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static SemaphoreSlim GetPathGate(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return PathLocks.GetOrAdd(
            fullPath,
            static _ => new SemaphoreSlim(1, 1));
    }
}
