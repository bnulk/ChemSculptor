using ChemSculptor.Compute;
using ChemSculptor.Skills.Gaussian.GaussianSinglePointResultValidation;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 单点输出文件验证器测试。</summary>
public class GaussianSinglePointOutputValidatorTests
{
    /// <summary>验证最后一个非空行包含正常终结时通过。</summary>
    [Fact]
    public async Task AcceptsNormalTerminationOnLastNonEmptyLine()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                "SCF Done\n" +
                "Normal termination of Gaussian 16\n" +
                "\n");

            CalculationValidationReport report =
                CreateValidator().Validate(CreateRequest(outputPath));

            Assert.True(report.Passed);
            Assert.Equal(CalculationValidationStatus.Passed, report.Status);
            Assert.Single(report.Checks);
            Assert.True(report.Checks[0].Passed);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证正常终结不在最后一行时失败。</summary>
    [Fact]
    public async Task RejectsNormalTerminationThatIsNotLast()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                "Normal termination of Gaussian 16\n" +
                "Unexpected trailing message\n");

            CalculationValidationReport report =
                CreateValidator().Validate(CreateRequest(outputPath));

            Assert.False(report.Passed);
            Assert.Equal(CalculationValidationStatus.Failed, report.Status);
            Assert.Single(report.Issues);
            Assert.Equal(
                "gaussian.output_last_line_normal_termination",
                report.Issues[0].Code);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static GaussianSinglePointOutputValidator CreateValidator()
    {
        return new GaussianSinglePointOutputValidator();
    }

    private static CalculationResultValidationRequest CreateRequest(
        string outputPath)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-gaussian-output-validation";
        job.Spec = CalculationDefaults.CreateDefaultSinglePoint();

        CalculationResult result = new CalculationResult();
        result.JobId = job.JobId;
        result.Program = job.Spec.Program;
        result.OutputFilePath = outputPath;

        CalculationResultValidationRequest request =
            new CalculationResultValidationRequest();
        request.Job = job;
        request.Result = result;
        return request;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
