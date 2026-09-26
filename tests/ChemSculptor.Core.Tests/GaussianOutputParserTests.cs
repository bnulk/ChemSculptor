using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 输出解析测试。</summary>
public class GaussianOutputParserTests
{
    /// <summary>验证正常结束和最终 SCF 能量可以被解析。</summary>
    [Fact]
    public async Task ParsesNormalTerminationAndEnergy()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            string outputText =
                " Entering Gaussian System, Link 0=g16\n" +
                " SCF Done:  E(RCAM-B3LYP) =  -76.3801014     A.U. after    8 cycles\n" +
                " Normal termination of Gaussian 16\n";

            await File.WriteAllTextAsync(outputPath, outputText);

            GaussianOutputParser parser = new GaussianOutputParser();
            GaussianOutput gaussianOutput = await parser.ParseAsync(outputPath);
            GaussianResultTranslator translator = new GaussianResultTranslator();
            CalculationResult result = translator.Translate(gaussianOutput);

            Assert.True(result.NormalTermination);
            Assert.NotNull(result.Energy);
            Assert.Equal(-76.3801014, result.Energy.Value, 7);
            Assert.Equal("RCAM-B3LYP", result.Method);
            Assert.Equal("Hartree", result.EnergyUnit);
            Assert.Equal(outputPath, result.OutputFilePath);
            Assert.Equal(CalculationFailureKind.None, result.FailureKind);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证错误结束会在结果中留下诊断。</summary>
    [Fact]
    public async Task ReportsErrorTerminationWithoutEnergy()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "output.log");

        try
        {
            string outputText =
                " Error termination via Lnk1e in g16.exe\n" +
                " Search path GAUSS_EXEDIR is empty\n";

            await File.WriteAllTextAsync(outputPath, outputText);

            GaussianOutputParser parser = new GaussianOutputParser();
            GaussianOutput gaussianOutput = await parser.ParseAsync(outputPath);
            GaussianResultTranslator translator = new GaussianResultTranslator();
            CalculationResult result = translator.Translate(gaussianOutput);

            Assert.False(result.NormalTermination);
            Assert.Null(result.Energy);
            Assert.Equal(CalculationFailureKind.ProgramError, result.FailureKind);

            bool errorTerminationDiagnosticFound = false;
            bool energyNotFoundDiagnosticFound = false;

            for (int index = 0; index < result.Diagnostics.Count; index++)
            {
                CalculationDiagnostic diagnostic = result.Diagnostics[index];

                if (diagnostic.Code == "gaussian.error_termination")
                {
                    errorTerminationDiagnosticFound = true;
                }

                if (diagnostic.Code == "gaussian.energy_not_found")
                {
                    energyNotFoundDiagnosticFound = true;
                }
            }

            Assert.True(errorTerminationDiagnosticFound);
            Assert.True(energyNotFoundDiagnosticFound);
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
