using System;

namespace FakeFantasyOnline.PluginApi
{

public interface IPluginEvent
{
    string EventName { get; }
}

public sealed class PlayerEnteredWorldEvent : IPluginEvent
{
    public const string Name = "player.entered_world";

    public PlayerEnteredWorldEvent(PlayerSnapshot player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public string EventName => Name;

    public PlayerSnapshot Player { get; }
}

public sealed class PlayerSnapshotChangedEvent : IPluginEvent
{
    public const string Name = "player.snapshot_changed";

    public PlayerSnapshotChangedEvent(PlayerSnapshot player)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
    }

    public string EventName => Name;

    public PlayerSnapshot Player { get; }
}

public interface IPluginEventStream
{
    IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IPluginEvent;
}
}
