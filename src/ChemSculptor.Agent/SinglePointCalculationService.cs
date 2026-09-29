using System.Text.Json;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>
/// 单点计算服务。
/// 负责创建作业、调用 Skill、提交执行后端、启动监控和查询结果。
/// </summary>
public sealed class SinglePointCalculationService : ISinglePointCalculationService
{
    private readonly ICalculationWorkspace _workspace;
    private readonly IComputeBackend _computeBackend;
    private readonly ICalculationRepository _repository;
    private readonly ISinglePointWorkflowEngine _workflowEngine;

    /// <summary>创建单点计算服务。</summary>
    public SinglePointCalculationService(
        ICalculationWorkspace workspace,
        IComputeBackend computeBackend,
        ICalculationRepository repository,
        ISinglePointWorkflowEngine workflowEngine)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (computeBackend == null)
        {
            throw new ArgumentNullException(nameof(computeBackend));
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
        _computeBackend = computeBackend;
        _repository = repository;
        _workflowEngine = workflowEngine;
    }

    /// <summary>提交单点计算。</summary>
    public async Task<SinglePointCalculationSubmissionResult> SubmitAsync(
        CalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        SinglePointCalculationSubmissionResult result =
            new SinglePointCalculationSubmissionResult();

        if (request == null)
        {
            result.Succeeded = false;
            result.Error = "计算请求不能为空。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.CoordinateText))
        {
            result.Succeeded = false;
            result.Error = "坐标文本不能为空。";
            return result;
        }

        try
        {
            CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();
            string overrideError = ApplyOverrides(spec, request.Overrides);

            if (overrideError.Length > 0)
            {
                result.Succeeded = false;
                result.Error = overrideError;
                return result;
            }

            if (spec.TaskType != CalculationTaskType.SinglePoint)
            {
                result.Succeeded = false;
                result.Error = "单点计算服务只能处理单点任务。";
                return result;
            }

            string jobId = "job-" + Guid.NewGuid().ToString("N");
            await _workspace.EnsureJobWorkspaceAsync(jobId, cancellationToken);

            string inputFileName = jobId + ".gjf";
            string inputPath = Path.Combine(
                _workspace.GetInputDirectory(jobId),
                inputFileName);
            string runInputPath = Path.Combine(
                _workspace.GetRunDirectory(jobId),
                inputFileName);
            string outputPath = _workspace.GetJobOutputPath(jobId);

            CalculationJob job = new CalculationJob();
            job.JobId = jobId;
            job.SessionId = request.SessionId;
            job.GeometryId = request.GeometryId;
            job.Goal = request.Goal;
            job.Spec = spec;
            job.State = CalculationJobState.Created;
            job.WorkspaceDirectory = _workspace.GetJobDirectory(jobId);
            job.RunDirectory = _workspace.GetRunDirectory(jobId);
            job.SourceInputFilePath = inputPath;
            job.InputFilePath = runInputPath;
            job.OutputFilePath = outputPath;

            CalculationInputGenerationRequest inputRequest =
                new CalculationInputGenerationRequest();
            inputRequest.Job = job;
            inputRequest.Spec = spec;
            inputRequest.CoordinateText = request.CoordinateText;
            inputRequest.InputFilePath = inputPath;
            inputRequest.RunInputFilePath = runInputPath;
            inputRequest.OutputFilePath = outputPath;

            WorkflowDefinition definition =
                SinglePointWorkflowDefinitionFactory.Create(
                    jobId,
                    request.Goal);

            Dictionary<string, string> workflowInputs =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            workflowInputs["request"] = SerializeSkillInput(inputRequest);

            WorkflowRun workflowRun = await _workflowEngine.StartAsync(
                definition,
                workflowInputs,
                job,
                cancellationToken);

            job.State = CalculationJobState.Running;
            job.StartedAt = DateTimeOffset.UtcNow;
            await _repository.SaveJobAsync(job, CancellationToken.None);

            result.Succeeded = true;
            result.Job = job;
            result.Message = spec.Program + " 单点计算已提交。";
            return result;
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.Error = ex.Message;
            return result;
        }
    }

    /// <summary>查询作业。</summary>
    public Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetJobAsync(jobId, cancellationToken);
    }

    /// <summary>查询结果。</summary>
    public Task<CalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetResultAsync(jobId, cancellationToken);
    }

    /// <summary>查询结果验证报告。</summary>
    public Task<CalculationValidationReport?> GetValidationAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        return _repository.GetValidationAsync(jobId, cancellationToken);
    }

    /// <summary>取消计算作业。</summary>
    public async Task<bool> CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        CalculationJob? job = await _repository.GetJobAsync(
            jobId,
            cancellationToken);

        if (job == null)
        {
            return false;
        }

        if (job.State != CalculationJobState.Running
            && job.State != CalculationJobState.Queued)
        {
            return false;
        }

        await _computeBackend.CancelAsync(job, cancellationToken);

        job.State = CalculationJobState.Canceled;
        job.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveJobAsync(job, cancellationToken);
        return true;
    }

    private static string ApplyOverrides(
        CalculationSpec spec,
        List<CalculationParameter> overrides)
    {
        if (overrides == null)
        {
            return string.Empty;
        }

        for (int index = 0; index < overrides.Count; index++)
        {
            CalculationParameter parameter = overrides[index];
            if (!ApplyOverride(spec, parameter))
            {
                return "不支持的计算参数覆盖：" + parameter.Name;
            }
        }

        return string.Empty;
    }

    private static string SerializeSkillInput<T>(T value)
    {
        JsonSerializerOptions options =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);
        return JsonSerializer.Serialize(value, options);
    }

    private static bool ApplyOverride(
        CalculationSpec spec,
        CalculationParameter parameter)
    {
        if (parameter == null || string.IsNullOrWhiteSpace(parameter.Name))
        {
            return false;
        }

        if (string.Equals(parameter.Name, "program", StringComparison.OrdinalIgnoreCase))
        {
            spec.Program = parameter.CurrentValue;
            UpdateParameter(spec, "program", parameter.CurrentValue);
            return true;
        }

        if (string.Equals(parameter.Name, "method", StringComparison.OrdinalIgnoreCase))
        {
            spec.Method = parameter.CurrentValue;
            UpdateParameter(spec, "method", parameter.CurrentValue);
            return true;
        }

        if (string.Equals(parameter.Name, "basis", StringComparison.OrdinalIgnoreCase))
        {
            spec.Basis = parameter.CurrentValue;
            UpdateParameter(spec, "basis", parameter.CurrentValue);
            return true;
        }

        if (string.Equals(parameter.Name, "charge", StringComparison.OrdinalIgnoreCase))
        {
            int charge;
            if (!int.TryParse(parameter.CurrentValue, out charge))
            {
                return false;
            }

            spec.Charge = charge;
            UpdateParameter(spec, "charge", parameter.CurrentValue);
            return true;
        }

        if (string.Equals(
            parameter.Name,
            "multiplicity",
            StringComparison.OrdinalIgnoreCase))
        {
            int multiplicity;
            if (!int.TryParse(parameter.CurrentValue, out multiplicity))
            {
                return false;
            }

            spec.Multiplicity = multiplicity;
            UpdateParameter(spec, "multiplicity", parameter.CurrentValue);
            return true;
        }

        return false;
    }

    private static void UpdateParameter(
        CalculationSpec spec,
        string name,
        string value)
    {
        for (int index = 0; index < spec.Parameters.Count; index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                parameter.CurrentValue = value;
                parameter.Source = ParameterSource.User;
                return;
            }
        }
    }
}
