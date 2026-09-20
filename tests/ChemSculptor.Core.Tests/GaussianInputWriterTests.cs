using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// Gaussian 输入文件生成测试。
/// </summary>
public class GaussianInputWriterTests
{
    /// <summary>验证默认单点方案生成的输入文件内容。</summary>
    [Fact]
    public async Task WritesDefaultSinglePointInput()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);
        string outputPath = Path.Combine(root, "job-123.gjf");

        try
        {
            CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();
            CanonicalGeometry geometry = CreateWaterGeometry();

            GaussianInputOptions options = new GaussianInputOptions();
            options.Memory = "4GB";
            options.ProcessorCount = 4;
            options.CheckpointFilePath = string.Empty;
            options.Title = "water single point";

            GaussianInputWriter writer = new GaussianInputWriter();
            await writer.WriteAsync(spec, geometry, options, outputPath);

            string text = await File.ReadAllTextAsync(outputPath);

            Assert.Contains("%nprocshared=4", text);
            Assert.Contains("%mem=4GB", text);
            Assert.Contains("#p CAM-B3LYP/6-31G* SP", text);
            Assert.Contains("water single point", text);
            Assert.Contains("0 1", text);
            Assert.Contains("O 0.000000 0.000000 0.117300", text);
            Assert.Contains("H 0.000000 0.757200 -0.469200", text);
            Assert.Contains(Path.ChangeExtension(outputPath, ".chk"), text);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    /// <summary>验证当前阶段拒绝非单点任务。</summary>
    [Fact]
    public async Task RejectsNonSinglePointTask()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);
        string outputPath = Path.Combine(root, "job-456.gjf");

        try
        {
            CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();
            spec.TaskType = CalculationTaskType.Optimization;

            CanonicalGeometry geometry = CreateWaterGeometry();

            GaussianInputOptions options = new GaussianInputOptions();
            options.Memory = "4GB";
            options.ProcessorCount = 4;

            GaussianInputWriter writer = new GaussianInputWriter();
            bool exceptionThrown = false;

            try
            {
                await writer.WriteAsync(spec, geometry, options, outputPath);
            }
            catch (NotSupportedException)
            {
                exceptionThrown = true;
            }

            Assert.True(exceptionThrown);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static CanonicalGeometry CreateWaterGeometry()
    {
        CanonicalGeometry geometry = new CanonicalGeometry();
        geometry.Atoms = new List<CanonicalAtom>();

        CanonicalAtom oxygen = new CanonicalAtom();
        oxygen.Index = 1;
        oxygen.Element = "O";
        oxygen.X = 0.000000;
        oxygen.Y = 0.000000;
        oxygen.Z = 0.117300;
        geometry.Atoms.Add(oxygen);

        CanonicalAtom hydrogenOne = new CanonicalAtom();
        hydrogenOne.Index = 2;
        hydrogenOne.Element = "H";
        hydrogenOne.X = 0.000000;
        hydrogenOne.Y = 0.757200;
        hydrogenOne.Z = -0.469200;
        geometry.Atoms.Add(hydrogenOne);

        CanonicalAtom hydrogenTwo = new CanonicalAtom();
        hydrogenTwo.Index = 3;
        hydrogenTwo.Element = "H";
        hydrogenTwo.X = 0.000000;
        hydrogenTwo.Y = -0.757200;
        hydrogenTwo.Z = -0.469200;
        geometry.Atoms.Add(hydrogenTwo);

        return geometry;
    }
}
