using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChemSculptor.Reporting;

/// <summary>
/// 报告契约的统一 JSON 序列化入口。
/// 使用固定选项，避免不同调用方生成不兼容的报告文件。
/// </summary>
public static class ReportContractSerializer
{
    /// <summary>把报告契约对象序列化为缩进 JSON。</summary>
    public static string Serialize<T>(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return JsonSerializer.Serialize(value, CreateOptions());
    }

    /// <summary>从 JSON 还原报告契约对象。</summary>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON 不能为空。", nameof(json));
        }

        return JsonSerializer.Deserialize<T>(json, CreateOptions());
    }

    /// <summary>创建报告契约的固定 JSON 选项。</summary>
    public static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.WriteIndented = true;
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
