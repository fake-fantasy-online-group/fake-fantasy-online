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

    // Login owns an authentication session, unlike registration/recovery. A second
    // login must never reuse a socket on which a Login request has already been sent.
    private bool _loginRequestSent;
    private bool _titleLoginInProgress;
    private bool _taskLoginInProgress;
    private CancellationToken _taskLoginCallerToken;

    public void ConnectTitleLoginForIndependentRuntime(Action<bool> onDone)
    {
        RetireCancelledTaskLogin();
        if (_titleLoginInProgress) return;
        if (_taskLoginInProgress || Volatile.Read(ref _authenticationWaiters) != 0)
        {
            onDone?.Invoke(false);
            return;
        }
        PrepareLoginConnection();
        _titleLoginInProgress = true;
        if (!BeginNativeConnection(ok =>
        {
            if (!ok) _titleLoginInProgress = false;
            onDone?.Invoke(ok);
        }, CancellationToken.None, out _, out _))
        {
            _titleLoginInProgress = false;
            onDone?.Invoke(false);
        }
    }

    private void RetireCancelledTaskLogin()
    {
        // Cancellation between Connect returning and Authenticate starting must not
        // leave an owner that permanently blocks the next login attempt.
        if (_taskLoginInProgress && _taskLoginCallerToken.IsCancellationRequested)
            DisposeConnectionCore();
    }

    private void PrepareLoginConnection()
    {
        if (_loginRequestSent || _stage == GatewayStage.Authenticated)
            DisposeConnectionCore();
    }

    public void CancelTitleLoginForIndependentRuntime()
    {
        if (_titleLoginInProgress) DisposeConnectionCore();
    }

    public void SendTitleLoginForIndependentRuntime(string account, string password)
    {
        if (!_titleLoginInProgress || _loginRequestSent) return;
        _loginRequestSent = true;
        _titleLoginResultWithoutWaiter = true;
        endgameAccount = account;
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
            if (loginWithoutWaiter)
            {
                _titleLoginInProgress = false;
                if (code == 0) _stage = GatewayStage.Authenticated;
            }
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
