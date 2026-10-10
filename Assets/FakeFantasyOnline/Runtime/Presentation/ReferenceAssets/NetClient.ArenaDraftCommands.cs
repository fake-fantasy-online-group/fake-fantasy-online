using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Presentation facet for the two official 1.5.8 commands ArenaDraftDlg emits:
//
//   NetClient.ArenaPick   0x06000744  instance void ArenaPick(System.Int32)
//                                     -> display class +0x10 = index
//                                     -> Action<Buf> 0x06000835 -> Send 0x1083
//   NetClient.ArenaReroll 0x06000745  instance void ArenaReroll()
//                                     -> cached Action<Buf> 0x060007a9 -> Send 0x1086
//
// Evidence: the frozen exports in
// Build/ReferenceReverseGhidra-1.5.8/queries/wbb030-arenapick-{arm64,x86_64} and
// .../wbb030-arenareroll-{arm64,x86_64} for the commands, plus .../wbb030-arenapick-writer-arm64
// (0x06000835) and .../wbb030-arenareroll-writer-arm64 (0x060007a9) for the writers. Both opcodes
// are read off the command bodies on both architectures, never from the private server.
//
// Like the arena revive facet these members do NOT write the wire: they only forward what the
// window decided, and the bytes come from the official writers, whose product carrier is the
// Protocol NetClient reached through PrivateServerGateway. Presentation never imports Protocol, so
// the writers are bound by the host; until a host binds them the members forward to null writers
// and the product sends nothing on its own.
internal partial class NetClient
{
    private Action<int> arenaPickWriter;
    private Action arenaRerollWriter;

    internal void BindArenaDraftWritersForIndependentRuntime(Action<int> pickWriter, Action rerollWriter)
    {
        arenaPickWriter = pickWriter;
        arenaRerollWriter = rerollWriter;
    }

    /// <summary>Official NetClient.ArenaPick (0x06000744): sends opcode 0x1083 with the clicked card
    /// slot as one Buf.U8. Which talent the slot holds stays server-authored.</summary>
    public void ArenaPickForIndependentRuntime(int index) => arenaPickWriter?.Invoke(index);

    /// <summary>Official NetClient.ArenaReroll (0x06000745): sends opcode 0x1086 with no payload.
    /// The reroll price and the free-reroll count stay server-authored.</summary>
    public void ArenaRerollForIndependentRuntime() => arenaRerollWriter?.Invoke();
}
}
