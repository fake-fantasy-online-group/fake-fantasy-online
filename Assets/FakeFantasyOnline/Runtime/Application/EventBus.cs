using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{

public interface IApplicationEvent
{
}

public interface IEventBus
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IApplicationEvent;

    void Publish<TEvent>(TEvent message)
        where TEvent : IApplicationEvent;
}

public sealed class InProcessEventBus : IEventBus
{
    private readonly object _gate = new object();
    private readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IApplicationEvent
    {
        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        lock (_gate)
        {
            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                handlers = new List<Delegate>();
                _handlers.Add(eventType, handlers);
            }

            handlers.Add(handler);
        }

        return new Subscription(() => Unsubscribe(handler));
    }

    public void Publish<TEvent>(TEvent message)
        where TEvent : IApplicationEvent
    {
        Delegate[] snapshot;
        lock (_gate)
        {
            if (!_handlers.TryGetValue(typeof(TEvent), out List<Delegate> handlers))
            {
                return;
            }

            snapshot = handlers.ToArray();
        }

        foreach (Delegate handler in snapshot)
        {
            ((Action<TEvent>)handler)(message);
        }
    }

    private void Unsubscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IApplicationEvent
    {
        lock (_gate)
        {
            Type eventType = typeof(TEvent);
            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _handlers.Remove(eventType);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private Action _dispose;

        public Subscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            Action dispose = _dispose;
            _dispose = null;
            dispose?.Invoke();
        }
    }
}
}
