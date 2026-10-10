namespace FakeFantasyOnline.Application
{
// The existing Buf cursor crosses the assembly boundary without copying packet or row data.
public interface ICombatEntityReader
{
    int Remaining { get; }
    int I32();
    int U8();
    string Str();
}

// Main-thread calls into the existing native registry. Returned identities are opaque
// observation handles; Protocol neither dereferences them nor chooses registry admission.
public interface ICombatRegistryReceiveOwner : IWorldReceiveContext
{
    object ReceiveRemotePlayer(WorldRemotePlayerState state);
    object ReceiveWorldEntity(WorldEntityState state);
    WorldEntityState ReceiveWorldEntity(ICombatEntityReader reader,
        IWorldReceiveContext context, out object identity);
    void ReceiveWorldRemoval(int id, byte reason, out bool trapPresent,
        out bool playerPresent, out bool monsterPresent);
    bool ReceiveRemotePk(int id, byte state);
    void ReceiveRemotePlayerLife(int id, bool alive);
    PlayerLifeReceiveResult ReceivePlayerLife(int id, bool alive, byte flags);
}
}
