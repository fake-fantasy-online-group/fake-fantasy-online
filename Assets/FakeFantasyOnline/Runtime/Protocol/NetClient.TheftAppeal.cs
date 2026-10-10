namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.TheftAppeal (0x060006e4, arm64 0x267ea0 / x86_64 0x2771d0):
    /// allocate the display class, store account/when/reason, then
    /// <c>Send(0x1090, &lt;TheftAppeal&gt;b__0)</c>.
    /// </summary>
    /// <remarks>
    /// The opcode is a literal in both frozen bodies — arm64
    /// <c>Assembly_CSharp__NetClient__Send__06000772(param_1,0x1090,uVar2)</c> and x86_64 the
    /// same immediate at 060006e4.c:47 — so 0x1090 is read off the instruction, not inferred
    /// from a neighbour's numbering. The private-server protocol manual records the same number
    /// (<c>0x1090 | TheftAppeal(account, when, reason) | account:Str, when:Str, reason:Str</c>).
    /// The three string writes of the frozen writer (0x060007b7, arm64 0x27514c / x86_64
    /// 0x284450) are <c>Buf.Str</c> (0x0600078e) in the order account, when, reason and nothing
    /// else: no device identifier and no <c>VmDetect</c> byte, exactly like
    /// <c>ResetPwd</c> (0x060006c2) and unlike <c>Register</c> (0x060006c1).
    /// Each write is guarded by a null test in the native body and falls back to the empty
    /// string literal (metadata_usage_10335), which is what <c>Buf.Str</c> already does.
    /// The trimming is not done by this member: the official caller captures
    /// <c>apAcc.Trim()</c>, <c>apWhen.Trim()</c> and <c>apReason.Trim()</c> in
    /// <c>TitleFlow.DoAppeal</c> (0x06000eee) before the connect, and it captures them into a
    /// display class that is allocated per invocation, so each send carries its own three strings.
    /// No reply is read here and none is awaited by the caller: the frozen client registers no
    /// dedicated appeal-result handler. That is a fact about this client's registration, not a
    /// proof that a server answers nothing — a generic notice or error packet is not excluded, and
    /// the local private server leaves 0x1090 未接, so nothing here claims end-to-end handling.
    /// </remarks>
    public void TheftAppeal(string account, string when, string reason) =>
        Send(ProtocolOpcodes.TheftAppealRequest, b =>
        {
            b.Str(account ?? "");
            b.Str(when ?? "");
            b.Str(reason ?? "");
        });
}
}
