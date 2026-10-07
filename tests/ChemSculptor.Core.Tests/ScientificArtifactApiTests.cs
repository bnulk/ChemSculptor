using ChemSculptor.Api;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>科学点成果包 API 协议测试。</summary>
public class ScientificArtifactApiTests
{
    /// <summary>
    /// 验证原始点和恢复点都进入清单，并保持统一文件名。
    /// </summary>
    [Fact]
    public void MapsOriginalAndRecoveryPointsToManifestResponse()
    {
        ScientificArtifactManifest manifest =
            new ScientificArtifactManifest();
        manifest.ResultId = "result-1";
        manifest.RootWorkflowId = "job-original";
        manifest.Succeeded = true;
        manifest.Points.Add(
            CreatePointManifest(
                1,
                "point-original",
                "01-original-m1",
                CalculationPointStatus.Superseded,
                1,
                "O2-original-m1.gjf",
                "O2-original-m1.log"));
        manifest.Points.Add(
            CreatePointManifest(
                2,
                "point-recovery",
                "02-recovery-m3",
                CalculationPointStatus.Accepted,
                3,
                "O2-recovery-m3.gjf",
                "O2-recovery-m3.log"));

        ScientificArtifactManifestResponse response =
            ScientificArtifactResponseMapper.Convert(
                manifest);

        Assert.Equal("result-1", response.ResultId);
        Assert.Equal("job-original", response.RootJobId);
        Assert.Equal(2, response.Points.Count);

        Assert.Equal(
            "01-original-m1",
            response.Points[0].DirectoryName);
        Assert.Equal(
            "Superseded",
            response.Points[0].Status);
        Assert.Equal(1, response.Points[0].Multiplicity);
        Assert.Equal(
            "O2-original-m1.gjf",
            response.Points[0].Files[0].DownloadFileName);
        Assert.Equal(
            "O2-original-m1.log",
            response.Points[0].Files[1].DownloadFileName);

        Assert.Equal(
            "02-recovery-m3",
            response.Points[1].DirectoryName);
        Assert.Equal(
            "Accepted",
            response.Points[1].Status);
        Assert.Equal(3, response.Points[1].Multiplicity);
        Assert.Equal(
            "O2-recovery-m3.gjf",
            response.Points[1].Files[0].DownloadFileName);
        Assert.Equal(
            "/scientific-results/result-1/artifacts/point-recovery/" +
            "artifact-1",
            response.Points[1].Files[0].DownloadPath);
    }

    /// <summary>
    /// 验证恢复作业也能定位到所属科学成果。
    /// </summary>
    [Fact]
    public async Task FindsScientificResultByRecoveryJobId()
    {
        string root = CreateTemporaryRoot();

        try
        {
            ScientificDataRepositoryOptions options =
                new ScientificDataRepositoryOptions();
            options.RootDirectory = Path.Combine(
                root,
                "scientific-data");
            FileScientificDataRepository repository =
                new FileScientificDataRepository(options);

            ScientificResult result = new ScientificResult();
            result.Id = "result-1";
            result.Metadata["rootWorkflowId"] = "job-original";
            result.PointSet.Points.Add(
                CreatePoint(
                    "point-original",
                    "job-original"));
            result.PointSet.Points.Add(
                CreatePoint(
                    "point-recovery",
                    "job-recovery"));
            await repository.SaveAsync(result);

            ScientificResult? byOriginal =
                ScientificResultLookup.FindByJobId(
                    repository,
                    "job-original");
            ScientificResult? byRecovery =
                ScientificResultLookup.FindByJobId(
                    repository,
                    "job-recovery");

            Assert.NotNull(byOriginal);
            Assert.NotNull(byRecovery);
            Assert.Equal(result.Id, byOriginal!.Id);
            Assert.Equal(result.Id, byRecovery!.Id);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static ScientificArtifactPointManifest
        CreatePointManifest(
            int sequence,
            string pointId,
            string directoryName,
            CalculationPointStatus status,
            int multiplicity,
            params string[] downloadFileNames)
    {
        ScientificArtifactPointManifest manifest =
            new ScientificArtifactPointManifest();
        manifest.Sequence = sequence;
        manifest.PointId = pointId;
        manifest.CalculationJobId =
            sequence == 1
                ? "job-original"
                : "job-recovery";
        manifest.DirectoryName = directoryName;
        manifest.Status = status;
        manifest.Multiplicity = multiplicity;

        for (int index = 0;
            index < downloadFileNames.Length;
            index++)
        {
            ScientificArtifactFileManifest file =
                new ScientificArtifactFileManifest();
            file.ArtifactId =
                "artifact-" + (index + 1).ToString();
            file.PointId = pointId;
            file.CalculationJobId =
                manifest.CalculationJobId;
            file.Kind = ScientificArtifactKind.Input;
            file.DownloadFileName =
                downloadFileNames[index];
            file.Length = 100;
            file.ExpectedSha256 = "abc123";
            file.IsAvailable = true;
            manifest.Artifacts.Add(file);
        }

        return manifest;
    }

    private static CalculationPoint CreatePoint(
        string pointId,
        string jobId)
    {
        CalculationPoint point = new CalculationPoint();
        point.Id = pointId;
        point.CalculationJobId = jobId;
        point.Status = CalculationPointStatus.Accepted;
        return point;
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
