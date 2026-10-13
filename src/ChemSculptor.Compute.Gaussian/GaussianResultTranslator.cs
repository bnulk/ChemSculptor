using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 结果翻译器。
/// 把 Gaussian 专用输出模型翻译为智能体使用的通用计算结果。
/// </summary>
public sealed class GaussianResultTranslator
{
    /// <summary>翻译 Gaussian 输出。</summary>
    public CalculationResult Translate(GaussianOutput gaussianOutput)
    {
        if (gaussianOutput == null)
        {
            throw new ArgumentNullException(nameof(gaussianOutput));
        }

        CalculationResult result = new CalculationResult();
        result.Program = Gaussian16ProgramAdapter.ProgramNameValue;
        result.OutputFilePath = gaussianOutput.OutputFilePath;
        result.NormalTermination = gaussianOutput.NormalTermination;
        result.Energy = gaussianOutput.Energy;
        result.ScfConverged = gaussianOutput.ScfConverged;
        result.ScfIterations = gaussianOutput.ScfIterations;
        result.EnergyUnit = "Hartree";
        result.Method = gaussianOutput.EnergyMethod;
        result.FailureKind = CalculationFailureKind.None;

        if (gaussianOutput.ScfConverged == false)
        {
            result.FailureKind =
                CalculationFailureKind.ScfNotConverged;
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.scf_not_converged",
                "Gaussian 输出显示 SCF 没有收敛。");
        }
        else if (gaussianOutput.NormalTermination)
        {
            if (!gaussianOutput.Energy.HasValue)
            {
                result.FailureKind = CalculationFailureKind.EnergyMissing;
            }

            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.normal_termination",
                "Gaussian 正常结束。");
        }
        else if (gaussianOutput.ErrorTermination)
        {
            result.FailureKind = CalculationFailureKind.ProgramError;
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.error_termination",
                "Gaussian 异常结束。");

            for (int index = 0; index < gaussianOutput.ErrorMessages.Count; index++)
            {
                AddDiagnostic(
                    result,
                    CalculationDiagnosticSeverity.Error,
                    "gaussian.error_message",
                    gaussianOutput.ErrorMessages[index]);
            }
        }
        else
        {
            result.FailureKind = CalculationFailureKind.NormalTerminationMissing;
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.normal_termination_missing",
                "输出中没有找到 Gaussian 正常结束标志。");
        }

        if (gaussianOutput.Energy.HasValue)
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.energy_found",
                "已提取最终 SCF 能量。");
        }
        else
        {
            if (result.FailureKind == CalculationFailureKind.None)
            {
                result.FailureKind = CalculationFailureKind.EnergyMissing;
            }

            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.energy_not_found",
                "输出中没有找到可解析的 SCF Done 能量。");
        }

        return result;
    }

    /// <summary>把 Gaussian 优化输出翻译为通用优化结果。</summary>
    public OptimizationResult TranslateOptimization(
        GaussianOutput gaussianOutput)
    {
        if (gaussianOutput == null)
        {
            throw new ArgumentNullException(nameof(gaussianOutput));
        }

        OptimizationResult result = new OptimizationResult();
        result.Program = Gaussian16ProgramAdapter.ProgramNameValue;
        result.Method = gaussianOutput.EnergyMethod;
        result.OutputFilePath = gaussianOutput.OutputFilePath;
        result.NormalTermination = gaussianOutput.NormalTermination;
        result.FinalEnergy = gaussianOutput.Energy;
        result.EnergyUnit = "Hartree";
        result.StepCount =
            gaussianOutput.LastStepNumber.GetValueOrDefault();

        if (result.StepCount == 0)
        {
            result.StepCount =
                gaussianOutput.OptimizationSteps.Count;
        }

        result.FinalGeometry =
            ResolveFinalGeometry(gaussianOutput);
        result.FinalConvergence =
            TranslateConvergence(
                gaussianOutput.FinalConvergence);
        result.Trajectory =
            TranslateTrajectory(gaussianOutput);

        if (gaussianOutput.OptimizationCompleted)
        {
            result.OptimizationConverged = true;
        }
        else if (gaussianOutput.NormalTermination
            && result.StepCount > 0)
        {
            result.OptimizationConverged = false;
        }

        result.FailureKind = ResolveFailureKind(
            gaussianOutput,
            result);
        AddDiagnostics(
            result,
            gaussianOutput);
        return result;
    }

    private static List<OptimizationStep> TranslateTrajectory(
        GaussianOutput gaussianOutput)
    {
        List<OptimizationStep> trajectory =
            new List<OptimizationStep>();

        for (int index = 0;
            index < gaussianOutput.OptimizationSteps.Count;
            index++)
        {
            GaussianOptimizationStep sourceStep =
                gaussianOutput.OptimizationSteps[index];
            OptimizationStep step = new OptimizationStep();
            step.StepIndex = sourceStep.StepNumber;
            step.Geometry = sourceStep.Geometry;
            step.Energy = sourceStep.Energy;
            step.ScfConverged = sourceStep.ScfConverged;
            step.ScfIterations = sourceStep.ScfIterations;
            step.Convergence =
                TranslateConvergence(sourceStep.Convergence);
            trajectory.Add(step);
        }

        return trajectory;
    }

    private static OptimizationConvergence?
        TranslateConvergence(
            GaussianOptimizationConvergence? source)
    {
        if (source == null)
        {
            return null;
        }

        OptimizationConvergence convergence =
            new OptimizationConvergence();
        convergence.Converged = source.Converged;
        convergence.Criteria = source.Criteria;
        convergence.MaximumForce = source.MaximumForce;
        convergence.MaximumForceConverged =
            source.MaximumForceConverged;
        convergence.MaximumForceThreshold =
            source.MaximumForceThreshold;
        convergence.RmsForce = source.RmsForce;
        convergence.RmsForceConverged =
            source.RmsForceConverged;
        convergence.RmsForceThreshold =
            source.RmsForceThreshold;
        convergence.MaximumDisplacement =
            source.MaximumDisplacement;
        convergence.MaximumDisplacementConverged =
            source.MaximumDisplacementConverged;
        convergence.MaximumDisplacementThreshold =
            source.MaximumDisplacementThreshold;
        convergence.RmsDisplacement =
            source.RmsDisplacement;
        convergence.RmsDisplacementConverged =
            source.RmsDisplacementConverged;
        convergence.RmsDisplacementThreshold =
            source.RmsDisplacementThreshold;
        return convergence;
    }

    private static CanonicalGeometry ResolveFinalGeometry(
        GaussianOutput gaussianOutput)
    {
        if (gaussianOutput.FinalGeometry != null
            && gaussianOutput.FinalGeometry.Atoms.Count > 0)
        {
            return gaussianOutput.FinalGeometry;
        }

        if (gaussianOutput.OptimizationSteps.Count > 0)
        {
            return gaussianOutput.OptimizationSteps[
                gaussianOutput.OptimizationSteps.Count - 1]
                .Geometry;
        }

        return new CanonicalGeometry();
    }

    private static OptimizationFailureKind ResolveFailureKind(
        GaussianOutput gaussianOutput,
        OptimizationResult result)
    {
        if (gaussianOutput.ErrorTermination)
        {
            return OptimizationFailureKind.ProgramError;
        }

        if (gaussianOutput.ScfConverged == false)
        {
            return OptimizationFailureKind.ScfNotConverged;
        }

        if (!gaussianOutput.NormalTermination)
        {
            return OptimizationFailureKind
                .NormalTerminationMissing;
        }

        if (!result.FinalEnergy.HasValue)
        {
            return OptimizationFailureKind.EnergyMissing;
        }

        if (gaussianOutput.OptimizationCompleted)
        {
            return OptimizationFailureKind.None;
        }

        if (gaussianOutput.MaximumOptimizationCycles.HasValue
            && gaussianOutput.LastStepNumber.HasValue
            && gaussianOutput.LastStepNumber.Value
                >= gaussianOutput.MaximumOptimizationCycles.Value)
        {
            return OptimizationFailureKind.MaxCyclesExceeded;
        }

        return OptimizationFailureKind
            .OptimizationNotConverged;
    }

    private static void AddDiagnostics(
        OptimizationResult result,
        GaussianOutput gaussianOutput)
    {
        if (gaussianOutput.NormalTermination)
        {
            AddOptimizationDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.normal_termination",
                "Gaussian 正常结束。");
        }

        if (gaussianOutput.OptimizationCompleted)
        {
            AddOptimizationDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.optimization_completed",
                "Gaussian 报告几何优化完成。");
        }
        else
        {
            AddOptimizationDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.optimization_not_completed",
                "Gaussian 输出中没有找到几何优化完成标志。");
        }

        if (result.FinalEnergy.HasValue)
        {
            AddOptimizationDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.energy_found",
                "已提取优化最终能量。");
        }
        else
        {
            AddOptimizationDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.energy_not_found",
                "输出中没有找到可解析的优化能量。");
        }

        for (int index = 0;
            index < gaussianOutput.ErrorMessages.Count;
            index++)
        {
            AddOptimizationDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.error_message",
                gaussianOutput.ErrorMessages[index]);
        }
    }

    private static void AddOptimizationDiagnostic(
        OptimizationResult result,
        CalculationDiagnosticSeverity severity,
        string code,
        string message)
    {
        CalculationDiagnostic diagnostic =
            new CalculationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }

    private static void AddDiagnostic(
        CalculationResult result,
        CalculationDiagnosticSeverity severity,
        string code,
        string message)
    {
        CalculationDiagnostic diagnostic = new CalculationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }
}
