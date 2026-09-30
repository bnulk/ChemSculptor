namespace ChemSculptor.Compute;

/// <summary>量子化学程序适配器注册表。</summary>
public interface IQuantumProgramAdapterRegistry
{
    /// <summary>按计算方案查找适配器。</summary>
    IQuantumProgramAdapter? Resolve(CalculationSpec spec);

    /// <summary>列出已注册程序名称。</summary>
    IReadOnlyList<string> ListPrograms();
}
