using System.Security.Cryptography;
using System.Text;
using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>科学点文件解析服务测试。</summary>
public class ScientificArtifactResolverTests
{
    /// <summary>验证完整文件引用可以解析并打开。</summary>
    [Fact]
    public async Task ResolvesManifestAndOpensVerifiedArtifact()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                CreateWorkspaceOptions(root);
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileScientificDataRepository repository =
                CreateRepository(root);
            await workspace.EnsureJobWorkspaceAsync("job-original");

            string runDirectory =
                workspace.GetRunDirectory("job-original");
            await WriteRunFileAsync(
                runDirectory,
                "job-original.gjf",
                "original input");

            ScientificResult result =
                CreateResult(
                    "result-1",
                    CreatePoint(
                        "point-original",
                        "job-original",
                        CreateArtifact(
                            "artifact-original-input",
                            "job-original",
                            "job-original.gjf",
                            "O2-original-m1.gjf",
                            ScientificArtifactKind.Input,
                            false,
                            "original input")));
            await repository.SaveAsync(result);

            ScientificArtifactResolver resolver =
                new ScientificArtifactResolver(
                    repository,
                    workspace);
            ScientificArtifactManifest manifest =
                await resolver.ResolveManifestAsync(result.Id);

            Assert.True(manifest.Succeeded);
            Assert.Equal(string.Empty, manifest.Error);
            Assert.Single(manifest.Points);
            Assert.True(manifest.Points[0].IsAvailable);

            ScientificArtifactFileManifest fileManifest =
                Assert.Single(
                    manifest.Points[0].Artifacts);
            Assert.True(fileManifest.IsAvailable);
            Assert.True(fileManifest.IsSha256Valid);
            Assert.Equal(
                fileManifest.ExpectedSha256,
                fileManifest.ActualSha256);
            Assert.Equal(
                "O2-original-m1.gjf",
                fileManifest.DownloadFileName);

            ScientificArtifactOpenResult openResult =
                await resolver.OpenArtifactAsync(
                    result.Id,
                    "point-original",
                    "artifact-original-input");

            Assert.True(openResult.Succeeded);
            Assert.Equal(string.Empty, openResult.Error);
            Assert.NotNull(openResult.Artifact);

            ScientificArtifactContent content =
                openResult.Artifact!;
            using (Stream stream = content.Content)
            {
                using (StreamReader reader =
                    new StreamReader(stream))
                {
                    Assert.Equal(
                        "original input",
                        await reader.ReadToEndAsync());
                }
            }
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>
    /// 验证缺失的 fchk 不影响其它文件解析和下载。
    /// </summary>
    [Fact]
    public async Task MissingCheckpointDoesNotBlockOtherArtifacts()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                CreateWorkspaceOptions(root);
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileScientificDataRepository repository =
                CreateRepository(root);
            await workspace.EnsureJobWorkspaceAsync("job-original");

            string runDirectory =
                workspace.GetRunDirectory("job-original");
            await WriteRunFileAsync(
                runDirectory,
                "job-original.gjf",
                "original input");
            await WriteRunFileAsync(
                runDirectory,
                "output.log",
                "normal termination");

            CalculationPoint point = CreatePoint(
                "point-original",
                "job-original",
                CreateArtifact(
                    "artifact-original-input",
                    "job-original",
                    "job-original.gjf",
                    "O2-original-m1.gjf",
                    ScientificArtifactKind.Input,
                    false,
                    "original input"),
                CreateArtifact(
                    "artifact-original-checkpoint",
                    "job-original",
                    "job-original.fchk",
                    "O2-original-m1.fchk",
                    ScientificArtifactKind.RestartState,
                    true,
                    "expected checkpoint"),
                CreateArtifact(
                    "artifact-original-output",
                    "job-original",
                    "output.log",
                    "O2-original-m1.log",
                    ScientificArtifactKind.PrimaryOutput,
                    false,
                    "normal termination"));
            ScientificResult result =
                CreateResult("result-1", point);
            await repository.SaveAsync(result);

            ScientificArtifactResolver resolver =
                new ScientificArtifactResolver(
                    repository,
                    workspace);
            ScientificArtifactManifest manifest =
                await resolver.ResolveManifestAsync(result.Id);

