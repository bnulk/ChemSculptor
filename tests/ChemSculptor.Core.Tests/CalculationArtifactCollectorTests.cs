using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>计算产物收集与持久化测试。</summary>
public class CalculationArtifactCollectorTests
{
    /// <summary>验证收集器会记录用途、长度、摘要和恢复能力。</summary>
    [Fact]
    public async Task CollectsArtifactMetadata()
    {
        string root = CreateTemporaryRoot();

        try
        {
            string outputPath = Path.Combine(root, "output.log");
            string restartPath = Path.Combine(root, "job.fchk");

            await File.WriteAllTextAsync(outputPath, "normal termination");
            await File.WriteAllTextAsync(restartPath, "restart state");

            List<CalculationArtifactPattern> patterns =
                new List<CalculationArtifactPattern>();
            patterns.Add(CreatePattern(
                "output.log",
                CalculationArtifactKind.PrimaryOutput,
                "text/plain; charset=utf-8",
                false));
            patterns.Add(CreatePattern(
                "*.fchk",
                CalculationArtifactKind.RestartState,
                "application/octet-stream",
                true));

            List<CalculationArtifactDescriptor> artifacts =
                CalculationArtifactCollector.Collect(root, patterns);

            Assert.Equal(2, artifacts.Count);
            Assert.Equal("job.fchk", artifacts[0].FileName);
            Assert.Equal(CalculationArtifactKind.RestartState, artifacts[0].Kind);
            Assert.True(artifacts[0].CanUseForRestart);
            Assert.Equal(64, artifacts[0].Sha256.Length);
            Assert.Equal("output.log", artifacts[1].FileName);
            Assert.Equal(CalculationArtifactKind.PrimaryOutput, artifacts[1].Kind);
            Assert.False(artifacts[1].CanUseForRestart);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证空文件不会进入计算产物清单。</summary>
    [Fact]
    public async Task IgnoresEmptyArtifacts()
    {
        string root = CreateTemporaryRoot();

        try
        {
            string emptyPath = Path.Combine(
                root,
                "stdout.log");
            string nonEmptyPath = Path.Combine(
                root,
                "stderr.log");
            await File.WriteAllTextAsync(
                emptyPath,
                string.Empty);
            await File.WriteAllTextAsync(
                nonEmptyPath,
                "process error");

            List<CalculationArtifactPattern> patterns =
                new List<CalculationArtifactPattern>();
            patterns.Add(CreatePattern(
                "stdout.log",
                CalculationArtifactKind.SupportingOutput,
                "text/plain; charset=utf-8",
                false));
            patterns.Add(CreatePattern(
                "stderr.log",
                CalculationArtifactKind.SupportingOutput,
                "text/plain; charset=utf-8",
                false));

            List<CalculationArtifactDescriptor> artifacts =
                CalculationArtifactCollector.Collect(
                    root,
                    patterns);

            Assert.Single(artifacts);
            Assert.Equal(
                "stderr.log",
                artifacts[0].FileName);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证结果文件不能借相对路径读取运行目录之外的文件。</summary>
    [Fact]
    public void RejectsPathTraversal()
    {
        string root = CreateTemporaryRoot();

        try
        {
            string siblingPath = Path.Combine(
                Path.GetDirectoryName(root) ?? root,
                "outside-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(siblingPath, "outside");

            string fullPath;
            bool resolved = CalculationArtifactCollector.TryResolveFullPath(
                root,
                Path.Combine("..", Path.GetFileName(siblingPath)),
                out fullPath);

            Assert.False(resolved);
            Assert.Equal(string.Empty, fullPath);
            File.Delete(siblingPath);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 result.json 会保存计算程序与文件清单。</summary>
    [Fact]
    public async Task RepositoryPersistsArtifactsInResultJson()
    {
        string root = CreateTemporaryRoot();
        string jobId = "job-artifact-test";

        try
        {
            CalculationWorkspaceOptions options =
                new CalculationWorkspaceOptions();
            options.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(options);
            await workspace.EnsureJobWorkspaceAsync(jobId);
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);

            CalculationResult result = new CalculationResult();
            result.JobId = jobId;
            result.Program = "Test Program";

            CalculationArtifactDescriptor descriptor =
                new CalculationArtifactDescriptor();
            descriptor.FileName = "output.log";
            descriptor.RelativePath = "output.log";
            descriptor.Kind = CalculationArtifactKind.PrimaryOutput;
            descriptor.MediaType = "text/plain; charset=utf-8";
            descriptor.Length = 17;
            descriptor.Sha256 = "abc";
            descriptor.CanUseForRestart = false;
            result.Artifacts.Add(descriptor);

            await repository.SaveResultAsync(result);

            string resultPath = workspace.GetJobResultPath(jobId);
            string json = await File.ReadAllTextAsync(resultPath);

            Assert.Contains("\"program\": \"Test Program\"", json);
            Assert.Contains("\"artifacts\"", json);
            Assert.Contains("\"canUseForRestart\": false", json);

            CalculationResult? loaded =
                await repository.GetResultAsync(jobId);

            Assert.NotNull(loaded);
            Assert.Single(loaded.Artifacts);
            Assert.Equal("output.log", loaded.Artifacts[0].FileName);
            Assert.Equal(
                CalculationArtifactKind.PrimaryOutput,
                loaded.Artifacts[0].Kind);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static CalculationArtifactPattern CreatePattern(
        string filePattern,
        CalculationArtifactKind kind,
        string mediaType,
        bool canUseForRestart)
    {
        CalculationArtifactPattern pattern =
            new CalculationArtifactPattern();
        pattern.FilePattern = filePattern;
        pattern.Kind = kind;
        pattern.MediaType = mediaType;
        pattern.CanUseForRestart = canUseForRestart;
        return pattern;
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
