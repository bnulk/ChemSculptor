using System.Collections.Concurrent;
using System.Text.Json;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>几何优化计算服务。</summary>
public sealed class OptimizationCalculationService
    : IOptimizationCalculationService
{
    private readonly ICalculationWorkspace _workspace;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly ICalculationRepository _repository;
    private readonly WorkflowEngine _workflowEngine;
    private readonly ConcurrentDictionary<string, OptimizationResult>
        _results;
    private readonly ConcurrentDictionary<
        string,
        CalculationValidationReport> _validations;
    private readonly ConcurrentDictionary<string, string> _finalJobIds;
    private readonly ConcurrentDictionary<string, int> _correctionCounts;

    /// <summary>创建几何优化计算服务。</summary>
    public OptimizationCalculationService(
        ICalculationWorkspace workspace,
        IQuantumProgramAdapterRegistry adapterRegistry,
        ICalculationRepository repository,
        WorkflowEngine workflowEngine)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (workflowEngine == null)
        {
            throw new ArgumentNullException(nameof(workflowEngine));
        }

        _workspace = workspace;
        _adapterRegistry = adapterRegistry;
        _repository = repository;
        _workflowEngine = workflowEngine;
        _results =
            new ConcurrentDictionary<string, OptimizationResult>(
                StringComparer.OrdinalIgnoreCase);
        _validations =
            new ConcurrentDictionary<
                string,
                CalculationValidationReport>(
                    StringComparer.OrdinalIgnoreCase);
        _finalJobIds =
            new ConcurrentDictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        _correctionCounts =
            new ConcurrentDictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>提交几何优化。</summary>
    public async Task<OptimizationSubmissionResult> SubmitAsync(
        OptimizationRequest request,
        CancellationToken cancellationToken = default)
    {
        OptimizationSubmissionResult result =
            new OptimizationSubmissionResult();

        if (request == null)
        {
            result.Error = "优化请求不能为空。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.CoordinateText))
        {
            result.Error = "坐标文本不能为空。";
            return result;
        }

        try
        {
            CalculationSpec spec = request.ResolvedSpec
                ?? CalculationDefaults.CreateDefaultOptimization();

            if (spec.TaskType != CalculationTaskType.Optimization)
            {
                result.Error = "几何优化服务只能处理优化任务。";
                return result;
            }

            IQuantumProgramAdapter? adapter =
                _adapterRegistry.Resolve(spec);

            if (adapter == null)
            {
                result.Error =
                    "没有可处理 " +
                    spec.Program +
                    " 的计算程序适配器。";
                return result;
            }

            string jobId =
                "job-" + Guid.NewGuid().ToString("N");
            await _workspace.EnsureJobWorkspaceAsync(
                jobId,
                cancellationToken);

            string inputFileName = adapter.GetInputFileName(jobId);
            string inputPath = Path.Combine(
                _workspace.GetInputDirectory(jobId),
                inputFileName);
            string runInputPath = Path.Combine(
                _workspace.GetRunDirectory(jobId),
                inputFileName);
            string outputPath =
                _workspace.GetJobOutputPath(jobId);

            CalculationJob job = new CalculationJob();
            job.JobId = jobId;
            job.WorkflowId = jobId;
            job.RootWorkflowId = jobId;
            job.SessionId = request.SessionId;
            job.GeometryId = request.GeometryId;
            job.Goal = request.Goal;
            job.Spec = spec;
            job.State = CalculationJobState.Created;
            job.WorkspaceDirectory =
                _workspace.GetJobDirectory(jobId);
            job.RunDirectory =
                _workspace.GetRunDirectory(jobId);
            job.SourceInputFilePath = inputPath;
            job.InputFilePath = runInputPath;
            job.OutputFilePath = outputPath;

            await SaveParameterAuditAsync(
                job,
                spec,
                request.ResolvedParameters,
                adapter,
                cancellationToken);

            CalculationInputGenerationRequest inputRequest =
                new CalculationInputGenerationRequest();
            inputRequest.Job = job;
            inputRequest.Spec = spec;
            inputRequest.CoordinateText =
                request.CoordinateText;
            inputRequest.InputFilePath = inputPath;
            inputRequest.RunInputFilePath = runInputPath;
            inputRequest.OutputFilePath = outputPath;

            WorkflowDefinition definition =
                OptimizationWorkflowDefinitionFactory.Create(
                    jobId,
                    request.Goal);
            Dictionary<string, string> workflowInputs =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
            workflowInputs["request"] =
                SerializeSkillInput(inputRequest);

            WorkflowRun workflowRun =
                await _workflowEngine.SubmitAsync(
                    definition,
                    workflowInputs,
                    cancellationToken);
            workflowRun = await _workflowEngine.RunAsync(
                workflowRun.Id,
                cancellationToken);

            OptimizationResult? optimizationResult =
                ExtractOptimizationResult(workflowRun);
            CalculationValidationReport? report =
                ResolveValidationReport(workflowRun);
            bool workflowPassed =
                workflowRun.State == WorkflowState.Passed
                && optimizationResult != null
                && (report == null || report.Passed);
            job.State = workflowPassed
                ? CalculationJobState.Validated
                : CalculationJobState.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _repository.SaveJobAsync(
                job,
                cancellationToken);

            if (optimizationResult == null)
            {
                result.Error =
                    "优化工作流没有返回 OptimizationResult。";
                return result;
            }

            string finalJobId = ResolveFinalJobId(
                workflowRun,
                jobId);
            int correctionCount = CountCorrections(workflowRun);
            _results[jobId] = optimizationResult;
            _finalJobIds[jobId] = finalJobId;
            _correctionCounts[jobId] = correctionCount;

            if (report != null)
            {
                _validations[jobId] = report;
            }

            result.Succeeded = workflowPassed;
            result.Job = job;
            result.Result = optimizationResult;
            result.Report = report;
            result.CorrectionCount = correctionCount;
            result.FinalJobId = finalJobId;
            AnomalyCheckResult? stability =
                ResolveStabilityResult(workflowRun);

            if (stability != null)
            {
                result.WavefunctionStabilityStatus =
                    MapStabilityStatus(stability.Status);
                result.WavefunctionStabilitySummary =
                    stability.Summary;
            }

            result.FrequencyResult =
                ResolveFrequencyResult(workflowRun);
            result.Message = result.Succeeded
                ? "几何优化已完成。"
                : "几何优化工作流未通过验证。";
            return result;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            return result;
        }
    }

    /// <summary>查询计算作业。</summary>
    public Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetJobAsync(
            jobId,
            cancellationToken);
    }

    /// <summary>查询几何优化结果。</summary>
    public Task<OptimizationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        OptimizationResult? result;
        _results.TryGetValue(jobId, out result);
        return Task.FromResult(result);
    }

    /// <summary>查询结果验证报告。</summary>
    public Task<CalculationValidationReport?> GetValidationAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        CalculationValidationReport? report;
        _validations.TryGetValue(jobId, out report);
        return Task.FromResult(report);
    }

    /// <summary>取消计算作业。</summary>
    public Task<bool> CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    /// <summary>读取最终采用的作业标识。</summary>
    public string GetFinalJobId(string jobId)
    {
        string? finalJobId;

        if (_finalJobIds.TryGetValue(jobId, out finalJobId))
        {
            return finalJobId;
        }

        return jobId;
    }

    /// <summary>读取恢复修正次数。</summary>
    public int GetCorrectionCount(string jobId)
    {
        int count;

        if (_correctionCounts.TryGetValue(jobId, out count))
        {
            return count;
        }

        return 0;
    }

    private static OptimizationResult?
        ExtractOptimizationResult(WorkflowRun run)
    {
        TaskResult? optimizationTask;

        if (!run.Results.TryGetValue(
            "optimization",
            out optimizationTask)
            || string.IsNullOrWhiteSpace(
                optimizationTask.Output))
        {
            return null;
        }

        OptimizationCalculationSkillResult optimization =
            JsonSerializer.Deserialize<
                OptimizationCalculationSkillResult>(
                    optimizationTask.Output,
                    CreateJsonOptions())!;

        TaskResult? recoveryTask;
        if (run.Results.TryGetValue(
                "optimization-stability-recovery-execution",
                out recoveryTask)
            && !string.IsNullOrWhiteSpace(
                recoveryTask.Output))
        {
            RecoveryJobExecutionResult stabilityRecovery =
                JsonSerializer.Deserialize<
                    RecoveryJobExecutionResult>(
                        recoveryTask.Output,
                        CreateJsonOptions())!;

            if (stabilityRecovery.OptimizationResult != null)
            {
                return stabilityRecovery.OptimizationResult;
            }
        }

        if (run.Results.TryGetValue(
            "optimization-recovery-execution",
            out recoveryTask)
            && !string.IsNullOrWhiteSpace(
                recoveryTask.Output))
        {
            OptimizationRecoveryExecutionResult recovery =
                JsonSerializer.Deserialize<
                    OptimizationRecoveryExecutionResult>(
                        recoveryTask.Output,
                        CreateJsonOptions())!;

            if (recovery.Result != null)
            {
                return recovery.Result;
            }
        }

        return optimization.Result;
    }

    private static string ResolveFinalJobId(
        WorkflowRun run,
        string originalJobId)
    {
        TaskResult? recoveryTask;

        if (run.Results.TryGetValue(
                "optimization-stability-recovery-execution",
                out recoveryTask)
            && !string.IsNullOrWhiteSpace(recoveryTask.Output))
        {
            RecoveryJobExecutionResult stabilityRecovery =
                JsonSerializer.Deserialize<
                    RecoveryJobExecutionResult>(
                        recoveryTask.Output,
                        CreateJsonOptions())!;

            if (stabilityRecovery.RecoveryJob != null)
            {
                return stabilityRecovery.RecoveryJob.JobId;
            }
        }

        if (!run.Results.TryGetValue(
            "optimization-recovery-execution",
            out recoveryTask)
            || string.IsNullOrWhiteSpace(recoveryTask.Output))
        {
            return originalJobId;
        }

        OptimizationRecoveryExecutionResult recovery =
            JsonSerializer.Deserialize<
                OptimizationRecoveryExecutionResult>(
                    recoveryTask.Output,
                    CreateJsonOptions())!;

        return recovery.RecoveryJob == null
            ? originalJobId
            : recovery.RecoveryJob.JobId;
    }

    private static CalculationValidationReport?
        ResolveValidationReport(WorkflowRun run)
    {
        TaskResult? validationTask;

        if (!run.Results.TryGetValue(
            "optimization-validation",
            out validationTask)
            || string.IsNullOrWhiteSpace(
                validationTask.Output))
        {
            return null;
        }

        OptimizationValidationSkillResult validation =
            JsonSerializer.Deserialize<
                OptimizationValidationSkillResult>(
                    validationTask.Output,
                    CreateJsonOptions())!;
        return validation.Report;
    }

    private static int CountCorrections(WorkflowRun run)
    {
        int count = 0;
        TaskResult? task;

        if (run.Results.TryGetValue(
                "optimization-recovery-execution",
                out task)
            && !string.IsNullOrWhiteSpace(task.Output))
        {
            count++;
        }

        if (run.Results.TryGetValue(
                "optimization-stability-recovery-execution",
                out task)
            && !string.IsNullOrWhiteSpace(task.Output))
        {
            count++;
        }

        return count;
    }

    private static AnomalyCheckResult? ResolveStabilityResult(
        WorkflowRun run)
    {
        TaskResult? task;

        if (run.Results.TryGetValue(
                "optimization-stability-recheck",
                out task)
            && !string.IsNullOrWhiteSpace(task.Output))
        {
            return JsonSerializer.Deserialize<AnomalyCheckResult>(
                task.Output,
                CreateJsonOptions());
        }

        if (run.Results.TryGetValue(
                "optimization-stability-check",
                out task)
            && !string.IsNullOrWhiteSpace(task.Output))
        {
            return JsonSerializer.Deserialize<AnomalyCheckResult>(
                task.Output,
                CreateJsonOptions());
        }

        return null;
    }

    private static string MapStabilityStatus(
        AnomalyCheckStatus status)
    {
        if (status == AnomalyCheckStatus.Passed)
        {
            return "Stable";
        }

        if (status == AnomalyCheckStatus.Finding)
        {
            return "Unstable";
        }

        if (status == AnomalyCheckStatus.Skipped)
        {
            return "Skipped";
        }

        if (status == AnomalyCheckStatus.ExecutionFailed)
        {
            return "Failed";
        }

        return status.ToString();
    }

    private static FrequencyAnalysisResult?
        ResolveFrequencyResult(WorkflowRun run)
    {
        TaskResult? task;

        if (!run.Results.TryGetValue(
                "optimization-frequency-analysis",
                out task)
            || string.IsNullOrWhiteSpace(task.Output))
        {
            return null;
        }

        return JsonSerializer.Deserialize<
            FrequencyAnalysisResult>(
                task.Output,
                CreateJsonOptions());
    }

    private async Task SaveParameterAuditAsync(
        CalculationJob job,
        CalculationSpec spec,
        List<ResolvedCalculationParameter> resolvedParameters,
        IQuantumProgramAdapter adapter,
        CancellationToken cancellationToken)
    {
        CalculationParameterAudit audit =
            new CalculationParameterAudit();
        audit.JobId = job.JobId;
        audit.Engine = spec.Program;
        audit.TaskType = spec.TaskType;
        audit.ResolvedParameters =
            resolvedParameters == null
                ? new List<ResolvedCalculationParameter>()
                : new List<ResolvedCalculationParameter>(
                    resolvedParameters);
        audit.ProgramKeywords =
            new List<ProgramParameterTranslation>(
                adapter.GetParameterTranslations(spec));
        await _repository.SaveParameterAuditAsync(
            audit,
            cancellationToken);
    }

    private static string SerializeSkillInput<T>(T value)
    {
        return JsonSerializer.Serialize(
            value,
            CreateJsonOptions());
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions(
            JsonSerializerDefaults.Web);
    }
}
