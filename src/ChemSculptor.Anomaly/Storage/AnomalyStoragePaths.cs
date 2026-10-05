using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Storage;

/// <summary>异常处理记录的存储路径。</summary>
public static class AnomalyStoragePaths
{
    /// <summary>异常目录名称。</summary>
    public const string DirectoryName = "anomaly";

    /// <summary>异常记录文件扩展名。</summary>
    public const string FileExtension = ".json";

    /// <summary>获取作业的异常处理目录。</summary>
    public static string GetDirectory(
        ICalculationWorkspace workspace,
        string jobId)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        return Path.Combine(
            workspace.GetResultDirectory(jobId),
            DirectoryName);
    }

    /// <summary>获取一条异常处理记录的文件路径。</summary>
    public static string GetRecordPath(
        ICalculationWorkspace workspace,
        string jobId,
        string recordId)
    {
        string safeRecordId = ValidateIdentifier(
            recordId,
            nameof(recordId));

        return Path.Combine(
            GetDirectory(workspace, jobId),
            safeRecordId + FileExtension);
    }

    private static string ValidateIdentifier(
        string identifier,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                "异常记录标识不能为空。",
                parameterName);
        }

        for (int index = 0; index < identifier.Length; index++)
        {
            char character = identifier[index];
            bool allowed = char.IsLetterOrDigit(character)
                || character == '-'
                || character == '_';

            if (!allowed)
            {
                throw new ArgumentException(
                    "异常记录标识只能包含字母、数字、连字符和下划线。",
                    parameterName);
            }
        }

        return identifier;
    }
}
