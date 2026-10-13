using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common.CalculationResultValidation;

namespace ChemSculptor.Skills.Common.OptimizationCalculation;

/// <summary>验证一次几何优化作业的正常结果。</summary>
public sealed class OptimizationCalculationValidationSkill : ISkill
{
    private readonly CalculationValidationService _validationService;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化结果验证 Skill。</summary>
    public OptimizationCalculationValidationSkill(
        CalculationValidationService validationService,
        ICalculationRepository repository)
    {
        if (validationService == null)
        {
            throw new ArgumentNullException(
                nameof(validationService));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _validationService = validationService;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationOptimizationValidation);
        _capabilities.Add("calculation.validation");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationOptimizationValidation;
        }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>验证优化结果并更新作业状态。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        CalculationWaitSkillResult waitResult;
        OptimizationResult optimizationResult;
        string? optimizationJson;

        if (request.Inputs.TryGetValue(
            "optimization",
            out optimizationJson)
            && !string.IsNullOrWhiteSpace(optimizationJson))
        {
            OptimizationCalculationSkillResult composite =
                SkillJson.Deserialize<
                    OptimizationCalculationSkillResult>(
                        optimizationJson);

            if (composite.Result == null)
            {
                throw new InvalidOperationException(
                    "优化复合结果中没有 OptimizationResult。");
            }

            waitResult = composite.Wait;
            optimizationResult = composite.Result;
        }
        else
        {
            string? waitJson;
            string? extractionJson;

            if (!request.Inputs.TryGetValue(
                "wait",
                out waitJson))
            {
                throw new InvalidOperationException(
                    "优化验证节点缺少 wait 或 optimization 输入。");
            }

            if (!request.Inputs.TryGetValue(
                "extraction",
                out extractionJson))
            {
                throw new InvalidOperationException(
                    "优化验证节点缺少 extraction 输入。");
            }

            if (string.IsNullOrWhiteSpace(waitJson)
                || string.IsNullOrWhiteSpace(extractionJson))
            {
                throw new InvalidOperationException(
                    "优化验证节点输入不能为空。");
            }

            waitResult =
                SkillJson.Deserialize<CalculationWaitSkillResult>(
                    waitJson);
            CalculationResultExtractionResult extractionResult =
                SkillJson.Deserialize<
                    CalculationResultExtractionResult>(
                        extractionJson);

            if (extractionResult.OptimizationResult == null)
            {
                throw new InvalidOperationException(
                    "结果提取中没有 OptimizationResult。");
            }

            optimizationResult =
                extractionResult.OptimizationResult;
        }
        CalculationResultValidationRequest validationRequest =
            new CalculationResultValidationRequest();
        validationRequest.Job = waitResult.Job;
        validationRequest.OptimizationResult =
            optimizationResult;
        CalculationResultValidationResult validationResult =
            _validationService.Validate(validationRequest);
        CalculationValidationReport report =
            validationResult.Report;
        CalculationJob job = waitResult.Job;

        if (waitResult.State == CalculationJobState.Completed
            && report.Passed)
        {
            job.State = CalculationJobState.Validated;
        }
        else
        {
            job.State = CalculationJobState.Failed;
        }

        await _repository.SaveValidationAsync(
            report,
            job.JobId,
            cancellationToken);
        await _repository.SaveJobAsync(job, cancellationToken);

        OptimizationValidationSkillResult result =
            new OptimizationValidationSkillResult();
        result.Passed = report.Passed;
        result.Job = job;
        result.Result = optimizationResult;
        result.Report = report;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return taskResult;
    }

    /// <summary>检查技能可用性。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
