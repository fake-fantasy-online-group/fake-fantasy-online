namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        // Official 1.5.8 0x0600076f and capture 0x06000874/0x06000875.
        public void SysMsgRespond(int reqId, byte accept)
        {
            Send(ProtocolOpcodes.SystemRequestResponse, b =>
            {
                b.I32(reqId);
                b.U8(accept);
            });
        }
    }
}
