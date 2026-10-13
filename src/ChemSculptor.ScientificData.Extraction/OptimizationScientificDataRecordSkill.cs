using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Extraction.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using System.Text.Json;

namespace ChemSculptor.ScientificData.Extraction;

/// <summary>把优化完整工作流的结果保存为科学成果。</summary>
public sealed class OptimizationScientificDataRecordSkill : ISkill
{
    private const string OptimizationKey = "optimization";
    private const string ProcessingPlanKey = "processingPlan";
    private const string RecoveryExecutionKey = "recoveryExecution";
    private const string RecoveryValidationKey = "recoveryValidation";
    private const string StabilityRecoveryExecutionKey =
        "stabilityRecoveryExecution";

    private readonly IScientificDataRepository _repository;
    private readonly List<string> _capabilities;
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>创建优化科学数据记录 Skill。</summary>
    public OptimizationScientificDataRecordSkill(
        IScientificDataRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            ScientificDataSkillIds.RecordOptimizationResult);
        _capabilities.Add("science.data");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return ScientificDataSkillIds.RecordOptimizationResult;
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

    /// <summary>提取并保存优化科学成果。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        OptimizationCalculationSkillResult optimization =
            DeserializeRequired<
                OptimizationCalculationSkillResult>(
                    request,
                    OptimizationKey);
        CalculationWorkflowProcessingPlanSkillResult processingPlan =
            DeserializeRequired<
                CalculationWorkflowProcessingPlanSkillResult>(
                    request,
                    ProcessingPlanKey);
        OptimizationRecoveryExecutionResult? recoveryExecution =
            DeserializeOptional<
                OptimizationRecoveryExecutionResult>(
                    request,
                    RecoveryExecutionKey);
        RecoveryValidationResult? recoveryValidation =
            DeserializeOptional<RecoveryValidationResult>(
                request,
                RecoveryValidationKey);
        RecoveryJobExecutionResult?
            stabilityRecoveryExecution =
                DeserializeOptional<
                    RecoveryJobExecutionResult>(
                        request,
                        StabilityRecoveryExecutionKey);

        ScientificResult result = CreateScientificResult(
            optimization,
            processingPlan,
            recoveryExecution,
            recoveryValidation,
            stabilityRecoveryExecution);
        await _repository.SaveAsync(result, cancellationToken);

        ScientificResultExtractionResult extractionResult =
            new ScientificResultExtractionResult();
        extractionResult.Succeeded = true;
        extractionResult.Result = result;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = JsonSerializer.Serialize(
            extractionResult,
            JsonOptions);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static ScientificResult CreateScientificResult(
        OptimizationCalculationSkillResult optimization,
        CalculationWorkflowProcessingPlanSkillResult processingPlan,
        OptimizationRecoveryExecutionResult? recoveryExecution,
        RecoveryValidationResult? recoveryValidation,
        RecoveryJobExecutionResult?
            stabilityRecoveryExecution)
    {
        OptimizationResult finalResult = optimization.Result;
        CalculationJob finalJob = optimization.Job;

        if (stabilityRecoveryExecution != null
            && stabilityRecoveryExecution.OptimizationResult
                != null
            && stabilityRecoveryExecution.RecoveryJob != null)
        {
            finalResult =
                stabilityRecoveryExecution.OptimizationResult;
            finalJob = stabilityRecoveryExecution.RecoveryJob;
        }

        if (recoveryExecution != null
            && recoveryExecution.Result != null
            && recoveryExecution.RecoveryJob != null)
        {
            finalResult = recoveryExecution.Result;
            finalJob = recoveryExecution.RecoveryJob;
        }

        ScientificResult result = new ScientificResult();
        result.Id = "optimization-" + finalJob.JobId;
        result.Title = "几何优化结果";
        result.Summary = processingPlan.Plan.Summary;
        result.Status =
            finalResult.NormalTermination
            && finalResult.OptimizationConverged == true
                ? ScientificResultStatus.Complete
                : ScientificResultStatus.Partial;
        result.Metadata["jobId"] = finalJob.JobId;
        result.Metadata["parentJobId"] = finalJob.ParentJobId;
        result.Metadata["program"] = finalResult.Program;
        result.Metadata["method"] = finalResult.Method;
        result.Metadata["basis"] = finalResult.Basis;
        result.Metadata["stepCount"] =
            finalResult.StepCount.ToString();

        if (finalResult.FinalEnergy.HasValue)
        {
            result.Metadata["finalEnergy"] =
                finalResult.FinalEnergy.Value.ToString(
                    "G17",
                    System.Globalization.CultureInfo.InvariantCulture);
        }

        if (recoveryValidation != null)
        {
            result.Metadata["recoveryResolvedOriginalAnomaly"] =
                recoveryValidation.ResolvedOriginalAnomaly
                    .ToString();
        }

        result.CompletedAt = DateTimeOffset.UtcNow;
        return result;
    }

    private static T DeserializeRequired<T>(
        TaskRequest request,
        string key)
        where T : class
    {
        string? json;

        if (!request.Inputs.TryGetValue(key, out json)
            || string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "优化科学数据节点缺少 " + key + " 输入。");
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }

    private static T? DeserializeOptional<T>(
        TaskRequest request,
        string key)
        where T : class
    {
        string? json;

        if (!request.Inputs.TryGetValue(key, out json)
            || string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }
}
