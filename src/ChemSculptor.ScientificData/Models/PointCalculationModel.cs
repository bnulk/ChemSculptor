namespace ChemSculptor.ScientificData.Models;

/// <summary>定义计算点所属势能面或模型化学的计算条件。</summary>
public sealed class PointCalculationModel
{
    /// <summary>计算程序。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>计算程序版本。</summary>
    public string ProgramVersion { get; set; } = string.Empty;

    /// <summary>方法或泛函。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>基组。</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>溶剂模型或环境描述。</summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>其它计算参数。</summary>
    public Dictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
