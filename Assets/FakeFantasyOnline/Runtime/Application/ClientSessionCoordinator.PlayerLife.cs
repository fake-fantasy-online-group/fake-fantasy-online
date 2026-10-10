namespace FakeFantasyOnline.Application
{

public sealed partial class ClientSessionCoordinator
{
    public void RecordAppliedPlayerLife(int entityId, bool alive, byte flags)
    {
        if (_gateway is IAppliedPlayerLifeSink sink)
            sink.RecordAppliedPlayerLife(entityId, alive, flags);
    }

}
}
