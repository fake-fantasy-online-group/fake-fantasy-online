namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    // Official 0x060006da NetClient.SendEmote(int face); writer 0x060008aa <SendEmote>b__0 is the
    // anonymous body `b => b.I32(face)`. Evidence: Build/ReferenceReverseGhidra-1.5.8/queries/
    // WBD-123-060006da-{arm64,x86_64}/060006da.c, WBD-123-060008aa-arm64/060008aa.c, frozen IL
    // line 1086310 (ldc.i4 4183 = 0x1057 = ProtocolOpcodes.EmoteRequest). Owner-only: no official
    // caller found, so no entry here - not the ChatUI `\eXX` token, not CombatWorld.ShowEmote.
    public void SendEmote(int face) => Send(ProtocolOpcodes.EmoteRequest, b => b.I32(face));
}
}
