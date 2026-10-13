using System.Globalization;
using System.Text.RegularExpressions;
using ChemSculptor.FundamentalConstants.Chemistry.Elements;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 输出文件解析器。
/// 只负责把文本解析为 Gaussian 专用数据结构，不生成通用计算结果。
/// </summary>
public sealed class GaussianOutputParser
{
    private static readonly Regex ScfDonePattern = new Regex(
        @"SCF Done:\s+E\(([^)]+)\)\s+=\s+(-?[0-9]+(?:\.[0-9]+)?(?:[DdEe][+-]?[0-9]+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ScfIterationsPattern = new Regex(
        @"after\s+([0-9]+)\s+cycles",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex StepNumberPattern = new Regex(
        @"Step number\s+([0-9]+)\s+out of a maximum of\s+([0-9]+)",
        RegexOptions.Compiled
        | RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant);

    private static readonly Regex ConvergenceItemPattern =
        new Regex(
            @"^\s*(Maximum Force|RMS\s+Force|Maximum Displacement|RMS\s+Displacement)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s+(YES|NO)\s*$",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant);

    private static readonly Regex CoordinatePattern = new Regex(
        @"^\s*([0-9]+)\s+([0-9]+)\s+([0-9]+)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 解析 Gaussian 输出文件。
    /// 读取全部文本后逐行查找结束标志和最后一次 SCF Done。
    /// </summary>
    public async Task<GaussianOutput> ParseAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        GaussianOutput result = new GaussianOutput();
        result.OutputFilePath = outputPath;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return result;
        }

        if (!File.Exists(outputPath))
        {
            return result;
        }

        string outputText = await File.ReadAllTextAsync(outputPath, cancellationToken);
        string[] lines = outputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);
        int? currentStepNumber = null;
        bool insideGeometry = false;
        List<CanonicalAtom> currentGeometryAtoms =
            new List<CanonicalAtom>();

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                "Normal termination of Gaussian",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.NormalTermination = true;
                continue;
            }

            if (line.IndexOf(
                "Error termination",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.ErrorTermination = true;
                result.ErrorMessages.Add(line.Trim());
                continue;
            }

            if (line.IndexOf(
                "Optimization completed",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.OptimizationCompleted = true;
                continue;
            }

            if (IsScfConvergenceFailure(line))
            {
                result.ScfConverged = false;
                GaussianOptimizationStep failedStep =
                    GetOrCreateStep(
                        result,
                        ref currentStepNumber);
                failedStep.ScfConverged = false;
            }

            Match stepMatch = StepNumberPattern.Match(line);
            if (stepMatch.Success)
            {
                int stepNumber;
                int maximumCycles;

                if (int.TryParse(
                        stepMatch.Groups[1].Value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out stepNumber)
                    && int.TryParse(
                        stepMatch.Groups[2].Value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out maximumCycles))
                {
                    currentStepNumber = stepNumber;
                    result.LastStepNumber = stepNumber;
                    result.MaximumOptimizationCycles =
                        maximumCycles;
                    GetOrCreateStep(
                        result,
                        stepNumber);
                }
            }

            Match convergenceMatch =
                ConvergenceItemPattern.Match(line);
            if (convergenceMatch.Success)
            {
                GaussianOptimizationStep convergenceStep =
                    GetOrCreateStep(
                        result,
                        ref currentStepNumber);
                GaussianOptimizationConvergence convergence =
                    GetOrCreateConvergence(
                        convergenceStep,
                        result);
                ApplyConvergenceItem(
                    convergence,
                    convergenceMatch);
                result.FinalConvergence = convergence;
            }

            if (TryReadOptimizationCriteria(
                line,
                result,
                ref currentStepNumber))
            {
                continue;
            }

            if (IsGeometryHeader(line))
            {
                insideGeometry = true;
                currentGeometryAtoms =
                    new List<CanonicalAtom>();
                continue;
            }

            if (insideGeometry)
            {
                Match coordinateMatch =
                    CoordinatePattern.Match(line);
                if (coordinateMatch.Success)
                {
                    CanonicalAtom atom =
                        CreateAtom(coordinateMatch);
                    currentGeometryAtoms.Add(atom);
                }
                else if (currentGeometryAtoms.Count > 0
                    && line.IndexOf(
                        "-----",
                        StringComparison.Ordinal) >= 0)
                {
                    CanonicalGeometry geometry =
                        CreateGeometry(currentGeometryAtoms);
                    result.FinalGeometry = geometry;
                    insideGeometry = false;
                }
            }

            Match match = ScfDonePattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            result.ScfConverged = true;
            GaussianOptimizationStep currentStep =
                GetOrCreateStep(
                    result,
                    ref currentStepNumber);
            currentStep.ScfConverged = true;

            Match iterationsMatch =
                ScfIterationsPattern.Match(line);

            if (iterationsMatch.Success)
            {
                int iterations;

                if (int.TryParse(
                    iterationsMatch.Groups[1].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out iterations))
                {
                    result.ScfIterations = iterations;
                    currentStep.ScfIterations = iterations;
                }
            }

            string energyText = match.Groups[2].Value;
            double energy;

            if (TryParseGaussianNumber(energyText, out energy))
            {
                result.Energy = energy;
                result.EnergyMethod = match.Groups[1].Value;
                currentStep.Energy = energy;

                if (result.FinalGeometry != null)
                {
                    currentStep.Geometry =
                        CloneGeometry(result.FinalGeometry);
                }
            }
        }

        return result;
    }

    private static bool IsGeometryHeader(string line)
    {
        return line.IndexOf(
            "Standard orientation:",
            StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf(
                "Input orientation:",
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static CanonicalAtom CreateAtom(Match match)
    {
        int atomIndex;
        int atomicNumber;
        double x;
        double y;
        double z;

        int.TryParse(
            match.Groups[1].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out atomIndex);
        int.TryParse(
            match.Groups[2].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out atomicNumber);
        TryParseGaussianNumber(
            match.Groups[4].Value,
            out x);
        TryParseGaussianNumber(
            match.Groups[5].Value,
            out y);
        TryParseGaussianNumber(
            match.Groups[6].Value,
            out z);

        CanonicalAtom atom = new CanonicalAtom();
        atom.Index = atomIndex;
        atom.Element = ResolveElementSymbol(atomicNumber);
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
        return atom;
    }

    private static string ResolveElementSymbol(int atomicNumber)
    {
        ChemicalElement element;

        if (ElementCatalog.TryGetByAtomicNumber(
            atomicNumber,
            out element))
        {
            return element.Symbol;
        }

        return atomicNumber.ToString(
            CultureInfo.InvariantCulture);
    }

    private static CanonicalGeometry CreateGeometry(
        List<CanonicalAtom> atoms)
    {
        CanonicalGeometry geometry = new CanonicalGeometry();
        geometry.Units = GeometryUnits.Angstrom;
        geometry.Atoms = new List<CanonicalAtom>(atoms);
        return geometry;
    }

    private static CanonicalGeometry CloneGeometry(
        CanonicalGeometry geometry)
    {
        CanonicalGeometry clone = new CanonicalGeometry();
        clone.Units = geometry.Units;

        for (int index = 0;
            index < geometry.Atoms.Count;
            index++)
        {
            CanonicalAtom atom = geometry.Atoms[index];
            CanonicalAtom cloneAtom = new CanonicalAtom();
            cloneAtom.Index = atom.Index;
            cloneAtom.Element = atom.Element;
            cloneAtom.X = atom.X;
            cloneAtom.Y = atom.Y;
            cloneAtom.Z = atom.Z;
            clone.Atoms.Add(cloneAtom);
        }

        return clone;
    }

    private static GaussianOptimizationStep GetOrCreateStep(
        GaussianOutput output,
        ref int? currentStepNumber)
    {
        int stepNumber;

        if (currentStepNumber.HasValue)
        {
            stepNumber = currentStepNumber.Value;
        }
        else
        {
            stepNumber = output.LastStepNumber.GetValueOrDefault() + 1;
            currentStepNumber = stepNumber;
        }

        return GetOrCreateStep(output, stepNumber);
    }

    private static GaussianOptimizationStep GetOrCreateStep(
        GaussianOutput output,
        int stepNumber)
    {
        for (int index = 0;
            index < output.OptimizationSteps.Count;
            index++)
        {
            GaussianOptimizationStep step =
                output.OptimizationSteps[index];

            if (step.StepNumber == stepNumber)
            {
                return step;
            }
        }

        GaussianOptimizationStep newStep =
            new GaussianOptimizationStep();
        newStep.StepNumber = stepNumber;
        output.OptimizationSteps.Add(newStep);

        if (!output.LastStepNumber.HasValue
            || stepNumber > output.LastStepNumber.Value)
        {
            output.LastStepNumber = stepNumber;
        }

        return newStep;
    }

    private static GaussianOptimizationConvergence
        GetOrCreateConvergence(
            GaussianOptimizationStep step,
            GaussianOutput output)
    {
        if (step.Convergence == null)
        {
            step.Convergence =
                new GaussianOptimizationConvergence();
        }

        if (output.FinalConvergence == null)
        {
            output.FinalConvergence = step.Convergence;
        }

        return step.Convergence;
    }

    private static void ApplyConvergenceItem(
        GaussianOptimizationConvergence convergence,
        Match match)
    {
        string itemName = match.Groups[1].Value;
        double value;

        if (!TryParseGaussianNumber(
            match.Groups[2].Value,
            out value))
        {
            return;
        }

        string normalizedItemName =
            Regex.Replace(
                itemName,
                @"\s+",
                " ").ToUpperInvariant();

        if (normalizedItemName == "MAXIMUM FORCE")
        {
            convergence.MaximumForce = value;
        }
        else if (normalizedItemName == "RMS FORCE")
        {
            convergence.RmsForce = value;
        }
        else if (normalizedItemName == "MAXIMUM DISPLACEMENT")
        {
            convergence.MaximumDisplacement = value;
        }
        else if (normalizedItemName == "RMS DISPLACEMENT")
        {
            convergence.RmsDisplacement = value;
        }

        bool itemConverged = string.Equals(
            match.Groups[4].Value,
            "YES",
            StringComparison.OrdinalIgnoreCase);
        convergence.Converged =
            (convergence.Converged ?? true)
            && itemConverged;
    }

    private static bool TryReadOptimizationCriteria(
        string line,
        GaussianOutput output,
        ref int? currentStepNumber)
    {
        string criteria = string.Empty;

        if (line.IndexOf(
            "very tight convergence",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            criteria = "VeryTight";
        }
        else if (line.IndexOf(
            "tight convergence",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            criteria = "Tight";
        }

        if (criteria.Length == 0)
        {
            return false;
        }

        GaussianOptimizationStep step =
            GetOrCreateStep(output, ref currentStepNumber);
        GaussianOptimizationConvergence convergence =
            GetOrCreateConvergence(step, output);
        convergence.Criteria = criteria;
        output.FinalConvergence = convergence;
        return true;
    }

    /// <summary>判断一行是否表示 SCF 收敛失败。</summary>
    private static bool IsScfConvergenceFailure(string line)
    {
        if (line.IndexOf(
            "Convergence failure",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (line.IndexOf(
            "SCF has not converged",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return line.IndexOf(
            "No convergence in SCF",
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 解析 Gaussian 数值。
    /// Gaussian 有时使用 D 表示指数，例如 1.0D+02。
    /// </summary>
    private static bool TryParseGaussianNumber(string text, out double value)
    {
        string normalizedText = text.Replace('D', 'E').Replace('d', 'e');

        return double.TryParse(
            normalizedText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

}
