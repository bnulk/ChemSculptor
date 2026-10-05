namespace ChemSculptor.Anomaly.Models;

/// <summary>电子态名称与自旋多重度的通用映射。</summary>
public static class SpinMultiplicityNames
{
    /// <summary>尝试把 Singlet、Doublet、Triplet 等名称转换为多重度。</summary>
    public static bool TryParse(
        string stateName,
        out int multiplicity)
    {
        multiplicity = 0;

        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        string name = stateName.Trim();
        int separatorIndex = name.IndexOf('-');

        if (separatorIndex > 0)
        {
            name = name.Substring(0, separatorIndex);
        }

        if (string.Equals(name, "Singlet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 1;
            return true;
        }

        if (string.Equals(name, "Doublet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 2;
            return true;
        }

        if (string.Equals(name, "Triplet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 3;
            return true;
        }

        if (string.Equals(name, "Quartet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 4;
            return true;
        }

        if (string.Equals(name, "Quintet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 5;
            return true;
        }

        if (string.Equals(name, "Sextet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 6;
            return true;
        }

        if (string.Equals(name, "Septet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 7;
            return true;
        }

        if (string.Equals(name, "Octet", StringComparison.OrdinalIgnoreCase))
        {
            multiplicity = 8;
            return true;
        }

        return false;
    }
}
