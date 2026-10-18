using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Checks;

/// <summary>
/// 几何优化收敛检查。
/// 复用单点失败语义，并补充优化独有的收敛和过程异常。
/// </summary>
public sealed class OptimizationConvergenceCheck : IAnomalyCheck
{
    private static readonly AnomalyCheckDescriptor DescriptorValue =
        CreateDescriptor();

    /// <summary>检查描述。</summary>
    public AnomalyCheckDescriptor Descriptor
    {
        get { return DescriptorValue; }
    }

    /// <summary>判断是否为可检查的优化结果。</summary>
    public bool CanCheck(AnomalyContext context)
    {
        if (context == null
            || context.Job == null
            || context.OptimizationResult == null)
        {
            return false;
        }

        return context.Job.Spec.TaskType
                == CalculationTaskType.Optimization
            || context.Job.Spec.TaskType
                == CalculationTaskType.OptimizationFrequency;
    }

    /// <summary>执行优化异常检查。</summary>
    public Task<AnomalyCheckResult> CheckAsync(
        AnomalyContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        AnomalyCheckResult result =
            new AnomalyCheckResult();
        result.Code = Descriptor.Code;
        result.ImplementationId = Descriptor.ImplementationId;
        result.DisplayName = Descriptor.DisplayName;
        result.Category = Descriptor.Category;
        result.Mechanism = Descriptor.Mechanism;
        result.IsRequired = Descriptor.IsRequired;
        result.StartedAt = DateTimeOffset.UtcNow;

        if (context.Job != null)
        {
            result.JobId = context.Job.JobId;
        }

        if (!CanCheck(context))
        {
            result.Status = AnomalyCheckStatus.Skipped;
            result.Summary = "当前上下文不是完整的几何优化结果。";
            result.CompletedAt = DateTimeOffset.UtcNow;
            return Task.FromResult(result);
        }

        OptimizationResult optimizationResult =
            context.OptimizationResult!;
        AddCommonFindings(result, optimizationResult);
        AddOptimizationFindings(result, optimizationResult);

        result.Status = result.Findings.Count > 0
            ? AnomalyCheckStatus.Finding
            : AnomalyCheckStatus.Passed;
        result.Summary = BuildSummary(result);
        result.CompletedAt = DateTimeOffset.UtcNow;
        return Task.FromResult(result);
    }

    private static void AddCommonFindings(
        AnomalyCheckResult result,
        OptimizationResult optimizationResult)
    {
        if (string.IsNullOrWhiteSpace(
                optimizationResult.OutputFilePath)
            || !File.Exists(
                optimizationResult.OutputFilePath))
        {
            AddFinding(
                result,
                CommonAnomalyCodes.OutputMissing,
                "优化输出文件缺失",
                "找不到几何优化输出文件。",
                "optimization.output.missing",
                "几何优化输出文件不存在。",
                false,
                false);
        }

        if (!optimizationResult.NormalTermination)
        {
            AddFinding(
                result,
                CommonAnomalyCodes.NormalTerminationMissing,
                "优化缺少正常结束标志",
                "几何优化输出中没有找到正常结束标志。",
                "optimization.normal_termination.missing",
                "输出中没有 Normal termination。",
                false,
                false);
        }

        if (!optimizationResult.FinalEnergy.HasValue)
        {
            AddFinding(
                result,
                CommonAnomalyCodes.EnergyMissing,
                "优化最终能量缺失",
                "几何优化结果中没有最终能量。",
                "optimization.energy.missing",
                "最终能量为空。",
                false,
                false);
        }

        for (int index = 0;
            index < optimizationResult.Trajectory.Count;
            index++)
        {
            OptimizationStep step =
                optimizationResult.Trajectory[index];

            if (step.ScfConverged == false)
            {
                AddFinding(
                    result,
                    CommonAnomalyCodes.ScfNotConverged,
                    "优化步骤 SCF 未收敛",
                    "优化步骤 " +
                    step.StepIndex.ToString() +
                    " 的 SCF 没有收敛。",
                    "optimization.step.scf_not_converged",
                    "步骤 " +
                    step.StepIndex.ToString() +
                    " 的 SCF 状态为未收敛。",
                    false,
                    true);

                if (step.ScfIterations.HasValue)
                {
                    AddFinding(
                        result,
                        CommonAnomalyCodes.ScfMaxCycles,
                        "优化步骤 SCF 达到迭代上限",
                        "优化步骤 " +
                        step.StepIndex.ToString() +
                        " 在 " +
                        step.ScfIterations.Value.ToString() +
                        " 次迭代后仍未收敛。",
                        "optimization.step.scf_max_cycles",
                        "步骤 " +
                        step.StepIndex.ToString() +
                        " 的 SCF 迭代次数为 " +
                        step.ScfIterations.Value.ToString() +
                        "。",
                        false,
                        true);
                }
            }
        }

        if (ContainsText(optimizationResult, "guess")
            && ContainsText(optimizationResult, "fail"))
        {
            AddFinding(
                result,
                CommonAnomalyCodes.ScfGuessFailed,
                "SCF 初始猜测失败",
                "输出中包含 SCF 初始猜测失败证据。",
                "optimization.scf.guess_failed",
                "发现 guess failure 相关诊断。",
                true,
                false);
        }
    }

