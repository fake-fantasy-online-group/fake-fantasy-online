using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Presentation facet for the official 1.5.8 NetClient.ArenaRevive (0x06000746), the single command
// both ArenaDeathDlg buttons emit:
//
//   NetClient.ArenaRevive  0x06000746  instance void ArenaRevive(System.Boolean)
//                                      -> display class +0x10 = surrender
//                                      -> Action<Buf> 0x06000837 -> Send 0x1084
//
// Evidence: the frozen exports in
// Build/ReferenceReverseGhidra-1.5.8/queries/wbb027-arenarevive-{arm64,x86_64} (the command) and
// .../wbb027-arenarevive-writer-{arm64,x86_64} (the writer). The opcode is read off the command
// body itself, not from the private server.
//
// Like the trade facets this member does NOT write the wire: it only captures the flag and forwards
// it, and the bytes come from the writer 0x06000837, whose product carrier is the Protocol
// NetClient.WriteArenaRevive reached through PrivateServerGateway. Presentation never imports
// Protocol, so the writer is bound by the host; until a host binds it the member forwards to a null
// writer and the product sends nothing on its own.
internal partial class NetClient
{
    private Action<bool> arenaReviveWriter;

    internal void BindArenaReviveWriterForIndependentRuntime(Action<bool> writer) =>
        arenaReviveWriter = writer;

    /// <summary>Official NetClient.ArenaRevive (0x06000746): captures the surrender flag and sends
    /// opcode 0x1084. The writer 0x06000837 writes it as one Buf.U8 - 0 to revive, 1 to surrender.
    /// Death, revive and the end of the round stay server-authored: nothing here decides them.</summary>
    public void ArenaReviveForIndependentRuntime(bool surrender) => arenaReviveWriter?.Invoke(surrender);
}
}
