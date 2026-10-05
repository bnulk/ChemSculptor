using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.InputProcessor;
using ChemSculptor.ScientificData.Extraction;
using ChemSculptor.ScientificData.Extraction.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>科学成果提取测试。</summary>
public class ScientificResultExtractorTests
{
    /// <summary>验证修正后的结果生成接受点和最终物理量。</summary>
    [Fact]
    public async Task ExtractsCorrectedPointAndObservable()
    {
        string root = CreateTemporaryRoot();

        try
        {
            ScientificResultExtractionRequest request =
                CreateRecoveryRequest();
            ScientificResultExtractor extractor =
                new ScientificResultExtractor(
                    new GeometryTextParser());

            ScientificResultExtractionResult result =
                await extractor.ExtractAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.Result);
            Assert.Equal(
                ScientificResultStatus.Complete,
                result.Result.Status);
            Assert.Equal(2, result.Result.PointSet.Points.Count);
            Assert.Single(
                result.Result.PointSet.Relations);
            Assert.Single(result.Result.Observables);

            CalculationPoint original =
                FindPoint(
                    result.Result,
                    "point-job-original");
            CalculationPoint corrected =
                FindPoint(
                    result.Result,
                    "point-job-recovery");

            Assert.Equal(
                CalculationPointStatus.Superseded,
                original.Status);
            Assert.Equal(
                CalculationPointStatus.Accepted,
                corrected.Status);
            Assert.Equal(
                1,
                corrected.Provenance.CorrectionCount);
            Assert.Equal(
                original.Id,
                corrected.Provenance.ParentPointId);
            Assert.Equal(
                3,
                corrected.ElectronicState.Multiplicity);
            Assert.Equal(
                ScientificObservableKind.SinglePointEnergy,
                result.Result.Observables[0].Kind);
            Assert.Equal(
                -150.0,
                result.Result.Observables[0].NumericValue);

            ScientificDataRepositoryOptions options =
                new ScientificDataRepositoryOptions();
            options.RootDirectory = Path.Combine(
                root,
                "scientific-data");
            FileScientificDataRepository repository =
                new FileScientificDataRepository(options);
            ScientificDataRecorder recorder =
                new ScientificDataRecorder(
                    extractor,
                    repository);

            ScientificResultExtractionResult recorded =
                await recorder.RecordAsync(request);
            ScientificResult? saved =
                await repository.GetAsync(
                    recorded.Result!.Id);

            Assert.NotNull(saved);
            Assert.Equal(
                result.Result.Status,
                saved.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static ScientificResultExtractionRequest
        CreateRecoveryRequest()
    {
        CalculationJob originalJob = new CalculationJob();
        originalJob.JobId = "job-original";
        originalJob.Goal = "氧气单点计算";
        originalJob.Spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        originalJob.Spec.Multiplicity = 1;

        CalculationResult originalResult =
            new CalculationResult();
        originalResult.JobId = originalJob.JobId;
        originalResult.NormalTermination = true;
        originalResult.Energy = -149.0;
        originalResult.EnergyUnit = "Hartree";
        originalResult.Multiplicity = 1;

        AnomalyCheckResult originalStability =
            new AnomalyCheckResult();
        originalStability.Code = "wavefunction-stability";
        originalStability.Status = AnomalyCheckStatus.Finding;
        originalStability.Summary = "RHF -> UHF 不稳定。";
        originalStability.IsRequired = true;

        CalculationJob recoveryJob = new CalculationJob();
        recoveryJob.JobId = "job-recovery";
        recoveryJob.ParentJobId = originalJob.JobId;
        recoveryJob.RootWorkflowId = "workflow-o2";
        recoveryJob.Spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        recoveryJob.Spec.Multiplicity = 3;
        recoveryJob.Spec.ElectronicStateObjective =
            ElectronicStateObjective.TargetSpinState;

        CalculationResult recoveryResult =
            new CalculationResult();
        recoveryResult.JobId = recoveryJob.JobId;
        recoveryResult.NormalTermination = true;
        recoveryResult.Energy = -150.0;
        recoveryResult.EnergyUnit = "Hartree";
        recoveryResult.Multiplicity = 3;

        RecoveryJobExecutionResult recoveryExecution =
            new RecoveryJobExecutionResult();
        recoveryExecution.Attempted = true;
        recoveryExecution.Succeeded = true;
        recoveryExecution.RecoveryJob = recoveryJob;
        recoveryExecution.Result = recoveryResult;

        AnomalyCheckResult recoveryStability =
            new AnomalyCheckResult();
        recoveryStability.Code = "wavefunction-stability";
        recoveryStability.Status = AnomalyCheckStatus.Passed;
        recoveryStability.Summary = "波函数稳定。";
        recoveryStability.IsRequired = true;

        CorrectionPlan plan = new CorrectionPlan();
        plan.Id = "plan-1";
        plan.SourceJobId = originalJob.JobId;
        plan.Option = new CorrectionOption();
        plan.Option.Id = "change-spin";
        plan.Option.IntentCode =
            CorrectionIntentCodes.ChangeSpinMultiplicity;
        plan.Option.Title = "修改自旋多重度";
        plan.Option.Description = "单重态修正为三重态。";

        WavefunctionStabilityCorrectionPlanningResult planning =
            new WavefunctionStabilityCorrectionPlanningResult();
        planning.PlanCreated = true;
        planning.Plan = plan;
        planning.AnomalyRecordId = "anomaly-1";

        ScientificResultExtractionRequest request =
            new ScientificResultExtractionRequest();
        request.RootWorkflowId = "workflow-o2";
        request.CoordinateText =
            "O 0.000000 0.000000 0.000000\n" +
            "O 0.000000 0.000000 1.207000";
        request.OriginalJob = originalJob;
        request.OriginalResult = originalResult;
        request.OriginalStabilityCheck = originalStability;
        request.CorrectionPlanning = planning;
        request.RecoveryExecution = recoveryExecution;
        request.RecoveryStabilityCheck = recoveryStability;
        return request;
    }

    private static CalculationPoint FindPoint(
        ScientificResult result,
        string pointId)
    {
        for (int index = 0;
            index < result.PointSet.Points.Count;
            index++)
        {
            CalculationPoint point =
                result.PointSet.Points[index];

            if (string.Equals(
                point.Id,
                pointId,
                StringComparison.OrdinalIgnoreCase))
            {
                return point;
            }
        }

        throw new InvalidOperationException(
            "没有找到计算点：" + pointId);
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
