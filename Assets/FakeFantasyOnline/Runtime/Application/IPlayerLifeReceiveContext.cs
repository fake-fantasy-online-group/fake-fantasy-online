namespace FakeFantasyOnline.Application
{

public readonly struct PlayerLifeReceiveResult
{
    public PlayerLifeReceiveResult(bool local, bool applied, int entityId)
    {
        Local = local;
        Applied = applied;
        EntityId = entityId;
    }
    public bool Local { get; }
    public bool Applied { get; }
    public int EntityId { get; }
}

public interface IPlayerLifeReceiveContext
{
    void BeginSelfData();
    void SetSelfEntityId(int entityId);
    PlayerLifeReceiveResult ReceivePlayerLife(int targetId, bool alive, byte flags);
}

// Records an already applied server-derived native state, never sends a game command.
public interface IAppliedPlayerLifeSink
{
    void RecordAppliedPlayerLife(int entityId, bool alive, byte flags);
}
}
