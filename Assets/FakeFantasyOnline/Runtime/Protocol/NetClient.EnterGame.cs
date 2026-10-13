namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.EnterGame (0x060006c4, arm64 0x265f74 / x86_64 0x275370):
    /// allocate the display class, store the name, then <c>Send(0x1004, &lt;EnterGame&gt;b__0)</c>.
    /// </summary>
    /// <remarks>
    /// The frozen writer 0x06000886 (arm64 0x277c90 / x86_64 0x286d30) emits one <c>Buf.Str</c>
    /// (0x0600078e) and nothing else; its <c>name ?? ""</c> coalesce is the same null-to-empty
    /// that <c>Buf.Str</c> itself already applies. 0x1004 is the immediate operand of the
    /// <c>Send</c> call on both architectures, not a numbering inference (arm64 060006c4.c:33,
    /// x86_64 :31). The single caller passes the selected roster row name (CharBrief +0x20 of
    /// TitleFlow +0x2d0, row stride 0x68) read inside the entry arm, and no reply is read here.
    /// </remarks>
    public void EnterGame(string name)
    {
        // Advertise before entry so the very first ground snapshot carries unique identity.
        SendClientCaps();
        Send(ProtocolOpcodes.EnterWorldRequest, b => b.Str(name ?? ""));
    }
}
}
