namespace FakeFantasyOnline.Application
{
// One 0x8052 native store followed by an observation of the already-written roots.
public interface ISelfPkReceiptOwner
{
    WorldSelfPkState StoreSelfPkMode(byte value);
    WorldSelfPkState StoreSelfPkKarma(long value);
    WorldSelfPkState StoreSelfPkState(byte value);
    void SynchronizeSelfPkMode();
    WorldSelfPkState StoreSelfPkMapSafe(byte value);
    WorldSelfPkState StoreSelfPkProtectionLevel(int value);
    WorldSelfPkState StoreSelfPkMapFree(byte value);
}
}
