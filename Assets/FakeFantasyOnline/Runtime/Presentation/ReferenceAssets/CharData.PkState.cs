using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    internal static int pkMode;
    internal static long karma;
    internal static byte pkState;
    internal static byte pkMapSafe;
    internal static byte pkMapFree;
    private static WorldSelfPkState boundSelfPk;

    // Map the Application snapshot to the original fields, retaining the original defaults
    // until this session receives its first self PK packet.
    internal static void BindPkStateForIndependentRuntime(WorldSelfPkState value)
    {
        if (value == null)
        {
            pkMode = 0;
            karma = 0L;
            pkState = 0;
            boundSelfPk = null;
            // 20 is not a guessed default: official CharData..cctor (0x06000f2d, arm64 0x395b74 /
            // x86_64 0x3a8ea0) stores 0x14 into the int32 slot +0xec that the frozen atlas resolves to
            // pkProtectLv (0x04001500), and stores 1 into the level slot +0x20. FreeMapHostile
            // and OpenWorldHostile compare against this value, so the reset above restores the
            // official static-initial value rather than inventing one.
            pkMapSafe = 0;
            pkProtectLv = 20;
            pkMapFree = 0;
            return;
        }

        // The receive host projects every 0x8052 receipt once, in the packet order NetClient.Update
        // dispatches them; the per-frame world projection afterwards hands back the same published
        // instance. Official 0x8052 writes these statics once per packet and nothing else rewrites
        // them, so reprojecting an already projected receipt must neither re-run the mode
        // synchronization nor push its stored values back over a later server write such as the
        // 0x8053 self byte. Instance identity is therefore only the replay guard, never a value
        // comparison: two distinct receipts with equal fields still synchronize twice, exactly as
        // two official 0x8052 packets do.
        if (ReferenceEquals(value, boundSelfPk))
        {
            return;
        }

        pkMode = value.Mode;
        karma = value.Karma;
        pkState = value.State;
        // A new immutable receipt must synchronize even when its mode equals the old one.
        // Official 0x8052 calls this after storing pkState and before storing pkMapSafe.
        boundSelfPk = value;
        SysConfigDlg.SyncPkFromServer(pkMode);
        pkMapSafe = value.MapSafe;
        pkProtectLv = value.ProtectionLevel;
        pkMapFree = value.MapFree;
    }

    // The receipt already performed these writes. Mark this exact observation as bound so
    // an old complete snapshot cannot replay over a later reached prefix or repeat SyncPk.
    internal static WorldSelfPkState RetainSelfPkObservationForIndependentRuntime()
    {
        var value = new WorldSelfPkState(unchecked((byte)pkMode), karma, pkState,
            pkMapSafe, pkProtectLv, pkMapFree);
        boundSelfPk = value;
        return value;
    }

    // The self arm of official case 0x8053 writes only this one byte, exactly the slot 0x8052
    // fills from its third field, and never reaches SysConfigDlg.SyncPkFromServer.
    internal static void SetLocalPkStateFromServer(byte value) => pkState = value;
}

}
