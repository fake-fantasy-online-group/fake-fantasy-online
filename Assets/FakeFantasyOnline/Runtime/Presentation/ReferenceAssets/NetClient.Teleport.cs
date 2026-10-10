using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeOpenTeleport;

    internal void BindOpenTeleportWriterForIndependentRuntime(Action<int> writer) =>
        writeOpenTeleport = writer;

    // The existing Protocol owner supplies OpenTeleport (1.6.0 060006f2) and its 0x1071 writer.
    // Bind before publishing this facade; retain synchronous sender failures at the caller.
    public void OpenTeleportForIndependentRuntime(int transList) => writeOpenTeleport(transList);

    // Official TransportUI.DoTeleport (0x06000f96) calls NetClient.Teleport (0x060006e9) inside the
    // same branch that has just hidden the window, so the facade hop stays one call and nothing is
    // enqueued. The bound writer is the single session/gateway/Protocol route to the official
    // writer; no second writer, second queue or local confirmation is introduced here, and no map
    // switch happens before the server answers.
    //
    // This class is a product facade, so it has one state the official command does not have: an
    // instance exists yet writeTeleport is unset. It is not the official 0x06000f96 singleton gate
    // and must not be described as equivalent to it. The only binding of that writer today is the
    // composition-root binding in ReferenceCharacterSkillUsePort, which runs in the same straight
    // line that later assigns NetClient.I, so a row click cannot observe the unset writer; that is
    // a static reachability fact about this product, not a recovered official branch.
    private Action<int, int> writeTeleport;

    internal void BindTeleportWriterForIndependentRuntime(Action<int, int> writer) =>
        writeTeleport = writer;

    /// <summary>Official NetClient.Teleport (0x060006e9): opcode 0x1072 with the transport list and
    /// the selected destination row as two Buf.I32 in that order. Distances, prices, availability
    /// and the resulting map stay server-authored.</summary>
    public void TeleportForIndependentRuntime(int transList, int index) => writeTeleport?.Invoke(transList, index);
}
}