    private static void AddOptimizationFindings(
        AnomalyCheckResult result,
        OptimizationResult optimizationResult)
    {
        if (optimizationResult.OptimizationConverged == false)
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.NotConverged,
                "几何优化未收敛",
                "几何优化正常结束，但没有达到收敛条件。",
                "optimization.not_converged",
                "OptimizationConverged 为 false。",
                true,
                true);
        }

        if (optimizationResult.FailureKind
            == OptimizationFailureKind.MaxCyclesExceeded)
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.MaxCyclesExceeded,
                "优化达到最大循环次数",
                "几何优化达到最大循环次数。",
                "optimization.max_cycles_exceeded",
                "FailureKind 为 MaxCyclesExceeded。",
                true,
                true);
        }

        OptimizationConvergence? finalConvergence =
            optimizationResult.FinalConvergence;
        bool forceFlagFailure =
            finalConvergence != null
            && (finalConvergence.MaximumForceConverged == false
                || finalConvergence.RmsForceConverged == false);
        bool displacementFlagFailure =
            finalConvergence != null
            && (finalConvergence.MaximumDisplacementConverged
                    == false
                || finalConvergence.RmsDisplacementConverged
                    == false);

        if (optimizationResult.FailureKind
                == OptimizationFailureKind.ForceNotConverged
            && !forceFlagFailure)
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.ForceNotConverged,
                "优化力指标未收敛",
                "几何优化的力收敛指标未满足。",
                "optimization.force_not_converged",
                "FailureKind 为 ForceNotConverged。",
                true,
                true);
        }

        if (optimizationResult.FailureKind
                == OptimizationFailureKind.DisplacementNotConverged
            && !displacementFlagFailure)
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.DisplacementNotConverged,
                "优化位移指标未收敛",
                "几何优化的位移收敛指标未满足。",
                "optimization.displacement_not_converged",
                "FailureKind 为 DisplacementNotConverged。",
                true,
                true);
        }

        AddConvergenceItemFindings(
            result,
            finalConvergence);

        if (optimizationResult.FailureKind
            == OptimizationFailureKind.Oscillation
            || ContainsText(optimizationResult, "oscillat"))
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.Oscillation,
                "优化过程振荡",
                "几何优化过程出现振荡。",
                "optimization.oscillation",
                "发现振荡相关证据。",
                true,
                true);
        }

        if (optimizationResult.FailureKind
            == OptimizationFailureKind.StepRejected
            || ContainsText(
                optimizationResult,
                "step rejected"))
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.StepRejected,
                "优化步骤被拒绝",
                "优化过程中有步骤被拒绝。",
                "optimization.step_rejected",
                "发现 step rejected 相关证据。",
                true,
                false);
        }

        if (ContainsText(
            optimizationResult,
            "trust radius"))
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.TrustRadiusFailure,
                "优化信赖半径失败",
                "优化过程出现信赖半径失败。",
                "optimization.trust_radius_failure",
                "发现 trust radius 相关证据。",
                true,
                false);
        }

        if (ContainsText(
                optimizationResult,
                "checkpoint")
            && (ContainsText(
                    optimizationResult,
                    "missing")
                || ContainsText(
                    optimizationResult,
                    "not found")))
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.CheckpointMissing,
                "优化检查点缺失",
                "优化需要的检查点文件缺失。",
                "optimization.checkpoint_missing",
                "发现 checkpoint 缺失证据。",
                false,
                false);
        }

        if (optimizationResult.FinalGeometry == null
            || optimizationResult.FinalGeometry.Atoms.Count == 0
            || ContainsText(
                optimizationResult,
                "invalid geometry"))
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.GeometryInvalid,
                "优化几何无效",
                "几何优化没有产生有效最终结构。",
                "optimization.geometry_invalid",
                "最终几何缺失、为空或输出报告无效几何。",
                true,
                true);
        }
    }

    private static void AddConvergenceItemFindings(
        AnomalyCheckResult result,
        OptimizationConvergence? convergence)
    {
        if (convergence == null)
        {
            return;
        }

        if (convergence.MaximumForceConverged == false
            || convergence.RmsForceConverged == false)
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.ForceNotConverged,
                "优化力指标未达标",
                "最大力或 RMS 力没有达到收敛阈值。",
                "optimization.convergence.force",
                BuildConvergenceMessage(
                    "力",
                    convergence.MaximumForce,
                    convergence.MaximumForceThreshold,
                    convergence.RmsForce,
                    convergence.RmsForceThreshold),
                true,
                true);
        }

        if (convergence.MaximumDisplacementConverged == false
            || convergence.RmsDisplacementConverged == false)
        {
            AddFinding(
                result,
                OptimizationAnomalyCodes.DisplacementNotConverged,
                "优化位移指标未达标",
                "最大位移或 RMS 位移没有达到收敛阈值。",
                "optimization.convergence.displacement",
                BuildConvergenceMessage(
                    "位移",
                    convergence.MaximumDisplacement,
                    convergence.MaximumDisplacementThreshold,
                    convergence.RmsDisplacement,
                    convergence.RmsDisplacementThreshold),
                true,
                true);
        }
    }

    private static string BuildConvergenceMessage(
        string displayName,
        double? maximumValue,
        double? maximumThreshold,
        double? rmsValue,
        double? rmsThreshold)
    {
        return displayName +
            "指标未达标；Maximum=" +
            FormatValue(maximumValue) +
            "/" +
            FormatValue(maximumThreshold) +
            "，RMS=" +
            FormatValue(rmsValue) +
            "/" +
            FormatValue(rmsThreshold);
    }

    private static void AddFinding(
        AnomalyCheckResult result,
        string code,
        string title,
        string message,
        string evidenceCode,
        string evidenceMessage,
        bool requiresScientificJudgment,
        bool isBlocking)
    {
        AnomalyEvidence evidence = new AnomalyEvidence();
        evidence.Code = evidenceCode;
        evidence.Message = evidenceMessage;
        evidence.Source = "OptimizationResult";

        AnomalyFinding finding = new AnomalyFinding();
        finding.Code = code;
        finding.Title = title;
        finding.Category = AnomalyCategory.Scientific;
        finding.Severity = isBlocking
            ? AnomalySeverity.Error
            : AnomalySeverity.Warning;
        finding.Confidence = 1.0;
        finding.IsBlocking = isBlocking;
        finding.RequiresScientificJudgment =
            requiresScientificJudgment;
        finding.Evidence.Add(evidence);
        finding.Details["summary"] = message;

        result.Evidence.Add(evidence);
        result.Findings.Add(finding);
    }

    private static bool ContainsText(
        OptimizationResult result,
        string text)
    {
        for (int index = 0;
            index < result.Diagnostics.Count;
            index++)
        {
            if (Contains(
                result.Diagnostics[index].Message,
                text)
                || Contains(
                    result.Diagnostics[index].Code,
                    text))
            {
                return true;
            }
        }

        for (int index = 0;
            index < result.Trajectory.Count;
            index++)
        {
            OptimizationStep step =
                result.Trajectory[index];

            for (int diagnosticIndex = 0;
                diagnosticIndex < step.Diagnostics.Count;
                diagnosticIndex++)
            {
                if (Contains(
                    step.Diagnostics[diagnosticIndex].Message,
                    text)
                    || Contains(
                        step.Diagnostics[diagnosticIndex].Code,
                        text))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool Contains(
        string value,
        string text)
    {
        return value.IndexOf(
            text,
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string BuildSummary(
        AnomalyCheckResult result)
    {
        if (result.Findings.Count == 0)
        {
            return "优化检查通过，未发现异常。";
        }

        string summary =
            "发现 " +
            result.Findings.Count.ToString() +
            " 项优化异常：";

        for (int index = 0;
            index < result.Findings.Count;
            index++)
        {
            if (index > 0)
            {
                summary = summary + "；";
            }

            summary = summary + result.Findings[index].Code;
        }

        return summary + "。";
    }

    private static string FormatValue(double? value)
    {
        if (!value.HasValue)
        {
            return "缺失";
        }

        return value.Value.ToString(
            "G6",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static AnomalyCheckDescriptor CreateDescriptor()
    {
        AnomalyCheckDescriptor descriptor =
            new AnomalyCheckDescriptor();
        descriptor.Code =
            CommonAnomalyCheckCodes.OptimizationConvergence;
        descriptor.ImplementationId =
            "common.optimization-convergence";
        descriptor.DisplayName = "几何优化收敛检查";
        descriptor.Category = AnomalyCategory.Scientific;
        descriptor.Mechanism = AnomalyCheckMechanism.OutputArtifact;
        descriptor.IsRequired = true;
        descriptor.Description =
            "检查优化步骤、最终收敛指标和优化过程异常。";
        return descriptor;
    }
}
