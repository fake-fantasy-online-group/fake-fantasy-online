using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Read-only mirror of the already recovered official NetClient.get_Connected (0x060006b7).
    // The real owner lives in Protocol: Protocol/NetClient.cs holds
    // `public bool Connected => _tcp != null && _tcp.Connected;` (arm64 0x2645f0 / 20 bytes,
    // x86_64 0x273a30 / 32 bytes, a single read of instance field _tcp (+0x88) and a tail call to
    // TcpClient.get_Connected), and PrivateServerGateway.Connected and
    // ClientSessionCoordinator.Connected forward that same value upward.
    // This Presentation mirror owns no connection state of its own and decides nothing: it only
    // forwards the one real owner's boolean so CombatWorld.TryAttack can evaluate the official
    // gate without the Presentation layer reaching into the transport. A missing local member was
    // never a reason to drop the gate, so no substitute predicate is introduced here.
    private Func<bool> readConnected;

    internal void BindConnectedReaderForIndependentRuntime(Func<bool> reader) => readConnected = reader;

    public bool ConnectedForIndependentRuntime => readConnected();

    // Read-only mirror of official NetClient.LastDropReason (+0xb0, atlas offset 176, volatile
    // string): the one owner is Protocol/NetClient, written by RawTx/transmit/receive and reset by
    // every EstablishConnection. TitleFlow.NoteDisconnected (0x06000ec0) reads it inside its own
    // body behind the NetClient.I null test; this mirror forwards that owner and decides nothing -
    // same shape as BindConnectedReaderForIndependentRuntime above. No fallback is added here: the
    // previous "unbound means I was null" note was wrong, and the real unbound case is unreachable
    // because ClientBootstrap.Awake binds the reader after ReferenceCharacterSkillUsePort has set
    // NetClient.I (ReferenceAssets/NetClient.SkillCommands.cs:21-29 sets I = this, called from the
    // port constructor, ReferenceCharacterSkillUsePort.cs:41). I is cleared only in
    // ReferenceCharacterSkillUsePort.Dispose (:623-627), which also destroys the core component
    // and unbinds the port, and the null case that leaves is still answered by the official-shaped
    // `NetClient.I != null` test at the read site, not by a substitute value.
    private Func<string> readLastDropReason;

    internal void BindLastDropReasonReaderForIndependentRuntime(Func<string> reader) =>
        readLastDropReason = reader;

    public string LastDropReasonForIndependentRuntime => readLastDropReason();
}
}
