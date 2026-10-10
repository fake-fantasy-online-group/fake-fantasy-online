using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ISelfPkReceiptOwner
{
    public WorldSelfPkState StoreSelfPkMode(byte value)
    {
        CharData.pkMode = value;
        return CharData.RetainSelfPkObservationForIndependentRuntime();
    }

    public WorldSelfPkState StoreSelfPkKarma(long value)
    {
        CharData.karma = value;
        return CharData.RetainSelfPkObservationForIndependentRuntime();
    }

    public WorldSelfPkState StoreSelfPkState(byte value)
    {
        CharData.pkState = value;
        return CharData.RetainSelfPkObservationForIndependentRuntime();
    }

    public void SynchronizeSelfPkMode() => SysConfigDlg.SyncPkFromServer(CharData.pkMode);

    public WorldSelfPkState StoreSelfPkMapSafe(byte value)
    {
        CharData.pkMapSafe = value;
        return CharData.RetainSelfPkObservationForIndependentRuntime();
    }

    public WorldSelfPkState StoreSelfPkProtectionLevel(int value)
    {
        CharData.pkProtectLv = value;
        return CharData.RetainSelfPkObservationForIndependentRuntime();
    }

    public WorldSelfPkState StoreSelfPkMapFree(byte value)
    {
        CharData.pkMapFree = value;
        return CharData.RetainSelfPkObservationForIndependentRuntime();
    }
}
}
