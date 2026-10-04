namespace ChemSculptor.FundamentalConstants.Chemistry.Elements;

/// <summary>
/// 元素原子质量的语义类型。
/// 同一元素在不同计算场景中可能需要不同定义的质量。
/// </summary>
public enum AtomicMassKind
{
    /// <summary>来源和使用范围尚不完全统一的旧程序通用质量。</summary>
    LegacyGeneralPurpose,

    /// <summary>IUPAC 标准原子量，适用于普通化学计量。</summary>
    StandardAtomicWeight,

    /// <summary>指定同位素的精确质量。</summary>
    IsotopeMass
}
