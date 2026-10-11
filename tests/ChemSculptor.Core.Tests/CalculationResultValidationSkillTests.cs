using ChemSculptor.Compute;
using ChemSculptor.Skills.Common.CalculationResultValidation;

namespace ChemSculptor.Core.Tests;

/// <summary>单点计算结果验证技能测试。</summary>
public class CalculationResultValidationSkillTests
{
    /// <summary>验证完整且一致的单点结果可以通过。</summary>
    [Fact]
    public async Task ValidSinglePointResultPasses()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            await File.WriteAllTextAsync(outputPath, "Normal termination");

            CalculationResultValidationRequest request =
                CreateValidRequest(outputPath);

            CalculationResultValidationSkillResult result =
                await ExecuteValidationAsync(CreateSkill(), request);

            Assert.True(result.Passed);
            Assert.Equal(
                CalculationValidationStatus.Passed,
                result.Report.Status);
            Assert.Equal("single-point-result-validator", result.Report.ValidatorName);
            Assert.Empty(result.Report.Issues);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证不一致和缺失信息会使结果失败。</summary>
    [Fact]
    public async Task InvalidSinglePointResultFails()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationResultValidationRequest request =
                CreateValidRequest(Path.Combine(root, "missing.log"));
            request.Result.Energy = null;
            request.Result.Multiplicity = 3;
            request.Result.FailureKind = CalculationFailureKind.EnergyMissing;

            CalculationResultValidationSkillResult result =
                await ExecuteValidationAsync(CreateSkill(), request);

            Assert.False(result.Passed);
            Assert.Equal(
                CalculationValidationStatus.Failed,
                result.Report.Status);

            bool energyIssueFound = false;
            bool multiplicityIssueFound = false;
            bool outputIssueFound = false;

            for (int index = 0; index < result.Report.Issues.Count; index++)
            {
                CalculationValidationIssue issue = result.Report.Issues[index];

                if (issue.Code == "calculation.energy_present")
                {
                    energyIssueFound = true;
                }

                if (issue.Code == "calculation.multiplicity")
                {
                    multiplicityIssueFound = true;
                }

                if (issue.Code == "calculation.output_file")
                {
                    outputIssueFound = true;
                }
            }

