using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>旧科学数据文件引用回填测试。</summary>
public class ScientificArtifactBackfillServiceTests
{
    /// <summary>
    /// 验证旧点可以从 CalculationResult 回填，并可重复执行。
    /// </summary>
    [Fact]
    public async Task BackfillsOldPointIdempotently()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = Path.Combine(
                root,
                "workspace");
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileCalculationRepository calculationRepository =
                new FileCalculationRepository(workspace);
            ScientificDataRepositoryOptions scientificOptions =
                new ScientificDataRepositoryOptions();
            scientificOptions.RootDirectory = Path.Combine(
                root,
                "scientific-data");
            FileScientificDataRepository scientificRepository =
                new FileScientificDataRepository(
                    scientificOptions);

            await SaveOldCalculationAsync(
                workspace,
                calculationRepository);

            ScientificResult result =
                CreateOldScientificResult();
            await scientificRepository.SaveAsync(result);

            ScientificArtifactBackfillService service =
                new ScientificArtifactBackfillService(
                    scientificRepository,
                    calculationRepository);
            ScientificArtifactBackfillResult first =
                await service.BackfillAsync(result.Id);

            Assert.True(first.Succeeded);
            Assert.Equal(1, first.PointsScanned);
            Assert.Equal(1, first.PointsBackfilled);
            Assert.Equal(0, first.PointsSkipped);

            ScientificResult? reloaded =
                await scientificRepository.GetAsync(result.Id);
            Assert.NotNull(reloaded);
            CalculationPoint point =
                reloaded!.PointSet.Points[0];
            Assert.Equal(3, point.Artifacts.Count);
            Assert.Contains(
                point.Artifacts,
                artifact => artifact.DownloadFileName
                    == "O2-original-m1.gjf");
            Assert.Contains(
                point.Artifacts,
                artifact => artifact.DownloadFileName
                    == "O2-original-m1.log");
            Assert.Contains(
                point.Artifacts,
                artifact => artifact.DownloadFileName
                    == "O2-original-m1.fchk");

            ScientificArtifactBackfillResult second =
                await service.BackfillAsync(result.Id);

            Assert.True(second.Succeeded);
            Assert.Equal(0, second.PointsBackfilled);
            Assert.Equal(1, second.PointsSkipped);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static async Task SaveOldCalculationAsync(
        WorkspaceManager workspace,
        FileCalculationRepository repository)
    {
        string jobId = "job-original";
        await workspace.EnsureJobWorkspaceAsync(jobId);

        CalculationJob job = new CalculationJob();
        job.JobId = jobId;
        job.Goal = "氧气单点计算";
        job.Spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        job.Spec.Multiplicity = 1;
        job.RunDirectory =
            workspace.GetRunDirectory(jobId);
        await repository.SaveJobAsync(job);

        CalculationResult result = new CalculationResult();
        result.JobId = jobId;
        result.NormalTermination = true;
        result.Multiplicity = 1;
        result.Artifacts.Add(
            CreateArtifact(
                "job-original.gjf",
                CalculationArtifactKind.Input,
                false));
        result.Artifacts.Add(
            CreateArtifact(
                "output.log",
                CalculationArtifactKind.PrimaryOutput,
                false));
        result.Artifacts.Add(
            CreateArtifact(
                "job-original.fchk",
                CalculationArtifactKind.RestartState,
                true));
        await repository.SaveResultAsync(result);
    }

    private static CalculationArtifactDescriptor CreateArtifact(
        string relativePath,
        CalculationArtifactKind kind,
        bool canUseForRestart)
    {
        CalculationArtifactDescriptor artifact =
            new CalculationArtifactDescriptor();
        artifact.FileName = Path.GetFileName(relativePath);
        artifact.RelativePath = relativePath;
        artifact.Kind = kind;
        artifact.MediaType = "application/octet-stream";
        artifact.Length = 100;
        artifact.Sha256 = "test-sha256";
        artifact.CanUseForRestart = canUseForRestart;
        return artifact;
    }

    private static ScientificResult CreateOldScientificResult()
    {
        ScientificResult result = new ScientificResult();
        result.Id = "old-result";
        result.Title = "旧氧气结果";
        result.Status = ScientificResultStatus.Complete;
        result.Metadata["rootWorkflowId"] = "job-original";

        CalculationPoint point = new CalculationPoint();
        point.Id = "point-original";
        point.Name = "原始计算点";
        point.Status = CalculationPointStatus.Accepted;
        point.CalculationJobId = "job-original";
        point.Metadata["formula"] = "O2";
        point.ElectronicState.Multiplicity = 1;
        result.PointSet.Points.Add(point);
        return result;
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
