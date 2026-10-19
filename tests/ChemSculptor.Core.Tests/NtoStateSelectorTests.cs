using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>NTO 激发态选择规则测试。</summary>
public class NtoStateSelectorTests
{
    /// <summary>验证默认模式按 f 降序选择最多五个状态。</summary>
    [Fact]
    public void SelectsTopFiveByOscillatorStrength()
    {
        TddftCalculationResult source =
            CreateSource(
                CreateState(1, 500.0, 0.20),
                CreateState(2, 450.0, 0.80),
                CreateState(3, 400.0, 0.30),
                CreateState(4, 350.0, 0.70),
                CreateState(5, 300.0, 0.10),
                CreateState(6, 250.0, 0.60));

        NtoStateSelectionResult result =
            NtoStateSelector.Select(
                source,
                NtoSelectionMode.Default,
                null,
                null);

        Assert.Equal(
            NtoSelectionOutcome.Selected,
            result.Outcome);
        Assert.Equal(5, result.States.Count);
        Assert.Equal(
            new int[] { 2, 4, 6, 3, 1 },
            result.States
                .Select(state => state.StateIndex)
                .ToArray());
    }

    /// <summary>验证不足五个正 f 状态时选择全部正 f 状态。</summary>
    [Fact]
    public void SelectsAllPositiveStatesWhenFewerThanFive()
    {
        TddftCalculationResult source =
            CreateSource(
                CreateState(1, 500.0, 0.20),
                CreateState(2, 450.0, 0.80),
                CreateState(3, 400.0, 0.00));

        NtoStateSelectionResult result =
            NtoStateSelector.Select(
                source,
                NtoSelectionMode.Default,
                null,
                null);

        Assert.Equal(2, result.States.Count);
        Assert.DoesNotContain(
            result.States,
            state => state.StateIndex == 3);
    }

    /// <summary>验证数量级差异会过滤与最强吸收差距过大的状态。</summary>
    [Fact]
    public void AppliesOscillatorMagnitudeThreshold()
    {
        TddftCalculationResult source =
            CreateSource(
                CreateState(1, 500.0, 1.00),
                CreateState(2, 450.0, 0.05),
                CreateState(3, 400.0, 0.04),
                CreateState(4, 350.0, 0.03),
                CreateState(5, 300.0, 0.02));

        NtoStateSelectionResult result =
            NtoStateSelector.Select(
                source,
                NtoSelectionMode.Default,
                null,
                null);

        Assert.Single(result.States);
        Assert.Equal(1, result.States[0].StateIndex);
    }

    /// <summary>验证没有正 f 状态时返回正常空结果。</summary>
    [Fact]
    public void ReturnsNoPositiveWithoutException()
    {
        TddftCalculationResult source =
            CreateSource(
                CreateState(1, 500.0, 0.00),
                CreateState(2, 450.0, 0.00));

        NtoStateSelectionResult result =
            NtoStateSelector.Select(
                source,
                NtoSelectionMode.Default,
                null,
                null);

        Assert.Equal(
            NtoSelectionOutcome
                .NoPositiveOscillatorStrength,
            result.Outcome);
        Assert.Empty(result.States);
    }

    /// <summary>验证波长范围包含边界且再次应用正 f 规则。</summary>
    [Fact]
    public void SelectsWithinInclusiveWavelengthRange()
    {
        TddftCalculationResult source =
            CreateSource(
                CreateState(1, 300.0, 0.50),
                CreateState(2, 400.0, 0.20),
                CreateState(3, 500.0, 0.10),
                CreateState(4, 600.0, 0.00));

        NtoStateSelectionResult result =
            NtoStateSelector.Select(
                source,
                NtoSelectionMode.WavelengthRange,
                300.0,
                500.0);

        Assert.Equal(3, result.States.Count);
        Assert.DoesNotContain(
            result.States,
            state => state.StateIndex == 4);
    }

    /// <summary>验证全部模式包含 f=0 的暗态。</summary>
    [Fact]
    public void AllModeIncludesDarkStates()
    {
        TddftCalculationResult source =
            CreateSource(
                CreateState(1, 500.0, 0.50),
                CreateState(2, 450.0, 0.00));

        NtoStateSelectionResult result =
            NtoStateSelector.Select(
                source,
                NtoSelectionMode.All,
                null,
                null);

        Assert.Equal(2, result.States.Count);
        Assert.Contains(
            result.States,
            state =>
                state.StateIndex == 2
                && state.OscillatorStrength == 0.0);
    }

    private static TddftCalculationResult CreateSource(
        params TddftExcitedState[] states)
    {
        TddftCalculationResult source =
            new TddftCalculationResult();
        source.NormalTermination = true;
        source.ExcitedStates.AddRange(states);
        return source;
    }

    private static TddftExcitedState CreateState(
        int index,
        double wavelengthNm,
        double oscillatorStrength)
    {
        TddftExcitedState state =
            new TddftExcitedState();
        state.StateIndex = index;
        state.StateLabel = "Singlet-" + index.ToString();
        state.WavelengthNm = wavelengthNm;
        state.OscillatorStrength = oscillatorStrength;
        return state;
    }
}
