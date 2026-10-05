using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Planning;

/// <summary>根据波函数稳定性矩阵生成本征值修正方案。</summary>
public sealed class WavefunctionStabilityCorrectionPlanner
{
    /// <summary>尝试生成通用修正方案。</summary>
    public bool TryCreatePlan(
        WavefunctionStabilityResult stabilityResult,
        out CorrectionOption? option,
        out string message)
    {
        option = null;
        message = string.Empty;

        if (stabilityResult == null)
        {
            message = "波函数稳定性结果不能为空。";
            return false;
        }

        WavefunctionStabilityEigenvector? selectedEigenvector =
            FindLowestEnergyEigenvector(stabilityResult);

        if (selectedEigenvector == null)
        {
            message = "稳定性结果中没有可用的本征向量。";
            return false;
        }

        if (selectedEigenvector.Eigenvalue >= 0.0)
        {
            message = "没有发现降低能量的稳定矩阵本征值。";
            return false;
        }

        if (selectedEigenvector.Multiplicity <= 0)
        {
            message = "无法从本征向量电子态名称确定自旋多重度。";
            return false;
        }

        if (selectedEigenvector.Multiplicity
            == stabilityResult.CurrentMultiplicity)
        {
            option = CreateWavefunctionOptimizationOption(
                selectedEigenvector,
                stabilityResult.CurrentMultiplicity);
        }
        else
        {
            if (stabilityResult.ElectronicStateObjective
                == ElectronicStateObjective.TargetSpinState)
            {
                message =
                    "当前任务指定了特定自旋态，不能自动修改自旋多重度。";
                return false;
            }

            if (stabilityResult.ElectronicStateObjective
                == ElectronicStateObjective.TargetExcitedState)
            {
                message =
                    "当前任务指定了激发态目标，不能自动修改自旋多重度。";
                return false;
            }

            option = CreateSpinMultiplicityChangeOption(
                selectedEigenvector,
                stabilityResult.CurrentMultiplicity,
                requiresApproval: false);
        }

        message = option.Description;
        return true;
    }

    private static WavefunctionStabilityEigenvector?
        FindLowestEnergyEigenvector(
            WavefunctionStabilityResult stabilityResult)
    {
        if (stabilityResult.Eigenvectors.Count == 0)
        {
            return null;
        }

        WavefunctionStabilityEigenvector selected =
            stabilityResult.Eigenvectors[0];

        for (int index = 1; index < stabilityResult.Eigenvectors.Count; index++)
        {
            WavefunctionStabilityEigenvector candidate =
                stabilityResult.Eigenvectors[index];

            if (candidate.Eigenvalue < selected.Eigenvalue)
            {
                selected = candidate;
            }
        }

        return selected;
    }

    private static CorrectionOption CreateWavefunctionOptimizationOption(
        WavefunctionStabilityEigenvector eigenvector,
        int currentMultiplicity)
    {
        CorrectionOption option = new CorrectionOption();
        option.Id = "wavefunction-optimization";
        option.IntentCode = CorrectionIntentCodes.WavefunctionOptimization;
        option.Title = "波函数优化";
        option.Description =
            "稳定性矩阵最低本征值对应的电子态与当前自旋多重度相同，" +
            "应执行同一自旋态下的波函数优化。";
        option.RiskLevel = CorrectionRiskLevel.Medium;
        option.RequiresApproval = false;
        option.CanUseForRestart = true;

        CorrectionChange change = new CorrectionChange();
        change.Name = "stability-correction";
        change.OldValue = "unstable-wavefunction";
        change.NewValue = "wavefunction-optimization";
        change.Reason =
            "最低本征值 " +
            eigenvector.Eigenvalue.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
            "，多重度 " +
            currentMultiplicity.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
            "。";
        option.Changes.Add(change);
        return option;
    }

    private static CorrectionOption CreateSpinMultiplicityChangeOption(
        WavefunctionStabilityEigenvector eigenvector,
        int currentMultiplicity,
        bool requiresApproval)
    {
        CorrectionOption option = new CorrectionOption();
        option.Id = "change-spin-multiplicity";
        option.IntentCode = CorrectionIntentCodes.ChangeSpinMultiplicity;
        option.Title = "修改自旋多重度";
        option.Description =
            "稳定性矩阵最低本征值对应的电子态与当前自旋多重度不同，" +
            "应修改体系自旋多重度。";
        option.RiskLevel = CorrectionRiskLevel.High;
        option.RequiresApproval = requiresApproval;
        option.CanUseForRestart = true;

        CorrectionChange change = new CorrectionChange();
        change.Name = "multiplicity";
        change.OldValue = currentMultiplicity.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        change.NewValue = eigenvector.Multiplicity.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        change.Reason =
            "最低本征值 " +
            eigenvector.Eigenvalue.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
            "，对应电子态 " +
            eigenvector.StateName +
            "。";
        option.Changes.Add(change);
        return option;
    }
}
