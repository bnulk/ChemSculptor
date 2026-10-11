using System.Text.Json;
using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>规范参数模型测试。</summary>
public class CalculationParameterModelsTests
{
    /// <summary>验证解析参数模型可以通过 JSON 往返。</summary>
    [Fact]
    public void ResolvedParameterRoundTripsThroughJson()
    {
        ResolvedCalculationParameter parameter =
            new ResolvedCalculationParameter();
        parameter.Key = CalculationOptionKeys.DispersionCorrection;
        parameter.Value = "GD3BJ";
        parameter.RawText = "加色散";
        parameter.Source = ParameterSource.User;
        parameter.Confidence = 1.0;
        parameter.RiskLevel = CalculationRiskLevel.Info;
        parameter.RequiresApproval = false;
        parameter.TranslatedKeyword = "em=GD3BJ";

        JsonSerializerOptions options =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string json = JsonSerializer.Serialize(parameter, options);
        ResolvedCalculationParameter? restored =
            JsonSerializer.Deserialize<ResolvedCalculationParameter>(
                json,
                options);

        Assert.NotNull(restored);
        Assert.Equal(parameter.Key, restored.Key);
        Assert.Equal(parameter.Value, restored.Value);
        Assert.Equal(parameter.RawText, restored.RawText);
        Assert.Equal(parameter.Source, restored.Source);
        Assert.Equal(parameter.Confidence, restored.Confidence);
        Assert.Equal(parameter.RiskLevel, restored.RiskLevel);
        Assert.Equal(
            parameter.RequiresApproval,
            restored.RequiresApproval);
        Assert.Equal(
            parameter.TranslatedKeyword,
            restored.TranslatedKeyword);
    }
}
