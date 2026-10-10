namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.Register (0x060006c1): allocate the display class, store
    /// acc/pw/sec/invite, then <c>Send(0x105d, &lt;Register&gt;b__0)</c>.
    /// </summary>
    /// <remarks>
    /// The four string writes of the frozen writer (0x06000880, arm64 0x277af4 /
    /// x86_64 0x286bb0) are <c>Buf.Str</c> (0x0600078e) in the order acc, pw, sec,
    /// <c>invite ?? ""</c>; the fifth is <c>SystemInfo.deviceUniqueIdentifier ?? ""</c> and the
    /// last is <c>Buf.U8</c> (0x0600078a) of <c>VmDetect.Detect(out _)</c>. There is no auth tag
    /// and no second detector byte: unlike <c>Login</c> (0x060006c0) this body writes a single
    /// U8 and stops. The same host capabilities as <c>Login</c> invoke the real device getter
    /// and recovered VM detector at these write positions, inside Send's connected gate.
    /// Registration never invokes the debug detector or computes an auth tag.
    /// </remarks>
    public void Register(string account, string password, string securityCode, string inviteCode) =>
        Send(ProtocolOpcodes.RegisterRequest, b =>
        {
            b.Str(account);
            b.Str(password);
            b.Str(securityCode);
            b.Str(inviteCode ?? "");
            b.Str(_loginEnvironment.DeviceUniqueIdentifier() ?? string.Empty);
            b.U8(_loginEnvironment.DetectVm(out _) ? (byte)1 : (byte)0);
        });
}
}
