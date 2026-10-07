using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificSummary.Models;

/// <summary>由科学数据生成的成果叙述包。</summary>
public sealed class ScientificNarrativePackage
{
    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>根工作流标识。</summary>
    public string RootJobId { get; set; } = string.Empty;

    /// <summary>最终科学摘要。</summary>
    public string FinalSummary { get; set; } = string.Empty;

    /// <summary>科学点和物理量的组织说明。</summary>
    public string Organization { get; set; } = string.Empty;

    /// <summary>逐科学点叙述。</summary>
    public List<ScientificPointNarrative> Points { get; set; } =
        new List<ScientificPointNarrative>();

    /// <summary>科学点关系。</summary>
    public List<CalculationPointRelation> Relations { get; set; } =
        new List<CalculationPointRelation>();

    /// <summary>由科学点导出的物理量。</summary>
    public List<ScientificObservable> Observables { get; set; } =
        new List<ScientificObservable>();
}
