using System.Globalization;
using System.Text;

namespace ChemSculptor.InputProcessor;

/// <summary>
/// 几何文本解析器契约。
/// </summary>
public interface IGeometryTextParser
{
    /// <summary>把坐标文本解析为分子几何对象。</summary>
    Task<MolecularGeometry> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 纯文本几何解析器。
/// 支持标准 XYZ 文本，也支持只有“元素 x y z”行的简化输入。
/// </summary>
public sealed class GeometryTextParser : IGeometryTextParser
{
    /// <summary>
    /// 解析坐标文本。
    /// 无法识别的行不会被当作原子，而是写入诊断信息。
    /// </summary>
    public Task<MolecularGeometry> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        // 同时按 Windows 的 \r\n 和 Linux 的 \n 拆分。
        char[] separators = new char[2];
        separators[0] = '\r';
        separators[1] = '\n';

        string[] lines = rawText.Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<GeometryAtom> atoms = new List<GeometryAtom>();
        List<string> diagnostics = new List<string>();
        string sourceName = "未命名分子";
        int? expectedCount = null;
        int lineIndex = 0;

        // 标准 XYZ 的第一行是原子数，第二行是名称；简化输入没有这两行。
        if (lines.Length > 0)
        {
            int count;
            if (int.TryParse(lines[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
            {
                expectedCount = count;
                lineIndex = 1;

                if (lines.Length > 1)
                {
                    sourceName = lines[1];
                    lineIndex = 2;
                }
            }
        }

        // 逐行解析“元素 x y z”。
        for (; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];
            char[] spaceSeparator = new char[1];
            spaceSeparator[0] = ' ';
            string[] parts = line.Split(spaceSeparator, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 4)
            {
                diagnostics.Add("第 " + (lineIndex + 1).ToString(CultureInfo.InvariantCulture) +
                    " 行不是坐标行，已忽略：" + line);
                continue;
            }

            double x;
            double y;
            double z;

            if (!TryParseDouble(parts[1], out x)
                || !TryParseDouble(parts[2], out y)
                || !TryParseDouble(parts[3], out z))
            {
                diagnostics.Add("第 " + (lineIndex + 1).ToString(CultureInfo.InvariantCulture) +
                    " 行坐标不是数字，已忽略：" + line);
                continue;
            }

            GeometryAtom atom = new GeometryAtom();
            atom.Element = NormalizeElement(parts[0]);
            atom.X = x;
            atom.Y = y;
            atom.Z = z;
            atoms.Add(atom);
        }

        // 如果首行声明了原子数，检查数量是否一致。
        if (expectedCount != null && atoms.Count != expectedCount.Value)
        {
            diagnostics.Add("首行声明 " + expectedCount.Value.ToString(CultureInfo.InvariantCulture) +
                " 个原子，实际解析到 " + atoms.Count.ToString(CultureInfo.InvariantCulture) + " 个。");
        }

        MolecularGeometry geometry = new MolecularGeometry();
        geometry.SourceName = sourceName;
        geometry.Formula = BuildFormula(atoms);
        geometry.RawText = rawText;
        geometry.Atoms = atoms;
        geometry.Diagnostics = diagnostics;

        return Task.FromResult(geometry);
    }

    /// <summary>把元素符号规范化为“首字母大写、其余小写”。</summary>
    private static string NormalizeElement(string element)
    {
        if (string.IsNullOrWhiteSpace(element))
        {
            return "?";
        }

        string value = element.Trim();
        return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
    }

    /// <summary>统计各元素数量并生成分子式。</summary>
    private static string BuildFormula(IReadOnlyList<GeometryAtom> atoms)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < atoms.Count; index++)
        {
            string element = atoms[index].Element;
            int currentCount;

            if (counts.TryGetValue(element, out currentCount))
            {
                counts[element] = currentCount + 1;
            }
            else
            {
                counts.Add(element, 1);
            }
        }

        List<string> elements = new List<string>();
        foreach (string key in counts.Keys)
        {
            elements.Add(key);
        }

        elements.Sort(StringComparer.OrdinalIgnoreCase);

        StringBuilder formula = new StringBuilder();
        for (int index = 0; index < elements.Count; index++)
        {
            string element = elements[index];
            int count = counts[element];
            formula.Append(element);

            if (count > 1)
            {
                formula.Append(count.ToString(CultureInfo.InvariantCulture));
            }
        }

        return formula.ToString();
    }

    /// <summary>使用固定区域文化解析坐标数值。</summary>
    private static bool TryParseDouble(string value, out double result)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