            Assert.True(manifest.Succeeded);
            Assert.Single(manifest.Points);
            Assert.True(manifest.Points[0].IsAvailable);
            Assert.Equal(3, manifest.Points[0].Artifacts.Count);

            ScientificArtifactFileManifest missingCheckpoint =
                FindManifestArtifact(
                    manifest,
                    "artifact-original-checkpoint");
            ScientificArtifactFileManifest availableInput =
                FindManifestArtifact(
                    manifest,
                    "artifact-original-input");
            ScientificArtifactFileManifest availableOutput =
                FindManifestArtifact(
                    manifest,
                    "artifact-original-output");

            Assert.False(missingCheckpoint.IsAvailable);
            Assert.Contains(
                "不存在",
                missingCheckpoint.Error);
            Assert.True(availableInput.IsAvailable);
            Assert.True(availableOutput.IsAvailable);

            ScientificArtifactOpenResult missingOpen =
                await resolver.OpenArtifactAsync(
                    result.Id,
                    "point-original",
                    "artifact-original-checkpoint");
            ScientificArtifactOpenResult outputOpen =
                await resolver.OpenArtifactAsync(
                    result.Id,
                    "point-original",
                    "artifact-original-output");

            Assert.False(missingOpen.Succeeded);
            Assert.Contains("不存在", missingOpen.Error);
            Assert.True(outputOpen.Succeeded);
            Assert.NotNull(outputOpen.Artifact);
            outputOpen.Artifact!.Content.Dispose();
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证解析服务拒绝运行目录之外的相对路径。</summary>
    [Fact]
    public async Task RejectsArtifactPathOutsideRunDirectory()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                CreateWorkspaceOptions(root);
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileScientificDataRepository repository =
                CreateRepository(root);
            await workspace.EnsureJobWorkspaceAsync("job-original");

            string runDirectory =
                workspace.GetRunDirectory("job-original");
            string outsidePath = Path.GetFullPath(
                Path.Combine(
                    runDirectory,
                    "..",
                    "outside.txt"));
            await File.WriteAllTextAsync(
                outsidePath,
                "server secret");

            ScientificResult result =
                CreateResult(
                    "result-1",
                    CreatePoint(
                        "point-original",
                        "job-original",
                        CreateArtifact(
                            "artifact-outside",
                            "job-original",
                            "../outside.txt",
                            "outside.txt",
                            ScientificArtifactKind.Other,
                            false,
                            "server secret")));
            await repository.SaveAsync(result);

            ScientificArtifactResolver resolver =
                new ScientificArtifactResolver(
                    repository,
                    workspace);
            ScientificArtifactManifest manifest =
                await resolver.ResolveManifestAsync(result.Id);
            ScientificArtifactFileManifest artifactManifest =
                FindManifestArtifact(
                    manifest,
                    "artifact-outside");
            ScientificArtifactOpenResult openResult =
                await resolver.OpenArtifactAsync(
                    result.Id,
                    "point-original",
                    "artifact-outside");

