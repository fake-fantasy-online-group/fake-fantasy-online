using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    public Task<Result> PetAwakenAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetCmd(11, 0, string.Empty);
            return Task.FromResult(Result.Success());
        }

        byte[] payload;
        PrivateServerPetCommandCodec.TryEncodeAwaken(out payload);
        return SendPetCommandAsync(payload, "pet.awaken", cancellationToken);
    }

    public Task<Result> PetForgeAsync(PetForgeRequest request, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            if (request == null)
                return Task.FromResult(Result.Failure("pet.forge.invalid", "The pet forge request is missing."));
            _netClient.PetForge(request.Op, request.Kind, request.PetTab,
                request.PetSlot, request.ItemTab, request.ItemSlot, request.SkillId,
                request.Pet2Tab, request.Pet2Slot);
            return Task.FromResult(Result.Success());
        }

        return PetForgeScheduledAsync(request, cancellationToken);
    }

    private async Task<Result> PetForgeScheduledAsync(PetForgeRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return Result.Failure("pet.forge.invalid", "The pet forge request is missing.");
        try
        {
            await SendAsync(() => _netClient.PetForge(request.Op, request.Kind, request.PetTab,
                request.PetSlot, request.ItemTab, request.ItemSlot, request.SkillId,
                request.Pet2Tab, request.Pet2Slot), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("pet.forge.send.failure", "The pet forge request could not be sent.");
        }
    }
}
}
