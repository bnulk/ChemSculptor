namespace ChemSculptor.ScientificData.Storage;

/// <summary>科学数据文件仓储配置。</summary>
public sealed class ScientificDataRepositoryOptions
{
    /// <summary>科学数据文件目录。</summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>科学数据文件扩展名。</summary>
    public string FileExtension { get; set; } = ".json";
}
