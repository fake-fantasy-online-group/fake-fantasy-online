using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<byte, int, int> changeSetPut;
    private Action<int> changeSetTake;
    private Action changeSetSwap;
    private Action<int> petDeploy;
    internal void BindChangeSetForIndependentRuntime(Action<byte, int, int> put, Action<int> take, Action swap, Action<int> deploy)
    { changeSetPut = put; changeSetTake = take; changeSetSwap = swap; petDeploy = deploy; }
    public void ChangeSetPutForIndependentRuntime(byte bagTab, int bagSlot, int cell) => changeSetPut(bagTab, bagSlot, cell);
    public void ChangeSetTakeForIndependentRuntime(int cell) => changeSetTake(cell);
    public void ChangeSetSwapForIndependentRuntime() => changeSetSwap();
    // Official 0x06000739, instance System.Void NetClient.PetDeploy(System.Int32 storeIdx).
    //
    // arm64 rva 0x26e954 (100 bytes) and x86_64 rva 0x27de90 (80 bytes) are both a single tail
    // call: NetClient.PetCmd(this, 8, storeIdx, "") - subcommand 8 is the deploy, the argument is
    // the bag slot and the text is the empty string (metadata usage 10335, kind 5). The only
    // official callers are the two arms of InventoryDlg.OnGUI 0x06000527 (arm64 0x45f8e8 for the
    // opened-pet row, arm64 0x460cbc for the drop onto a pet slot), each sending once.
    // PrivateServerPetCommandCodec names the same code Subcommand.Deploy, and the writer installed
    // by ReferenceCharacterSkillUsePort reaches PrivateServerGateway.DeployPetAsync, which invokes
    // the official member inside the single send gate.
    public void PetDeployForIndependentRuntime(int bagSlot) => petDeploy(bagSlot);
}
}
