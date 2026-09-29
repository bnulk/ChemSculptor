using System.Text.Json;

namespace ChemSculptor.Skills.Common;

/// <summary>技能节点之间使用的 JSON 序列化助手。</summary>
public static class SkillJson
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>序列化节点结果。</summary>
    public static string Serialize<T>(T value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>反序列化节点结果。</summary>
    public static T Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("节点输入 JSON 不能为空。", nameof(json));
        }

        T? value = JsonSerializer.Deserialize<T>(json, JsonOptions);

        if (value == null)
        {
            throw new InvalidOperationException("无法反序列化节点输入 JSON。");
        }

        return value;
    }
}
