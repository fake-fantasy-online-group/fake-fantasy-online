namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 NetClient 06000708–0600070c and writer closures 060007f5–060007fd.
    public void OpenFurnace(byte modType) => Send(0x1022, b => b.U8(modType));

    public void Craft(byte modType, int resultItem) => Send(0x1023, b =>
    {
        b.U8(modType);
        b.I32(resultItem);
    });

    public void CraftUpgrade(byte mode, byte equipSlot, byte execute, byte tab = 0,
        int bagSlot = 0, byte protect = 0) => Send(0x1024, b =>
    {
        b.U8(mode);
        b.U8(equipSlot);
        b.U8(execute);
        b.U8(tab);
        b.I32(bagSlot);
        b.U8(protect);
    });

    public void Drill(byte targetKind, byte tab, int slot, byte execute, byte protect = 0) =>
        Send(0x1029, b =>
        {
            b.U8(targetKind == 1 ? unchecked((byte)slot) : (byte)0);
            b.U8(execute);
            b.U8(protect);
            b.U8(targetKind);
            b.U8(tab);
            b.I32(targetKind == 0 ? slot : 0);
        });

    public void Inlay(byte targetKind, byte tab, int slot, byte holeIdx, byte runeTab,
        int runeBagSlot) => Send(0x102a, b =>
    {
        b.U8(targetKind == 1 ? unchecked((byte)slot) : (byte)0);
        b.U8(holeIdx);
        b.U8(runeTab);
        b.I32(runeBagSlot);
        b.U8(targetKind);
        b.U8(tab);
        b.I32(targetKind == 0 ? slot : 0);
    });
}
}
