using System.Collections.ObjectModel;

namespace ChemSculptor.FundamentalConstants.Chemistry.Elements;

/// <summary>
/// 元素数据目录。
/// 当前保留旧程序覆盖范围：虚拟元素 0 和氢到氙。
/// </summary>
public static class ElementCatalog
{
    private static readonly ChemicalElement[] ElementArray =
    {
        new ChemicalElement(0, "X", "Dummy", 0.0),
        new ChemicalElement(1, "H", "Hydrogen", 1.00783),
        new ChemicalElement(2, "He", "Helium", 4.002602),
        new ChemicalElement(3, "Li", "Lithium", 6.94),
        new ChemicalElement(4, "Be", "Beryllium", 9.0121831),
        new ChemicalElement(5, "B", "Boron", 10.81),
        new ChemicalElement(6, "C", "Carbon", 12.000),
        new ChemicalElement(7, "N", "Nitrogen", 14.00307),
        new ChemicalElement(8, "O", "Oxygen", 15.99491),
        new ChemicalElement(9, "F", "Fluorine", 18.99840316),
        new ChemicalElement(10, "Ne", "Neon", 20.1797),
        new ChemicalElement(11, "Na", "Sodium", 22.98976928),
        new ChemicalElement(12, "Mg", "Magnesium", 24.305),
        new ChemicalElement(13, "Al", "Aluminum", 26.9815385),
        new ChemicalElement(14, "Si", "Silicon", 28.085),
        new ChemicalElement(15, "P", "Phosphorus", 30.97376199),
        new ChemicalElement(16, "S", "Sulfur", 32.06),
        new ChemicalElement(17, "Cl", "Chlorine", 35.45),
        new ChemicalElement(18, "Ar", "Argon", 39.948),
        new ChemicalElement(19, "K", "Potassium", 39.0983),
        new ChemicalElement(20, "Ca", "Calcium", 40.078),
        new ChemicalElement(21, "Sc", "Scandium", 44.955908),
        new ChemicalElement(22, "Ti", "Titanium", 47.867),
        new ChemicalElement(23, "V", "Vanadium", 50.9415),
        new ChemicalElement(24, "Cr", "Chromium", 51.9961),
        new ChemicalElement(25, "Mn", "Manganese", 54.938044),
        new ChemicalElement(26, "Fe", "Iron", 55.845),
        new ChemicalElement(27, "Co", "Cobalt", 58.933194),
        new ChemicalElement(28, "Ni", "Nickel", 58.6934),
        new ChemicalElement(29, "Cu", "Copper", 63.546),
        new ChemicalElement(30, "Zn", "Zinc", 65.38),
        new ChemicalElement(31, "Ga", "Gallium", 69.723),
        new ChemicalElement(32, "Ge", "Germanium", 72.630),
        new ChemicalElement(33, "As", "Arsenic", 74.921595),
        new ChemicalElement(34, "Se", "Selenium", 78.971),
        new ChemicalElement(35, "Br", "Bromine", 79.904),
        new ChemicalElement(36, "Kr", "Krypton", 83.798),
        new ChemicalElement(37, "Rb", "Rubidium", 85.4678),
        new ChemicalElement(38, "Sr", "Strontium", 87.62),
        new ChemicalElement(39, "Y", "Yttrium", 88.90584),
        new ChemicalElement(40, "Zr", "Zirconium", 91.224),
        new ChemicalElement(41, "Nb", "Niobium", 92.90637),
        new ChemicalElement(42, "Mo", "Molybdenum", 95.95),
        new ChemicalElement(43, "Tc", "Technetium", 98.0),
        new ChemicalElement(44, "Ru", "Ruthenium", 101.07),
        new ChemicalElement(45, "Rh", "Rhodium", 102.90550),
        new ChemicalElement(46, "Pd", "Palladium", 106.42),
        new ChemicalElement(47, "Ag", "Silver", 107.8682),
        new ChemicalElement(48, "Cd", "Cadmium", 112.414),
        new ChemicalElement(49, "In", "Indium", 114.818),
        new ChemicalElement(50, "Sn", "Tin", 118.710),
        new ChemicalElement(51, "Sb", "Antimony", 121.760),
        new ChemicalElement(52, "Te", "Tellurium", 127.60),
        new ChemicalElement(53, "I", "Iodine", 126.90447),
        new ChemicalElement(54, "Xe", "Xenon", 131.293)
    };

    private static readonly ReadOnlyCollection<ChemicalElement> ElementList =
        Array.AsReadOnly(ElementArray);

    private static readonly Dictionary<string, ChemicalElement> ElementsBySymbol =
        CreateSymbolIndex();

    private static readonly Dictionary<int, ChemicalElement> ElementsByNumber =
        CreateNumberIndex();

    /// <summary>按原子序数排列的元素只读列表。</summary>
    public static IReadOnlyList<ChemicalElement> All
    {
        get { return ElementList; }
    }

    /// <summary>尝试按元素符号查找元素，忽略大小写。</summary>
    public static bool TryGetBySymbol(
        string? symbol,
        out ChemicalElement element)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            element = default(ChemicalElement);
            return false;
        }

        return ElementsBySymbol.TryGetValue(symbol.Trim(), out element);
    }

    /// <summary>尝试按原子序数查找元素。</summary>
    public static bool TryGetByAtomicNumber(
        int atomicNumber,
        out ChemicalElement element)
    {
        return ElementsByNumber.TryGetValue(atomicNumber, out element);
    }

    private static Dictionary<string, ChemicalElement> CreateSymbolIndex()
    {
        Dictionary<string, ChemicalElement> index =
            new Dictionary<string, ChemicalElement>(
                StringComparer.OrdinalIgnoreCase);

        for (int position = 0; position < ElementArray.Length; position++)
        {
            ChemicalElement element = ElementArray[position];
            index.Add(element.Symbol, element);
        }

        return index;
    }

    private static Dictionary<int, ChemicalElement> CreateNumberIndex()
    {
        Dictionary<int, ChemicalElement> index =
            new Dictionary<int, ChemicalElement>();

        for (int position = 0; position < ElementArray.Length; position++)
        {
            ChemicalElement element = ElementArray[position];
            index.Add(element.AtomicNumber, element);
        }

        return index;
    }
}
