using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChemSculptor.Compute;

/// <summary>
/// 基于文件的计算作业与结果仓储。
/// 作业清单写入 manifest.json，规范化结果写入 result.json。
/// </summary>
public sealed class FileCalculationRepository : ICalculationRepository
{
    private readonly ICalculationWorkspace _workspace;
    private readonly ICalculationWorkflowEvidenceLedgerRepository
        _evidenceLedger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 创建文件仓储。
    /// 使用同工作区的默认证据账本仓储登记作业尝试索引。
    /// </summary>
    public FileCalculationRepository(ICalculationWorkspace workspace)
        : this(
            workspace,
            new FileCalculationWorkflowEvidenceLedgerRepository(
                workspace))
    {
    }

    /// <summary>创建带指定证据账本仓储的文件仓储。</summary>
    public FileCalculationRepository(
        ICalculationWorkspace workspace,
        ICalculationWorkflowEvidenceLedgerRepository evidenceLedger)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (evidenceLedger == null)
        {
            throw new ArgumentNullException(nameof(evidenceLedger));
        }

        _workspace = workspace;
        _evidenceLedger = evidenceLedger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.WriteIndented = true;
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>保存作业清单。</summary>
    public async Task SaveJobAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        string path = _workspace.GetJobManifestPath(job.JobId);
        await WriteJsonAsync(path, job, cancellationToken);
        await RegisterJobAttemptAsync(job, cancellationToken);
    }

    /// <summary>读取作业清单。</summary>
    public Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        string path = _workspace.GetJobManifestPath(jobId);
        return ReadJsonAsync<CalculationJob>(path, cancellationToken);
    }

    /// <summary>保存规范化计算结果。</summary>
    public Task SaveResultAsync(
        CalculationResult result,
        CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        string path = _workspace.GetJobResultPath(result.JobId);
        return WriteJsonAsync(path, result, cancellationToken);
    }

    /// <summary>读取规范化计算结果。</summary>
    public Task<CalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        string path = _workspace.GetJobResultPath(jobId);
        return ReadJsonAsync<CalculationResult>(path, cancellationToken);
    }

    /// <summary>保存通用处理方案。</summary>
    public Task SaveProcessingPlanAsync(
        CalculationProcessingPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        string path = _workspace.GetJobProcessingPlanPath(plan.JobId);
        return WriteJsonAsync(path, plan, cancellationToken);
    }

    /// <summary>保存程序专用处理方案。</summary>
    public Task SaveProgramProcessingPlanAsync(
        ProgramProcessingPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        string path = _workspace.GetJobProgramProcessingPlanPath(plan.JobId);
        return WriteJsonAsync(path, plan, cancellationToken);
    }

    /// <summary>保存结果验证报告。</summary>
    public Task SaveValidationAsync(
        CalculationValidationReport report,
        string jobId,
        CancellationToken cancellationToken = default)
    {
        if (report == null)
        {
            throw new ArgumentNullException(nameof(report));
        }

        string path = _workspace.GetJobValidationPath(jobId);
        return WriteJsonAsync(path, report, cancellationToken);
    }

    /// <summary>读取结果验证报告。</summary>
    public Task<CalculationValidationReport?> GetValidationAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        string path = _workspace.GetJobValidationPath(jobId);
        return ReadJsonAsync<CalculationValidationReport>(path, cancellationToken);
    }

    /// <summary>保存参数审计。</summary>
    public Task SaveParameterAuditAsync(
        CalculationParameterAudit audit,
        CancellationToken cancellationToken = default)
    {
        if (audit == null)
        {
            throw new ArgumentNullException(nameof(audit));
        }

        audit.UpdatedAt = DateTimeOffset.UtcNow;
        string path = _workspace.GetJobParametersPath(audit.JobId);
        return WriteJsonAsync(path, audit, cancellationToken);
    }

    /// <summary>读取参数审计。</summary>
    public Task<CalculationParameterAudit?> GetParameterAuditAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        string path = _workspace.GetJobParametersPath(jobId);
        return ReadJsonAsync<CalculationParameterAudit>(
            path,
            cancellationToken);
    }

    private async Task WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        EnsureParentDirectory(path);
        string json = JsonSerializer.Serialize(value, _jsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private async Task<T?> ReadJsonAsync<T>(
        string path,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private static void EnsureParentDirectory(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private async Task RegisterJobAttemptAsync(
        CalculationJob job,
        CancellationToken cancellationToken)
    {
        string rootWorkflowId = ResolveRootWorkflowId(job);

        if (string.IsNullOrWhiteSpace(rootWorkflowId))
        {
            return;
        }

        string stageId = ResolveEvidenceStageId(job);
        await _evidenceLedger.AttachJobAsync(
            rootWorkflowId,
            stageId,
            job,
            cancellationToken);
    }

    private static string ResolveEvidenceStageId(CalculationJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.EvidenceStageId))
        {
            return job.EvidenceStageId;
        }

        return CalculationWorkflowStageIds.ResolveDefaultStageId(
            job.Spec?.TaskType
                ?? CalculationTaskType.SinglePoint);
    }

    private static string ResolveRootWorkflowId(CalculationJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.RootWorkflowId))
        {
            return job.RootWorkflowId;
        }

        if (!string.IsNullOrWhiteSpace(job.WorkflowId))
        {
            return job.WorkflowId;
        }

        return string.Empty;
    }
}
