using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>根据优化异常诊断生成修正计划。</summary>
public sealed class OptimizationCorrectionPlanningSkill : ISkill
{
    private const string DiagnosisKey = "diagnosis";

    private readonly ICalculationRepository _calculationRepository;
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化修正规划 Skill。</summary>
    public OptimizationCorrectionPlanningSkill(
        ICalculationRepository calculationRepository,
        IAnomalyRepository anomalyRepository)
    {
        if (calculationRepository == null)
        {
            throw new ArgumentNullException(
                nameof(calculationRepository));
        }

        if (anomalyRepository == null)
        {
            throw new ArgumentNullException(
                nameof(anomalyRepository));
        }

        _calculationRepository = calculationRepository;
        _anomalyRepository = anomalyRepository;
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.PlanOptimizationCorrection);
        _capabilities.Add("anomaly.correction-planning");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.PlanOptimizationCorrection;
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

    /// <summary>生成并保存优化修正计划。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? diagnosisJson;

        if (!request.Inputs.TryGetValue(
            DiagnosisKey,
            out diagnosisJson)
            || string.IsNullOrWhiteSpace(diagnosisJson))
        {
            throw new InvalidOperationException(
                "优化修正规划节点缺少 diagnosis 输入。");
        }

        OptimizationFailureDiagnosisResult diagnosis =
            SkillJson.Deserialize<
                OptimizationFailureDiagnosisResult>(
                    diagnosisJson);
        OptimizationCorrectionPlanningResult planningResult =
            await CreatePlanAsync(
                diagnosis,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(planningResult);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private async Task<OptimizationCorrectionPlanningResult>
        CreatePlanAsync(
            OptimizationFailureDiagnosisResult diagnosis,
            CancellationToken cancellationToken)
    {
        OptimizationCorrectionPlanningResult result =
            new OptimizationCorrectionPlanningResult();
        result.AnomalyRecordId = diagnosis.AnomalyRecordId;

        if (diagnosis.Diagnoses.Count == 0
            || string.IsNullOrWhiteSpace(diagnosis.SourceJobId))
        {
            result.PlanCreated = false;
            result.Message = "诊断结果中没有可规划的优化异常。";
            return result;
        }

        CalculationJob? sourceJob =
            await _calculationRepository.GetJobAsync(
                diagnosis.SourceJobId,
                cancellationToken);
        DiagnosisReport selected =
            SelectDiagnosis(diagnosis.Diagnoses);
        CorrectionOption? option =
            CreateOption(selected, sourceJob);

        if (option == null)
        {
            result.PlanCreated = false;
            result.Message =
                "当前异常没有安全的自动修正方案：" +
                selected.AnomalyCode;
            return result;
        }

        CorrectionPlan plan = new CorrectionPlan();
        plan.Id = "plan-" + Guid.NewGuid().ToString("N");
        plan.SourceJobId = diagnosis.SourceJobId;
        plan.Option = option;

        result.PlanCreated = true;
        result.Plan = plan;
        result.Message = option.Description;

        await SavePlanAsync(
            diagnosis,
            plan,
            cancellationToken);
        return result;
    }

    private static DiagnosisReport SelectDiagnosis(
        List<DiagnosisReport> diagnoses)
    {
        DiagnosisReport selected = diagnoses[0];
        int selectedPriority = GetPriority(selected.AnomalyCode);

        for (int index = 1; index < diagnoses.Count; index++)
        {
            int priority = GetPriority(
                diagnoses[index].AnomalyCode);

            if (priority < selectedPriority)
            {
                selected = diagnoses[index];
                selectedPriority = priority;
            }
        }

        return selected;
    }

    private static int GetPriority(string code)
    {
        if (code == OptimizationAnomalyCodes.GeometryInvalid)
        {
            return 0;
        }

        if (code == OptimizationAnomalyCodes.MaxCyclesExceeded)
        {
            return 1;
        }

        if (code == OptimizationAnomalyCodes.NotConverged)
        {
            return 2;
        }

        if (code == CommonAnomalyCodes.ScfNotConverged
            || code == CommonAnomalyCodes.ScfMaxCycles)
        {
            return 3;
        }

        if (code == OptimizationAnomalyCodes.ForceNotConverged)
        {
            return 4;
        }

        if (code == OptimizationAnomalyCodes.DisplacementNotConverged)
        {
            return 5;
        }

        if (code == OptimizationAnomalyCodes.Oscillation)
        {
            return 6;
        }

        if (code == OptimizationAnomalyCodes.StepRejected
            || code == OptimizationAnomalyCodes.TrustRadiusFailure)
        {
            return 7;
        }

        if (code == OptimizationAnomalyCodes.CheckpointMissing)
        {
            return 8;
        }

        return 99;
    }

    private static CorrectionOption? CreateOption(
        DiagnosisReport diagnosis,
        CalculationJob? sourceJob)
    {
        if (diagnosis.AnomalyCode
            == OptimizationAnomalyCodes.GeometryInvalid)
        {
            return null;
        }

        if (diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.MaxCyclesExceeded
            || diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.NotConverged)
        {
            return CreateIncreaseMaxCyclesOption(sourceJob);
        }

        if (diagnosis.AnomalyCode
                == CommonAnomalyCodes.ScfNotConverged
            || diagnosis.AnomalyCode
                == CommonAnomalyCodes.ScfMaxCycles)
        {
            return CreateScfRecoveryOption(sourceJob);
        }

        if (diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.ForceNotConverged
            || diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.Oscillation
            || diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.StepRejected
            || diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.TrustRadiusFailure
            || diagnosis.AnomalyCode
                == OptimizationAnomalyCodes.CheckpointMissing)
        {
            return CreateInitialHessianOption(
                "CalcFC",
                CorrectionIntentCodes.UseOptimizationCalcFc);
        }

        if (diagnosis.AnomalyCode
            == OptimizationAnomalyCodes.DisplacementNotConverged)
        {
            return CreateInitialHessianOption(
                "ReadFC",
                CorrectionIntentCodes.UseOptimizationReadFc);
        }

        return null;
    }

    private static CorrectionOption CreateIncreaseMaxCyclesOption(
        CalculationJob? sourceJob)
    {
        int currentMaxCycles =
            ReadIntOption(
                sourceJob,
                CalculationOptionKeys.OptimizationMaxCycles,
                CalculationDefaults.DefaultOptimizationMaxCycles);
        int targetMaxCycles = currentMaxCycles + 100;

        if (targetMaxCycles > 1000)
        {
            targetMaxCycles = 1000;
        }

        CorrectionOption option = new CorrectionOption();
        option.Id = "increase-optimization-max-cycles";
        option.IntentCode =
            CorrectionIntentCodes.IncreaseOptimizationMaxCycles;
        option.Title = "增加优化最大循环次数";
        option.Description =
            "把 opt=maxcycles 从 " +
            currentMaxCycles.ToString() +
            " 增加到 " +
            targetMaxCycles.ToString() +
            "，并继续几何优化。";
        option.RiskLevel = CorrectionRiskLevel.Low;
        option.RequiresApproval = false;
        option.CanUseForRestart = true;

        CorrectionChange change = new CorrectionChange();
        change.Name = CalculationOptionKeys.OptimizationMaxCycles;
        change.OldValue = currentMaxCycles.ToString();
        change.NewValue = targetMaxCycles.ToString();
        change.Reason = "优化达到循环上限或仍未收敛。";
        option.Changes.Add(change);
        return option;
    }

    private static CorrectionOption CreateInitialHessianOption(
        string hessian,
        string intentCode)
    {
        CorrectionOption option = new CorrectionOption();
        option.Id = "optimization-initial-hessian-" + hessian;
        option.IntentCode = intentCode;
        option.Title = "使用 " + hessian + " 初始 Hessian";
        option.Description =
            "使用 " +
            hessian +
            " 重新开始几何优化，以改善收敛稳定性。";
        option.RiskLevel = CorrectionRiskLevel.Medium;
        option.RequiresApproval = false;
        option.CanUseForRestart = true;

        CorrectionChange change = new CorrectionChange();
        change.Name =
            CalculationOptionKeys.OptimizationInitialHessian;
        change.OldValue = "None";
        change.NewValue = hessian;
        change.Reason = "改善初始 Hessian 和优化收敛行为。";
        option.Changes.Add(change);
        return option;
    }

    private static CorrectionOption CreateScfRecoveryOption(
        CalculationJob? sourceJob)
    {
        int currentLimit = ReadIntOption(
            sourceJob,
            CalculationOptionKeys.ScfIterationLimit,
            CalculationDefaults.DefaultScfIterationLimit);
        int targetLimit = currentLimit + 100;

        if (targetLimit > 1000)
        {
            targetLimit = 1000;
        }

        CorrectionOption option = new CorrectionOption();
        option.Id = "reuse-scf-recovery";
        option.IntentCode = CorrectionIntentCodes.ReuseScfRecovery;
        option.Title = "复用 SCF 修复方案";
        option.Description =
            "增加 SCF 迭代上限并重新执行优化。";
        option.RiskLevel = CorrectionRiskLevel.Low;
        option.RequiresApproval = false;
        option.CanUseForRestart = true;

        CorrectionChange change = new CorrectionChange();
        change.Name = CalculationOptionKeys.ScfIterationLimit;
        change.OldValue = currentLimit.ToString();
        change.NewValue = targetLimit.ToString();
        change.Reason = "SCF 达到迭代上限或未收敛。";
        option.Changes.Add(change);
        return option;
    }

    private static int ReadIntOption(
        CalculationJob? sourceJob,
        string key,
        int fallback)
    {
        if (sourceJob == null)
        {
            return fallback;
        }

        string? value;

        if (!sourceJob.Spec.ExtraOptions.TryGetValue(
            key,
            out value))
        {
            return fallback;
        }

        int number;

        if (!int.TryParse(value, out number))
        {
            return fallback;
        }

        return number;
    }

    private async Task SavePlanAsync(
        OptimizationFailureDiagnosisResult diagnosis,
        CorrectionPlan plan,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(diagnosis.SourceJobId)
            || string.IsNullOrWhiteSpace(diagnosis.AnomalyRecordId))
        {
            return;
        }

        AnomalyRecord? record =
            await _anomalyRepository.GetAsync(
                diagnosis.SourceJobId,
                diagnosis.AnomalyRecordId,
                cancellationToken);

        if (record == null)
        {
            return;
        }

        record.CorrectionPlans.Add(plan);
        record.Status = plan.Option != null
            && plan.Option.RequiresApproval
                ? AnomalyRecordStatus.AwaitingApproval
                : AnomalyRecordStatus.ReadyForRecovery;
        await _anomalyRepository.SaveAsync(
            record,
            cancellationToken);
    }
}
