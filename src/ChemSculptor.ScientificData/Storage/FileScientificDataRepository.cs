using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Storage;

/// <summary>
/// 基于 JSON 文件的科学数据仓储。
/// 每项科学成果保存为一个独立文件。
/// </summary>
public sealed class FileScientificDataRepository
    : IScientificDataRepository
{
    private readonly ScientificDataRepositoryOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>创建文件仓储。</summary>
    public FileScientificDataRepository(
        ScientificDataRepositoryOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.RootDirectory))
        {
            throw new ArgumentException(
                "科学数据目录不能为空。",
                nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.FileExtension)
            || options.FileExtension.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException(
                "科学数据文件扩展名无效。",
                nameof(options));
        }

        _options = options;
        _jsonOptions = new JsonSerializerOptions(
            JsonSerializerDefaults.Web);
        _jsonOptions.WriteIndented = true;
        _jsonOptions.Converters.Add(
            new JsonStringEnumConverter());
    }

    /// <summary>保存一项科学成果。</summary>
    public async Task SaveAsync(
        ScientificResult result,
        CancellationToken cancellationToken = default)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        string path = ResolvePath(result.Id);
        string? directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        result.CompletedAt ??= DateTimeOffset.UtcNow;
        string json = JsonSerializer.Serialize(
            result,
            _jsonOptions);
        await File.WriteAllTextAsync(
            path,
            json,
            cancellationToken);
    }

    /// <summary>按标识读取一项科学成果。</summary>
    public async Task<ScientificResult?> GetAsync(
        string resultId,
        CancellationToken cancellationToken = default)
    {
        string path = ResolvePath(resultId);

        if (!File.Exists(path))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(
            path,
            cancellationToken);
        return JsonSerializer.Deserialize<ScientificResult>(
            json,
            _jsonOptions);
    }

    /// <summary>列出全部科学成果。</summary>
    public IReadOnlyList<ScientificResult> List()
    {
        List<ScientificResult> results =
            new List<ScientificResult>();

        if (!Directory.Exists(_options.RootDirectory))
        {
            return results;
        }

        string[] paths = Directory.GetFiles(
            _options.RootDirectory,
            "*" + _options.FileExtension,
            SearchOption.TopDirectoryOnly);
        Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < paths.Length; index++)
        {
            string json = File.ReadAllText(paths[index]);
            ScientificResult? result =
                JsonSerializer.Deserialize<ScientificResult>(
                    json,
                    _jsonOptions);

            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private string ResolvePath(string resultId)
    {
        if (string.IsNullOrWhiteSpace(resultId))
        {
            throw new ArgumentException(
                "科学成果标识不能为空。",
                nameof(resultId));
        }

        char[] invalidCharacters =
            Path.GetInvalidFileNameChars();

        if (resultId.IndexOfAny(invalidCharacters) >= 0
            || resultId.Contains(
                Path.DirectorySeparatorChar)
            || resultId.Contains(
                Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException(
                "科学成果标识包含无效的文件名字符。",
                nameof(resultId));
        }

        return Path.Combine(
            _options.RootDirectory,
            resultId + _options.FileExtension);
    }
}
