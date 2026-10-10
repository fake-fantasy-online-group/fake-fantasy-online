using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte> writeOpenFurnace;
    private Action<byte, int> writeCraft;
    private Action<byte, byte, byte, byte, int, byte> writeCraftUpgrade;
    private Action<byte, byte, int, byte, byte> writeDrill;
    private Action<byte, byte, int, byte, byte, int> writeInlay;
    internal void BindFurnaceWritersForIndependentRuntime(Action<byte> open, Action<byte, int> craft,
        Action<byte, byte, byte, byte, int, byte> upgrade, Action<byte, byte, int, byte, byte> drill,
        Action<byte, byte, int, byte, byte, int> inlay)
    {
        writeOpenFurnace = open; writeCraft = craft; writeCraftUpgrade = upgrade;
        writeDrill = drill; writeInlay = inlay;
    }
    public void OpenFurnaceForIndependentRuntime(byte modType) => writeOpenFurnace(modType);
    public void CraftForIndependentRuntime(byte modType, int resultItem) => writeCraft(modType, resultItem);
    public void CraftUpgradeForIndependentRuntime(byte mode, byte equipSlot, byte execute, byte tab = 0, int bagSlot = 0,
        byte protect = 0) => writeCraftUpgrade(mode, equipSlot, execute, tab, bagSlot, protect);
    public void DrillForIndependentRuntime(byte targetKind, byte tab, int slot, byte execute, byte protect = 0) =>
        writeDrill(targetKind, tab, slot, execute, protect);
    public void InlayForIndependentRuntime(byte targetKind, byte tab, int slot, byte holeIdx, byte runeTab, int runeBagSlot) =>
        writeInlay(targetKind, tab, slot, holeIdx, runeTab, runeBagSlot);
}
}
