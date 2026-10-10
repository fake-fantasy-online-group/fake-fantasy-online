namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.DeleteChar (0x060006c5, arm64 0x266050 / x86_64 0x275440):
    /// allocate the display class, store the name, then <c>Send(0x105f, &lt;DeleteChar&gt;b__0)</c>.
    /// </summary>
    /// <remarks>
    /// The frozen writer 0x06000888 (arm64 0x277cfc / x86_64 0x286d90) emits one <c>Buf.Str</c>
    /// (0x0600078e) and nothing else; its <c>name ?? ""</c> coalesce is the same null-to-empty
    /// that <c>Buf.Str</c> itself already applies. 0x105f is the immediate operand of the
    /// <c>Send</c> call on both architectures, not a numbering inference (arm64 060006c5.c:33,
    /// x86_64 :31). The single caller passes TitleFlow's roster row name (CharBrief +0x20 of the
    /// selected slot) read inside the confirm branch, and no reply is read here.
    /// </remarks>
    public void DeleteChar(string name) =>
        Send(ProtocolOpcodes.DeleteCharacterRequest, b => b.Str(name ?? ""));
}
}
