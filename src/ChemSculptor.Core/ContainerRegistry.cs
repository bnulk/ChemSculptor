using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 线程安全的技能容器注册表。
/// </summary>
public sealed class ContainerRegistry : IContainerRegistry
{
    private readonly ConcurrentDictionary<string, ISkillContainer> _containers =
        new ConcurrentDictionary<string, ISkillContainer>(StringComparer.OrdinalIgnoreCase);

    /// <summary>注册或覆盖同名技能容器。</summary>
    public Task RegisterAsync(ISkillContainer container, CancellationToken cancellationToken = default)
    {
        _containers[container.Name] = container;
        return Task.CompletedTask;
    }

    /// <summary>按名称查找技能容器；找不到时返回 null。</summary>
    public ISkillContainer? Resolve(string containerId)
    {
        ISkillContainer? container;
        if (_containers.TryGetValue(containerId, out container))
        {
            return container;
        }

        return null;
    }

    /// <summary>列出所有已注册容器的元信息。</summary>
    public IReadOnlyList<ContainerDescriptor> List()
    {
        List<ContainerDescriptor> descriptors = new List<ContainerDescriptor>();

        foreach (ISkillContainer container in _containers.Values)
        {
            ContainerDescriptor descriptor = new ContainerDescriptor();
            descriptor.Id = container.Name;
            descriptor.Version = container.Version;
            descriptor.Capabilities = new List<string>(container.Capabilities);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }
}
