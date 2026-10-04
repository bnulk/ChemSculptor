using ChemSculptor.FundamentalConstants.Chemistry.Elements;

namespace ChemSculptor.InputProcessor.Chemistry;

/// <summary>
/// 根据原子列表和总电荷计算体系的总电子数。
/// </summary>
public static class ElectronCountCalculator
{
    /// <summary>
    /// 计算总电子数。
    /// 总电子数等于全部原子序数之和减去总电荷。
    /// </summary>
    /// <param name="atoms">分子中的原子列表。</param>
    /// <param name="charge">体系总电荷。</param>
    /// <param name="electronCount">成功时返回总电子数。</param>
    /// <param name="error">失败时返回原因。</param>
    /// <returns>全部元素可识别且电子数非负时返回 true。</returns>
    public static bool TryCalculate(
        IReadOnlyList<GeometryAtom> atoms,
        int charge,
        out int electronCount,
        out string error)
    {
        electronCount = 0;
        error = string.Empty;

        if (atoms == null || atoms.Count == 0)
        {
            error = "原子列表不能为空。";
            return false;
        }

        int totalAtomicNumber = 0;

        for (int index = 0; index < atoms.Count; index++)
        {
            GeometryAtom atom = atoms[index];
            ChemicalElement element;

            if (!ElementCatalog.TryGetBySymbol(atom.Element, out element))
            {
                error = "无法识别元素符号：" + atom.Element;
                return false;
            }

            totalAtomicNumber += element.AtomicNumber;
        }

        electronCount = totalAtomicNumber - charge;

        if (electronCount < 0)
        {
            error = "总电荷使体系电子数小于 0。";
            return false;
        }

        return true;
    }
}
