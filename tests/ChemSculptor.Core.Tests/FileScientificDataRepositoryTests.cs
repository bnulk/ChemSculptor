using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>科学数据文件仓储测试。</summary>
public class FileScientificDataRepositoryTests
{
    /// <summary>验证科学成果可以保存并读取。</summary>
    [Fact]
    public async Task SavesAndReadsScientificResult()
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
            result.Title = "测试科学成果";
            result.Status = ScientificResultStatus.Complete;

            CalculationPoint point = new CalculationPoint();
            point.Id = "point-1";
            point.Status = CalculationPointStatus.Accepted;
            point.CalculationJobId = "job-1";

            PointArtifactReference artifact =
                new PointArtifactReference();
            artifact.ArtifactId = "artifact-1";
            artifact.CalculationJobId = "job-1";
            artifact.Kind = ScientificArtifactKind.Input;
            artifact.RelativePath = "input.gjf";
            artifact.DownloadFileName = "H2-original-m1.gjf";
            artifact.CanonicalStem = "H2-original-m1";
            artifact.CanonicalExtension = ".gjf";
            artifact.MediaType = "text/plain";
            artifact.Length = 128;
            artifact.Sha256 = "abc123";
            artifact.CanDownload = true;
            artifact.CanUseForRestart = false;
            point.Artifacts.Add(artifact);

            result.PointSet.Points.Add(point);

            ScientificObservable observable =
                new ScientificObservable();
            observable.Id = "observable-1";
            observable.Name = "测试能量";
            observable.NumericValue = -1.0;
            observable.Unit = "Hartree";
            result.Observables.Add(observable);

            await repository.SaveAsync(result);

            ScientificResult? saved =
                await repository.GetAsync(result.Id);
            IReadOnlyList<ScientificResult> listed =
                repository.List();

            Assert.NotNull(saved);
            Assert.Equal(result.Title, saved.Title);
            Assert.Single(saved.PointSet.Points);
            Assert.Equal(
                "job-1",
                saved.PointSet.Points[0].CalculationJobId);
            Assert.Single(saved.PointSet.Points[0].Artifacts);
            Assert.Equal(
                "H2-original-m1",
                saved.PointSet.Points[0]
                    .Artifacts[0]
                    .CanonicalStem);
            Assert.Equal(
                ".gjf",
                saved.PointSet.Points[0]
                    .Artifacts[0]
                    .CanonicalExtension);
            Assert.Single(saved.Observables);
            Assert.Single(listed);
            Assert.True(File.Exists(
                Path.Combine(
                    options.RootDirectory,
                    "result-1.json")));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>
    /// 验证缺少阶段 1 字段的旧科学成果 JSON 仍可读取。
    /// </summary>
    [Fact]
    public async Task ReadsLegacyScientificResultWithoutArtifactFields()
    {
        string root = CreateTemporaryRoot();

        try
        {
            ScientificDataRepositoryOptions options =
                new ScientificDataRepositoryOptions();
            options.RootDirectory = Path.Combine(
                root,
                "scientific-data");
            Directory.CreateDirectory(options.RootDirectory);

            string path = Path.Combine(
                options.RootDirectory,
                "legacy-result.json");
            string json = CreateLegacyJson();
            await File.WriteAllTextAsync(path, json);

            FileScientificDataRepository repository =
                new FileScientificDataRepository(options);
            ScientificResult? saved =
                await repository.GetAsync("legacy-result");

            Assert.NotNull(saved);
            Assert.Single(saved.PointSet.Points);

            CalculationPoint point =
                saved.PointSet.Points[0];
            Assert.Equal(string.Empty, point.CalculationJobId);
            Assert.NotNull(point.ProgramData);
            Assert.Empty(point.Artifacts);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>
    /// 验证阶段 1 模型的完整 JSON 往返。
    /// </summary>
    [Fact]
    public void SerializesAndDeserializesArtifactReference()
    {
        PointArtifactReference artifact =
            new PointArtifactReference();
        artifact.ArtifactId = "artifact-1";
        artifact.CalculationJobId = "job-1";
        artifact.Kind = ScientificArtifactKind.PrimaryOutput;
        artifact.RelativePath = "output.log";
        artifact.DownloadFileName = "H2-original-m1.log";
        artifact.CanonicalStem = "H2-original-m1";
        artifact.CanonicalExtension = ".log";
        artifact.MediaType = "text/plain";
        artifact.Length = 1024;
        artifact.Sha256 = "abc123";
        artifact.CanDownload = true;
        artifact.CanUseForRestart = false;

        JsonSerializerOptions options =
            new JsonSerializerOptions(
                JsonSerializerDefaults.Web);
        options.Converters.Add(
            new JsonStringEnumConverter());

        string json = JsonSerializer.Serialize(
            artifact,
            options);
        PointArtifactReference? restored =
            JsonSerializer.Deserialize<PointArtifactReference>(
                json,
                options);

        Assert.NotNull(restored);
        Assert.Equal(
            artifact.ArtifactId,
            restored.ArtifactId);
        Assert.Equal(
            artifact.CalculationJobId,
            restored.CalculationJobId);
        Assert.Equal(artifact.Kind, restored.Kind);
        Assert.Equal(
            artifact.RelativePath,
            restored.RelativePath);
        Assert.Equal(
            artifact.DownloadFileName,
            restored.DownloadFileName);
        Assert.Equal(
            artifact.CanonicalStem,
            restored.CanonicalStem);
        Assert.Equal(
            artifact.CanonicalExtension,
            restored.CanonicalExtension);
        Assert.Equal(
            artifact.MediaType,
            restored.MediaType);
        Assert.Equal(artifact.Length, restored.Length);
        Assert.Equal(artifact.Sha256, restored.Sha256);
        Assert.Equal(
            artifact.CanDownload,
            restored.CanDownload);
        Assert.Equal(
            artifact.CanUseForRestart,
            restored.CanUseForRestart);
    }

    private static string CreateLegacyJson()
    {
        return """
        {
          "id": "legacy-result",
          "title": "旧科学成果",
          "summary": "阶段 1 之前保存的数据",
          "status": "Complete",
          "pointSet": {
            "id": "legacy-point-set",
            "name": "旧点集",
            "description": "不含 Artifacts 字段",
            "points": [
              {
                "id": "legacy-point",
                "name": "旧点",
                "kind": "Unknown",
                "status": "Accepted",
                "geometry": {},
                "components": [],
                "electronicState": {},
                "calculationModel": {},
                "properties": [],
                "validations": [],
                "provenance": {},
                "labels": [],
                "metadata": {},
                "createdAt": "2026-10-05T00:00:00+00:00",
                "updatedAt": "2026-10-05T00:00:00+00:00"
              }
            ],
            "relations": []
          },
          "observables": [],
          "metadata": {},
          "createdAt": "2026-10-05T00:00:00+00:00",
          "completedAt": "2026-10-05T01:00:00+00:00"
        }
        """;
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
