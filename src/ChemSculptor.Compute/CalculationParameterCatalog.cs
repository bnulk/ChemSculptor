namespace ChemSculptor.Compute;

/// <summary>规范计算参数目录。</summary>
public static class CalculationParameterCatalog
{
    private static readonly List<CalculationParameterDefinition> Definitions =
        CreateDefinitions();

    /// <summary>列出所有规范参数定义。</summary>
    public static IReadOnlyList<CalculationParameterDefinition> List()
    {
        return Definitions;
    }

    /// <summary>按规范键查找参数定义；找不到时返回 null。</summary>
    public static CalculationParameterDefinition? Find(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        for (int index = 0; index < Definitions.Count; index++)
        {
            CalculationParameterDefinition definition =
                Definitions[index];

            if (string.Equals(
                definition.Key,
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                return definition;
            }
        }

        return null;
    }

    private static List<CalculationParameterDefinition>
        CreateDefinitions()
    {
        List<CalculationParameterDefinition> definitions =
            new List<CalculationParameterDefinition>();

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Engine,
            "计算引擎",
            CalculationParameterValueKind.Enum,
            new string[] { "Gaussian 16" },
            "Gaussian 16",
            CalculationRiskLevel.Info,
            new string[]
            {
                "Gaussian",
                "Gaussian 16",
                "g16",
                "高斯"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Method,
            "计算方法",
            CalculationParameterValueKind.String,
            new string[] { "CAM-B3LYP" },
            "CAM-B3LYP",
            CalculationRiskLevel.Info,
            new string[]
            {
                "CAM-B3LYP",
                "cam-b3lyp",
                "CAMB3LYP",
                "CAM B3LYP"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Basis,
            "计算基组",
            CalculationParameterValueKind.String,
            new string[] { "6-31G*" },
            "6-31G*",
            CalculationRiskLevel.Info,
            new string[]
            {
                "6-31G*",
                "6-31g*"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.DispersionCorrection,
            "色散校正",
            CalculationParameterValueKind.Enum,
            new string[] { "None", "GD3", "GD3BJ" },
            "GD3BJ",
            CalculationRiskLevel.Info,
            new string[]
            {
                "em=GD3BJ",
                "GD3BJ",
                "gd3bj",
                "D3BJ",
                "d3bj",
                "色散",
                "色散校正",
                "加色散",
                "经验色散",
                "em=GD3",
                "GD3",
                "gd3",
                "D3",
                "d3",
                "关闭色散",
                "不加色散",
                "不考虑色散",
                "no dispersion",
                "none"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Symmetry,
            "对称性模式",
            CalculationParameterValueKind.Enum,
            new string[] { "Default", "Use", "Ignore" },
            "Default",
            CalculationRiskLevel.Warning,
            new string[]
            {
                "默认对称性",
                "程序默认对称性",
                "对称性",
                "symm",
                "symmetry",
                "使用对称性",
                "保持对称性",
                "开启对称性",
                "nosymm",
                "no symmetry",
                "不使用对称性",
                "关闭对称性",
                "不要对称化"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.RoutePrintLevel,
            "Route 输出信息量",
            CalculationParameterValueKind.Enum,
            new string[] { "Default", "Terse", "Normal", "Verbose" },
            "Verbose",
            CalculationRiskLevel.Info,
            new string[]
            {
                "#",
                "#t",
                "#n",
                "#p",
                "terse",
                "normal",
                "verbose",
                "信息量少",
                "信息量中等",
                "信息量多",
                "简短输出",
                "普通输出",
                "详细输出"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.ScfIterationLimit,
            "SCF 迭代上限",
            CalculationParameterValueKind.Integer,
            new string[0],
            "200",
            CalculationRiskLevel.Info,
            new string[]
            {
                "scfcyc",
                "SCF 最多",
                "SCF 最大",
                "SCF 迭代上限",
                "最多迭代",
                "SCF cycles",
                "SCF"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Solvent,
            "溶剂",
            CalculationParameterValueKind.Enum,
            new string[] { "None", "Water" },
            "None",
            CalculationRiskLevel.Info,
            new string[]
            {
                "气相",
                "无溶剂",
                "不加溶剂",
                "溶剂",
                "gas",
                "no solvent",
                "水",
                "水中",
                "水溶剂",
                "water",
                "aqueous"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Charge,
            "总电荷",
            CalculationParameterValueKind.Integer,
            new string[0],
            "0",
            CalculationRiskLevel.Warning,
            new string[]
            {
                "charge",
                "电荷",
                "总电荷"
            }));

        definitions.Add(CreateDefinition(
            CalculationOptionKeys.Multiplicity,
            "自旋多重度",
            CalculationParameterValueKind.Integer,
            new string[0],
            string.Empty,
            CalculationRiskLevel.Blocking,
            new string[]
            {
                "multiplicity",
                "多重度",
                "自旋多重度",
                "singlet",
                "单重态",
                "doublet",
                "二重态",
                "triplet",
                "三重态",
                "quartet",
                "四重态",
                "quintet",
                "五重态",
                "sextet",
                "六重态",
                "septet",
                "七重态",
                "octet",
                "八重态"
            }));

        return definitions;
    }

    private static CalculationParameterDefinition CreateDefinition(
        string key,
        string displayName,
        CalculationParameterValueKind valueKind,
        string[] allowedValues,
        string defaultValue,
        CalculationRiskLevel riskLevel,
        string[] aliases)
    {
        CalculationParameterDefinition definition =
            new CalculationParameterDefinition();
        definition.Key = key;
        definition.DisplayName = displayName;
        definition.ValueKind = valueKind;
        definition.AllowedValues =
            new List<string>(allowedValues);
        definition.DefaultValue = defaultValue;
        definition.RiskLevel = riskLevel;
        definition.RequiresApproval = false;
        definition.Aliases = new List<string>(aliases);
        definition.TaskTypes = new List<CalculationTaskType>();
        definition.TaskTypes.Add(CalculationTaskType.SinglePoint);
        definition.Programs = new List<string>();
        definition.Programs.Add(CalculationDefaults.DefaultProgram);
        return definition;
    }
}
