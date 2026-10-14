using System.Globalization;
using System.Text.RegularExpressions;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.Tddft;

/// <summary>Gaussian TDDFT 输出解析器。</summary>
public sealed class GaussianTddftOutputParser : ITddftOutputParser
{
    private static readonly Regex ScfDonePattern = new Regex(
        @"SCF Done:\s+E\([^)]+\)\s+=\s+(-?[0-9]+(?:\.[0-9]+)?(?:[DdEe][+-]?[0-9]+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ExcitedStatePattern = new Regex(
        @"Excited State\s+(?<index>\d+):\s+(?<label>\S+)\s+(?<energy>-?[0-9]+(?:\.[0-9]+)?)\s+eV\s+(?<wavelength>-?[0-9]+(?:\.[0-9]+)?)\s+nm\s+f=(?<f>[0-9]+(?:\.[0-9]+)?)\s+<S\*\*2>=(?<s2>-?[0-9]+(?:\.[0-9]+)?)",
        RegexOptions.Compiled
        | RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant);

    private static readonly Regex TransitionPattern = new Regex(
        @"^\s*(?<from>\d+)\s*->\s*(?<to>\d+)\s+(?<coefficient>-?[0-9]+(?:\.[0-9]+)?(?:[DdEe][+-]?[0-9]+)?)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>解析 TDDFT 输出。</summary>
    public async Task<TddftCalculationResult> ParseTddftOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        TddftCalculationResult result =
            new TddftCalculationResult();
        result.OutputFilePath = outputPath;

        if (string.IsNullOrWhiteSpace(outputPath)
            || !File.Exists(outputPath))
        {
            return result;
        }

        string text = await File.ReadAllTextAsync(
            outputPath,
            cancellationToken);
        string[] lines = text.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);
        TddftExcitedState? currentState = null;
        bool readingVelocityQuadrupole = false;
        List<string> quadrupoleColumns =
            new List<string>();

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                "Normal termination",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.NormalTermination = true;
            }

            Match scfDone = ScfDonePattern.Match(line);

            if (scfDone.Success)
            {
                double energy;

                if (TryParseGaussianNumber(
                    scfDone.Groups[1].Value,
                    out energy))
                {
                    result.GroundStateEnergy = energy;
                }
            }

            Match excitedState = ExcitedStatePattern.Match(line);

            if (excitedState.Success)
            {
                currentState = CreateExcitedState(
                    excitedState);
                result.ExcitedStates.Add(currentState);
                readingVelocityQuadrupole = false;
                continue;
            }

            if (currentState != null)
            {
                Match transition =
                    TransitionPattern.Match(line);

                if (transition.Success)
                {
                    currentState.Transitions.Add(
                        CreateTransition(transition));
                    continue;
                }

                if (line.Trim().Length == 0)
                {
                    currentState = null;
                }
            }

            if (line.IndexOf(
                "Ground to excited state transition velocity quadrupole moments",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                readingVelocityQuadrupole = true;
                quadrupoleColumns =
                    new List<string>();
                continue;
            }

            if (readingVelocityQuadrupole)
            {
                if (TryParseQuadrupoleRow(
                    line,
                    quadrupoleColumns,
                    result))
                {
                    continue;
                }

                if (line.Trim().Length == 0)
                {
                    readingVelocityQuadrupole = false;
                }
                else
                {
                    quadrupoleColumns = ReadColumnNames(line);
                }
            }
        }

        return result;
    }

    private static TddftExcitedState CreateExcitedState(
        Match match)
    {
        TddftExcitedState state =
            new TddftExcitedState();
        int stateIndex;

        int.TryParse(
            match.Groups["index"].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out stateIndex);
        state.StateIndex = stateIndex;
        state.StateLabel = match.Groups["label"].Value;
        state.EnergyEv = ParseDouble(match.Groups["energy"].Value);
        state.WavelengthNm =
            ParseDouble(match.Groups["wavelength"].Value);
        state.OscillatorStrength =
            ParseDouble(match.Groups["f"].Value);
        state.S2 = ParseDouble(match.Groups["s2"].Value);
        return state;
    }

    private static TddftOrbitalTransition CreateTransition(
        Match match)
    {
        TddftOrbitalTransition transition =
            new TddftOrbitalTransition();
        int fromOrbital;
        int toOrbital;
        int.TryParse(
            match.Groups["from"].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out fromOrbital);
        int.TryParse(
            match.Groups["to"].Value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out toOrbital);
        transition.FromOrbital = fromOrbital;
        transition.ToOrbital = toOrbital;
        transition.Coefficient =
            ParseDouble(match.Groups["coefficient"].Value);
        return transition;
    }

    private static bool TryParseQuadrupoleRow(
        string line,
        List<string> columns,
        TddftCalculationResult result)
    {
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] tokens = line.Trim().Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length < 3)
        {
            return false;
        }

        int stateIndex;

        if (!int.TryParse(
            tokens[0],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out stateIndex))
        {
            return false;
        }

        TddftTransitionMoment moment =
            new TddftTransitionMoment();
        moment.StateIndex = stateIndex;
        moment.RawText = line.Trim();

        for (int index = 1; index < tokens.Length; index++)
        {
            double value;

            if (!TryParseGaussianNumber(tokens[index], out value))
            {
                return false;
            }

            TddftTransitionMomentComponent component =
                new TddftTransitionMomentComponent();
            component.Name =
                index - 1 < columns.Count
                    ? columns[index - 1]
                    : "Component" + index.ToString();
            component.Value = value;
            moment.Components.Add(component);
        }

        result.TransitionVelocityQuadrupoleMoments.Add(
            moment);
        return true;
    }

    private static List<string> ReadColumnNames(string line)
    {
        List<string> columns = new List<string>();
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] tokens = line.Trim().Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);

        for (int index = 0; index < tokens.Length; index++)
        {
            double ignored;

            if (!TryParseGaussianNumber(tokens[index], out ignored))
            {
                columns.Add(tokens[index]);
            }
        }

        if (columns.Count > 0
            && string.Equals(
                columns[0],
                "state",
                StringComparison.OrdinalIgnoreCase))
        {
            columns.RemoveAt(0);
        }

        return columns;
    }

    private static double ParseDouble(string value)
    {
        double number;
        TryParseGaussianNumber(value, out number);
        return number;
    }

    private static bool TryParseGaussianNumber(
        string text,
        out double value)
    {
        string normalized = text.Replace('D', 'E')
            .Replace('d', 'e');
        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }
}
