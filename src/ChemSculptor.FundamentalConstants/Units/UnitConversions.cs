using ChemSculptor.FundamentalConstants.Physics;

namespace ChemSculptor.FundamentalConstants.Units;

/// <summary>
/// 计算化学中常用单位之间的换算因子。
/// 换算因子由物理常数推导，不应当与物理常数混在同一个类中。
/// </summary>
public static class UnitConversions
{
    /// <summary>1 卡路里对应的焦耳数，约定值。</summary>
    public const double CalorieToJoule = 4.184;

    /// <summary>1 玻尔对应的米数。</summary>
    public const double BohrToMeters = PhysicalConstants.BohrRadius;

    /// <summary>1 玻尔对应的埃数。</summary>
    public const double BohrToAngstroms =
        PhysicalConstants.BohrRadius * 1.0e10;

    /// <summary>1 玻尔对应的厘米数。</summary>
    public const double BohrToCentimeters =
        PhysicalConstants.BohrRadius * 100.0;

    /// <summary>1 原子质量单位对应的千克数。</summary>
    public const double AtomicMassUnitToKilograms =
        PhysicalConstants.AtomicMassConstant;

    /// <summary>1 原子质量单位对应的克数。</summary>
    public const double AtomicMassUnitToGrams =
        PhysicalConstants.AtomicMassConstant * 1000.0;

    /// <summary>1 原子单位质量对应的原子质量单位数。</summary>
    public const double AtomicUnitOfMassToAtomicMassUnits =
        PhysicalConstants.ElectronMass
        / PhysicalConstants.AtomicMassConstant;

    /// <summary>1 哈特里对应的焦耳数。</summary>
    public const double HartreeToJoules =
        PhysicalConstants.HartreeEnergy;

    /// <summary>1 哈特里对应的电子伏特数。</summary>
    public const double HartreeToElectronVolts =
        PhysicalConstants.HartreeEnergy
        / PhysicalConstants.ElementaryCharge;

    /// <summary>1 哈特里对应的波数，单位 cm^-1。</summary>
    public const double HartreeToWavenumbers =
        PhysicalConstants.HartreeEnergy
        / (PhysicalConstants.PlanckConstant
            * PhysicalConstants.SpeedOfLightInVacuum
            * 100.0);

    /// <summary>1 哈特里对应的千卡每摩尔数。</summary>
    public const double HartreeToKilocaloriesPerMole =
        PhysicalConstants.HartreeEnergy
        * PhysicalConstants.AvogadroConstant
        / (CalorieToJoule * 1000.0);

    /// <summary>1 哈特里对应的千焦每摩尔数。</summary>
    public const double HartreeToKilojoulesPerMole =
        PhysicalConstants.HartreeEnergy
        * PhysicalConstants.AvogadroConstant
        / 1000.0;

    /// <summary>1 哈特里对应的兆赫兹数。</summary>
    public const double HartreeToMegahertz =
        PhysicalConstants.HartreeEnergy
        / (PhysicalConstants.PlanckConstant * 1.0e6);

    /// <summary>1 千卡每摩尔对应的波数，单位 cm^-1。</summary>
    public const double KilocaloriesPerMoleToWavenumbers =
        (CalorieToJoule * 1000.0
            / PhysicalConstants.AvogadroConstant)
        / (PhysicalConstants.PlanckConstant
            * PhysicalConstants.SpeedOfLightInVacuum
            * 100.0);

    /// <summary>1 原子单位偶极矩对应的库仑米数。</summary>
    public const double AtomicUnitOfDipoleMomentToCoulombMeters =
        PhysicalConstants.ElementaryCharge
        * PhysicalConstants.BohrRadius;

    /// <summary>1 德拜对应的库仑米数。</summary>
    public const double DebyeToCoulombMeters =
        1.0e-21 / PhysicalConstants.SpeedOfLightInVacuum;

    /// <summary>1 原子单位偶极矩对应的德拜数。</summary>
    public const double AtomicUnitOfDipoleMomentToDebye =
        AtomicUnitOfDipoleMomentToCoulombMeters
        / DebyeToCoulombMeters;
}
