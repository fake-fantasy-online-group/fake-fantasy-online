namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    // Official 1.5.8 0x06000748/749, payload closures 0x0600083b/83d.
    public void ChangePwd(string oldPassword, string newPassword) => Send(0x1088, b =>
    { b.Str(oldPassword ?? ""); b.Str(newPassword ?? ""); });
    public void ChangeSecCode(string password, string oldCode, string newCode) => Send(0x1089, b =>
    { b.Str(password ?? ""); b.Str(oldCode ?? ""); b.Str(newCode ?? ""); });
}
}
