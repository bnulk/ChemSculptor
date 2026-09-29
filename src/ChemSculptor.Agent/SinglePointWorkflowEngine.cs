using System.Collections.Concurrent;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>
/// 单点计算工作流引擎。
/// 负责提交并后台运行声明式工作流，同时维护计算作业状态。
/// </summary>
public sealed class SinglePointWorkflowEngine : ISinglePointWorkflowEngine
{
    private readonly WorkflowEngine _workflowEngine;
    private readonly ICalculationRepository _repository;
    private readonly ConcurrentDictionary<string, Task> _workflowTasks =
        new ConcurrentDictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

    /// <summary>创建工作流执行器。</summary>
    public SinglePointWorkflowEngine(
        WorkflowEngine workflowEngine,
        ICalculationRepository repository)
    {
        if (workflowEngine == null)
        {
            throw new ArgumentNullException(nameof(workflowEngine));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _workflowEngine = workflowEngine;
        _repository = repository;
    }

    /// <summary>提交工作流并在后台执行。</summary>
    public async Task<WorkflowRun> StartAsync(
        WorkflowDefinition definition,
        IReadOnlyDictionary<string, string> inputs,
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        WorkflowRun run = await _workflowEngine.SubmitAsync(
            definition,
            inputs,
            cancellationToken);

        Task workflowTask = ExecuteWorkflowAsync(job, run.Id);
        _workflowTasks[job.JobId] = workflowTask;
        return run;
    }

    private async Task ExecuteWorkflowAsync(
        CalculationJob job,
        string workflowId)
    {
        try
        {
            WorkflowRun run = await _workflowEngine.RunAsync(
                workflowId,
                CancellationToken.None);

            if (run.State != WorkflowState.Passed)
            {
                CalculationJob? currentJob =
                    await _repository.GetJobAsync(
                        job.JobId,
                        CancellationToken.None);

                if (currentJob != null
                    && currentJob.State == CalculationJobState.Canceled)
                {
                    return;
                }

                job.State = CalculationJobState.Failed;
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _repository.SaveJobAsync(job, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            job.State = CalculationJobState.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;

            CalculationDiagnostic diagnostic = new CalculationDiagnostic();
            diagnostic.Severity = CalculationDiagnosticSeverity.Error;
            diagnostic.Code = "workflow.execution_failed";
            diagnostic.Message = ex.Message;
            job.Diagnostics.Add(diagnostic);

            await _repository.SaveJobAsync(job, CancellationToken.None);
        }
        finally
        {
            Task? ignoredTask;
            _workflowTasks.TryRemove(job.JobId, out ignoredTask);
        }
    }
}