            Assert.False(artifactManifest.IsAvailable);
            Assert.Contains(
                "越界",
                artifactManifest.Error);
            Assert.False(openResult.Succeeded);
            Assert.Contains("越界", openResult.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>
    /// 验证文件引用与科学点的计算作业号必须一致。
    /// </summary>
    [Fact]
    public async Task RejectsMismatchedCalculationJobId()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                CreateWorkspaceOptions(root);
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileScientificDataRepository repository =
                CreateRepository(root);
            await workspace.EnsureJobWorkspaceAsync("job-original");

            string runDirectory =
                workspace.GetRunDirectory("job-original");
            await WriteRunFileAsync(
                runDirectory,
                "job-original.gjf",
                "original input");

            ScientificResult result =
                CreateResult(
                    "result-1",
                    CreatePoint(
                        "point-original",
                        "job-original",
                        CreateArtifact(
                            "artifact-mismatch",
                            "job-recovery",
                            "job-original.gjf",
                            "O2-original-m1.gjf",
                            ScientificArtifactKind.Input,
                            false,
                            "original input")));
            await repository.SaveAsync(result);

            ScientificArtifactResolver resolver =
                new ScientificArtifactResolver(
                    repository,
                    workspace);
            ScientificArtifactManifest manifest =
                await resolver.ResolveManifestAsync(result.Id);
            ScientificArtifactFileManifest artifactManifest =
                FindManifestArtifact(
                    manifest,
                    "artifact-mismatch");
            ScientificArtifactOpenResult openResult =
                await resolver.OpenArtifactAsync(
                    result.Id,
                    "point-original",
                    "artifact-mismatch");

            Assert.False(artifactManifest.IsAvailable);
            Assert.Contains(
                "不一致",
                artifactManifest.Error);
            Assert.False(openResult.Succeeded);
            Assert.Contains("不一致", openResult.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static CalculationWorkspaceOptions
        CreateWorkspaceOptions(string root)
    {
        CalculationWorkspaceOptions options =
            new CalculationWorkspaceOptions();
        options.RootDirectory = Path.Combine(
            root,
            "workspace");
        return options;
    }

    private static FileScientificDataRepository
        CreateRepository(string root)
    {
        ScientificDataRepositoryOptions options =
            new ScientificDataRepositoryOptions();
        options.RootDirectory = Path.Combine(
            root,
            "scientific-data");
        return new FileScientificDataRepository(options);
    }

    private static ScientificResult CreateResult(
        string resultId,
        params CalculationPoint[] points)
    {
        ScientificResult result = new ScientificResult();
        result.Id = resultId;
        result.Title = "文件解析测试";
        result.Status = ScientificResultStatus.Complete;

        for (int index = 0;
            index < points.Length;
            index++)
        {
            result.PointSet.Points.Add(points[index]);
        }

        return result;
    }

    private static CalculationPoint CreatePoint(
        string pointId,
        string calculationJobId,
        params PointArtifactReference[] artifacts)
    {
        CalculationPoint point = new CalculationPoint();
        point.Id = pointId;
        point.CalculationJobId = calculationJobId;
        point.Status = CalculationPointStatus.Accepted;

        for (int index = 0;
            index < artifacts.Length;
            index++)
        {
            point.Artifacts.Add(artifacts[index]);
        }

        return point;
    }

    private static PointArtifactReference CreateArtifact(
        string artifactId,
        string calculationJobId,
        string relativePath,
        string downloadFileName,
        ScientificArtifactKind kind,
        bool canUseForRestart,
        string expectedContent)
    {
        PointArtifactReference artifact =
            new PointArtifactReference();
        artifact.ArtifactId = artifactId;
        artifact.CalculationJobId = calculationJobId;
        artifact.Kind = kind;
        artifact.RelativePath = relativePath;
        artifact.DownloadFileName = downloadFileName;
        artifact.CanonicalStem =
            Path.GetFileNameWithoutExtension(
                downloadFileName);
        artifact.CanonicalExtension =
            Path.GetExtension(downloadFileName);
        artifact.MediaType = "application/octet-stream";
        artifact.Length =
            Encoding.UTF8.GetByteCount(expectedContent);
        artifact.Sha256 = ComputeSha256(
            expectedContent);
        artifact.CanDownload = true;
        artifact.CanUseForRestart = canUseForRestart;
        return artifact;
    }

    private static async Task WriteRunFileAsync(
        string runDirectory,
        string relativePath,
        string content)
    {
        string path = Path.Combine(
            runDirectory,
            relativePath);
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, content);
    }

    private static ScientificArtifactFileManifest
        FindManifestArtifact(
            ScientificArtifactManifest manifest,
            string artifactId)
    {
        for (int pointIndex = 0;
            pointIndex < manifest.Points.Count;
            pointIndex++)
        {
            ScientificArtifactPointManifest point =
                manifest.Points[pointIndex];

            for (int artifactIndex = 0;
                artifactIndex < point.Artifacts.Count;
                artifactIndex++)
            {
                ScientificArtifactFileManifest artifact =
                    point.Artifacts[artifactIndex];

                if (string.Equals(
                    artifact.ArtifactId,
                    artifactId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return artifact;
                }
            }
        }

        throw new InvalidOperationException(
            "没有找到文件引用清单项：" + artifactId);
    }

    private static string ComputeSha256(string content)
    {
        byte[] input = Encoding.UTF8.GetBytes(content);
        byte[] hash = SHA256.HashData(input);
        return Convert.ToHexString(hash);
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
