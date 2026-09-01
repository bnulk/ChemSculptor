using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Guid, Func<WorkflowEvent, CancellationToken, Task>> _handlers = [];
    private readonly Lock _gate = new();

    public async Task PublishAsync(WorkflowEvent @event, CancellationToken cancellationToken = default)
    {
        Func<WorkflowEvent, CancellationToken, Task>[] handlers;
        lock (_gate)
        {
            handlers = _handlers.Values.ToArray();
        }

        await Task.WhenAll(handlers.Select(handler => handler(@event, cancellationToken)));
    }

    public IDisposable Subscribe(Func<WorkflowEvent, CancellationToken, Task> handler)
    {
        var id = Guid.NewGuid();
        lock (_gate)
        {
            _handlers[id] = handler;
        }

        return new Lease(() =>
        {
            lock (_gate)
            {
                _handlers.Remove(id);
            }
        });
    }

    private sealed class Lease(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
