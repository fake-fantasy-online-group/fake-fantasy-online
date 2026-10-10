namespace FakeFantasyOnline.Application
{
/// <summary>Main-thread owner for one received 0x8052 self-PK packet.</summary>
public interface ISelfPkStateReceiveContext
{
    /// <summary>
    /// Projects one self PK receipt exactly once, in the order the packets were received.
    /// The receipt instance is the very one the gateway published, so a caller that later
    /// reprojects the published snapshot must recognize it and must not apply it twice.
    /// </summary>
    void ReceiveSelfPkState(WorldSelfPkState state);
}
}
