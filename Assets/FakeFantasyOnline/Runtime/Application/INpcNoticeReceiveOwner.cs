namespace FakeFantasyOnline.Application
{

/// <summary>Host-thread delivery of the two native NPC/system notice case tails.</summary>
/// <remarks>Receives one packet at a time, before any world-stage FIFO or frame projection.</remarks>
public interface INpcNoticeReceiveOwner
{
    void ReceiveNpcNotice(string text, byte displayMode);
    void ReceiveInvitationCodeResult(WorldInvitationCodeResult result);
}

}
