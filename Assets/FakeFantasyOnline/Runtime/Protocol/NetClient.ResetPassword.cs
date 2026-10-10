namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.ResetPwd (0x060006c2): allocate the display class, store
    /// acc/sec/newPw, then <c>Send(0x105e, &lt;ResetPwd&gt;b__0)</c>.
    /// </summary>
    /// <remarks>
    /// The three string writes of the frozen writer (0x06000882, arm64 0x277be8 /
    /// x86_64 0x286c90) are <c>Buf.Str</c> (0x0600078e) in the order acc, sec, newPw and
    /// nothing else. Unlike <c>Register</c> (0x060006c1), whose writer (0x06000880) appends
    /// <c>SystemInfo.deviceUniqueIdentifier</c> and a <c>VmDetect.Detect</c> U8, this body has
    /// no device identifier and no detector byte, so neither probe is taken here.
    /// The trimming is not done by this member: the official caller
    /// <c>TitleFlow.&lt;DoRecover&gt;b__172_0</c> (0x06000f08, arm64 :34-39 / x86_64 :30-35)
    /// passes <c>recAcc.Trim()</c> and <c>recSec.Trim()</c> and leaves <c>recPw</c> (+0x228)
    /// raw, exactly as <c>&lt;DoRegister&gt;b__176_0</c> leaves <c>regPw</c> raw.
    /// </remarks>
    public void ResetPwd(string account, string securityCode, string newPassword) =>
        Send(ProtocolOpcodes.ResetPasswordRequest, b =>
        {
            b.Str(account);
            b.Str(securityCode);
            b.Str(newPassword);
        });
}
}
