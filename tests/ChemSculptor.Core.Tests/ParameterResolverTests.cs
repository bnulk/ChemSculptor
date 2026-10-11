using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>第十二步要求的参数 Resolver 测试。</summary>
public class ParameterResolverTests
{
    /// <summary>验证用户显式值覆盖默认值。</summary>
    [Fact]
    public void AppliesUserOverrideOverDefault()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.Solvent,
            "Water",
            "水溶剂"));

        CalculationParameterResolutionResult result =
            Resolve(spec, intents, 16);

        Assert.Equal(
            CalculationParameterResolutionStatus.Resolved,
            result.Status);
        Assert.Equal("Water", result.Spec.Solvent);
        Assert.Contains(
            result.AppliedParameters,
            parameter =>
                parameter.Key == CalculationOptionKeys.Solvent
                && parameter.Value == "Water"
                && parameter.Source == ParameterSource.User);
    }

    /// <summary>验证不支持的参数会拒绝整个解析。</summary>
    [Fact]
    public void RejectsUnsupportedParameter()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        CalculationParameterIntent intent =
            new CalculationParameterIntent();
        intent.Key = "program-extra-keywords";
        intent.Value = "unknown=value";
        intent.RawText = "unknown=value";
        intent.State =
            CalculationParameterIntentState.Unsupported;
        intent.Source = ParameterSource.User;
        intent.Confidence = 0.5;

        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(intent);

        CalculationParameterResolutionResult result =
            Resolve(spec, intents, 16);

        Assert.Equal(
            CalculationParameterResolutionStatus.Rejected,
            result.Status);
        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code == "parameter.unsupported");
    }

    /// <summary>验证首期显式电荷不触发审批。</summary>
    [Fact]
    public void DoesNotRequireApprovalForCharge()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.Charge,
            "-1",
            "电荷 -1"));

        CalculationParameterResolutionResult result =
            Resolve(spec, intents, 11);

        Assert.Equal(
            CalculationParameterResolutionStatus.Resolved,
            result.Status);
        Assert.False(result.RequiresApproval);
        Assert.Contains(
            result.AppliedParameters,
            parameter =>
                parameter.Key == CalculationOptionKeys.Charge
                && parameter.Value == "-1"
                && !parameter.RequiresApproval);
    }

    /// <summary>验证参数来源会被保留到最终参数记录。</summary>
    [Fact]
    public void PreservesParameterSource()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.Solvent,
            "Water",
            "水溶剂"));
        intents.Add(CreateKeyOnlyIntent(
            CalculationOptionKeys.DispersionCorrection,
            "加色散"));

        CalculationParameterResolutionResult result =
            Resolve(spec, intents, 16);

        Assert.Contains(
            result.AppliedParameters,
            parameter =>
                parameter.Key == CalculationOptionKeys.Solvent
                && parameter.Source == ParameterSource.User);
        Assert.Contains(
            result.AppliedParameters,
            parameter =>
                parameter.Key
                    == CalculationOptionKeys.DispersionCorrection
                && parameter.Value == "GD3BJ"
                && parameter.Source == ParameterSource.Default
                && parameter.RawText == "加色散");
    }

    private static CalculationParameterResolutionResult Resolve(
        CalculationSpec spec,
        List<CalculationParameterIntent> intents,
        int neutralElectronCount)
    {
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.SinglePoint;
        context.ElectronCount = neutralElectronCount;
        return new CalculationParameterResolver().Resolve(
            spec,
            intents,
            context);
    }

    private static CalculationParameterIntent CreateIntent(
        string key,
        string value,
        string rawText)
    {
        CalculationParameterIntent intent =
            new CalculationParameterIntent();
        intent.Key = key;
        intent.Value = value;
        intent.RawText = rawText;
        intent.State =
            CalculationParameterIntentState.ExplicitValue;
        intent.Source = ParameterSource.User;
        intent.Confidence = 1.0;
        return intent;
    }

    private static CalculationParameterIntent CreateKeyOnlyIntent(
        string key,
        string rawText)
    {
        CalculationParameterIntent intent =
            new CalculationParameterIntent();
        intent.Key = key;
        intent.RawText = rawText;
        intent.State =
            CalculationParameterIntentState.KeyOnly;
        intent.Source = ParameterSource.User;
        intent.Confidence = 0.9;
        return intent;
    }
}
