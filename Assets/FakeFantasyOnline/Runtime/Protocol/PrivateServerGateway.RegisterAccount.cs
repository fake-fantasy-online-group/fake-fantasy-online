using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAccountRegistrationCommands
{
    public void ConnectForAccount(Action<bool> onDone) =>
        BeginNativeConnection(onDone, System.Threading.CancellationToken.None, out _, out _);

    public void BindAccountResults(Action<int, string> register, Action<int, string> reset)
    {
        // Official SetupNet's two assignments, not event subscription/multicast or per-request
        // continuations. Closing a form does not unbind either callback.
        _netClient.OnRegisterResult = register;
        _netClient.OnResetResult = reset;
    }

    public void RegisterAccount(string account, string password, string securityCode, string inviteCode) =>
        _netClient.Register(account, password, securityCode, inviteCode);

    // The two existing Dispatch cases share the native I32/Str tail. Load the slot once before
    // reading the buffer; a missing delegate consumes the opcode without reading its payload.
    // No stage gate, trailing-byte check, result substitution or async reply queue is involved.
    private bool TryReceiveAccountResult(ushort opcode, byte[] payload)
    {
        Action<int, string> callback;
        if (opcode == ProtocolOpcodes.RegisterResult)
            callback = _netClient.OnRegisterResult;
        else if (opcode == ProtocolOpcodes.ResetPasswordResult)
            callback = _netClient.OnResetResult;
        else
            return false;

        if (callback != null)
        {
            var reader = new NetClient.Buf(payload);
            int code = reader.I32();
            string message = reader.Str();
            callback(code, message);
        }
        return true;
    }
}
}
