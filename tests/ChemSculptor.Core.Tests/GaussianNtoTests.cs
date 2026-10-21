using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.Nto;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian NTO 输入和输出测试。</summary>
public class GaussianNtoTests
{
    /// <summary>验证 NTO route 使用源参数并写入 Transition 编号。</summary>
    [Fact]
    public async Task WritesNtoInputWithTransitionIndex()
    {
        string root = CreateTemporaryRoot();
        string sourcePath = Path.Combine(root, "tddft.gjf");
        string targetPath = Path.Combine(root, "nto.gjf");

        try
        {
            await File.WriteAllTextAsync(
                sourcePath,
                "%chk=tddft.chk\n" +
                "%mem=4GB\n" +
                "%nprocshared=4\n\n" +
                "#p CAM-B3LYP/6-31G* td(nstates=50) em=GD3BJ nosymm\n\n" +
                "tddft source\n\n" +
                "0 1\n" +
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n\n");

            NtoInputWriteRequest request =
                new NtoInputWriteRequest();
            request.SourceInputPath = sourcePath;
            request.TargetInputPath = targetPath;
            request.SourceCheckpointFileName = "tddft.chk";
            request.CheckpointFileName = "tddft_State3.chk";
            request.TransitionIndex = 3;

            await new GaussianNtoInputWriter()
                .WriteAsync(request);
            string text = await File.ReadAllTextAsync(
                targetPath);

            Assert.StartsWith(
                "%oldchk=tddft.chk" +
                Environment.NewLine +
                "%chk=tddft_State3.chk",
                text);
            Assert.Contains(
                "CAM-B3LYP/6-31G*",
                text);
            Assert.DoesNotContain(
                "td(nstates=50)",
                text,
                StringComparison.OrdinalIgnoreCase);
            Assert.Contains(
                "Density=(Check,Transition=3)",
                text);
            Assert.Contains(
                "Geom=AllCheck",
                text);
            Assert.Contains(
                "Guess=(Read,Only)",
                text);
            Assert.Contains(
                "Pop=(Minimal,NTO,SaveNTO)",
                text);
            Assert.DoesNotContain("tddft source", text);
            Assert.DoesNotContain("0 1", text);
            Assert.DoesNotContain(
                "O 0.000000 0.000000 0.117300",
                text);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 NTO 输出可以识别正常结束和 NTO 标记。</summary>
    [Fact]
    public async Task ParsesNtoOutput()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "nto.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " Natural Transition Orbitals\n" +
                " Normal termination of Gaussian 16\n");
            NtoSelectedState state = new NtoSelectedState();
            state.StateIndex = 2;
            state.StateLabel = "Singlet-2";

            NtoTransitionResult result =
                await new GaussianNtoOutputParser()
                    .ParseAsync(outputPath, state);

            Assert.True(result.Succeeded);
            Assert.True(result.NormalTermination);
            Assert.True(result.NtoFound);
            Assert.Equal(2, result.StateIndex);
            Assert.Equal("Singlet-2", result.StateLabel);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证真实 Gaussian NTO 布居段落可以被识别。</summary>
    [Fact]
    public async Task ParsesGaussianNtoPopulationOutput()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "nto.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " Using densities on the checkpoint file.\n" +
                " Saving Alpha spin NTOs for state    1 to file 20524.\n" +
                " Populations using transition density between ground and state  1.\n" +
                " Normal termination of Gaussian 16\n");
            NtoSelectedState state = new NtoSelectedState();
            state.StateIndex = 1;
            state.StateLabel = "Singlet-1";

            NtoTransitionResult result =
                await new GaussianNtoOutputParser()
                    .ParseAsync(outputPath, state);

            Assert.True(result.Succeeded);
            Assert.True(result.NormalTermination);
            Assert.True(result.NtoFound);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 route 回显中的 NTO 不会被误判为计算产物。</summary>
    [Fact]
    public async Task DoesNotTreatRouteEchoAsNtoOutput()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "nto.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                "#p CAM-B3LYP/6-31G* Pop=(Minimal,NTO,SaveNTO)\n" +
                " Normal termination of Gaussian 16\n");
            NtoSelectedState state = new NtoSelectedState();
            state.StateIndex = 1;

            NtoTransitionResult result =
                await new GaussianNtoOutputParser()
                    .ParseAsync(outputPath, state);

            Assert.False(result.Succeeded);
            Assert.True(result.NormalTermination);
            Assert.False(result.NtoFound);
            Assert.Contains("没有解析到自然过渡轨道", result.Error);
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
