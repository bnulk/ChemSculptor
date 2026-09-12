using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 进程内事件总线实现。
/// 发布者与订阅者在同一个进程内通过事件解耦。
/// </summary>
public sealed class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Guid, Func<WorkflowEvent, CancellationToken, Task>> _handlers =
        new Dictionary<Guid, Func<WorkflowEvent, CancellationToken, Task>>();

    private readonly Lock _gate = new Lock();

    /// <summary>发布事件给当前所有订阅者。</summary>
    public async Task PublishAsync(WorkflowEvent eventData, CancellationToken cancellationToken = default)
    {
        Func<WorkflowEvent, CancellationToken, Task>[] handlers;

        // 在锁内复制订阅者列表，避免遍历时集合被修改。
        lock (_gate)
        {
            handlers = new Func<WorkflowEvent, CancellationToken, Task>[_handlers.Count];
            _handlers.Values.CopyTo(handlers, 0);
        }

        // 在锁外依次等待处理器，避免慢处理器长时间占住锁。
        for (int index = 0; index < handlers.Length; index++)
        {
            await handlers[index](eventData, cancellationToken);
        }
    }

    /// <summary>注册事件处理器，返回可用于退订的对象。</summary>
    public IDisposable Subscribe(Func<WorkflowEvent, CancellationToken, Task> handler)
    {
        Guid id = Guid.NewGuid();

        lock (_gate)
        {
            _handlers.Add(id, handler);
        }

        return new HandlerRegistration(this, id);
    }

    /// <summary>移除指定订阅者。</summary>
    private void RemoveHandler(Guid id)
    {
        lock (_gate)
        {
            _handlers.Remove(id);
        }
    }

    /// <summary>
    /// 订阅凭证。
    /// 调用 Dispose 时移除订阅，且重复调用只会生效一次。
    /// </summary>
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
