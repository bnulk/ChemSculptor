using ChemSculptor.Compute;

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
        result.EnergyUnit = "Hartree";
        result.Method = gaussianOutput.EnergyMethod;
        result.FailureKind = CalculationFailureKind.None;

        if (gaussianOutput.NormalTermination)
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
