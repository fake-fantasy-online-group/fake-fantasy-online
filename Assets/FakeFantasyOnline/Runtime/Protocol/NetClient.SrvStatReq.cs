namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.SrvStatReq (0x060006d5, arm64 0x266e44 / x86_64 0x2762a0):
    /// hand the cached non-capturing <c>Action&lt;Buf&gt;</c> to <c>Send(0x107d, ...)</c>.
    /// </summary>
    /// <remarks>
    /// The delegate is the <c>NetClient+&lt;&gt;c</c> singleton's cached lambda
    /// (<c>metadata_usage_09553_kind3</c> = MethodDef <c>NetClient+&lt;&gt;c.&lt;SrvStatReq&gt;b__80_0</c>,
    /// filled into static slot <c>+0x20</c> the first time the body runs) with the <c>&lt;&gt;c</c>
    /// instance as target - arm64 060006d5.c:50 <c>FUN_00616058(lVar4,uVar5,usage_09553,0)</c> and
    /// x86_64 :47 <c>FUN_00628380(lVar5,uVar2,usage_09553,0)</c>. The single real statement is the
    /// send: arm64 060006d5.c:56 and x86_64 :57 are the same
    /// <c>Assembly_CSharp__NetClient__Send__06000772(this,0x107d,cached)</c> call, so 0x107d is the
    /// immediate operand and not a numbering inference. No instance field, no connection test and
    /// no payload gate precede it: the connected test lives inside <c>Send</c> (0x06000772).
    /// <para>
    /// The frozen writer 0x0600079e (arm64 0x2750a4 / 4 bytes, x86_64 0x2842a0 / 16 bytes) is an
    /// empty body - a bare <c>return</c> on both architectures - so the request frame carries zero
    /// payload bytes and no reply is awaited here: the count comes back as Dispatch case 0x805b.
    /// </para>
    /// <para>
    /// The only official caller is TitleFlow.DrawServer (0x06000ef4), which re-requests on a
    /// five-second cadence: arm64 0x3873e4 <c>bl</c> realtimeSinceStartup /
    /// 0x3873ec <c>fcmp s0,s1</c> against the float at TitleFlow <c>+0x318</c> /
    /// 0x3873f0 <c>b.lt</c> (skip), then 0x38759c <c>fmov s1,#5.0</c> / 0x3875a0 <c>fadd</c> /
    /// 0x3875a4 <c>str s0,[x19,#0x318]</c> before the 0x3875cc call; x86_64 is the same shape at
    /// 0x39a288 / 0x39a291 <c>ucomiss 0x318(%rax)</c> / 0x39a298 <c>jb</c> and 0x39a324
    /// <c>addss</c> of the 5.0f literal at 0x1117d44 / 0x39a330 <c>movss</c>.
    /// </para>
    /// </remarks>
    public void SrvStatReq() =>
        Send(ProtocolOpcodes.ServerStatusRequest, b => { });
}
}
