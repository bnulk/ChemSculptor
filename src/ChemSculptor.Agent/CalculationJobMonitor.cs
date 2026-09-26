using System.Collections.Concurrent;
using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 计算作业后台监控器。
/// 当前使用轮询方式查询执行后端，进程结束后调用程序适配器解析输出。
/// </summary>
public sealed class CalculationJobMonitor : ICalculationJobMonitor
{
    private readonly IComputeBackend _computeBackend;
    private readonly ICalculationProcessingPlanner _processingPlanner;
    private readonly ICalculationRepository _repository;
    private readonly ISkillInvoker _skillInvoker;
    private readonly CalculationJobMonitorOptions _options;
    private readonly ConcurrentDictionary<string, Task> _monitorTasks =
        new ConcurrentDictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

    /// <summary>创建计算作业监控器。</summary>
    public CalculationJobMonitor(
        IComputeBackend computeBackend,
        ICalculationProcessingPlanner processingPlanner,
        ICalculationRepository repository,
        ISkillInvoker skillInvoker,
        CalculationJobMonitorOptions options)
    {
        if (computeBackend == null)
        {
            throw new ArgumentNullException(nameof(computeBackend));
        }

        if (processingPlanner == null)
        {
            throw new ArgumentNullException(nameof(processingPlanner));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (skillInvoker == null)
        {
            throw new ArgumentNullException(nameof(skillInvoker));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.PollingIntervalMilliseconds <= 0)
        {
            throw new ArgumentException(
                "轮询间隔必须大于零。",
                nameof(options));
        }

        _computeBackend = computeBackend;
        _processingPlanner = processingPlanner;
        _repository = repository;
        _skillInvoker = skillInvoker;
        _options = options;
    }

    /// <summary>开始监控作业；该方法立即返回，不等待计算结束。</summary>
    public void Start(CalculationJob job)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        Task monitorTask = MonitorAsync(job);
        _monitorTasks[job.JobId] = monitorTask;
    }

    private async Task MonitorAsync(CalculationJob job)
    {
        try
        {
            CalculationJobState finalState = await WaitForFinalStateAsync(job);

            job.State = finalState;
            job.CompletedAt = DateTimeOffset.UtcNow;

            if (finalState == CalculationJobState.Canceled)
            {
                await _repository.SaveJobAsync(job, CancellationToken.None);
                return;
            }

            CalculationResult result;

            if (!File.Exists(job.OutputFilePath))
            {
                job.State = CalculationJobState.Failed;
                result = new CalculationResult();
                result.FailureKind = CalculationFailureKind.OutputMissing;
                AddJobDiagnostic(
                    job,
                    CalculationDiagnosticSeverity.Error,
                    "calculation.output_not_found",
                    "计算结束后没有找到输出文件：" + job.OutputFilePath);
            }
            else
            {
                CalculationResultExtractionRequest extractionRequest =
                    new CalculationResultExtractionRequest();
                extractionRequest.JobId = job.JobId;
                extractionRequest.Program = job.Spec.Program;
                extractionRequest.OutputFilePath = job.OutputFilePath;
                extractionRequest.Spec = job.Spec;

                CalculationResultExtractionResult extractionResult =
                    await _skillInvoker.InvokeAsync<
                        CalculationResultExtractionRequest,
                        CalculationResultExtractionResult>(
                            CalculationSkillIds.GaussianSinglePointResultExtraction,
                            extractionRequest,
                            CancellationToken.None);

                result = extractionResult.Result;

                if (finalState == CalculationJobState.Failed
                    && result.FailureKind == CalculationFailureKind.None)
                {
                    result.FailureKind = CalculationFailureKind.ProcessFailed;
                }
            }

            result.JobId = job.JobId;
            result.Program = job.Spec.Program;
            result.Method = job.Spec.Method;
            result.Basis = job.Spec.Basis;
            result.Charge = job.Spec.Charge;
            result.Multiplicity = job.Spec.Multiplicity;
            result.OutputFilePath = job.OutputFilePath;

            CalculationProcessingPlan processingPlan =
                _processingPlanner.CreatePlan(job, result);

            CalculationResultValidationRequest validationRequest =
                new CalculationResultValidationRequest();
            validationRequest.Job = job;
            validationRequest.Result = result;

            CalculationResultValidationResult validationResult =
                await _skillInvoker.InvokeAsync<
                    CalculationResultValidationRequest,
                    CalculationResultValidationResult>(
                        CalculationSkillIds.CalculationResultValidation,
                        validationRequest,
                        CancellationToken.None);

            await _repository.SaveResultAsync(result, CancellationToken.None);
            await _repository.SaveProcessingPlanAsync(
                processingPlan,
                CancellationToken.None);

            if (finalState == CalculationJobState.Completed
                && validationResult.Passed)
            {
                job.State = CalculationJobState.Parsed;
            }
            else
            {
                job.State = CalculationJobState.Failed;
            }

            await _repository.SaveJobAsync(job, CancellationToken.None);
        }
        catch (Exception ex)
        {
            job.State = CalculationJobState.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            AddJobDiagnostic(
                job,
                CalculationDiagnosticSeverity.Error,
                "calculation.monitor_failed",
                ex.Message);

            try
            {
                await _repository.SaveJobAsync(job, CancellationToken.None);
            }
            catch
            {
                // 监控异常不能因为仓储再抛异常而变成未观察的后台错误。
            }
        }
        finally
        {
            Task? ignoredTask;
            _monitorTasks.TryRemove(job.JobId, out ignoredTask);
        }
    }

    private async Task<CalculationJobState> WaitForFinalStateAsync(CalculationJob job)
    {
        while (true)
        {
            CalculationJobState state = await _computeBackend.GetStatusAsync(
                job,
                CancellationToken.None);

            if (state == CalculationJobState.Completed
                || state == CalculationJobState.Failed
                || state == CalculationJobState.Canceled)
            {
                return state;
            }

            await Task.Delay(_options.PollingIntervalMilliseconds, CancellationToken.None);
        }
    }

    private static void AddJobDiagnostic(
        CalculationJob job,
        CalculationDiagnosticSeverity severity,
        string code,
        string message)
    {
        CalculationDiagnostic diagnostic = new CalculationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        job.Diagnostics.Add(diagnostic);
    }
}
