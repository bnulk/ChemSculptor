namespace ChemSculptor.InputProcessor.GeometryIntake;

/// <summary>
/// 客户端提交几何数据时的格式提示。
/// 客户端可以明确指定格式，也可以交给服务器自动选择解析器。
/// </summary>
public enum GeometryFormatHint
{
    /// <summary>由服务器自动判断格式。</summary>
    Auto,

    /// <summary>标准或简化 XYZ 坐标文本。</summary>
    Xyz,

    /// <summary>Gaussian 输入文件或日志。</summary>
    Gaussian,

    /// <summary>ORCA 输入文件或日志。</summary>
    Orca,

    /// <summary>带分层的 ONIOM 几何定义。</summary>
    Oniom,

    /// <summary>未知格式。</summary>
    Unknown
}

/// <summary>坐标单位。</summary>
public enum GeometryUnits
{
    /// <summary>尚未确定。</summary>
    Unknown,

    /// <summary>埃。</summary>
    Angstrom,

    /// <summary>玻尔半径。</summary>
    Bohr
}

/// <summary>诊断信息的严重程度。</summary>
public enum GeometryDiagnosticSeverity
{
    /// <summary>普通信息。</summary>
    Info,

    /// <summary>警告，可继续处理。</summary>
    Warning,

    /// <summary>错误，不能继续处理。</summary>
    Error
}

/// <summary>几何解析状态。</summary>
public enum GeometryParseStatus
{
    /// <summary>解析器框架已存在，但具体解析逻辑尚未实现。</summary>
    NotImplemented,

    /// <summary>解析成功。</summary>
    Succeeded,

    /// <summary>部分解析成功，需要人工检查诊断信息。</summary>
    PartiallySucceeded,

    /// <summary>解析失败。</summary>
    Failed
}

/// <summary>几何验证状态。</summary>
public enum GeometryValidationStatus
{
    /// <summary>验证器框架已存在，但具体规则尚未实现。</summary>
    NotImplemented,

    /// <summary>验证通过。</summary>
    Passed,

    /// <summary>验证通过但存在警告。</summary>
    PassedWithWarnings,

    /// <summary>验证失败。</summary>
    Failed
}

/// <summary>几何约束类型。</summary>
public enum GeometryConstraintKind
{
    /// <summary>距离约束。</summary>
    Distance,

    /// <summary>键角约束。</summary>
    Angle,

    /// <summary>二面角约束。</summary>
    Dihedral,

    /// <summary>固定原子或坐标。</summary>
    Fixed
}

/// <summary>
/// 客户端提交的原始几何文件。
/// 接收层只保存原始内容，不做格式解释。
/// </summary>
public sealed class RawGeometryFile
{
    /// <summary>原始文件名。</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>原始内容类型，例如 text/plain。</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>原始文本内容。</summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 一次几何提交。
/// 可以只包含一个文件，也可以为以后多文件提交预留多个文件。
/// </summary>
public sealed class RawGeometrySubmission
{
    /// <summary>本次提交的唯一标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>来源名称，例如上传文件名或用户描述。</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>客户端提供的格式提示。</summary>
    public GeometryFormatHint FormatHint { get; set; } = GeometryFormatHint.Auto;

    /// <summary>提交的原始文件列表。</summary>
    public List<RawGeometryFile> Files { get; set; } = new List<RawGeometryFile>();

    /// <summary>提交时间。</summary>
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 规范几何模型中的单个原子。
/// </summary>
public sealed class CanonicalAtom
{
    /// <summary>原子序号，从 1 开始。</summary>
    public int Index { get; set; }

    /// <summary>元素符号。</summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>X 坐标。</summary>
    public double X { get; set; }

    /// <summary>Y 坐标。</summary>
    public double Y { get; set; }

    /// <summary>Z 坐标。</summary>
    public double Z { get; set; }
}

/// <summary>
/// 几何片段，例如一个分子或一个层内片段。
/// </summary>
public sealed class GeometryFragment
{
    /// <summary>片段标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>片段名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>片段包含的原子序号。</summary>
    public List<int> AtomIndices { get; set; } = new List<int>();
}

/// <summary>
/// ONIOM 等分层模型中的一层。
/// </summary>
public sealed class GeometryLayer
{
    /// <summary>层标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>层名称，例如 high、medium、low。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>该层包含的原子序号。</summary>
    public List<int> AtomIndices { get; set; } = new List<int>();
}

/// <summary>
/// 分层模型中的链接原子。
/// </summary>
public sealed class LinkAtom
{
    /// <summary>链接原子标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>链接原子元素符号。</summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>X 坐标。</summary>
    public double X { get; set; }

    /// <summary>Y 坐标。</summary>
    public double Y { get; set; }

    /// <summary>Z 坐标。</summary>
    public double Z { get; set; }

    /// <summary>所属高层原子序号。</summary>
    public int HighAtomIndex { get; set; }

    /// <summary>所属低层原子序号。</summary>
    public int LowAtomIndex { get; set; }
}

/// <summary>
/// 几何约束。
/// </summary>
public sealed class GeometryConstraint
{
    /// <summary>约束标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>约束类型。</summary>
    public GeometryConstraintKind Kind { get; set; }

    /// <summary>参与约束的原子序号。</summary>
    public List<int> AtomIndices { get; set; } = new List<int>();

    /// <summary>约束目标值；固定约束可以为空。</summary>
    public double? Value { get; set; }
}

/// <summary>
/// 规范几何模型。
/// 所有解析器最终都应输出该模型，内核和技能容器只依赖它。
/// </summary>
public sealed class CanonicalGeometry
{
    /// <summary>坐标单位。</summary>
    public GeometryUnits Units { get; set; } = GeometryUnits.Unknown;

    /// <summary>原子列表。</summary>
    public List<CanonicalAtom> Atoms { get; set; } = new List<CanonicalAtom>();

    /// <summary>片段列表。</summary>
    public List<GeometryFragment> Fragments { get; set; } = new List<GeometryFragment>();

    /// <summary>分层列表，用于 ONIOM 等模型。</summary>
    public List<GeometryLayer> Layers { get; set; } = new List<GeometryLayer>();

    /// <summary>链接原子列表。</summary>
    public List<LinkAtom> LinkAtoms { get; set; } = new List<LinkAtom>();

    /// <summary>几何约束列表。</summary>
    public List<GeometryConstraint> Constraints { get; set; } = new List<GeometryConstraint>();

    /// <summary>总电荷；未提供时为 null。</summary>
    public int? Charge { get; set; }

    /// <summary>自旋多重度；未提供时为 null。</summary>
    public int? Multiplicity { get; set; }

    /// <summary>来源提交标识。</summary>
    public string SourceSubmissionId { get; set; } = string.Empty;
}

/// <summary>
/// 一条几何解析或验证诊断。
/// </summary>
public sealed class GeometryDiagnostic
{
    /// <summary>严重程度。</summary>
    public GeometryDiagnosticSeverity Severity { get; set; } = GeometryDiagnosticSeverity.Info;

    /// <summary>诊断代码，便于程序识别。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>面向人的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>来源文件名；不适用时为空。</summary>
    public string? FileName { get; set; }

    /// <summary>来源行号；不适用时为空。</summary>
    public int? LineNumber { get; set; }
}
