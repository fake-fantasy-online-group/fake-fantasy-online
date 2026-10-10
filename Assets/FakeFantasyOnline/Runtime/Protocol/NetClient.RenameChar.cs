namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 0600075d. Trimming belongs to RenameDlg.Confirm, not this writer.
    public void SendRenameChar(string name) =>
        Send(ProtocolOpcodes.RenameCharacterRequest, b => b.Str(name));
}
}
