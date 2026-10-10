namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    /// <summary>
    /// Recovered official <c>SysConfigDlg.SyncPkFromServer</c> owner (0x06000df3).
    /// </summary>
    /// <remarks>
    /// Verified branch by branch against arm64 rva 0x33df14 (emitted extent 276 bytes) and
    /// x86_64 rva 0x350970 (192 bytes), reached only from Dispatch case 0x8052 after that case
    /// stored its third field and before it stores pkMapSafe. The single static slot of
    /// TypeInfo:SysConfigDlg (usage 2475, class static block at +0xb8) is <c>I</c> (frozen atlas:
    /// SysConfigDlg.I, field token 0x0400131f, offset 0), tested once with
    /// <c>UnityEngine.Object.op_Inequality</c> reached through usage 2095
    /// (TypeInfo:UnityEngine.Object), i.e. the Unity-aware null test of a
    /// MonoBehaviour singleton. Inside that branch the body re-loads the same static and stores
    /// the argument into instance field +0x24, which the atlas resolves to
    /// <c>SysConfigDlg.pk</c> (System.Int32, offset 36, token 0x04001322): one four-byte store
    /// into I + 0x24 on both architectures. The store is preceded by the
    /// non-returning null-check helper FUN_00177a74 / FUN_0016c1b0, the IL2CPP dereference check
    /// that throws instead of skipping, and that is exactly what the plain C# field store does.
    /// A Unity-null <c>I</c> returns without any write, there is no clamp, no mode whitelist, no
    /// comparison against the previous value and no further call, so two receipts carrying equal
    /// modes each synchronize once; ordering therefore belongs to the caller, not to this owner.
    /// The parameter keeps its official int32 width: official CharData.pkMode is an int32
    /// slot filled from a wire byte, so the widening happens in case 0x8052, not here.
    /// </remarks>
    public static void SyncPkFromServer(int mode)
    {
        if (I != null)
            I.pk = mode;
    }
}
}
