using System;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : ITitleResultReceiptSource
{
    // Counts only the pre-existing cancellable AuthenticateAsync readers. The ordinary title
    // calls the native writer inline and never creates one of those response readers.
    private int _authenticationWaiters;
    // This only selects the host observer for the actual send route. It never filters the
    // native callback, correlates a packet to an attempt, or owns a second result queue.
    private bool _titleLoginResultWithoutWaiter;

    public void SendTitleLoginForIndependentRuntime(string account, string password)
    {
        _titleLoginResultWithoutWaiter = true;
        _netClient.Login(account, password);
    }

    public void BindLoginResultReceipt(Action<int, string> receive) =>
        _netClient.OnLoginResult = receive;

    public void BindCreateResultReceipt(Action<int, string> receive) =>
        _netClient.OnCreateResult = receive;

    private bool TryReceiveTitleResultPacket(ushort opcode, byte[] payload)
    {
        if (opcode != ProtocolOpcodes.LoginResult &&
            opcode != ProtocolOpcodes.CharacterMutationResult) return false;
        bool loginWithoutWaiter = opcode == ProtocolOpcodes.LoginResult &&
            _titleLoginResultWithoutWaiter &&
            Volatile.Read(ref _authenticationWaiters) == 0;

        // Dispatch constructs Buf before its switch. Each case then loads the live slot
        // once, before either read. Unlike 0x8004, a null delegate skips payload decoding.
        var reader = new NetClient.Buf(payload);
        Action<int, string> receive = opcode == ProtocolOpcodes.LoginResult
            ? _netClient.OnLoginResult : _netClient.OnCreateResult;
        if (receive != null)
        {
            int code;
            string message;
            try
            {
                code = reader.I32();
                message = reader.Str();
            }
            catch (Exception exception) when (
                exception is IndexOutOfRangeException || exception is ArgumentException)
            {
                // Keep the existing Task adapter's strict malformed-response failure.
                // This is the same raw packet, not a successful decoded result. Native
                // decode failure still propagates; the callback is never invoked.
                if (!loginWithoutWaiter) DispatchPacket(new ProtocolPacket(opcode, payload));
                throw;
            }
            // Only an actual decoded success supplies the host authentication state after an
            // ordinary native send. Existing Task readers retain their linked-lifetime commit.
            if (loginWithoutWaiter && code == 0) _stage = GatewayStage.Authenticated;
            // No message normalization, trailing-byte guard or callback exception catch.
            receive(code, message);
        }

        // Existing login/create/delete Tasks still require and validate this actual reply.
        // Their stricter WireReader is an unchanged host boundary, not the native decoder.
        // A callback exception reaches neither this queue nor a manufactured success.
        if (!loginWithoutWaiter) DispatchPacket(new ProtocolPacket(opcode, payload));
        return true;
    }
}
}
