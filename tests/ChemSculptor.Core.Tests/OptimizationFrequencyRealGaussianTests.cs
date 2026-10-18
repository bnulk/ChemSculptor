using System.Diagnostics;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.FrequencyAnalysis;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Core.Tests;

/// <summary>真实 Gaussian 16 的 opt freq 冒烟测试。</summary>
public class OptimizationFrequencyRealGaussianTests
{
    /// <summary>验证真实 opt freq 作业可同时解析优化和频率结果。</summary>
    [Fact]
    public async Task RunsRealGaussianOptimizationFrequency()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(
                    "CHEMSCULPTOR_RUN_REAL_GAUSSIAN"),
                "1",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string executablePath = ResolveGaussianExecutable();

        if (executablePath.Length == 0)
        {
            return;
        }

        string root = CreateTemporaryRoot();
        string inputPath = Path.Combine(root, "opt-freq.gjf");
        string outputPath = Path.Combine(root, "opt-freq.log");

        try
        {
            CalculationSpec spec =
                CalculationDefaults
                    .CreateDefaultOptimizationFrequency();
            spec.Charge = 0;
            spec.Multiplicity = 1;
            spec.ExtraOptions[
                CalculationOptionKeys.OptimizationMaxCycles] = "20";

            GaussianInputOptions options =
                new GaussianInputOptions();
            options.Memory = "1GB";
            options.ProcessorCount = 2;
            options.CheckpointFilePath = string.Empty;
            options.Title = "ChemSculptor real opt freq";

            GaussianInputWriter writer =
                new GaussianInputWriter();
            await writer.WriteAsync(
                spec,
                CreateWaterGeometry(),
                options,
                inputPath);

            string executableDirectory =
                Path.GetDirectoryName(executablePath)
                ?? string.Empty;
            string scratchDirectory =
                Path.Combine(root, "scratch");
            Directory.CreateDirectory(scratchDirectory);

            ProcessStartInfo startInfo =
                new ProcessStartInfo();
            startInfo.FileName = executablePath;
            startInfo.WorkingDirectory = root;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            startInfo.ArgumentList.Add(inputPath);
            startInfo.ArgumentList.Add(outputPath);

            if (executableDirectory.Length > 0)
            {
                startInfo.Environment["GAUSS_EXEDIR"] =
                    executableDirectory;
            }

            startInfo.Environment["GAUSS_SCRDIR"] =
                scratchDirectory;

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                Task<string> standardOutput =
                    process.StandardOutput.ReadToEndAsync();
                Task<string> standardError =
                    process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                string diagnostic =
                    await standardOutput +
                    await standardError;

                Assert.True(
                    process.ExitCode == 0,
                    "Gaussian 进程退出码 " +
                    process.ExitCode.ToString() +
                    "：" +
                    diagnostic);
            }

            Assert.True(File.Exists(outputPath));
            GaussianOutputParser outputParser =
                new GaussianOutputParser();
            GaussianOutput output =
                await outputParser.ParseAsync(outputPath);
            OptimizationResult optimization =
                new GaussianResultTranslator()
                    .TranslateOptimization(output);
            FrequencyAnalysisResult frequency =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.True(optimization.NormalTermination);
            Assert.True(optimization.OptimizationConverged);
            Assert.True(frequency.NormalTermination);
            Assert.NotEmpty(frequency.Frequencies);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string ResolveGaussianExecutable()
    {
        string? path = Environment.GetEnvironmentVariable("PATH");

        if (!string.IsNullOrWhiteSpace(path))
        {
            string[] directories =
                path.Split(Path.PathSeparator);

            for (int index = 0;
                index < directories.Length;
                index++)
            {
                string directory =
                    directories[index].Trim().Trim('"');
                string candidate = Path.Combine(
                    directory,
                    "g16.exe");

                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        string fallback = @"C:\G16W\g16.exe";
        return File.Exists(fallback)
            ? fallback
            : string.Empty;
    }

    private static CanonicalGeometry CreateWaterGeometry()
    {
        CanonicalGeometry geometry = new CanonicalGeometry();
        CanonicalAtom oxygen = new CanonicalAtom();
        oxygen.Index = 1;
        oxygen.Element = "O";
        oxygen.Z = 0.1173;
        geometry.Atoms.Add(oxygen);
        CanonicalAtom firstHydrogen = new CanonicalAtom();
        firstHydrogen.Index = 2;
        firstHydrogen.Element = "H";
        firstHydrogen.Y = 0.7572;
        firstHydrogen.Z = -0.4692;
        geometry.Atoms.Add(firstHydrogen);
        CanonicalAtom secondHydrogen = new CanonicalAtom();
        secondHydrogen.Index = 3;
        secondHydrogen.Element = "H";
        secondHydrogen.Y = -0.7572;
        secondHydrogen.Z = -0.4692;
        geometry.Atoms.Add(secondHydrogen);
        return geometry;
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
