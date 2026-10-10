using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.PluginApi
{

/// <summary>A transport-neutral API. Implementations are called on the client main thread.</summary>
public interface IGameApi
{
    GameSnapshot Observe(GameQuery query);
    IReadOnlyList<GameActionDefinition> GetActions();
    GameActionReceipt Execute(GameActionRequest request);
    GameActionReceipt GetAction(string actionId);
    bool CancelAction(string actionId);
    GameEventBatch ReadEvents(long afterSequence, int maxCount);
}

/// <summary>Optional self-description of observation domains and their accepted filters.</summary>
public interface IGameObservationCatalog
{
    IReadOnlyList<GameQueryDefinition> GetQueries();
}

public sealed class GameQueryDefinition
{
    public GameQueryDefinition(string domain, string description, IReadOnlyDictionary<string, object> args = null)
    {
        Domain = GameApiData.RequireName(domain, nameof(domain));
        Description = GameApiData.CheckText(description, nameof(description)) ?? string.Empty;
        Arguments = GameApiData.CopyObject(args);
    }
    public string Domain { get; }
    public string Description { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
}

public sealed class GameQuery
{
    public GameQuery(string domain, IReadOnlyDictionary<string, object> args = null)
    {
        Domain = GameApiData.RequireName(domain, nameof(domain));
        Arguments = GameApiData.CopyObject(args);
    }
    public string Domain { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
}

public sealed class GameSnapshot
{
    public GameSnapshot(string domain, long sceneRevision, long revision, IReadOnlyDictionary<string, object> data)
    {
        Domain = GameApiData.RequireName(domain, nameof(domain));
        SceneRevision = sceneRevision;
        Revision = revision;
        Data = GameApiData.CopyObject(data);
    }
    public string Domain { get; }
    public long SceneRevision { get; }
    public long Revision { get; }
    public IReadOnlyDictionary<string, object> Data { get; }
}

public sealed class GameActionRequest
{
    public GameActionRequest(string name, IReadOnlyDictionary<string, object> args = null,
        string requestId = null, long? expectedSceneRevision = null)
    {
        Name = GameApiData.RequireName(name, nameof(name));
        Arguments = GameApiData.CopyObject(args);
        RequestId = GameApiData.CheckText(requestId, nameof(requestId));
        ExpectedSceneRevision = expectedSceneRevision;
    }
    public string Name { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
    public string RequestId { get; }
    public long? ExpectedSceneRevision { get; }
}

/// <summary>Sent is transport submission, never an assertion of server acceptance.</summary>
public enum GameActionState { Queued, Sent, Running, Completed, Rejected, Cancelled, TimedOut, Unknown }

public sealed class GameActionReceipt
{
    public GameActionReceipt(string actionId, string requestId, string name, GameActionState state,
        string code, string message, IReadOnlyDictionary<string, object> data = null)
    {
        ActionId = GameApiData.CheckText(actionId, nameof(actionId));
        RequestId = GameApiData.CheckText(requestId, nameof(requestId));
        Name = GameApiData.CheckText(name, nameof(name)) ?? string.Empty;
        if (!Enum.IsDefined(typeof(GameActionState), state)) throw new ArgumentOutOfRangeException(nameof(state));
        State = state;
        Code = GameApiData.CheckText(code, nameof(code)) ?? string.Empty;
        Message = GameApiData.CheckText(message, nameof(message)) ?? string.Empty;
        Data = GameApiData.CopyObject(data);
    }
    public string ActionId { get; }
    public string RequestId { get; }
    public string Name { get; }
    public GameActionState State { get; }
    public string Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, object> Data { get; }
}

public sealed class GameActionDefinition
{
    public GameActionDefinition(string name, string description, IReadOnlyDictionary<string, object> args = null,
        bool available = true, string unavailableReason = null)
    {
        Name = GameApiData.RequireName(name, nameof(name));
        Description = GameApiData.CheckText(description, nameof(description)) ?? string.Empty;
        Arguments = GameApiData.CopyObject(args);
        Available = available;
        UnavailableReason = GameApiData.CheckText(unavailableReason, nameof(unavailableReason));
    }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyDictionary<string, object> Arguments { get; }
    public bool Available { get; }
    public string UnavailableReason { get; }
}

public sealed class GameEvent
{
    public GameEvent(long sequence, long sceneRevision, string name, IReadOnlyDictionary<string, object> data)
    {
        Sequence = sequence;
        SceneRevision = sceneRevision;
        Name = GameApiData.RequireName(name, nameof(name));
        Data = GameApiData.CopyObject(data);
    }
    public long Sequence { get; }
    public long SceneRevision { get; }
    public string Name { get; }
    public IReadOnlyDictionary<string, object> Data { get; }
}

public sealed class GameEventBatch
{
    public GameEventBatch(IReadOnlyList<GameEvent> events, long nextSequence, bool resyncRequired)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        if (events.Count > GameApiData.MaxNodes) throw new ArgumentException("Event batch exceeds the item limit.", nameof(events));
        var copy = new List<GameEvent>(events.Count);
        foreach (var item in events)
        {
            if (item == null) throw new ArgumentException("Event batch contains a null event.", nameof(events));
            copy.Add(item);
        }
        Events = new ReadOnlyCollection<GameEvent>(copy);
        NextSequence = nextSequence;
        ResyncRequired = resyncRequired;
    }
    public IReadOnlyList<GameEvent> Events { get; }
    public long NextSequence { get; }
    public bool ResyncRequired { get; }
}
}
