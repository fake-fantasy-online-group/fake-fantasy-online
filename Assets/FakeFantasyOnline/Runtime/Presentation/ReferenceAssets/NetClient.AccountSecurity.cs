using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<string, string> writeChangePassword;
    private Action<string, string, string> writeChangeSecurityCode;
    internal void BindAccountSecurityWritersForIndependentRuntime(Action<string, string> password,
        Action<string, string, string> code)
    { writeChangePassword = password; writeChangeSecurityCode = code; }
    public void ChangePwdForIndependentRuntime(string oldPassword, string newPassword) => writeChangePassword(oldPassword, newPassword);
    public void ChangeSecCodeForIndependentRuntime(string password, string oldCode, string newCode) => writeChangeSecurityCode(password, oldCode, newCode);
}
}
