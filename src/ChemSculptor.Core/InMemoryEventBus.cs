using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Guid, Func<WorkflowEvent, CancellationToken, Task>> _handlers =
        new Dictionary<Guid, Func<WorkflowEvent, CancellationToken, Task>>();

    private readonly Lock _gate = new Lock();

    public async Task PublishAsync(WorkflowEvent eventData, CancellationToken cancellationToken = default)
    {
        Func<WorkflowEvent, CancellationToken, Task>[] handlers;

        lock (_gate)
        {
            handlers = new Func<WorkflowEvent, CancellationToken, Task>[_handlers.Count];
            _handlers.Values.CopyTo(handlers, 0);
        }

        for (int index = 0; index < handlers.Length; index++)
        {
            await handlers[index](eventData, cancellationToken);
        }
    }

    public IDisposable Subscribe(Func<WorkflowEvent, CancellationToken, Task> handler)
    {
        Guid id = Guid.NewGuid();

        lock (_gate)
        {
            _handlers.Add(id, handler);
        }

        return new HandlerRegistration(this, id);
    }

    private void RemoveHandler(Guid id)
    {
        lock (_gate)
        {
            _handlers.Remove(id);
        }
    }

    private sealed class HandlerRegistration : IDisposable
    {
        private InMemoryEventBus? _owner;
        private readonly Guid _id;

        public HandlerRegistration(InMemoryEventBus owner, Guid id)
        {
            _owner = owner;
            _id = id;
        }

        public void Dispose()
        {
            InMemoryEventBus? owner = Interlocked.Exchange(ref _owner, null);
            if (owner != null)
            {
                owner.RemoveHandler(_id);
            }
        }
    }
}
