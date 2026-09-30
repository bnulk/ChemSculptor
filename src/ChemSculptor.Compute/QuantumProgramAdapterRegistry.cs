namespace ChemSculptor.Compute;

/// <summary>
/// 基于已注册适配器的程序注册表。
/// </summary>
public sealed class QuantumProgramAdapterRegistry : IQuantumProgramAdapterRegistry
{
    private readonly List<IQuantumProgramAdapter> _adapters;

    /// <summary>创建程序适配器注册表。</summary>
    public QuantumProgramAdapterRegistry(
        IEnumerable<IQuantumProgramAdapter> adapters)
    {
        if (adapters == null)
        {
            throw new ArgumentNullException(nameof(adapters));
        }

        _adapters = new List<IQuantumProgramAdapter>(adapters);
    }

    /// <summary>按计算方案查找适配器。</summary>
    public IQuantumProgramAdapter? Resolve(CalculationSpec spec)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        for (int index = 0; index < _adapters.Count; index++)
        {
            if (_adapters[index].CanRun(spec))
            {
                return _adapters[index];
            }
        }

        return null;
    }

    /// <summary>列出已注册程序名称。</summary>
    public IReadOnlyList<string> ListPrograms()
    {
        List<string> programs = new List<string>();

        for (int index = 0; index < _adapters.Count; index++)
        {
            programs.Add(_adapters[index].ProgramName);
        }

        return programs;
    }
}
