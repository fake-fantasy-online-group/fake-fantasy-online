using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // The local native-world return is already on the Unity host. It has no server
    // world-drain/roster handoff; preserve the original writer's own connected gate.
    public void SendTitleWorldReturnForIndependentRuntime() => _netClient.LeaveWorld();

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> LeaveWorldForTitleAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationTokenSource connection;
        CancellationToken connectionToken;
        lock (_worldDrainSync)
        {
            if (_stage != GatewayStage.InWorld || _connectionLifetime == null)
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "session.stage.invalid", "The session is not in the world.");
            connection = _connectionLifetime;
            connectionToken = connection.Token;
            // Retire this world's untouched retained tail under the same authority lock.
            // Later roster replies arrive after this boundary and must remain available.
            _worldDrainActive = false;
            while (_dispatchedReady.Wait(0))
                _dispatchedDownlinks.TryDequeue(out _);
            _stage = GatewayStage.LeavingWorld;
        }

        using (var handoff = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectionToken))
        {
            try
            {
                await StopWorldDrainAsync().ConfigureAwait(false);
                handoff.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(connection, _connectionLifetime))
                    return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                        "session.connection.changed", "The world connection changed during return.");

                ClearDepartedPlayerSnapshots(handoff.Token);
                await SendAsync(() => _netClient.LeaveWorld(), handoff.Token).ConfigureAwait(false);
                ProtocolPacket packet = await ReadUntilAsync(ProtocolOpcodes.CharacterList, handoff.Token)
                    .ConfigureAwait(false);
                IReadOnlyList<ServerCharacterRecord> roster = ReadReceivedCharacterRoster(packet);
                handoff.Token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(connection, _connectionLifetime))
                    return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                        "session.connection.changed", "The world connection changed during return.");
                _stage = GatewayStage.Authenticated;
                return Result<IReadOnlyList<ServerCharacterRecord>>.Success(roster);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (ReferenceEquals(connection, _connectionLifetime))
                    await DisposeConnectionAsync().ConfigureAwait(false);
                throw;
            }
            catch (OperationCanceledException)
            {
                if (ReferenceEquals(connection, _connectionLifetime))
                    await DisposeConnectionAsync().ConfigureAwait(false);
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "world.return.cancelled", "The world connection ended or the character list timed out.");
            }
            catch (Exception exception) when (IsWireFailure(exception))
            {
                if (ReferenceEquals(connection, _connectionLifetime))
                    await DisposeConnectionAsync().ConfigureAwait(false);
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "world.return.failure", "The world return request or character list failed.");
            }
        }
    }

    private void ClearDepartedPlayerSnapshots(CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Interlocked.Exchange(ref _localPlayerLifeState, null) != null)
                Interlocked.Increment(ref _localPlayerLifeStateVersion);
            if (Interlocked.Exchange(ref _localRidingState, null) != null)
                Interlocked.Increment(ref _localRidingStateVersion);
        }
    }

}
}
