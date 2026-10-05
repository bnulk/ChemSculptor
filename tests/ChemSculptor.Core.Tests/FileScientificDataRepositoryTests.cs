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
