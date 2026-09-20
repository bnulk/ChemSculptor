using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 计算工作区路径与目录创建测试。
/// </summary>
public class WorkspaceManagerTests
{
    /// <summary>验证作业目录和子目录路径符合约定。</summary>
    [Fact]
    public void GetJobDirectoryReturnsExpectedPath()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = root;

        WorkspaceManager manager = new WorkspaceManager(options);
        string jobDirectory = Path.Combine(root, "jobs", "job-123");

        Assert.Equal(jobDirectory, manager.GetJobDirectory("job-123"));
        Assert.Equal(
            Path.Combine(jobDirectory, "input"),
            manager.GetInputDirectory("job-123"));
        Assert.Equal(
            Path.Combine(jobDirectory, "run"),
            manager.GetRunDirectory("job-123"));
        Assert.Equal(
            Path.Combine(jobDirectory, "results"),
            manager.GetResultDirectory("job-123"));
    }

    /// <summary>验证创建工作区时目录会被实际创建。</summary>
    [Fact]
    public async Task EnsureJobWorkspaceCreatesDirectories()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = root;

        WorkspaceManager manager = new WorkspaceManager(options);

        try
        {
            await manager.EnsureJobWorkspaceAsync("job-123");

            Assert.True(Directory.Exists(manager.GetInputDirectory("job-123")));
            Assert.True(Directory.Exists(manager.GetRunDirectory("job-123")));
            Assert.True(Directory.Exists(manager.GetResultDirectory("job-123")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    /// <summary>验证非法标识不能用于工作区路径。</summary>
    [Fact]
    public void InvalidIdentifierIsRejected()
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = Path.Combine(Path.GetTempPath(), "ChemSculptorTests");

        WorkspaceManager manager = new WorkspaceManager(options);
        bool exceptionThrown = false;

        try
        {
            manager.GetJobDirectory("../escape");
        }
        catch (ArgumentException)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown);
    }
}
