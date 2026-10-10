namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        // Official 1.5.8 0x06000741/0x06000831: operation, bag tab, bag index, target cell.
        public void ChangeSetPut(byte bagTab, int bagSlot, int cell) => Send(0x109a, b =>
        {
            b.U8(0); b.U8(bagTab); b.I32(bagSlot); b.I32(cell);
        });

        // Official 0x06000742/0x06000833 retains both zero source fields.
        public void ChangeSetTake(int cell) => Send(0x109a, b =>
        {
            b.U8(1); b.U8(0); b.I32(0); b.I32(cell);
        });

        // Official 0x06000743/0x060007a8: the callback body is empty.
        public void ChangeSetSwap() => Send(0x109b, b => { });
    }
}
