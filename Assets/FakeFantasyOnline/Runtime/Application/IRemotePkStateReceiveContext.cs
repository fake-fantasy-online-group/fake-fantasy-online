namespace FakeFantasyOnline.Application
{
/// <summary>Main-thread owner for the local branch of the 0x8053 remote-PK receive case.</summary>
public interface IRemotePkStateReceiveContext
{
    /// <summary>
    /// Applies the local arm of official case 0x8053. Returns true only when the entity id
    /// equals the local entity id, the case's own test; false asks the caller to publish the
    /// packet down the ordinary remote-actor path.
    /// </summary>
    bool ReceiveRemotePkState(int entityId, byte pkState);
}
}