            Assert.True(energyIssueFound);
            Assert.True(multiplicityIssueFound);
            Assert.True(outputIssueFound);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 SCF 未收敛时单点结果不能通过。</summary>
    [Fact]
    public async Task ScfNotConvergedFailsValidation()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                "Normal termination");
            CalculationResultValidationRequest request =
                CreateValidRequest(outputPath);
            request.Result.ScfConverged = false;

            CalculationResultValidationSkillResult result =
                await ExecuteValidationAsync(
                    CreateSkill(),
                    request);

            Assert.False(result.Passed);

            bool issueFound = false;

            for (int index = 0;
                index < result.Report.Issues.Count;
                index++)
            {
                if (result.Report.Issues[index].Code
                    == "calculation.scf_converged")
                {
                    issueFound = true;
                }
            }

            Assert.True(issueFound);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证建议级科学检查失败时只产生警告。</summary>
    [Fact]
    public async Task RecommendedScientificFailureProducesWarning()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            await File.WriteAllTextAsync(outputPath, "Normal termination");

            List<ICalculationResultValidator> validators =
                new List<ICalculationResultValidator>();
            validators.Add(new SinglePointCalculationResultValidator());
            validators.Add(
                new SyntheticScientificValidator(
                    CalculationValidationRequirement.Recommended));

            CalculationValidationService validationService =
                new CalculationValidationService(validators);
            CalculationResultValidationSkill skill =
                new CalculationResultValidationSkill(validationService);
            CalculationResultValidationSkillResult result =
                await ExecuteValidationAsync(
                    skill,
                    CreateValidRequest(outputPath));

            Assert.True(result.Passed);
            Assert.Equal(
                CalculationValidationStatus.PassedWithWarnings,
                result.Report.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证必需级科学检查失败时整体失败。</summary>
    [Fact]
    public async Task RequiredScientificFailureRejectsResult()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            await File.WriteAllTextAsync(outputPath, "Normal termination");

            List<ICalculationResultValidator> validators =
                new List<ICalculationResultValidator>();
            validators.Add(new SinglePointCalculationResultValidator());
            validators.Add(
                new SyntheticScientificValidator(
                    CalculationValidationRequirement.Required));

            CalculationValidationService validationService =
                new CalculationValidationService(validators);
            CalculationResultValidationSkill skill =
                new CalculationResultValidationSkill(validationService);
            CalculationResultValidationSkillResult result =
                await ExecuteValidationAsync(
                    skill,
                    CreateValidRequest(outputPath));

            Assert.False(result.Passed);
            Assert.Equal(
                CalculationValidationStatus.Failed,
                result.Report.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static CalculationResultValidationSkill CreateSkill()
    {
        List<ICalculationResultValidator> validators =
            new List<ICalculationResultValidator>();
        validators.Add(new SinglePointCalculationResultValidator());
        CalculationValidationService validationService =
            new CalculationValidationService(validators);
        return new CalculationResultValidationSkill(validationService);
    }

    private static CalculationResultValidationRequest CreateValidRequest(
        string outputPath)
    {
        CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();

        CalculationJob job = new CalculationJob();
        job.JobId = "job-validation";
        job.Spec = spec;

        CalculationResult result = new CalculationResult();
        result.JobId = job.JobId;
        result.Program = spec.Program;
        result.Method = spec.Method;
        result.Basis = spec.Basis;
        result.Charge = spec.Charge;
        result.Multiplicity = spec.Multiplicity;
        result.NormalTermination = true;
        result.FailureKind = CalculationFailureKind.None;
        result.ScfConverged = true;
        result.ScfIterations = 8;
        result.Energy = -76.3801014;
        result.EnergyUnit = "Hartree";
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

    private static async Task<CalculationResultValidationSkillResult>
        ExecuteValidationAsync(
            CalculationResultValidationSkill skill,
            CalculationResultValidationRequest request)
    {
        System.Text.Json.JsonSerializerOptions jsonOptions =
            new System.Text.Json.JsonSerializerOptions(
                System.Text.Json.JsonSerializerDefaults.Web);
        string json = System.Text.Json.JsonSerializer.Serialize(
            request,
            jsonOptions);
        ChemSculptor.Domain.TaskRequest taskRequest =
            new ChemSculptor.Domain.TaskRequest();
        taskRequest.SkillId = skill.Name;
        taskRequest.Inputs["request"] = json;
        ChemSculptor.Domain.TaskResult taskResult =
            await skill.ExecuteAsync(taskRequest);

        if (string.IsNullOrWhiteSpace(taskResult.Output))
        {
            throw new InvalidOperationException("验证技能没有返回结果。");
        }

        CalculationResultValidationSkillResult? result =
            System.Text.Json.JsonSerializer.Deserialize<
                CalculationResultValidationSkillResult>(
                    taskResult.Output,
                    jsonOptions);

        if (result == null)
        {
            throw new InvalidOperationException("无法反序列化验证技能结果。");
        }

        return result;
    }

    private sealed class SyntheticScientificValidator : ICalculationResultValidator
    {
        private readonly CalculationValidationRequirement _requirement;

        public SyntheticScientificValidator(
            CalculationValidationRequirement requirement)
        {
            _requirement = requirement;
        }

        public string Name
        {
            get { return "synthetic-scientific-validator"; }
        }

        public bool CanValidate(CalculationResultValidationRequest request)
        {
            return true;
        }

        public CalculationValidationReport Validate(
            CalculationResultValidationRequest request)
        {
            CalculationValidationReport report =
                new CalculationValidationReport();
            report.ValidatorName = Name;

            CalculationValidationCheck check =
                new CalculationValidationCheck();
            check.Code = "scientific.synthetic-check";
            check.Description = "合成的科学合理性检查";
            check.Passed = false;
            check.Requirement = _requirement;
            check.Scope = CalculationValidationScope.ScientificPlausibility;

            CalculationValidationIssue issue = new CalculationValidationIssue();

            if (_requirement == CalculationValidationRequirement.Required)
            {
                check.Severity = CalculationDiagnosticSeverity.Error;
                issue.Severity = CalculationDiagnosticSeverity.Error;
                report.Passed = false;
                report.Status = CalculationValidationStatus.Failed;
            }
            else
            {
                check.Severity = CalculationDiagnosticSeverity.Warning;
                issue.Severity = CalculationDiagnosticSeverity.Warning;
                report.Passed = true;
                report.Status = CalculationValidationStatus.PassedWithWarnings;
            }

            check.Message = "合成的未通过检查。";
            issue.Code = check.Code;
            issue.Message = check.Message;
            report.Checks.Add(check);
            report.Issues.Add(issue);
            return report;
        }
    }
}
