using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian.Anomaly.Recovery;
using ChemSculptor.Skills.Gaussian.Anomaly.Recovery;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 派生作业创建测试。</summary>
public class GaussianRecoveryJobProviderTests
{
    /// <summary>验证修改自旋多重度会创建派生作业和输入。</summary>
    [Fact]
    public async Task CreatesDerivedJobForSpinMultiplicityChange()
    {
        string root = CreateTemporaryRoot();
        string sourcePath = Path.Combine(root, "original.gjf");

        try
        {
            string sourceText =
                "%chk=original.chk\n" +
                "%mem=4GB\n" +
                "%nprocshared=4\n" +
                "\n" +
                "#p CAM-B3LYP/6-31G* SP scfcyc=200\n" +
                "\n" +
                "oxygen single point\n" +
                "\n" +
                "0 1\n" +
                "O 0.000000 0.000000 0.000000\n" +
                "O 0.000000 0.000000 1.207000\n";
            await File.WriteAllTextAsync(sourcePath, sourceText);

            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = Path.Combine(root, "workspace");
            WorkspaceManager workspace = new WorkspaceManager(options);
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);

            CalculationJob sourceJob = new CalculationJob();
            sourceJob.JobId = "job-original";
            sourceJob.InputFilePath = sourcePath;
            sourceJob.SourceInputFilePath = sourcePath;
            sourceJob.Spec =
                CalculationDefaults.CreateDefaultSinglePoint();
            sourceJob.Spec.Multiplicity = 1;
            await repository.SaveJobAsync(sourceJob);

            CorrectionPlan plan = new CorrectionPlan();
            plan.Id = "plan-1";
            plan.SourceJobId = sourceJob.JobId;
            plan.Option = new CorrectionOption();
            plan.Option.Id = "change-spin";
            plan.Option.IntentCode =
                CorrectionIntentCodes.ChangeSpinMultiplicity;
            plan.Option.CanUseForRestart = true;

            CorrectionChange change = new CorrectionChange();
            change.Name = "multiplicity";
            change.OldValue = "1";
            change.NewValue = "3";
            plan.Option.Changes.Add(change);

            GaussianRecoveryJobProvider provider =
                new GaussianRecoveryJobProvider(
                    workspace,
                    repository,
                    new TestQuantumProgramAdapterRegistry(),
                    new GaussianRecoveryInputWriter());
            RecoveryJobPreparationRequest request =
                new RecoveryJobPreparationRequest();
            request.SourceJob = sourceJob;
            request.Plan = plan;

            RecoveryJobPreparationResult result =
                await provider.PrepareAsync(request);

            Assert.True(result.Succeeded);
            Assert.NotNull(result.RecoveryJob);
            Assert.NotNull(result.Attempt);
            Assert.Equal(
                sourceJob.JobId,
                result.RecoveryJob.ParentJobId);
            Assert.Equal(1, result.RecoveryJob.AttemptNumber);
            Assert.Equal(
                CalculationJobState.InputGenerated,
                result.RecoveryJob.State);
            Assert.Equal(3, result.RecoveryJob.Spec.Multiplicity);
            Assert.Equal(
                ElectronicStateObjective.TargetSpinState,
                result.RecoveryJob.Spec.ElectronicStateObjective);

            string recoveryInput = await File.ReadAllTextAsync(
                result.RecoveryJob.SourceInputFilePath);

            Assert.Contains("0 3", recoveryInput);
            Assert.Contains("scfcyc=200", recoveryInput);
            Assert.Contains(
                "%chk=" + result.RecoveryJob.JobId + ".chk",
                recoveryInput);

            CalculationJob? savedJob =
                await repository.GetJobAsync(
                    result.RecoveryJob.JobId);

            Assert.NotNull(savedJob);
            Assert.Equal(
                sourceJob.JobId,
                savedJob.ParentJobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
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
