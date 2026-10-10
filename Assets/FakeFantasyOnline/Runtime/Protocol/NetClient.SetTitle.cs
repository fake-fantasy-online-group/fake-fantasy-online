namespace FakeFantasyOnline.Protocol
{

public sealed partial class NetClient
{
    // Official 0x060006c7 NetClient.SetTitle(System.String): capture title by reference in
    // <>c__DisplayClass66_0 (+0x10, write barrier) and tail call NetClient.Send 0x06000772 with the
    // opcode immediate 0x1005. Writer <SetTitle>b__0 0x0600088a is the anonymous body below - one
    // Buf.Str 0x0600078e - so serialization runs inside Send, after its connected test. Null, length
    // and owned-list handling belong to Buf.Str and the EquipDlg arms: queries/WBD-126-060006c7.
    public void SetTitle(string title)
    {
        Send(ProtocolOpcodes.SetCharacterTitleRequest, b => b.Str(title));
    }
}
}
