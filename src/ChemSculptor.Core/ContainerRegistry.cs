using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class ContainerRegistry : IContainerRegistry
{
    private readonly ConcurrentDictionary<string, ISkillContainer> _containers =
        new(StringComparer.OrdinalIgnoreCase);

    public Task RegisterAsync(ISkillContainer container, CancellationToken cancellationToken = default)
    {
        _containers[container.Name] = container;
        return Task.CompletedTask;
    }

    public ISkillContainer? Resolve(string containerId) =>
        _containers.TryGetValue(containerId, out var container) ? container : null;

    public IReadOnlyList<ContainerDescriptor> List() =>
        _containers.Values
            .Select(container => new ContainerDescriptor
            {
                Id = container.Name,
                Version = container.Version,
                Capabilities = container.Capabilities
            })
            .ToList();
}
