using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Planning;
using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>波函数稳定性修正规划测试。</summary>
public class WavefunctionStabilityCorrectionPlannerTests
{
    /// <summary>验证不同多重度时生成修改自旋多重度方案。</summary>
    [Fact]
    public void PlansSpinMultiplicityChange()
    {
        WavefunctionStabilityResult stability =
            CreateStabilityResult(currentMultiplicity: 1);
        WavefunctionStabilityCorrectionPlanner planner =
            new WavefunctionStabilityCorrectionPlanner();
        CorrectionOption? option;
        string message;

        bool created = planner.TryCreatePlan(
            stability,
            out option,
            out message);

        Assert.True(created);
        Assert.NotNull(option);
        Assert.Equal(
            CorrectionIntentCodes.ChangeSpinMultiplicity,
            option.IntentCode);
        Assert.Single(option.Changes);
        Assert.Equal("multiplicity", option.Changes[0].Name);
        Assert.Equal("1", option.Changes[0].OldValue);
        Assert.Equal("3", option.Changes[0].NewValue);
        Assert.Equal(CorrectionRiskLevel.High, option.RiskLevel);
        Assert.False(option.RequiresApproval);
        Assert.Contains("修改", message);
    }

    /// <summary>验证相同多重度时生成波函数优化方案。</summary>
    [Fact]
    public void PlansWavefunctionOptimization()
    {
        WavefunctionStabilityResult stability =
            CreateStabilityResult(currentMultiplicity: 3);
        WavefunctionStabilityCorrectionPlanner planner =
            new WavefunctionStabilityCorrectionPlanner();
        CorrectionOption? option;
        string message;

        bool created = planner.TryCreatePlan(
            stability,
            out option,
            out message);

        Assert.True(created);
        Assert.NotNull(option);
        Assert.Equal(
            CorrectionIntentCodes.WavefunctionOptimization,
            option.IntentCode);
        Assert.Equal(CorrectionRiskLevel.Medium, option.RiskLevel);
        Assert.False(option.RequiresApproval);
    }

    /// <summary>验证指定自旋态任务不会自动切换多重度。</summary>
    [Fact]
    public void RejectsAutomaticSpinChangeForTargetSpinState()
    {
        WavefunctionStabilityResult stability =
            CreateStabilityResult(currentMultiplicity: 1);
        stability.ElectronicStateObjective =
            ElectronicStateObjective.TargetSpinState;
        stability.TargetMultiplicity = 1;
        WavefunctionStabilityCorrectionPlanner planner =
            new WavefunctionStabilityCorrectionPlanner();
        CorrectionOption? option;
        string message;

        bool created = planner.TryCreatePlan(
            stability,
            out option,
            out message);

        Assert.False(created);
        Assert.Null(option);
        Assert.Contains("不能自动修改", message);
    }

    /// <summary>验证没有负本征值时不会生成降低能量方案。</summary>
    [Fact]
    public void RejectsPlanWithoutNegativeEigenvalue()
    {
        WavefunctionStabilityResult stability =
            CreateStabilityResult(currentMultiplicity: 1);
        stability.Eigenvectors[0].Eigenvalue = 0.1;
        WavefunctionStabilityCorrectionPlanner planner =
            new WavefunctionStabilityCorrectionPlanner();
        CorrectionOption? option;
        string message;

        bool created = planner.TryCreatePlan(
            stability,
            out option,
            out message);

        Assert.False(created);
        Assert.Null(option);
        Assert.Contains("没有发现", message);
    }

    private static WavefunctionStabilityResult CreateStabilityResult(
        int currentMultiplicity)
    {
        WavefunctionStabilityResult stability =
            new WavefunctionStabilityResult();
        stability.CurrentMultiplicity = currentMultiplicity;
        stability.ElectronicStateObjective =
            ElectronicStateObjective.GroundState;

        WavefunctionStabilityEigenvector triplet =
            new WavefunctionStabilityEigenvector();
        triplet.Index = 1;
        triplet.StateName = "Triplet-?Sym";
        triplet.Multiplicity = 3;
        triplet.Eigenvalue = -0.1026197;
        stability.Eigenvectors.Add(triplet);

        WavefunctionStabilityEigenvector singlet =
            new WavefunctionStabilityEigenvector();
        singlet.Index = 2;
        singlet.StateName = "Singlet-?Sym";
        singlet.Multiplicity = 1;
        singlet.Eigenvalue = 0.0;
        stability.Eigenvectors.Add(singlet);

        return stability;
    }
}
