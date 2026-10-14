using System.Collections.Concurrent;
using System.Text.Json;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>
/// TDDFT 单点计算服务。
/// 负责创建作业、执行 TDDFT 单点工作流并读取结果。
/// </summary>
public sealed class TddftCalculationService
    : ITddftCalculationService
{
    private readonly ICalculationWorkspace _workspace;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly ICalculationRepository _repository;
    private readonly WorkflowEngine _workflowEngine;
    private readonly ConcurrentDictionary<
        string,
        TddftCalculationResult> _results;

    /// <summary>创建 TDDFT 单点计算服务。</summary>
    public TddftCalculationService(
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
            new ConcurrentDictionary<
                string,
                TddftCalculationResult>(
                    StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>提交 TDDFT 单点计算。</summary>
    public async Task<TddftSubmissionResult> SubmitAsync(
        TddftCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        TddftSubmissionResult result =
            new TddftSubmissionResult();

        if (request == null)
        {
            result.Error = "TDDFT 请求不能为空。";
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
                ?? CalculationDefaults.CreateDefaultTddftSinglePoint();

            if (spec.TaskType != CalculationTaskType.TdDft)
            {
                result.Error = "TDDFT 服务只能处理 TdDft 任务。";
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
                TddftWorkflowDefinitionFactory.Create(
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

            TddftSinglePointCalculationSkillResult?
                skillResult = ExtractSkillResult(workflowRun);
            bool succeeded =
                workflowRun.State == WorkflowState.Passed
                && skillResult != null
                && skillResult.Passed
                && skillResult.Result != null;

            job.State = succeeded
                ? CalculationJobState.Validated
                : CalculationJobState.Failed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await _repository.SaveJobAsync(
                job,
                cancellationToken);

            if (!succeeded
                || skillResult == null
                || skillResult.Result == null)
            {
                result.Error =
                    "TDDFT 单点工作流没有返回通过验证的结果。";
                return result;
            }

            result.Succeeded = true;
            result.Job = job;
            result.Result = skillResult.Result;
            _results[jobId] = skillResult.Result;
            result.Message = "TDDFT 单点计算已完成。";
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

    /// <summary>查询 TDDFT 计算结果。</summary>
    public Task<TddftCalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        TddftCalculationResult? result;
        _results.TryGetValue(jobId, out result);
        return Task.FromResult(result);
    }

    private static TddftSinglePointCalculationSkillResult?
        ExtractSkillResult(WorkflowRun run)
    {
        TaskResult? task;

        if (!run.Results.TryGetValue(
                "tddft-single-point",
                out task)
            || string.IsNullOrWhiteSpace(task.Output))
        {
            return null;
        }

        return JsonSerializer.Deserialize<
            TddftSinglePointCalculationSkillResult>(
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
