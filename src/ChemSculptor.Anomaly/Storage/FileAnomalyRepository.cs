using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Storage;

/// <summary>
/// 基于文件的异常处理记录仓储。
/// 每条记录写入作业 results/anomaly 目录下的独立 JSON 文件。
/// </summary>
public sealed class FileAnomalyRepository : IAnomalyRepository
{
    private readonly ICalculationWorkspace _workspace;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>创建文件异常仓储。</summary>
    public FileAnomalyRepository(ICalculationWorkspace workspace)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        _workspace = workspace;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.WriteIndented = true;
        _jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>保存异常处理记录。</summary>
    public async Task SaveAsync(
        AnomalyRecord record,
        CancellationToken cancellationToken = default)
    {
        if (record == null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        string path = AnomalyStoragePaths.GetRecordPath(
            _workspace,
            record.JobId,
            record.Id);
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        record.UpdatedAt = DateTimeOffset.UtcNow;
        string json = JsonSerializer.Serialize(record, _jsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    /// <summary>读取一条异常处理记录。</summary>
    public async Task<AnomalyRecord?> GetAsync(
        string jobId,
        string recordId,
        CancellationToken cancellationToken = default)
    {
        string path = AnomalyStoragePaths.GetRecordPath(
            _workspace,
            jobId,
            recordId);

        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(
            path,
            cancellationToken);

        return JsonSerializer.Deserialize<AnomalyRecord>(
            json,
            _jsonOptions);
    }

    /// <summary>列出指定作业的全部异常处理记录。</summary>
    public async Task<IReadOnlyList<AnomalyRecord>> ListByJobAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        List<AnomalyRecord> records = new List<AnomalyRecord>();
        string directory = AnomalyStoragePaths.GetDirectory(
            _workspace,
            jobId);

        if (!Directory.Exists(directory))
        {
            return records;
        }

        string[] paths = Directory.GetFiles(
            directory,
            "*" + AnomalyStoragePaths.FileExtension,
            SearchOption.TopDirectoryOnly);
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < paths.Length; index++)
        {
            string json = await File.ReadAllTextAsync(
                paths[index],
                cancellationToken);
            AnomalyRecord? record =
                JsonSerializer.Deserialize<AnomalyRecord>(
                    json,
                    _jsonOptions);

            if (record != null)
            {
                records.Add(record);
            }
        }

        return records;
    }
}
