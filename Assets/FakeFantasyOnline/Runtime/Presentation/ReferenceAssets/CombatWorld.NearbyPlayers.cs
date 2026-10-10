using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    /// <summary>
    /// Official 1.5.8 <c>CombatWorld.NearbyPlayers</c> (0x0600013e): the whole
    /// <c>CombatWorld.I.players</c> dictionary as (entity id, name) pairs, in the dictionary's own
    /// order and with no distance, radius, sort, de-duplication or self-exclusion step.
    /// </summary>
    /// <remarks>
    /// Verified branch by branch against arm64 rva 0x240a7c (960 bytes emitted extent) and x86_64
    /// rva 0x24e4a0 (976 bytes), both exported from the frozen 1.5.8 persistent Ghidra project
    /// (Build/ReferenceReverseGhidra-1.5.8/queries/wb142-nearbyplayers-20261001). Both bodies agree
    /// on every decision, so nothing below is an interpretation of one architecture alone.
    /// <list type="bullet">
    /// <item>Allocation order: <c>List&lt;(int,string)&gt;</c> is allocated (usage 745,
    /// TypeInfo:Il2CppType[2318]) and constructed (usage 4891, MethodRef[43907], arm64 0x908000 /
    /// x86_64 0x9150b0) <b>before</b> any null test, so a null <c>I</c> returns an allocated empty
    /// list and never null.</item>
    /// <item>The single CombatWorld static slot <c>I</c> (usage 1463 TypeInfo:CombatWorld, class
    /// static block +0xb8, field offset 0) is tested with
    /// <c>UnityEngine.Object.op_Equality</c> (arm64 104570c / x86_64 FUN_01038840, usage 2095
    /// TypeInfo:UnityEngine.Object); the body enters the enumeration only when that comparison is
    /// false, i.e. on <c>I == null</c> it returns the already allocated empty list.</item>
    /// <item>Instance field +0x28 is <c>players</c> (frozen atlas: CombatWorld offset 40,
    /// System.Collections.Generic.Dictionary&lt;System.Int32,RemotePlayer&gt;, field token
    /// 0x04000191). It is loaded once under the "I == 0 or players == 0" guard that calls the
    /// non-returning helper FUN_00177a74 / FUN_0016c1b0, which is the IL2CPP null-check throw and
    /// not a skip.</item>
    /// <item>Enumeration is the dictionary's own: GetEnumerator (usage 3644, MethodRef[42963],
    /// arm64 0x66debc / x86_64 0x6820c0) writes a 40-byte enumerator struct that both bodies zero
    /// first, MoveNext (usage 4537, MethodRef[43631], arm64 0x753e60 / x86_64 0x762120) drives the
    /// loop and Dispose (usage 4536, MethodRef[43630], arm64 0x753f80 / x86_64 0x762250) runs on the
    /// normal exit only. No list, array or second collection is enumerated.</item>
    /// <item>Per element the key is taken as a 32-bit int and the value is re-tested with
    /// <c>UnityEngine.Object.op_Inequality</c> (arm64 104b39c / x86_64 FUN_0103dde0): a Unity-null
    /// (absent or destroyed) RemotePlayer is skipped, and the redundant <c>player == 0</c> guard the
    /// code generator emits before the field read reaches the same throw helper.</item>
    /// <item>Item1 is the dictionary key and Item2 is <c>RemotePlayer.pname</c> (instance field
    /// +0x28, System.String), stored through <c>ValueTuple`2..ctor</c> (usage 6499,
    /// MethodRef[44761], arm64 0xbe3728 / x86_64 0xbe2d30). <c>List.Add</c> is inlined on both
    /// architectures - size at +0x1c, items at +0x10, version at +0x18, a 0x10-stride element store
    /// of Item1 at +0x20 and Item2 at +0x28 with the write barrier on the string slot
    /// (thunk_FUN_00178e5c / FUN_0016bdd0) - and the growth path calls the generic
    /// <c>List`1.Add</c> (usage 4893, MethodRef[43909], arm64 0x908aa4 / x86_64 0x915a70).</item>
    /// <item>There is no centre, radius, squared-distance, angle, height or map test anywhere in
    /// either body - no float operation at all - and no sort, no de-duplication and no exclusion of
    /// the local player. "Nearby" is only what the <c>players</c> dictionary already holds; the
    /// result order is the dictionary's enumeration order and one fresh list is built per call with
    /// nothing cached in a static.</item>
    /// </list>
    /// The frozen 1.5.8 atlas has exactly one call edge to this member (edge_index 5440, arm64 bl at
    /// 0x329800 inside <c>TeamConfigDlg.OnGUI</c> 0x06000d4f), so the invite dialog's nearby list is
    /// its only official consumer.
    /// </remarks>
    public static List<(int, string)> NearbyPlayers()
    {
        List<(int, string)> list = new List<(int, string)>();
        if (I == null) return list;
        foreach (KeyValuePair<int, RemotePlayer> pair in I.players)
        {
            if (pair.Value != null) list.Add((pair.Key, pair.Value.pname));
        }
        return list;
    }
}

}
