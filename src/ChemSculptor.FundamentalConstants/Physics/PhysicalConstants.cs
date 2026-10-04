namespace ChemSculptor.FundamentalConstants.Physics;

/// <summary>
/// 独立于具体分子和计算任务的物理常数。
/// 精确的 SI 定义值来自当前国际单位制；
/// 其他值采用 CODATA 2022 推荐值。
/// </summary>
public static class PhysicalConstants
{
    /// <summary>圆周率。</summary>
    public const double Pi = Math.PI;

    /// <summary>真空光速，单位 m/s，精确值。</summary>
    public const double SpeedOfLightInVacuum = 299792458.0;

    /// <summary>普朗克常数，单位 J s，精确值。</summary>
    public const double PlanckConstant = 6.62607015e-34;

    /// <summary>约化普朗克常数，单位 J s。</summary>
    public const double ReducedPlanckConstant =
        PlanckConstant / (2.0 * Pi);

    /// <summary>玻尔兹曼常数，单位 J/K，精确值。</summary>
    public const double BoltzmannConstant = 1.380649e-23;

    /// <summary>阿伏伽德罗常数，单位 1/mol，精确值。</summary>
    public const double AvogadroConstant = 6.02214076e23;

    /// <summary>摩尔气体常数，单位 J/(mol K)。</summary>
    public const double MolarGasConstant =
        AvogadroConstant * BoltzmannConstant;

    /// <summary>元电荷，单位 C，精确值。</summary>
    public const double ElementaryCharge = 1.602176634e-19;

    /// <summary>电子静止质量，单位 kg，CODATA 2022。</summary>
    public const double ElectronMass = 9.1093837139e-31;

    /// <summary>原子质量常数，单位 kg，CODATA 2022。</summary>
    public const double AtomicMassConstant = 1.66053906892e-27;

    /// <summary>玻尔半径，单位 m，CODATA 2022。</summary>
    public const double BohrRadius = 5.29177210544e-11;

    /// <summary>哈特里能量，单位 J，CODATA 2022。</summary>
    public const double HartreeEnergy = 4.3597447222060e-18;

    /// <summary>精细结构常数，CODATA 2022。</summary>
    public const double FineStructureConstant = 7.2973525643e-3;

    /// <summary>真空电容率，单位 F/m。</summary>
    public const double VacuumElectricPermittivity = 8.8541878188e-12;

    /// <summary>真空磁导率，单位 H/m。</summary>
    public const double VacuumMagneticPermeability =
        1.0 / (VacuumElectricPermittivity
            * SpeedOfLightInVacuum
            * SpeedOfLightInVacuum);

    /// <summary>原子单位中的真空光速。</summary>
    public const double SpeedOfLightInAtomicUnits =
        1.0 / FineStructureConstant;
}
