namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static int LocalFaction;

    /// <summary>
    /// Recovered official <c>CombatWorld.SetPlayerPkState</c> owner (0x06000134).
    /// </summary>
    /// <remarks>
    /// Verified branch by branch against arm64 rva 0x23f5b4 (emitted extent 408 bytes) and
    /// x86_64 rva 0x24d310 (304 bytes), both reached only from Dispatch case 0x8053: static
    /// <c>I</c> is the single static slot of TypeInfo:CombatWorld (usage 1463, class static block
    /// at +0xb8), tested with <c>UnityEngine.Object.op_Inequality</c> reached through usage 2095
    /// (TypeInfo:UnityEngine.Object), i.e. the Unity-aware null test of a MonoBehaviour singleton. Instance field +0x28 is <c>players</c> (frozen atlas: CombatWorld offset 40,
    /// System.Collections.Generic.Dictionary&lt;System.Int32,RemotePlayer&gt;, field token
    /// 0x04000191); it is loaded once under the "I == 0 or players == 0" guard that calls the
    /// non-returning helper FUN_00177a74 / FUN_0016c1b0, which is the IL2CPP null-check throw and
    /// not a skip. <c>TryGetValue</c> is the shared generic adapter (usage 3646, MethodRef[42965],
    /// arm64 0x66f32c / x86_64 0x6835e0) called with the id and an out slot zeroed before the
    /// call, exactly like the sibling CombatWorld owners recovered earlier. A true return re-tests
    /// the found value with the Unity operator and then stores the single byte into
    /// RemotePlayer.pkState (instance field +0x11c, atlas offset 284, System.Byte, token
    /// 0x04000ef3), widening nothing: arm64 strb param_2 / x86_64 movb. An id that is absent, a
    /// Unity-null dictionary value and a Unity-null <c>I</c> all return without any write, and no
    /// actor is created: the "entity absent" decision belongs to this owner and is duplicated in
    /// no other layer. The only direct caller in the official client is the remote arm of case
    /// 0x8053; in this product that arm reaches this owner through the published remote world
    /// event, which ReferenceFirstWorldInitialFrame.RemoteEvents dispatches to
    /// ReferenceWorldRemotePlayerActor.ApplyPkEvent, and that single consumption point calls this
    /// method instead of writing RemotePlayer.pkState itself. ApplyRemoteReceiveWindow re-points
    /// CombatWorld.I at the frame's owner before any edge is consumed and each live actor is
    /// registered under its entity id, so the owner's own lookup decides every write.
    /// </remarks>
    public static void SetPlayerPkState(int id, byte st)
    {
        if (I != null && I.players.TryGetValue(id, out RemotePlayer player) && player != null)
        {
            player.SetPkState(st);
        }
    }
}

}
