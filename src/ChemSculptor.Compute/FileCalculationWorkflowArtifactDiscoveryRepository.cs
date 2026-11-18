using System.Collections.Concurrent;
using System.IO.Enumeration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChemSculptor.Compute;

/// <summary>
/// 基于工作区文件系统和根工作流清单的全过程产物发现仓储。
/// 只登记持久化候选文件，不复制文件，也不计算 SHA-256。
/// </summary>
public sealed class FileCalculationWorkflowArtifactDiscoveryRepository
    : ICalculationWorkflowArtifactDiscoveryRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim>
        PathLocks = new ConcurrentDictionary<string, SemaphoreSlim>(
            StringComparer.OrdinalIgnoreCase);

    private readonly ICalculationWorkspace _workspace;
    private readonly ICalculationWorkflowInventoryRepository _inventory;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>创建使用默认清单和空程序注册表的发现仓储。</summary>
    public FileCalculationWorkflowArtifactDiscoveryRepository(
        ICalculationWorkspace workspace)
        : this(
            workspace,
            new FileCalculationWorkflowInventoryRepository(workspace),
            new EmptyQuantumProgramAdapterRegistry())
    {
    }

    /// <summary>创建使用指定清单和程序注册表的发现仓储。</summary>
    public FileCalculationWorkflowArtifactDiscoveryRepository(
        ICalculationWorkspace workspace,
        ICalculationWorkflowInventoryRepository inventory,
        IQuantumProgramAdapterRegistry adapterRegistry)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        _workspace = workspace;
        _inventory = inventory;
        _adapterRegistry = adapterRegistry;
        _jsonOptions = new JsonSerializerOptions(
            JsonSerializerDefaults.Web);
        _jsonOptions.WriteIndented = true;
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>建立并持久化一个根工作流的全过程产物发现快照。</summary>
    public async Task<CalculationWorkflowArtifactDiscoverySnapshot> DiscoverAsync(
        string rootWorkflowId,
        CancellationToken cancellationToken = default)
    {
        string root = NormalizeRootWorkflowId(rootWorkflowId);
        CalculationWorkflowArtifactDiscoverySnapshot snapshot =
            new CalculationWorkflowArtifactDiscoverySnapshot();
        snapshot.PolicyVersion =
            CalculationWorkflowArtifactDiscoveryPolicy.Version;
        snapshot.RootWorkflowId = root;

        CalculationWorkflowJobInventory inventory =
            await _inventory.QueryAsync(
                new CalculationWorkflowJobInventoryQuery
                {
                    RootWorkflowId = root,
                    IncludeUnindexedJobs = true
                },
                cancellationToken);
        snapshot.CapturedAt = inventory.CapturedAt;
        CopyInventoryIssues(inventory, snapshot);

        if (inventory.Ledger == null)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes.LedgerMissing,
                "根工作流账本不存在，无法确认全部作业范围。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        List<CalculationWorkflowJobInventoryEntry> jobs =
            new List<CalculationWorkflowJobInventoryEntry>(inventory.Jobs);
        jobs.Sort(
            delegate(
                CalculationWorkflowJobInventoryEntry left,
                CalculationWorkflowJobInventoryEntry right)
            {
                int leftOrder = ResolveStageOrder(
                    inventory.Ledger,
                    left.StageId);
                int rightOrder = ResolveStageOrder(
                    inventory.Ledger,
                    right.StageId);
                int order = leftOrder.CompareTo(rightOrder);

                if (order != 0)
                {
                    return order;
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

        for (int index = 0; index < jobs.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DiscoverJob(
                snapshot,
                inventory.Ledger,
                jobs[index],
                cancellationToken);
        }

        NormalizeSnapshot(snapshot);
        RecalculateSummary(snapshot);
        await SaveAsync(snapshot, cancellationToken);
        return snapshot;
    }

    /// <summary>读取最近一次持久化的产物发现快照。</summary>
    public async Task<CalculationWorkflowArtifactDiscoverySnapshot?> GetAsync(
        string rootWorkflowId,
        CancellationToken cancellationToken = default)
    {
        string root = NormalizeRootWorkflowId(rootWorkflowId);
        string path = _workspace.GetWorkflowRunArtifactDiscoveryPath(root);
        SemaphoreSlim gate = GetPathGate(path);
        await gate.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(
                path,
                cancellationToken);
            return JsonSerializer.Deserialize<
                CalculationWorkflowArtifactDiscoverySnapshot>(
                    json,
                    _jsonOptions);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task SaveAsync(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CancellationToken cancellationToken)
    {
        string path = _workspace.GetWorkflowRunArtifactDiscoveryPath(
            snapshot.RootWorkflowId);
        SemaphoreSlim gate = GetPathGate(path);
        await gate.WaitAsync(cancellationToken);

        try
        {
            string? directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            string temporaryPath = path
                + "."
                + Guid.NewGuid().ToString("N")
                + CalculationWorkspacePaths.JsonTemporaryFileSuffix;

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
        finally
        {
            gate.Release();
        }
    }

    private static SemaphoreSlim GetPathGate(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return PathLocks.GetOrAdd(
            fullPath,
            static _ => new SemaphoreSlim(1, 1));
    }

    private static string NormalizeRootWorkflowId(string rootWorkflowId)
    {
        if (string.IsNullOrWhiteSpace(rootWorkflowId))
        {
            throw new ArgumentException(
                "根工作流标识不能为空。",
                nameof(rootWorkflowId));
        }

        return rootWorkflowId.Trim();
    }

    private void DiscoverJob(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowEvidenceLedger? ledger,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        CancellationToken cancellationToken)
    {
        int stageOrder = ResolveStageOrder(
            ledger,
            inventoryEntry.StageId);
        CalculationWorkflowArtifactDiscoveryJobSummary jobSummary =
            new CalculationWorkflowArtifactDiscoveryJobSummary();
        jobSummary.StageId = inventoryEntry.StageId;
        jobSummary.StageOrder = stageOrder;
        jobSummary.JobId = inventoryEntry.JobId;
        jobSummary.AttemptNumber = inventoryEntry.AttemptNumber;
        jobSummary.State = inventoryEntry.State;
        jobSummary.TaskType = inventoryEntry.TaskType;
        jobSummary.Program = inventoryEntry.Program;
        jobSummary.ManifestStatus = inventoryEntry.ManifestStatus;

        string jobDirectory = _workspace.GetJobDirectory(
            inventoryEntry.JobId);
        string runDirectory = _workspace.GetRunDirectory(
            inventoryEntry.JobId);
        jobSummary.JobDirectoryExists = Directory.Exists(jobDirectory);
        jobSummary.RunDirectoryExists = Directory.Exists(runDirectory);
        snapshot.Jobs.Add(jobSummary);

        Dictionary<string, CalculationWorkflowArtifactDiscoveryCandidate>
            candidates = new Dictionary<
                string,
                CalculationWorkflowArtifactDiscoveryCandidate>(
                StringComparer.OrdinalIgnoreCase);
        AddFixedCandidates(
            snapshot,
            inventoryEntry,
            jobDirectory,
            candidates);

        CalculationJob discoveryJob = inventoryEntry.Job
            ?? CreateDiscoveryJob(inventoryEntry);
        ValidateRunDirectory(
            snapshot,
            inventoryEntry,
            discoveryJob,
            runDirectory);

        if (inventoryEntry.ManifestStatus
            != CalculationWorkflowJobManifestStatus.Present)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .ManifestUnavailable,
                "作业清单不可用，无法完整解析程序专用产物规则。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                CalculationWorkspacePaths.JobManifestFileName);
        }

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(discoveryJob.Spec);

        if (adapter == null)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .AdapterMissing,
                "没有可处理该作业的程序适配器。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                string.Empty);
        }
        else
        {
            AddAdapterCandidates(
                snapshot,
                inventoryEntry,
                jobDirectory,
                discoveryJob,
                adapter,
                candidates,
                cancellationToken);
        }

        if (!jobSummary.JobDirectoryExists)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .JobDirectoryMissing,
                "账本引用的作业目录不存在。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                string.Empty);
        }
        else
        {
            ScanDirectory(
                snapshot,
                inventoryEntry,
                jobDirectory,
                jobDirectory,
                candidates,
                cancellationToken);
        }

        ProcessCandidates(
            snapshot,
            inventoryEntry,
            candidates);
        UpdateJobSummary(snapshot, jobSummary);
    }

    private static CalculationJob CreateDiscoveryJob(
        CalculationWorkflowJobInventoryEntry entry)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = entry.JobId;
        job.RootWorkflowId = string.Empty;
        job.EvidenceStageId = entry.StageId;
        job.AttemptNumber = entry.AttemptNumber;
        job.State = entry.State;
        job.Spec.TaskType = entry.TaskType;
        job.Spec.Program = entry.Program;
        job.CreatedAt = entry.CreatedAt;
        return job;
    }

    private static void ValidateRunDirectory(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry entry,
        CalculationJob job,
        string workspaceRunDirectory)
    {
        if (string.IsNullOrWhiteSpace(job.RunDirectory))
        {
            return;
        }

        try
        {
            string expected = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(workspaceRunDirectory));
            string actual = Path.TrimEndingDirectorySeparator(
                Path.GetFullPath(job.RunDirectory));
            StringComparison comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!string.Equals(expected, actual, comparison))
            {
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .RunDirectoryConflict,
                    "工作区约定和作业清单给出了不同的运行目录；发现过程采用工作区约定目录。",
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    entry.StageId,
                    entry.JobId,
                    "run");
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException
            || exception is NotSupportedException
            || exception is PathTooLongException)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .RunDirectoryConflict,
                "运行目录无法规范化：" + exception.Message,
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                entry.StageId,
                entry.JobId,
                "run");
        }
    }

    private void AddFixedCandidates(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string jobDirectory,
        Dictionary<string, CalculationWorkflowArtifactDiscoveryCandidate> candidates)
    {
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            CalculationWorkspacePaths.JobManifestFileName,
            CalculationWorkflowArtifactRole.JobManifest,
            CalculationWorkflowArtifactRequirement.Required,
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "application/json",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            CalculationWorkspacePaths.JobParametersFileName,
            CalculationWorkflowArtifactRole.ParameterAudit,
            RequireWhenStarted(inventoryEntry.State),
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "application/json",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            "input/" + CalculationWorkspacePaths.JobCoordinatesFileName,
            CalculationWorkflowArtifactRole.GeometryInput,
            RequireWhenStarted(inventoryEntry.State),
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "text/plain; charset=utf-8",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            "run/" + CalculationWorkspacePaths.JobOutputFileName,
            CalculationWorkflowArtifactRole.PrimaryOutput,
            RequiresPrimaryOutput(inventoryEntry.State),
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "text/plain; charset=utf-8",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            "results/processing-plan.json",
            CalculationWorkflowArtifactRole.ProcessingPlan,
            CalculationWorkflowArtifactRequirement.Optional,
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "application/json",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            "results/program-processing-plan.json",
            CalculationWorkflowArtifactRole.ProcessingPlan,
            CalculationWorkflowArtifactRequirement.Optional,
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "application/json",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            "results/" + CalculationWorkspacePaths.JobResultFileName,
            CalculationWorkflowArtifactRole.NormalizedResult,
            RequiresNormalizedResult(inventoryEntry.State),
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "application/json",
            false,
            candidates);
        AddCandidate(
            snapshot,
            inventoryEntry,
            jobDirectory,
            "results/" + CalculationWorkspacePaths.JobValidationFileName,
            CalculationWorkflowArtifactRole.ValidationEvidence,
            inventoryEntry.State == CalculationJobState.Validated
                ? CalculationWorkflowArtifactRequirement.Required
                : CalculationWorkflowArtifactRequirement.Optional,
            CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
            "application/json",
            false,
            candidates);

        CalculationJob? job = inventoryEntry.Job;

        if (job != null
            && !string.IsNullOrWhiteSpace(job.SourceInputFilePath))
        {
            if (CalculationWorkflowArtifactPathPolicy.TryResolveWithinJob(
                    jobDirectory,
                    job.SourceInputFilePath,
                    out string sourcePath)
                && File.Exists(sourcePath))
            {
                AddCandidate(
                    snapshot,
                    inventoryEntry,
                    jobDirectory,
                    sourcePath,
                    CalculationWorkflowArtifactRole.SourceInput,
                    CalculationWorkflowArtifactRequirement.Optional,
                    CalculationWorkflowArtifactSourceKind.WorkspaceEvidence,
                    "application/octet-stream",
                    false,
                    candidates);
            }
        }
    }

    private void AddAdapterCandidates(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string jobDirectory,
        CalculationJob job,
        IQuantumProgramAdapter adapter,
        Dictionary<string, CalculationWorkflowArtifactDiscoveryCandidate> candidates,
        CancellationToken cancellationToken)
    {
        CalculationArtifactDiscoveryContext context =
            new CalculationArtifactDiscoveryContext();
        context.Job = job;
        IReadOnlyList<CalculationArtifactPattern> patterns;

        try
        {
            patterns = adapter.GetArtifactPatterns(context);
        }
        catch (Exception exception) when (
            exception is InvalidOperationException
            || exception is ArgumentException
            || exception is NotSupportedException)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .AdapterPatternInvalid,
                "程序适配器无法生成产物规则：" + exception.Message,
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                string.Empty);
            return;
        }

        for (int index = 0; index < patterns.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CalculationArtifactPattern? pattern = patterns[index];

            if (pattern == null
                || string.IsNullOrWhiteSpace(pattern.FilePattern)
                || Path.IsPathRooted(pattern.FilePattern)
                || pattern.FilePattern.Contains('/')
                || pattern.FilePattern.Contains('\\')
                || pattern.FilePattern.Contains("..", StringComparison.Ordinal))
            {
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .AdapterPatternInvalid,
                    "程序适配器给出了不安全的空规则或路径规则。",
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    string.Empty);
                continue;
            }

            string scopeDirectory = ResolveScopeDirectory(
                jobDirectory,
                pattern.Scope);
            bool hasWildcard = pattern.FilePattern.IndexOf('*') >= 0
                || pattern.FilePattern.IndexOf('?') >= 0;
            CalculationWorkflowArtifactRole role =
                CalculationWorkflowArtifactDiscoveryPolicy.MapRole(
                    pattern.Kind,
                    pattern.Scope,
                    pattern.FilePattern);
            CalculationWorkflowArtifactRequirement requirement =
                pattern.IsRequired
                    ? CalculationWorkflowArtifactRequirement.Required
                    : CalculationWorkflowArtifactRequirement.Optional;

            if (!Directory.Exists(scopeDirectory))
            {
                if (!hasWildcard && pattern.IsRequired)
                {
                    AddCandidate(
                        snapshot,
                        inventoryEntry,
                        jobDirectory,
                        Path.Combine(scopeDirectory, pattern.FilePattern),
                        role,
                        requirement,
                        CalculationWorkflowArtifactSourceKind.AdapterPattern,
                        pattern.MediaType,
                        pattern.CanUseForRestart,
                        candidates);
                }

                continue;
            }

            if (!CalculationWorkflowArtifactPathPolicy.TryEnumerate(
                    scopeDirectory,
                    out List<CalculationWorkflowArtifactPathEntry> entries,
                    out string enumerationError))
            {
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .DirectoryUnreadable,
                    "程序产物目录无法枚举：" + enumerationError,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    CalculationWorkflowArtifactPathPolicy.GetLogicalPath(
                        jobDirectory,
                        scopeDirectory));
                continue;
            }

            int matchCount = 0;

            for (int entryIndex = 0;
                entryIndex < entries.Count;
                entryIndex++)
            {
                CalculationWorkflowArtifactPathEntry entry =
                    entries[entryIndex];

                if (entry.IsDirectory
                    || !string.IsNullOrWhiteSpace(entry.AttributesError)
                    || !FileSystemName.MatchesSimpleExpression(
                        pattern.FilePattern,
                        entry.Name,
                        true))
                {
                    continue;
                }

                AddCandidate(
                    snapshot,
                    inventoryEntry,
                    jobDirectory,
                    entry.FullPath,
                    role,
                    requirement,
                    CalculationWorkflowArtifactSourceKind.AdapterPattern,
                    pattern.MediaType,
                    pattern.CanUseForRestart,
                    candidates);
                matchCount++;
            }

            if (matchCount == 0
                && !hasWildcard
                && pattern.IsRequired)
            {
                AddCandidate(
                    snapshot,
                    inventoryEntry,
                    jobDirectory,
                    Path.Combine(scopeDirectory, pattern.FilePattern),
                    role,
                    requirement,
                    CalculationWorkflowArtifactSourceKind.AdapterPattern,
                    pattern.MediaType,
                    pattern.CanUseForRestart,
                    candidates);
            }
        }
    }

    private static string ResolveScopeDirectory(
        string jobDirectory,
        CalculationArtifactPatternScope scope)
    {
        switch (scope)
        {
            case CalculationArtifactPatternScope.InputDirectory:
                return Path.Combine(jobDirectory, "input");
            case CalculationArtifactPatternScope.ResultDirectory:
                return Path.Combine(jobDirectory, "results");
            case CalculationArtifactPatternScope.JobDirectory:
                return jobDirectory;
            default:
                return Path.Combine(jobDirectory, "run");
        }
    }

    private void ScanDirectory(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string jobDirectory,
        string directory,
        Dictionary<string, CalculationWorkflowArtifactDiscoveryCandidate> candidates,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!CalculationWorkflowArtifactPathPolicy.TryEnumerate(
                directory,
                out List<CalculationWorkflowArtifactPathEntry> entries,
                out string error))
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .DirectoryUnreadable,
                "目录无法枚举：" + error,
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                CalculationWorkflowArtifactPathPolicy.GetLogicalPath(
                    jobDirectory,
                    directory));
            return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CalculationWorkflowArtifactPathEntry item = entries[index];
            string logicalPath =
                CalculationWorkflowArtifactPathPolicy.GetLogicalPath(
                    jobDirectory,
                    item.FullPath);

            if (!string.IsNullOrWhiteSpace(item.AttributesError))
            {
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .FileUnreadable,
                    "无法读取文件属性：" + item.AttributesError,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    logicalPath);
                continue;
            }

            if (item.IsReparsePoint)
            {
                AddSecurityRejectedEntry(
                    snapshot,
                    inventoryEntry,
                    logicalPath,
                    item.IsDirectory);
                continue;
            }

            if (item.IsDirectory)
            {
                CalculationWorkflowArtifactExclusionDecision? directoryDecision =
                    CalculationWorkflowArtifactDiscoveryPolicy
                        .GetDirectoryDecision(item.Name);

                if (directoryDecision == null)
                {
                    ScanDirectory(
                        snapshot,
                        inventoryEntry,
                        jobDirectory,
                        item.FullPath,
                        candidates,
                        cancellationToken);
                    continue;
                }

                CalculationWorkflowArtifactDirectoryMeasurement measurement =
                    CalculationWorkflowArtifactPathPolicy.MeasureDirectory(
                        item.FullPath);
                AddExcludedDirectoryEntry(
                    snapshot,
                    inventoryEntry,
                    logicalPath,
                    directoryDecision,
                    measurement);
                continue;
            }

            if (candidates.ContainsKey(logicalPath))
            {
                continue;
            }

            long? length = null;

            if (CalculationWorkflowArtifactPathPolicy.TryGetFileLength(
                    item.FullPath,
                    out long measuredLength,
                    out string metadataError))
            {
                length = measuredLength;
            }
            else if (!string.IsNullOrWhiteSpace(metadataError))
            {
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .FileUnreadable,
                    "无法读取文件元数据：" + metadataError,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    logicalPath);
            }

            CalculationWorkflowArtifactRole role =
                CalculationWorkflowArtifactDiscoveryPolicy
                    .InferExistingFileRole(logicalPath);
            CalculationWorkflowArtifactExclusionDecision? fileDecision =
                CalculationWorkflowArtifactDiscoveryPolicy.GetFileDecision(
                    logicalPath,
                    item.Name,
                    length,
                    role);

            if (fileDecision != null)
            {
                AddExcludedFileEntry(
                    snapshot,
                    inventoryEntry,
                    logicalPath,
                    role,
                    length,
                    fileDecision,
                    candidates.TryGetValue(
                            logicalPath,
                            out CalculationWorkflowArtifactDiscoveryCandidate? candidate)
                        ? candidate.Requirement
                        : CalculationWorkflowArtifactRequirement.Optional);
                continue;
            }

            AddCandidate(
                snapshot,
                inventoryEntry,
                jobDirectory,
                item.FullPath,
                role,
                CalculationWorkflowArtifactRequirement.Optional,
                CalculationWorkflowArtifactSourceKind.ExistingFile,
                "application/octet-stream",
                false,
                candidates);
        }
    }

    private void ProcessCandidates(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        Dictionary<string, CalculationWorkflowArtifactDiscoveryCandidate> candidates)
    {
        List<CalculationWorkflowArtifactDiscoveryCandidate> ordered =
            new List<CalculationWorkflowArtifactDiscoveryCandidate>(
                candidates.Values);
        ordered.Sort(
            delegate(
                CalculationWorkflowArtifactDiscoveryCandidate left,
                CalculationWorkflowArtifactDiscoveryCandidate right)
            {
                return string.Compare(
                    left.LogicalPath,
                    right.LogicalPath,
                    StringComparison.OrdinalIgnoreCase);
            });

        for (int index = 0; index < ordered.Count; index++)
        {
            CalculationWorkflowArtifactDiscoveryCandidate candidate =
                ordered[index];
            CalculationWorkflowArtifactDiscoveryEntry entry =
                CreateEntry(
                    inventoryEntry,
                    candidate.LogicalPath,
                    candidate.Role,
                    candidate.Requirement,
                    candidate.SourceKind,
                    candidate.MediaType,
                    candidate.CanUseForRestart);
            CalculationWorkflowArtifactPathProbeResult probe =
                CalculationWorkflowArtifactPathPolicy.Probe(
                    _workspace.GetJobDirectory(inventoryEntry.JobId),
                    candidate.FullPath,
                    out _,
                    out string probeMessage);

            if (probe == CalculationWorkflowArtifactPathProbeResult.Missing)
            {
                entry.Status = candidate.Requirement
                        == CalculationWorkflowArtifactRequirement.Required
                    ? CalculationWorkflowArtifactDiscoveryStatus.Missing
                    : CalculationWorkflowArtifactDiscoveryStatus.NotExpected;
                entry.Message = candidate.Requirement
                        == CalculationWorkflowArtifactRequirement.Required
                    ? "必要条件未生成。"
                    : "可选文件未生成。";

                if (candidate.Requirement
                    == CalculationWorkflowArtifactRequirement.Required)
                {
                    AddIssue(
                        snapshot,
                        CalculationWorkflowArtifactDiscoveryIssueCodes
                            .RequiredEvidenceMissing,
                        "必要条件未生成：" + entry.Message,
                        CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                        inventoryEntry.StageId,
                        inventoryEntry.JobId,
                        entry.LogicalPath);
                }

                snapshot.Files.Add(entry);
                continue;
            }

            if (probe == CalculationWorkflowArtifactPathProbeResult.OutsideJob)
            {
                entry.Status =
                    CalculationWorkflowArtifactDiscoveryStatus.SecurityRejected;
                entry.Message = probeMessage;
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .PathOutsideJob,
                    probeMessage,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    entry.LogicalPath);
                snapshot.Files.Add(entry);
                continue;
            }

            if (probe == CalculationWorkflowArtifactPathProbeResult.ReparsePoint)
            {
                entry.Status =
                    CalculationWorkflowArtifactDiscoveryStatus.SecurityRejected;
                entry.Message = probeMessage;
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .ReparsePointRejected,
                    probeMessage,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    entry.LogicalPath);
                snapshot.Files.Add(entry);
                continue;
            }

            if (probe == CalculationWorkflowArtifactPathProbeResult.Unreadable)
            {
                entry.Status =
                    CalculationWorkflowArtifactDiscoveryStatus.Unreadable;
                entry.Message = probeMessage;
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .FileUnreadable,
                    "候选文件无法读取：" + probeMessage,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    entry.LogicalPath);
                snapshot.Files.Add(entry);
                continue;
            }

            if (!CalculationWorkflowArtifactPathPolicy.TryGetFileLength(
                    candidate.FullPath,
                    out long length,
                    out string metadataError))
            {
                entry.Status =
                    CalculationWorkflowArtifactDiscoveryStatus.Unreadable;
                entry.Message = string.IsNullOrWhiteSpace(metadataError)
                    ? "候选路径不是可读取的持久化文件。"
                    : metadataError;
                AddIssue(
                    snapshot,
                    CalculationWorkflowArtifactDiscoveryIssueCodes
                        .FileUnreadable,
                    entry.Message,
                    CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                    inventoryEntry.StageId,
                    inventoryEntry.JobId,
                    entry.LogicalPath);
                snapshot.Files.Add(entry);
                continue;
            }

            entry.Exists = true;
            entry.Length = length;
            entry.ExcludedFileCount = 1;
            CalculationWorkflowArtifactExclusionDecision? decision =
                CalculationWorkflowArtifactDiscoveryPolicy.GetFileDecision(
                    entry.LogicalPath,
                    Path.GetFileName(candidate.FullPath),
                    length,
                    entry.Role);

            if (decision != null)
            {
                ApplyExclusion(entry, decision);

                if (candidate.Requirement
                    == CalculationWorkflowArtifactRequirement.Required)
                {
                    AddIssue(
                        snapshot,
                        CalculationWorkflowArtifactDiscoveryIssueCodes
                            .RequiredEvidenceExcluded,
                        "必要条件被排除：" + decision.Reason,
                        CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                        inventoryEntry.StageId,
                        inventoryEntry.JobId,
                        entry.LogicalPath);
                }

                snapshot.Files.Add(entry);
                continue;
            }

            entry.Status = CalculationWorkflowArtifactDiscoveryStatus.Present;
            snapshot.Files.Add(entry);
        }
    }

    private void AddCandidate(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string jobDirectory,
        string pathOrFullPath,
        CalculationWorkflowArtifactRole role,
        CalculationWorkflowArtifactRequirement requirement,
        CalculationWorkflowArtifactSourceKind sourceKind,
        string mediaType,
        bool canUseForRestart,
        Dictionary<string, CalculationWorkflowArtifactDiscoveryCandidate> candidates)
    {
        string candidatePath = Path.IsPathRooted(pathOrFullPath)
            ? pathOrFullPath
            : Path.Combine(jobDirectory, pathOrFullPath);

        if (!CalculationWorkflowArtifactPathPolicy.TryResolveWithinJob(
                jobDirectory,
                candidatePath,
                out string fullPath))
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes.PathOutsideJob,
                "产物候选位于作业目录之外。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                pathOrFullPath.Replace('\\', '/'));
            return;
        }

        string logicalPath =
            CalculationWorkflowArtifactPathPolicy.GetLogicalPath(
                jobDirectory,
                fullPath);

        if (string.IsNullOrWhiteSpace(logicalPath)
            || logicalPath.StartsWith("..", StringComparison.Ordinal))
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes.PathOutsideJob,
                "产物候选无法规范化为作业目录内路径。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                pathOrFullPath);
            return;
        }

        if (candidates.TryGetValue(
                logicalPath,
                out CalculationWorkflowArtifactDiscoveryCandidate? existing))
        {
            if (requirement
                == CalculationWorkflowArtifactRequirement.Required)
            {
                existing.Requirement =
                    CalculationWorkflowArtifactRequirement.Required;
            }

            if (existing.Role == CalculationWorkflowArtifactRole.Other
                && role != CalculationWorkflowArtifactRole.Other)
            {
                existing.Role = role;
            }

            if ((int)sourceKind < (int)existing.SourceKind)
            {
                existing.SourceKind = sourceKind;
            }

            if (string.IsNullOrWhiteSpace(existing.MediaType)
                || existing.MediaType == "application/octet-stream")
            {
                existing.MediaType = mediaType;
            }

            existing.CanUseForRestart =
                existing.CanUseForRestart || canUseForRestart;
            return;
        }

        CalculationWorkflowArtifactDiscoveryCandidate candidate =
            new CalculationWorkflowArtifactDiscoveryCandidate();
        candidate.LogicalPath = logicalPath;
        candidate.FullPath = fullPath;
        candidate.Role = role;
        candidate.Requirement = requirement;
        candidate.SourceKind = sourceKind;
        candidate.MediaType = string.IsNullOrWhiteSpace(mediaType)
            ? "application/octet-stream"
            : mediaType;
        candidate.CanUseForRestart = canUseForRestart;
        candidates.Add(logicalPath, candidate);
    }

    private void AddSecurityRejectedEntry(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string logicalPath,
        bool isDirectory)
    {
        CalculationWorkflowArtifactDiscoveryEntry entry = CreateEntry(
            inventoryEntry,
            logicalPath,
            CalculationWorkflowArtifactDiscoveryPolicy
                .InferExistingFileRole(logicalPath),
            CalculationWorkflowArtifactRequirement.Optional,
            CalculationWorkflowArtifactSourceKind.ExistingFile,
            "application/octet-stream",
            false);
        entry.Status =
            CalculationWorkflowArtifactDiscoveryStatus.SecurityRejected;
        entry.IsAggregate = isDirectory;
        entry.Message = "路径包含重解析点或符号链接，拒绝跟随。";
        snapshot.Files.Add(entry);
        AddIssue(
            snapshot,
            CalculationWorkflowArtifactDiscoveryIssueCodes
                .ReparsePointRejected,
            entry.Message,
            CalculationWorkflowArtifactDiscoveryIssueSeverity.Error,
            inventoryEntry.StageId,
            inventoryEntry.JobId,
            logicalPath);
    }

    private void AddExcludedDirectoryEntry(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string logicalPath,
        CalculationWorkflowArtifactExclusionDecision decision,
        CalculationWorkflowArtifactDirectoryMeasurement measurement)
    {
        CalculationWorkflowArtifactDiscoveryEntry entry = CreateEntry(
            inventoryEntry,
            logicalPath.TrimEnd('/') + "/",
            CalculationWorkflowArtifactRole.Other,
            CalculationWorkflowArtifactRequirement.Optional,
            CalculationWorkflowArtifactSourceKind.ExistingFile,
            "application/octet-stream",
            false);
        entry.Status =
            CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy;
        entry.IsAggregate = true;
        entry.ExcludedFileCount = measurement.FileCount;
        entry.Length = measurement.TotalBytes;
        entry.ExclusionCategory = decision.Category;
        entry.CanRegenerate = decision.CanRegenerate;
        entry.Message = decision.Reason;
        snapshot.Files.Add(entry);

        if (!string.IsNullOrWhiteSpace(measurement.Error))
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .DirectoryUnreadable,
                "排除目录中的文件数量或大小无法完整读取：" + measurement.Error,
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Warning,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                entry.LogicalPath);
        }

        for (int index = 0;
            index < measurement.ReparsePointPaths.Count;
            index++)
        {
            AddIssue(
                snapshot,
                CalculationWorkflowArtifactDiscoveryIssueCodes
                    .ReparsePointRejected,
                "排除目录内发现重解析点，未跟随。",
                CalculationWorkflowArtifactDiscoveryIssueSeverity.Warning,
                inventoryEntry.StageId,
                inventoryEntry.JobId,
                CalculationWorkflowArtifactPathPolicy.GetLogicalPath(
                    _workspace.GetJobDirectory(inventoryEntry.JobId),
                    measurement.ReparsePointPaths[index]));
        }
    }

    private void AddExcludedFileEntry(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string logicalPath,
        CalculationWorkflowArtifactRole role,
        long? length,
        CalculationWorkflowArtifactExclusionDecision decision,
        CalculationWorkflowArtifactRequirement requirement)
    {
        CalculationWorkflowArtifactDiscoveryEntry entry = CreateEntry(
            inventoryEntry,
            logicalPath,
            role,
            requirement,
            CalculationWorkflowArtifactSourceKind.ExistingFile,
            "application/octet-stream",
            false);
        entry.Exists = true;
        entry.Length = length;
        entry.ExcludedFileCount = 1;
        ApplyExclusion(entry, decision);
        snapshot.Files.Add(entry);
    }

    private static void ApplyExclusion(
        CalculationWorkflowArtifactDiscoveryEntry entry,
        CalculationWorkflowArtifactExclusionDecision decision)
    {
        entry.Status =
            CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy;
        entry.ExclusionCategory = decision.Category;
        entry.CanRegenerate = decision.CanRegenerate;
        entry.Message = decision.Reason;
    }

    private static CalculationWorkflowArtifactDiscoveryEntry CreateEntry(
        CalculationWorkflowJobInventoryEntry inventoryEntry,
        string logicalPath,
        CalculationWorkflowArtifactRole role,
        CalculationWorkflowArtifactRequirement requirement,
        CalculationWorkflowArtifactSourceKind sourceKind,
        string mediaType,
        bool canUseForRestart)
    {
        CalculationWorkflowArtifactDiscoveryEntry entry =
            new CalculationWorkflowArtifactDiscoveryEntry();
        entry.ArtifactId = inventoryEntry.JobId + ":" + logicalPath;
        entry.StageId = inventoryEntry.StageId;
        entry.StageOrder = ResolveDefaultStageOrder(
            inventoryEntry.StageId);
        entry.JobId = inventoryEntry.JobId;
        entry.AttemptNumber = inventoryEntry.AttemptNumber;
        entry.LogicalPath = logicalPath.Replace('\\', '/');
        entry.Role = role;
        entry.Requirement = requirement;
        entry.SourceKind = sourceKind;
        entry.MediaType = string.IsNullOrWhiteSpace(mediaType)
            ? "application/octet-stream"
            : mediaType;
        entry.CanUseForRestart = canUseForRestart;
        return entry;
    }

    private static CalculationWorkflowArtifactRequirement RequireWhenStarted(
        CalculationJobState state)
    {
        return state == CalculationJobState.Created
            ? CalculationWorkflowArtifactRequirement.Optional
            : CalculationWorkflowArtifactRequirement.Required;
    }

    private static CalculationWorkflowArtifactRequirement RequiresPrimaryOutput(
        CalculationJobState state)
    {
        return state == CalculationJobState.Completed
                || state == CalculationJobState.Parsed
                || state == CalculationJobState.Validated
            ? CalculationWorkflowArtifactRequirement.Required
            : CalculationWorkflowArtifactRequirement.Optional;
    }

    private static CalculationWorkflowArtifactRequirement RequiresNormalizedResult(
        CalculationJobState state)
    {
        return state == CalculationJobState.Parsed
                || state == CalculationJobState.Validated
            ? CalculationWorkflowArtifactRequirement.Required
            : CalculationWorkflowArtifactRequirement.Optional;
    }

    private static int ResolveStageOrder(
        CalculationWorkflowEvidenceLedger? ledger,
        string stageId)
    {
        if (ledger != null)
        {
            for (int index = 0; index < ledger.Stages.Count; index++)
            {
                if (string.Equals(
                        ledger.Stages[index].StageId,
                        stageId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return ledger.Stages[index].Order;
                }
            }
        }

        return ResolveDefaultStageOrder(stageId);
    }

    private static int ResolveDefaultStageOrder(string stageId)
    {
        return CalculationWorkflowStageIds.ResolveDefaultOrder(stageId);
    }

    private void UpdateJobSummary(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        CalculationWorkflowArtifactDiscoveryJobSummary jobSummary)
    {
        int stageOrder = jobSummary.StageOrder;

        for (int index = 0; index < snapshot.Files.Count; index++)
        {
            CalculationWorkflowArtifactDiscoveryEntry entry =
                snapshot.Files[index];

            if (!string.Equals(
                    entry.JobId,
                    jobSummary.JobId,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            entry.StageOrder = stageOrder;
            jobSummary.FileCount++;

            switch (entry.Status)
            {
                case CalculationWorkflowArtifactDiscoveryStatus.Present:
                    jobSummary.PresentCount++;
                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.Missing:
                    jobSummary.MissingCount++;
                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy:
                    jobSummary.ExcludedCount += Math.Max(
                        1,
                        entry.ExcludedFileCount);
                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.Unreadable:
                    jobSummary.UnreadableCount++;
                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.SecurityRejected:
                    jobSummary.SecurityRejectedCount++;
                    break;
            }
        }
    }

    private static void CopyInventoryIssues(
        CalculationWorkflowJobInventory inventory,
        CalculationWorkflowArtifactDiscoverySnapshot snapshot)
    {
        for (int index = 0; index < inventory.Issues.Count; index++)
        {
            CalculationWorkflowInventoryIssue issue = inventory.Issues[index];
            CalculationWorkflowArtifactDiscoveryIssueSeverity severity;

            switch (issue.Severity)
            {
                case CalculationWorkflowInventoryIssueSeverity.Info:
                    severity =
                        CalculationWorkflowArtifactDiscoveryIssueSeverity.Info;
                    break;
                case CalculationWorkflowInventoryIssueSeverity.Warning:
                    severity =
                        CalculationWorkflowArtifactDiscoveryIssueSeverity.Warning;
                    break;
                default:
                    severity =
                        CalculationWorkflowArtifactDiscoveryIssueSeverity.Error;
                    break;
            }

            AddIssue(
                snapshot,
                issue.Code,
                issue.Message,
                severity,
                issue.StageId,
                issue.JobId,
                issue.Path);
        }
    }

    private static void AddIssue(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot,
        string code,
        string message,
        CalculationWorkflowArtifactDiscoveryIssueSeverity severity,
        string stageId,
        string jobId,
        string logicalPath)
    {
        CalculationWorkflowArtifactDiscoveryIssue issue =
            new CalculationWorkflowArtifactDiscoveryIssue();
        issue.Code = code;
        issue.Message = message;
        issue.Severity = severity;
        issue.StageId = stageId;
        issue.JobId = jobId;
        issue.LogicalPath = logicalPath;
        snapshot.Issues.Add(issue);
    }

    private static void NormalizeSnapshot(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot)
    {
        snapshot.SchemaVersion = string.IsNullOrWhiteSpace(
            snapshot.SchemaVersion)
            ? "1.0"
            : snapshot.SchemaVersion;
        snapshot.PolicyVersion =
            CalculationWorkflowArtifactDiscoveryPolicy.Version;
        snapshot.Jobs.Sort(
            delegate(
                CalculationWorkflowArtifactDiscoveryJobSummary left,
                CalculationWorkflowArtifactDiscoveryJobSummary right)
            {
                int stage = left.StageOrder.CompareTo(right.StageOrder);

                if (stage != 0)
                {
                    return stage;
                }

                int attempt = left.AttemptNumber.CompareTo(
                    right.AttemptNumber);

                if (attempt != 0)
                {
                    return attempt;
                }

                return string.Compare(
                    left.JobId,
                    right.JobId,
                    StringComparison.OrdinalIgnoreCase);
            });
        snapshot.Files.Sort(
            delegate(
                CalculationWorkflowArtifactDiscoveryEntry left,
                CalculationWorkflowArtifactDiscoveryEntry right)
            {
                int stage = left.StageOrder.CompareTo(right.StageOrder);

                if (stage != 0)
                {
                    return stage;
                }

                int attempt = left.AttemptNumber.CompareTo(
                    right.AttemptNumber);

                if (attempt != 0)
                {
                    return attempt;
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
                    left.LogicalPath,
                    right.LogicalPath,
                    StringComparison.OrdinalIgnoreCase);
            });
        snapshot.Issues.Sort(
            delegate(
                CalculationWorkflowArtifactDiscoveryIssue left,
                CalculationWorkflowArtifactDiscoveryIssue right)
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
                    left.LogicalPath,
                    right.LogicalPath,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private static void RecalculateSummary(
        CalculationWorkflowArtifactDiscoverySnapshot snapshot)
    {
        CalculationWorkflowArtifactDiscoverySummary summary =
            snapshot.Summary;
        summary.JobCount = snapshot.Jobs.Count;
        summary.FileCount = snapshot.Files.Count;

        Dictionary<string, CalculationWorkflowArtifactExclusionSummary>
            exclusions = new Dictionary<
                string,
                CalculationWorkflowArtifactExclusionSummary>(
                StringComparer.OrdinalIgnoreCase);
        bool hasRequiredFailure = false;

        for (int index = 0; index < snapshot.Files.Count; index++)
        {
            CalculationWorkflowArtifactDiscoveryEntry entry =
                snapshot.Files[index];

            if (entry.Length.HasValue)
            {
                summary.TotalBytes += entry.Length.Value;
            }

            switch (entry.Status)
            {
                case CalculationWorkflowArtifactDiscoveryStatus.Present:
                    summary.PresentCount++;
                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.Missing:
                    summary.MissingCount++;

                    if (entry.Requirement
                        == CalculationWorkflowArtifactRequirement.Required)
                    {
                        hasRequiredFailure = true;
                    }

                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy:
                    int fileCount = Math.Max(1, entry.ExcludedFileCount);
                    summary.ExcludedCount += fileCount;

                    if (entry.Requirement
                        == CalculationWorkflowArtifactRequirement.Required)
                    {
                        hasRequiredFailure = true;
                    }

                    if (!string.IsNullOrWhiteSpace(entry.ExclusionCategory))
                    {
                        AccumulateExclusion(
                            exclusions,
                            entry,
                            fileCount);
                    }

                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.Unreadable:
                    summary.UnreadableCount++;
                    hasRequiredFailure = true;
                    break;
                case CalculationWorkflowArtifactDiscoveryStatus.SecurityRejected:
                    summary.SecurityRejectedCount++;
                    hasRequiredFailure = true;
                    break;
            }
        }

        summary.ErrorCount = snapshot.Issues.Count(
            issue => issue.Severity
                == CalculationWorkflowArtifactDiscoveryIssueSeverity.Error);
        summary.IsComplete = !hasRequiredFailure && summary.ErrorCount == 0;
        summary.Exclusions = new List<
            CalculationWorkflowArtifactExclusionSummary>(
                exclusions.Values);
        summary.Exclusions.Sort(
            delegate(
                CalculationWorkflowArtifactExclusionSummary left,
                CalculationWorkflowArtifactExclusionSummary right)
            {
                return string.Compare(
                    left.Category,
                    right.Category,
                    StringComparison.OrdinalIgnoreCase);
            });
    }

    private static void AccumulateExclusion(
        Dictionary<string, CalculationWorkflowArtifactExclusionSummary> exclusions,
        CalculationWorkflowArtifactDiscoveryEntry entry,
        int fileCount)
    {
        string key = entry.ExclusionCategory
            + "|"
            + entry.Message;

        if (!exclusions.TryGetValue(
                key,
                out CalculationWorkflowArtifactExclusionSummary? summary))
        {
            summary =
                new CalculationWorkflowArtifactExclusionSummary();
            summary.Category = entry.ExclusionCategory;
            summary.Reason = entry.Message;
            summary.CanRegenerate = entry.CanRegenerate;
            summary.AffectsReproduction = false;
            exclusions.Add(key, summary);
        }

        summary.FileCount += fileCount;
        summary.TotalBytes += entry.Length ?? 0;
    }

    private sealed class CalculationWorkflowArtifactDiscoveryCandidate
    {
        public string LogicalPath { get; set; } = string.Empty;

        public string FullPath { get; set; } = string.Empty;

        public CalculationWorkflowArtifactRole Role { get; set; } =
            CalculationWorkflowArtifactRole.Other;

        public CalculationWorkflowArtifactRequirement Requirement { get; set; } =
            CalculationWorkflowArtifactRequirement.Optional;

        public CalculationWorkflowArtifactSourceKind SourceKind { get; set; } =
            CalculationWorkflowArtifactSourceKind.ExistingFile;

        public string MediaType { get; set; } =
            "application/octet-stream";

        public bool CanUseForRestart { get; set; }
    }

    private sealed class EmptyQuantumProgramAdapterRegistry
        : IQuantumProgramAdapterRegistry
    {
        public IQuantumProgramAdapter? Resolve(CalculationSpec spec)
        {
            return null;
        }

        public IReadOnlyList<string> ListPrograms()
        {
            return new List<string>();
        }
    }
}
