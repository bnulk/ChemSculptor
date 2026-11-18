using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChemSculptor.Compute;

/// <summary>
/// 基于文件系统和根工作流账本的作业清单仓储。
/// 不复制、不修改任何作业文件。
/// </summary>
public sealed class FileCalculationWorkflowInventoryRepository
    : ICalculationWorkflowInventoryRepository
{
    private readonly ICalculationWorkspace _workspace;
    private readonly ICalculationWorkflowEvidenceLedgerRepository _ledgerRepository;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>创建带默认账本仓储的文件作业清单仓储。</summary>
    public FileCalculationWorkflowInventoryRepository(
        ICalculationWorkspace workspace)
        : this(
            workspace,
            new FileCalculationWorkflowEvidenceLedgerRepository(workspace))
    {
    }

    /// <summary>创建带指定账本仓储的文件作业清单仓储。</summary>
    public FileCalculationWorkflowInventoryRepository(
        ICalculationWorkspace workspace,
        ICalculationWorkflowEvidenceLedgerRepository ledgerRepository)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (ledgerRepository == null)
        {
            throw new ArgumentNullException(nameof(ledgerRepository));
        }

        _workspace = workspace;
        _ledgerRepository = ledgerRepository;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>按根工作流枚举和筛选全部作业。</summary>
    public async Task<CalculationWorkflowJobInventory> QueryAsync(
        CalculationWorkflowJobInventoryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (string.IsNullOrWhiteSpace(query.RootWorkflowId))
        {
            throw new ArgumentException(
                "根工作流标识不能为空。",
                nameof(query));
        }

        string rootWorkflowId = query.RootWorkflowId.Trim();
        CalculationWorkflowJobInventory inventory =
            new CalculationWorkflowJobInventory();
        inventory.RootWorkflowId = rootWorkflowId;
        inventory.Ledger = await _ledgerRepository.GetAsync(
            rootWorkflowId,
            cancellationToken);

        if (inventory.Ledger == null)
        {
            inventory.Issues.Add(CreateIssue(
                CalculationWorkflowInventoryIssueCodes.LedgerMissing,
                "根工作流账本不存在，无法确认全部作业范围。",
                CalculationWorkflowInventoryIssueSeverity.Error,
                string.Empty,
                string.Empty,
                _workspace.GetWorkflowRunLedgerPath(rootWorkflowId)));
        }
        else
        {
            ValidateLedgerRelations(inventory.Ledger, inventory.Issues);
        }

        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attempts =
            BuildAttemptIndex(inventory.Ledger);
        HashSet<string> indexedJobIds = new HashSet<string>(
            attempts.Keys,
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, CalculationJob> manifests =
            new Dictionary<string, CalculationJob>(
                StringComparer.OrdinalIgnoreCase);
        HashSet<string> invalidManifestJobIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> scannedJobIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string jobId in attempts.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            scannedJobIds.Add(jobId);
            CalculationWorkflowJobAttemptEvidence attempt = attempts[jobId];
            CalculationJob? manifest = await ReadManifestAsync(
                jobId,
                rootWorkflowId,
                attempt.StageId,
                inventory.Issues,
                cancellationToken);

            if (manifest != null)
            {
                manifests[jobId] = manifest;
            }
            else if (File.Exists(_workspace.GetJobManifestPath(jobId)))
            {
                invalidManifestJobIds.Add(jobId);
            }
        }

        if (query.IncludeUnindexedJobs)
        {
            await DiscoverUnindexedJobsAsync(
                rootWorkflowId,
                scannedJobIds,
                attempts,
                manifests,
                inventory.Issues,
                cancellationToken);
        }

        List<CalculationWorkflowJobInventoryEntry> entries =
            BuildEntries(
                indexedJobIds,
                invalidManifestJobIds,
                attempts,
                manifests,
                inventory.Issues);
        inventory.Jobs.AddRange(
            entries.Where(entry => Matches(query, entry)));
        SortEntries(inventory.Jobs);
        inventory.Issues.Sort(CompareIssues);
        return inventory;
    }

    private async Task<CalculationJob?> ReadManifestAsync(
        string jobId,
        string rootWorkflowId,
        string ledgerStageId,
        List<CalculationWorkflowInventoryIssue> issues,
        CancellationToken cancellationToken)
    {
        string jobDirectory = _workspace.GetJobDirectory(jobId);
        string manifestPath = _workspace.GetJobManifestPath(jobId);

        if (!Directory.Exists(jobDirectory))
        {
            issues.Add(CreateIssue(
                CalculationWorkflowInventoryIssueCodes.JobDirectoryMissing,
                "账本引用的作业目录不存在。",
                CalculationWorkflowInventoryIssueSeverity.Error,
                jobId,
                ledgerStageId,
                jobDirectory));
            return null;
        }

        if (!File.Exists(manifestPath))
        {
            issues.Add(CreateIssue(
                CalculationWorkflowInventoryIssueCodes.ManifestMissing,
                "账本引用的作业清单不存在。",
                CalculationWorkflowInventoryIssueSeverity.Error,
                jobId,
                ledgerStageId,
                manifestPath));
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(
                manifestPath,
                cancellationToken);
            CalculationJob? job = JsonSerializer.Deserialize<CalculationJob>(
                json,
                _jsonOptions);

            if (job == null)
            {
                AddInvalidManifestIssue(
                    issues,
                    jobId,
                    ledgerStageId,
                    manifestPath,
                    "作业清单反序列化结果为空。");
                return null;
            }

            ValidateManifestIdentity(
                job,
                jobId,
                rootWorkflowId,
                ledgerStageId,
                manifestPath,
                issues);
            return job;
        }
        catch (JsonException exception)
        {
            AddInvalidManifestIssue(
                issues,
                jobId,
                ledgerStageId,
                manifestPath,
                exception.Message);
            return null;
        }
        catch (IOException exception)
        {
            AddInvalidManifestIssue(
                issues,
                jobId,
                ledgerStageId,
                manifestPath,
                exception.Message);
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            AddInvalidManifestIssue(
                issues,
                jobId,
                ledgerStageId,
                manifestPath,
                exception.Message);
            return null;
        }
    }

    private async Task DiscoverUnindexedJobsAsync(
        string rootWorkflowId,
        HashSet<string> scannedJobIds,
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attempts,
        Dictionary<string, CalculationJob> manifests,
        List<CalculationWorkflowInventoryIssue> issues,
        CancellationToken cancellationToken)
    {
        string jobsDirectory = _workspace.GetJobsDirectory();

        if (!Directory.Exists(jobsDirectory))
        {
            return;
        }

        string[] directories = Directory.GetDirectories(
            jobsDirectory,
            "*",
            SearchOption.TopDirectoryOnly);
        Array.Sort(directories, StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < directories.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string jobId = Path.GetFileName(directories[index]);

            if (string.IsNullOrWhiteSpace(jobId)
                || scannedJobIds.Contains(jobId))
            {
                continue;
            }

            string manifestPath = Path.Combine(
                directories[index],
                CalculationWorkspacePaths.JobManifestFileName);

            if (!File.Exists(manifestPath))
            {
                continue;
            }

            CalculationJob? job = await TryReadManifestAsync(
                manifestPath,
                cancellationToken);

            if (job == null
                || !BelongsToRootWorkflow(job, rootWorkflowId))
            {
                continue;
            }

            string stageId = ResolveStageId(job);
            attempts[jobId] = CreateAttemptFromJob(job, stageId);
            manifests[jobId] = job;
            scannedJobIds.Add(jobId);
            issues.Add(CreateIssue(
                CalculationWorkflowInventoryIssueCodes.JobUnindexed,
                "作业清单可以归属当前根工作流，但账本未登记。",
                CalculationWorkflowInventoryIssueSeverity.Warning,
                jobId,
                stageId,
                manifestPath));
        }
    }

    private async Task<CalculationJob?> TryReadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken)
    {
        try
        {
            string json = await File.ReadAllTextAsync(
                manifestPath,
                cancellationToken);
            return JsonSerializer.Deserialize<CalculationJob>(
                json,
                _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private List<CalculationWorkflowJobInventoryEntry> BuildEntries(
        HashSet<string> indexedJobIds,
        HashSet<string> invalidManifestJobIds,
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attempts,
        Dictionary<string, CalculationJob> manifests,
        List<CalculationWorkflowInventoryIssue> issues)
    {
        List<CalculationWorkflowJobInventoryEntry> entries =
            new List<CalculationWorkflowJobInventoryEntry>();
        List<string> jobIds = new List<string>(attempts.Keys);
        jobIds.Sort(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < jobIds.Count; index++)
        {
            string jobId = jobIds[index];
            CalculationWorkflowJobAttemptEvidence attempt = attempts[jobId];
            CalculationJob? manifest;
            manifests.TryGetValue(jobId, out manifest);
            bool isIndexed = indexedJobIds.Contains(jobId);
            CalculationWorkflowJobInventoryEntry entry =
                new CalculationWorkflowJobInventoryEntry();
            entry.JobId = jobId;
            entry.IsIndexed = isIndexed;
            entry.StageId = attempt.StageId;
            entry.ParentJobId = attempt.ParentJobId;
            entry.AttemptNumber = attempt.AttemptNumber;
            entry.State = attempt.State;
            entry.TaskType = attempt.TaskType;
            entry.Program = attempt.Program;
            entry.CreatedAt = attempt.CreatedAt;
            entry.LedgerAttempt = isIndexed ? attempt : null;
            entry.JobDirectory = _workspace.GetJobDirectory(jobId);
            entry.JobDirectoryExists =
                Directory.Exists(entry.JobDirectory);
            entry.RunDirectory = _workspace.GetRunDirectory(jobId);

            if (manifest == null)
            {
                entry.ManifestStatus =
                    invalidManifestJobIds.Contains(jobId)
                        ? CalculationWorkflowJobManifestStatus.Invalid
                        : CalculationWorkflowJobManifestStatus.Missing;
                entry.RunDirectoryExists =
                    Directory.Exists(entry.RunDirectory);
                entries.Add(entry);
                continue;
            }

            entry.Job = manifest;
            entry.State = manifest.State;
            entry.TaskType = manifest.Spec.TaskType;
            entry.Program = manifest.Spec.Program;
            entry.ParentJobId = manifest.ParentJobId;
            entry.AttemptNumber = manifest.AttemptNumber;
            entry.CreatedAt = manifest.CreatedAt;
            entry.ManifestStatus =
                CalculationWorkflowJobManifestStatus.Present;

            if (!string.IsNullOrWhiteSpace(manifest.RunDirectory))
            {
                entry.RunDirectory = manifest.RunDirectory;
            }

            entry.RunDirectoryExists =
                Directory.Exists(entry.RunDirectory);
            if (!entry.RunDirectoryExists)
            {
                issues.Add(CreateIssue(
                    CalculationWorkflowInventoryIssueCodes.RunDirectoryMissing,
                    "作业清单存在，但运行目录不存在。",
                    CalculationWorkflowInventoryIssueSeverity.Error,
                    jobId,
                    entry.StageId,
                    entry.RunDirectory));
            }

            entries.Add(entry);
        }

        return entries;
    }

    private static bool Matches(
        CalculationWorkflowJobInventoryQuery query,
        CalculationWorkflowJobInventoryEntry entry)
    {
        if (query.StageIds.Count > 0
            && !query.StageIds.Any(
                stageId => string.Equals(
                    stageId,
                    entry.StageId,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (query.States.Count > 0
            && !query.States.Contains(entry.State))
        {
            return false;
        }

        if (query.TaskTypes.Count > 0
            && !query.TaskTypes.Contains(entry.TaskType))
        {
            return false;
        }

        if (query.Programs.Count > 0
            && !query.Programs.Any(
                program => string.Equals(
                    program,
                    entry.Program,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ParentJobId)
            && !string.Equals(
                query.ParentJobId,
                entry.ParentJobId,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool isRecovery =
            entry.AttemptNumber > 0
            || !string.IsNullOrWhiteSpace(entry.ParentJobId);

        if (query.RecoveryAttempt.HasValue
            && query.RecoveryAttempt.Value != isRecovery)
        {
            return false;
        }

        return true;
    }

    private void ValidateLedgerRelations(
        CalculationWorkflowEvidenceLedger ledger,
        List<CalculationWorkflowInventoryIssue> issues)
    {
        HashSet<string> stageIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < ledger.Stages.Count; index++)
        {
            CalculationWorkflowStageEvidence stage = ledger.Stages[index];

            if (!stageIds.Add(stage.StageId))
            {
                issues.Add(CreateIssue(
                    CalculationWorkflowInventoryIssueCodes.DuplicateStage,
                    "账本中同一个阶段重复登记。",
                    CalculationWorkflowInventoryIssueSeverity.Error,
                    string.Empty,
                    stage.StageId,
                    _workspace.GetWorkflowRunLedgerPath(
                        ledger.RootWorkflowId)));
            }
        }

        HashSet<string> attemptJobIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < ledger.JobAttempts.Count; index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt =
                ledger.JobAttempts[index];

            if (!attemptJobIds.Add(attempt.CalculationJobId))
            {
                issues.Add(CreateIssue(
                    CalculationWorkflowInventoryIssueCodes.DuplicateAttempt,
                    "账本中同一个作业尝试重复登记。",
                    CalculationWorkflowInventoryIssueSeverity.Error,
                    attempt.CalculationJobId,
                    attempt.StageId,
                    _workspace.GetWorkflowRunLedgerPath(
                        ledger.RootWorkflowId)));
            }

            if (!string.IsNullOrWhiteSpace(attempt.StageId)
                && !stageIds.Contains(attempt.StageId))
            {
                issues.Add(CreateIssue(
                    CalculationWorkflowInventoryIssueCodes.AttemptStageMissing,
                    "账本作业尝试引用了不存在的阶段。",
                    CalculationWorkflowInventoryIssueSeverity.Error,
                    attempt.CalculationJobId,
                    attempt.StageId,
                    _workspace.GetWorkflowRunLedgerPath(
                        ledger.RootWorkflowId)));
            }

            CalculationWorkflowStageEvidence? stage =
                ledger.Stages.FirstOrDefault(
                    candidate => string.Equals(
                        candidate.StageId,
                        attempt.StageId,
                        StringComparison.OrdinalIgnoreCase));
            if (stage != null
                && !stage.JobAttemptIds.Contains(
                    attempt.CalculationJobId,
                    StringComparer.OrdinalIgnoreCase))
            {
                issues.Add(CreateIssue(
                    CalculationWorkflowInventoryIssueCodes.StageJobReferenceNotListed,
                    "作业尝试已登记，但阶段没有列出该作业。",
                    CalculationWorkflowInventoryIssueSeverity.Error,
                    attempt.CalculationJobId,
                    attempt.StageId,
                    _workspace.GetWorkflowRunLedgerPath(
                        ledger.RootWorkflowId)));
            }
        }

        for (int stageIndex = 0;
            stageIndex < ledger.Stages.Count;
            stageIndex++)
        {
            CalculationWorkflowStageEvidence stage =
                ledger.Stages[stageIndex];

            for (int jobIndex = 0;
                jobIndex < stage.JobAttemptIds.Count;
                jobIndex++)
            {
                string jobId = stage.JobAttemptIds[jobIndex];

                if (!attemptJobIds.Contains(jobId))
                {
                    issues.Add(CreateIssue(
                        CalculationWorkflowInventoryIssueCodes.StageJobReferenceMissing,
                        "阶段引用了账本中不存在的作业尝试。",
                        CalculationWorkflowInventoryIssueSeverity.Error,
                        jobId,
                        stage.StageId,
                        _workspace.GetWorkflowRunLedgerPath(
                            ledger.RootWorkflowId)));
                }
            }
        }
    }

    private static void ValidateManifestIdentity(
        CalculationJob job,
        string expectedJobId,
        string rootWorkflowId,
        string stageId,
        string manifestPath,
        List<CalculationWorkflowInventoryIssue> issues)
    {
        if (!string.Equals(
                job.JobId,
                expectedJobId,
                StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(CreateIssue(
                CalculationWorkflowInventoryIssueCodes.ManifestJobMismatch,
                "作业清单中的作业标识与目录或账本不一致。",
                CalculationWorkflowInventoryIssueSeverity.Error,
                expectedJobId,
                stageId,
                manifestPath));
        }

        if (!BelongsToRootWorkflow(job, rootWorkflowId))
        {
            issues.Add(CreateIssue(
                CalculationWorkflowInventoryIssueCodes.ManifestRootMismatch,
                "作业清单中的根工作流标识与查询范围不一致。",
                CalculationWorkflowInventoryIssueSeverity.Error,
                expectedJobId,
                stageId,
                manifestPath));
        }
    }

    private static void AddInvalidManifestIssue(
        List<CalculationWorkflowInventoryIssue> issues,
        string jobId,
        string stageId,
        string path,
        string message)
    {
        issues.Add(CreateIssue(
            CalculationWorkflowInventoryIssueCodes.ManifestInvalid,
            "作业清单无法读取或解析：" + message,
            CalculationWorkflowInventoryIssueSeverity.Error,
            jobId,
            stageId,
            path));
    }

    private static Dictionary<string, CalculationWorkflowJobAttemptEvidence>
        BuildAttemptIndex(
            CalculationWorkflowEvidenceLedger? ledger)
    {
        Dictionary<string, CalculationWorkflowJobAttemptEvidence> attempts =
            new Dictionary<string, CalculationWorkflowJobAttemptEvidence>(
                StringComparer.OrdinalIgnoreCase);

        if (ledger == null)
        {
            return attempts;
        }

        for (int index = 0; index < ledger.JobAttempts.Count; index++)
        {
            CalculationWorkflowJobAttemptEvidence attempt =
                ledger.JobAttempts[index];

            if (!string.IsNullOrWhiteSpace(attempt.CalculationJobId)
                && !attempts.ContainsKey(attempt.CalculationJobId))
            {
                attempts[attempt.CalculationJobId] = attempt;
            }
        }

        return attempts;
    }

    private static CalculationWorkflowJobAttemptEvidence
        CreateAttemptFromJob(
            CalculationJob job,
            string stageId)
    {
        CalculationWorkflowJobAttemptEvidence attempt =
            new CalculationWorkflowJobAttemptEvidence();
        attempt.CalculationJobId = job.JobId;
        attempt.StageId = stageId;
        attempt.ParentJobId = job.ParentJobId;
        attempt.AttemptNumber = job.AttemptNumber;
        attempt.State = job.State;
        attempt.TaskType = job.Spec.TaskType;
        attempt.Program = job.Spec.Program;
        attempt.CorrectionPlanId = job.CorrectionPlanId;
        attempt.CreatedAt = job.CreatedAt;
        attempt.StartedAt = job.StartedAt;
        attempt.CompletedAt = job.CompletedAt;
        return attempt;
    }

    private static string ResolveStageId(CalculationJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.EvidenceStageId))
        {
            return job.EvidenceStageId;
        }

        return CalculationWorkflowStageIds.ResolveDefaultStageId(
            job.Spec.TaskType);
    }

    private static bool BelongsToRootWorkflow(
        CalculationJob job,
        string rootWorkflowId)
    {
        if (!string.IsNullOrWhiteSpace(job.RootWorkflowId))
        {
            return string.Equals(
                job.RootWorkflowId,
                rootWorkflowId,
                StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(job.WorkflowId))
        {
            return string.Equals(
                job.WorkflowId,
                rootWorkflowId,
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            job.JobId,
            rootWorkflowId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static CalculationWorkflowInventoryIssue CreateIssue(
        string code,
        string message,
        CalculationWorkflowInventoryIssueSeverity severity,
        string jobId,
        string stageId,
        string path)
    {
        CalculationWorkflowInventoryIssue issue =
            new CalculationWorkflowInventoryIssue();
        issue.Code = code;
        issue.Message = message;
        issue.Severity = severity;
        issue.JobId = jobId;
        issue.StageId = stageId;
        issue.Path = path;
        return issue;
    }

    private static void SortEntries(
        List<CalculationWorkflowJobInventoryEntry> entries)
    {
        entries.Sort(
            delegate(
                CalculationWorkflowJobInventoryEntry left,
                CalculationWorkflowJobInventoryEntry right)
            {
                int stage = string.Compare(
                    left.StageId,
                    right.StageId,
                    StringComparison.OrdinalIgnoreCase);

                if (stage != 0)
                {
                    return stage;
                }

                int created = left.CreatedAt.CompareTo(right.CreatedAt);

                if (created != 0)
                {
                    return created;
                }

                return string.Compare(
                    left.JobId,
                    right.JobId,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private static int CompareIssues(
        CalculationWorkflowInventoryIssue left,
        CalculationWorkflowInventoryIssue right)
    {
        int severity = right.Severity.CompareTo(left.Severity);

        if (severity != 0)
        {
            return severity;
        }

        int code = string.Compare(
            left.Code,
            right.Code,
            StringComparison.OrdinalIgnoreCase);

        if (code != 0)
        {
            return code;
        }

        int job = string.Compare(
            left.JobId,
            right.JobId,
            StringComparison.OrdinalIgnoreCase);

        if (job != 0)
        {
            return job;
        }

        return string.Compare(
            left.Path,
            right.Path,
            StringComparison.OrdinalIgnoreCase);
    }
}

