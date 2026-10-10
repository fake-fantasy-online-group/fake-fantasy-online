namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    public void SetHotbar(int[] ids, int[] kind, bool expanded)
    {
        Send(ProtocolOpcodes.SaveHotbarRequest,
            b => WriteHotbarForIndependentRuntime(b, ids, kind, expanded));
    }

    // Original SetHotbar writer body, shared with the existing asynchronous gateway.
    internal static void WriteHotbarForIndependentRuntime(Buf b, int[] ids, int[] kind, bool expanded)
    {
        for (int i = 0; i < 20; i++)
        {
            b.I32(ids[i]);
            b.U8(unchecked((byte)kind[i]));
        }
        b.U8(expanded ? (byte)1 : (byte)0);
        for (int i = 20; i < 30; i++)
        {
            b.I32(i < ids.Length ? ids[i] : 0);
            b.U8(unchecked((byte)(i < kind.Length ? kind[i] : 0)));
        }
    }
}

}
