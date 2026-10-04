namespace ChemSculptor.FundamentalConstants.Chemistry.Elements;

/// <summary>
/// 化学元素的基础数据。
/// </summary>
public readonly struct ChemicalElement
{
    /// <summary>创建化学元素数据。</summary>
    /// <param name="atomicNumber">原子序数；虚拟元素使用 0。</param>
    /// <param name="symbol">元素符号。</param>
    /// <param name="name">元素名称。</param>
    /// <param name="atomicMass">迁移自旧程序的一般原子质量，单位为 u。</param>
    public ChemicalElement(
        int atomicNumber,
        string symbol,
        string name,
        double atomicMass)
        : this(
            atomicNumber,
            symbol,
            name,
            atomicMass,
            AtomicMassKind.LegacyGeneralPurpose,
            "Migrated from ZhangCang")
    {
    }

    /// <summary>创建带有质量语义和来源信息的化学元素数据。</summary>
    /// <param name="atomicNumber">原子序数；虚拟元素使用 0。</param>
    /// <param name="symbol">元素符号。</param>
    /// <param name="name">元素名称。</param>
    /// <param name="atomicMass">原子质量，单位为 u。</param>
    /// <param name="atomicMassKind">原子质量的定义类型。</param>
    /// <param name="atomicMassSource">原子质量来源说明。</param>
    public ChemicalElement(
        int atomicNumber,
        string symbol,
        string name,
        double atomicMass,
        AtomicMassKind atomicMassKind,
        string atomicMassSource)
    {
        if (atomicNumber < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(atomicNumber),
                "原子序数不能小于 0。");
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("元素符号不能为空。", nameof(symbol));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("元素名称不能为空。", nameof(name));
        }

        if (atomicMass < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(atomicMass),
                "原子质量不能小于 0。");
        }

        if (string.IsNullOrWhiteSpace(atomicMassSource))
        {
            throw new ArgumentException(
                "原子质量来源不能为空。",
                nameof(atomicMassSource));
        }

        AtomicNumber = atomicNumber;
        Symbol = symbol;
        Name = name;
        AtomicMass = atomicMass;
        AtomicMassKind = atomicMassKind;
        AtomicMassSource = atomicMassSource;
    }

    /// <summary>原子序数；虚拟元素为 0。</summary>
    public int AtomicNumber { get; }

    /// <summary>规范大小写形式的元素符号。</summary>
    public string Symbol { get; }

    /// <summary>元素名称。</summary>
    public string Name { get; }

    /// <summary>
    /// 迁移自旧程序的一般原子质量，单位为 u。
    /// 该值不等同于标准原子量，也不等同于某个特定同位素质量。
    /// </summary>
    public double AtomicMass { get; }

    /// <summary>原子质量的定义类型。</summary>
    public AtomicMassKind AtomicMassKind { get; }

    /// <summary>原子质量的来源说明。</summary>
    public string AtomicMassSource { get; }

    /// <summary>返回元素符号和原子序数。</summary>
    public override string ToString()
    {
        return Symbol + " (" + AtomicNumber.ToString() + ")";
    }
}
