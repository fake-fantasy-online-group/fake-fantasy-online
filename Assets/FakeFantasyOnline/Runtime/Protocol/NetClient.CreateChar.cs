namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    /// <summary>
    /// Official 1.5.8 NetClient.CreateChar (0x060006c3, arm64 0x265e70 / x86_64 0x275270):
    /// allocate the display class, store name/race/gender/head/hair, then
    /// <c>Send(0x1003, &lt;CreateChar&gt;b__0)</c>.
    /// </summary>
    /// <remarks>
    /// The frozen writer 0x06000884 (arm64 0x277c30 / x86_64 0x286cd0) emits one <c>Buf.Str</c>
    /// (0x0600078e) and then four <c>Buf.U8</c> (0x0600078a) in the order name, race, gender,
    /// head, hair, and nothing else. 0x1003 is the immediate operand of the <c>Send</c> call on
    /// both architectures, not a numbering inference (arm64 060006c3.c:41, x86_64 :38).
    /// No coalescing or width guard exists in this member, so <c>Buf.Str</c>'s null-to-empty and
    /// U8's unchecked truncation are the whole official behaviour here.
    /// Timing: the single caller writes these five values before the call and the dispatch re-reads
    /// them afterwards as the arguments, so each invocation carries its own DisplayClass62_0 copy
    /// (arm64 TitleFlow.DrawCharCreate 0x06000ef9 at 0x38b964-0x38b97c). No reply is read here.
    /// </remarks>
    public void CreateChar(string name, int race, int gender, int head, int hair) =>
        Send(ProtocolOpcodes.CreateCharacterRequest, b =>
        {
            b.Str(name);
            b.U8(race);
            b.U8(gender);
            b.U8(head);
            b.U8(hair);
        });
}
}
