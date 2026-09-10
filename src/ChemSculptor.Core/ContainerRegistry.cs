using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class ContainerRegistry : IContainerRegistry
{
    private readonly ConcurrentDictionary<string, ISkillContainer> _containers =
        new ConcurrentDictionary<string, ISkillContainer>(StringComparer.OrdinalIgnoreCase);

    public Task RegisterAsync(ISkillContainer container, CancellationToken cancellationToken = default)
    {
        _containers[container.Name] = container;
        return Task.CompletedTask;
    }

    public ISkillContainer? Resolve(string containerId)
    {
        ISkillContainer? container;
        if (_containers.TryGetValue(containerId, out container))
        {
            return container;
        }

        return null;
    }

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
