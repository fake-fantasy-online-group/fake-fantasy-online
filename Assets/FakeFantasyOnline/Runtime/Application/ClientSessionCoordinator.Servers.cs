using System;
using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
// Lossless access to the actual transport-owned endpoint. No directory-index reconstruction,
// normalization, DNS lookup, connection attempt, or second endpoint cache lives in this seam.
public interface ITitleServerEndpointPort
{
    string TitleServerHost { get; }
    int TitleServerPort { get; }
    int TitleServerCount { get; }
    IReadOnlyList<ServerSelectionItem> ReadTitleServers();
    void SetTitleServerEndpoint(string host, int port);
}

public sealed partial class ClientSessionCoordinator
{
    private ITitleServerEndpointPort TitleServerEndpoint =>
        _gateway as ITitleServerEndpointPort ??
        throw new InvalidOperationException("The gateway has no title endpoint binding.");

    public string ServerHostForIndependentRuntime => TitleServerEndpoint.TitleServerHost;
    public int ServerPortForIndependentRuntime => TitleServerEndpoint.TitleServerPort;
    public int TitleServerCountForIndependentRuntime => TitleServerEndpoint.TitleServerCount;
    public IReadOnlyList<ServerSelectionItem> ReadTitleServersForIndependentRuntime() =>
        TitleServerEndpoint.ReadTitleServers();

    // TitleFlow owns the native endpoint comparison. This bridge applies that decision to the
    // existing session owner; the old public ConfirmServerSelection remains for non-GUI callers.
    // The copied SrvLine endpoint is passed through even if a same-length directory has changed;
    // looking its endpoint up again by row index would defeat the original tree cache semantics.
    // These stage/index errors are retained adapter boundaries, never reported as a switch.
    public Result ApplyServerSelectionForIndependentRuntime(int directoryIndex, string host, int port,
        bool switchEndpoint, int? nativeDirectoryCount = null)
    {
        if (_state != ClientSessionState.ServerSelection)
            return Result.Failure("session.stage.invalid", "Server selection is not active.");
        if (switchEndpoint)
        {
            if (directoryIndex < 0 || directoryIndex >= (nativeDirectoryCount ?? _directory.Count))
                return Result.Failure("server.selection.invalid", "The selected server does not exist.");
            _gateway.Dispose();
            TitleServerEndpoint.SetTitleServerEndpoint(host, port);
            _preferredServerIndex = directoryIndex;
            Interlocked.Exchange(ref _localPlayerStatusEffectConsumedRevision, 0);
            _connectedServerIndex = -1;
            _characters = Array.Empty<ServerCharacterRecord>();
            TransitionTo(ClientSessionState.Offline, ServerSwitchReauthenticationCode);
            return Result.Failure(ServerSwitchReauthenticationCode,
                "Log in again to connect to the selected server.");
        }
        TransitionTo(ClientSessionState.CharacterSelection, string.Empty);
        return Result.Success();
    }

    // ef4 Return is only state=Login. It neither closes transport nor clears the roster.
    public void ReturnToLoginFromServerPageForIndependentRuntime() =>
        TransitionTo(ClientSessionState.Offline, string.Empty);
}
}
