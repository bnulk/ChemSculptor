using System.Globalization;

namespace ChemSculptor.InputProcessor;

public interface IGeometryTextParser
{
    Task<MolecularGeometry> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default);
}

public sealed class GeometryTextParser : IGeometryTextParser
{
    public Task<MolecularGeometry> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        var lines = rawText.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var atoms = new List<GeometryAtom>();
        var diagnostics = new List<string>();
        var sourceName = "未命名分子";
        var expectedCount = (int?)null;
        var lineIndex = 0;

        if (lines.Length > 0 && int.TryParse(lines[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
        {
            expectedCount = count;
            lineIndex = 1;

            if (lines.Length > 1)
            {
                sourceName = lines[1];
                lineIndex = 2;
            }
        }

        for (; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4)
            {
                diagnostics.Add($"第 {lineIndex + 1} 行不是坐标行，已忽略：{line}");
                continue;
            }

            if (!TryParseDouble(parts[1], out var x)
                || !TryParseDouble(parts[2], out var y)
                || !TryParseDouble(parts[3], out var z))
            {
                diagnostics.Add($"第 {lineIndex + 1} 行坐标不是数字，已忽略：{line}");
                continue;
            }

            atoms.Add(new GeometryAtom(NormalizeElement(parts[0]), x, y, z));
        }

        if (expectedCount is not null && atoms.Count != expectedCount)
        {
            diagnostics.Add($"首行声明 {expectedCount} 个原子，实际解析到 {atoms.Count} 个。");
        }

        return Task.FromResult(new MolecularGeometry
        {
            SourceName = sourceName,
            Formula = BuildFormula(atoms),
            RawText = rawText,
            Atoms = atoms,
            Diagnostics = diagnostics
        });
    }

    private static string NormalizeElement(string element)
    {
        if (string.IsNullOrWhiteSpace(element))
        {
            return "?";
        }

        var value = element.Trim();
        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    private static string BuildFormula(IReadOnlyList<GeometryAtom> atoms)
    {
        var counts = atoms
            .GroupBy(atom => atom.Element)
            .ToDictionary(group => group.Key, group => group.Count());

        return string.Concat(counts
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value == 1 ? pair.Key : $"{pair.Key}{pair.Value}"));
    }

    private static bool TryParseDouble(string value, out double result) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
}
