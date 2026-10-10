using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, int, byte, byte> writeRepair, writeRepairConfirm;
    internal void BindRepairWritersForIndependentRuntime(Action<byte, int, byte, byte> query, Action<byte, int, byte, byte> confirm)
    { writeRepair = query; writeRepairConfirm = confirm; }
    public void RepairForIndependentRuntime(byte mode, int slot, byte targetKind = 1, byte tab = 0) => writeRepair(mode, slot, targetKind, tab);
    public void RepairConfirmForIndependentRuntime(byte mode, int slot, byte targetKind = 1, byte tab = 0) => writeRepairConfirm(mode, slot, targetKind, tab);
}
}
