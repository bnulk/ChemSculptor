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

    /// <summary>验证参数审计可以通过文件仓储保存和读取。</summary>
    [Fact]
    public async Task ParameterAuditRoundTripsThroughRepository()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;
            WorkspaceManager workspace =
                new WorkspaceManager(options);
            string jobId = "job-parameter-audit";
            await workspace.EnsureJobWorkspaceAsync(jobId);
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);

            CalculationParameterAudit audit =
                new CalculationParameterAudit();
            audit.JobId = jobId;
            audit.Engine = CalculationDefaults.DefaultProgram;
            audit.TaskType = CalculationTaskType.SinglePoint;
            audit.InputHash = "ABC123";
            audit.TranslatorName = "test-translator";
            audit.TranslatorVersion = "1.0.0";
            audit.ResolvedParameters.Add(
                new ResolvedCalculationParameter
                {
                    Key = CalculationOptionKeys.DispersionCorrection,
                    Value = "GD3BJ",
                    RawText = "加色散",
                    Source = ParameterSource.User,
                    Confidence = 1.0
                });
            audit.ProgramKeywords.Add(
                new ProgramParameterTranslation
                {
                    Key = CalculationOptionKeys.DispersionCorrection,
                    Value = "GD3BJ",
                    RawKeyword = "em=GD3BJ",
                    TranslatorName = "test-translator",
                    TranslatorVersion = "1.0.0"
                });

            await repository.SaveParameterAuditAsync(audit);
            CalculationParameterAudit? restored =
                await repository.GetParameterAuditAsync(jobId);

            Assert.NotNull(restored);
            Assert.Equal(jobId, restored.JobId);
            Assert.Equal("ABC123", restored.InputHash);
            Assert.Single(restored.ResolvedParameters);
            Assert.Single(restored.ProgramKeywords);
            Assert.Equal(
                "em=GD3BJ",
                restored.ProgramKeywords[0].RawKeyword);
            Assert.True(
                File.Exists(
                    workspace.GetJobParametersPath(jobId)));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
