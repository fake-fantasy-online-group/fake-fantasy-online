using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{

public sealed partial class ClientSessionCoordinator
{
    // Native GameBootstrap startup creates a local world without login or SelfData.
    // Its real return still selects the original requested title page. This changes
    // only title state: no authenticated server identity, InWorld state or roster receipt.
    public void SelectTitleAfterNativeWorldReturnForIndependentRuntime(bool characterSelection)
    {
        _characters = Array.Empty<ServerCharacterRecord>();
        TransitionTo(characterSelection ? ClientSessionState.CharacterSelection : ClientSessionState.ServerSelection,
            string.Empty);
    }

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> ReturnFromWorldAsync(
        bool characterSelection, CancellationToken cancellationToken)
    {
        bool lockTaken = false;
        try
        {
            await _operation.WaitAsync(cancellationToken);
            lockTaken = true;
            if (_state != ClientSessionState.InWorld)
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "session.stage.invalid", "World return requires an active world session.");
            if (!(_gateway is IWorldReturnCommands commands))
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "world.return.unsupported", "The gateway does not provide world return.");

            Task<Result<IReadOnlyList<ServerCharacterRecord>>> receipt =
                commands.LeaveWorldForTitleAsync(cancellationToken);
            _characters = Array.Empty<ServerCharacterRecord>();
            TransitionTo(characterSelection ? ClientSessionState.CharacterSelection : ClientSessionState.ServerSelection,
                string.Empty);

            Result<IReadOnlyList<ServerCharacterRecord>> roster = await receipt;
            if (!roster.IsSuccess)
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(roster.Error.Code, roster.Error.Message);
            if (_state != ClientSessionState.CharacterSelection &&
                _state != ClientSessionState.ServerSelection &&
                _state != ClientSessionState.CharacterCreation)
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "world.return.superseded", "The title session changed before the roster arrived.");

            return Result<IReadOnlyList<ServerCharacterRecord>>.Success(
                ObserveCharacterRosterResponse(roster.Value));
        }
        catch (OperationCanceledException)
        {
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "world.return.cancelled", "World return was cancelled.");
        }
        finally
        {
            if (lockTaken)
                _operation.Release();
        }
    }
}
}
