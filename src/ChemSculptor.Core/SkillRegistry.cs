using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 线程安全的技能注册表。
/// </summary>
public sealed class SkillRegistry : ISkillRegistry
{
    private readonly ConcurrentDictionary<string, ISkill> _skills =
        new ConcurrentDictionary<string, ISkill>(StringComparer.OrdinalIgnoreCase);

    /// <summary>注册或覆盖同名技能。</summary>
    public Task RegisterAsync(ISkill skill, CancellationToken cancellationToken = default)
    {
        _skills[skill.Name] = skill;
        return Task.CompletedTask;
    }

    /// <summary>按名称查找技能；找不到时返回 null。</summary>
    public ISkill? Resolve(string skillId)
    {
        ISkill? skill;
        if (_skills.TryGetValue(skillId, out skill))
        {
            return skill;
        }

        return null;
    }

    /// <summary>列出所有已注册技能的元信息。</summary>
    public IReadOnlyList<SkillDescriptor> List()
    {
        List<SkillDescriptor> descriptors = new List<SkillDescriptor>();

        foreach (ISkill skill in _skills.Values)
        {
            SkillDescriptor descriptor = new SkillDescriptor();
            descriptor.Id = skill.Name;
            descriptor.Version = skill.Version;
            descriptor.Capabilities = new List<string>(skill.Capabilities);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }
}
