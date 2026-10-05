namespace ChemSculptor.ScientificData.Models;

/// <summary>计算点在科学数据中的接受状态。</summary>
public enum CalculationPointStatus
{
    /// <summary>候选点，尚未完成验证。</summary>
    Candidate,

    /// <summary>已经通过必要验证，可以作为后续科学工作的输入。</summary>
    Accepted,

    /// <summary>已经明确拒绝。</summary>
    Rejected,

    /// <summary>已经被新的接受点取代。</summary>
    Superseded
}

/// <summary>计算点在科研语义中的基本类型。</summary>
public enum CalculationPointKind
{
    /// <summary>尚未分类。</summary>
    Unknown,

    /// <summary>能量极小点。</summary>
    Minimum,

    /// <summary>鞍点或过渡态。</summary>
    TransitionState,

    /// <summary>反应中间体。</summary>
    Intermediate,

    /// <summary>解离碎片或独立组分。</summary>
    Fragment,

    /// <summary>路径上的离散点，例如 IRC 点。</summary>
    PathPoint,

    /// <summary>组分彼此分离的状态。</summary>
    SeparatedState
}

/// <summary>电子态的描述类别。</summary>
public enum PointElectronicStateKind
{
    /// <summary>尚未指定。</summary>
    Unknown,

    /// <summary>基态。</summary>
    GroundState,

    /// <summary>指定自旋态。</summary>
    SpinState,

    /// <summary>激发态。</summary>
    ExcitedState
}

/// <summary>两个计算点之间的关系类型。</summary>
public enum CalculationPointRelationKind
{
    /// <summary>一般的来源关系。</summary>
    DerivedFrom,

    /// <summary>路径或序列关系。</summary>
    Path,

    /// <summary>组分属于某个组合体系。</summary>
    ComponentOf,

    /// <summary>反应物关系。</summary>
    Reactant,

    /// <summary>产物关系。</summary>
    Product,

    /// <summary>构象关系。</summary>
    Conformer,

    /// <summary>异构体关系。</summary>
    Isomer
}

/// <summary>计算点属性的数据类别。</summary>
public enum ScientificPropertyKind
{
    /// <summary>能量。</summary>
    Energy,

    /// <summary>梯度。</summary>
    Gradient,

    /// <summary>Hessian。</summary>
    Hessian,

    /// <summary>频率。</summary>
    Frequency,

    /// <summary>偶极矩。</summary>
    DipoleMoment,

    /// <summary>其它数组或结构化性质。</summary>
    Array,

    /// <summary>文本说明。</summary>
    Text,

    /// <summary>其它数值。</summary>
    Number,

    /// <summary>其它性质。</summary>
    Other
}

/// <summary>科学成果从点集导出的物理量类别。</summary>
public enum ScientificObservableKind
{
    /// <summary>单点计算得到的能量。</summary>
    SinglePointEnergy,

    /// <summary>能量差。</summary>
    EnergyDifference,

    /// <summary>反应能。</summary>
    ReactionEnergy,

    /// <summary>解离能。</summary>
    DissociationEnergy,

    /// <summary>势垒。</summary>
    Barrier,

    /// <summary>光谱量。</summary>
    Spectrum,

    /// <summary>其它导出量。</summary>
    Other
}

/// <summary>科学成果的完成状态。</summary>
public enum ScientificResultStatus
{
    /// <summary>草稿，数据尚不完整。</summary>
    Draft,

    /// <summary>已经完整。</summary>
    Complete,

    /// <summary>部分完成，仍有未解决项。</summary>
    Partial,

    /// <summary>失败或不可用。</summary>
    Failed
}

/// <summary>验证记录的状态。</summary>
public enum PointValidationStatus
{
    /// <summary>尚未执行。</summary>
    NotRun,

    /// <summary>通过。</summary>
    Passed,

    /// <summary>发现异常。</summary>
    Finding,

    /// <summary>跳过。</summary>
    Skipped,

    /// <summary>无法得出结论。</summary>
    Inconclusive,

    /// <summary>执行失败。</summary>
    Failed
}
