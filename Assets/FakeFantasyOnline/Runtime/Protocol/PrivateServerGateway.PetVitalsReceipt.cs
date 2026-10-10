using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private IWorldPetVitalsReceiveOwner _petVitalsReceiveOwner;
    // Only the optional/headless path owns these reached values. Binding transfers them once
    // into the actual roots and clears this projection; there is never a second live owner.
    private WorldPetVitalsState _unboundPetVitals;

    private void BindPetVitalsReceiveOwner(IWorldPetVitalsReceiveOwner owner)
    {
        if (_petVitalsReceiveOwner != null || owner == null) return;
        lock (_worldStateSync)
        {
            _petVitalsReceiveOwner = owner;
            _unboundPetVitals?.ApplyTo(owner);
            _unboundPetVitals = null;
            PublishPetVitalsProjectionLocked();
        }
    }

    private WorldPetVitalsState CurrentPetVitals =>
        _petVitalsReceiveOwner?.PetVitals ?? _unboundPetVitals;

    private void StorePetVital(WorldPetVital field, int value)
    {
        lock (_worldStateSync)
        {
            if (_petVitalsReceiveOwner != null)
                _petVitalsReceiveOwner.ReceivePetVital(field, value);
            else
                _unboundPetVitals = (_unboundPetVitals ?? new WorldPetVitalsState())
                    .WithValue(field, value);
            // Each reached store has its mirror before the next primitive read can fail.
            // No self-status, prior-pet or cancellation predicate can discard the owner write.
            PublishPetVitalsProjectionLocked();
        }
    }

    private void PublishPetVitalsProjectionLocked()
    {
        WorldPlayerStatusState current = Volatile.Read(ref _playerStatusState);
        WorldPetVitalsState vitals = CurrentPetVitals;
        if (current == null || vitals == null) return;
        Volatile.Write(ref _playerStatusState, current.WithPetVitals(vitals));
        Interlocked.Increment(ref _playerStatusStateVersion);
    }

    private bool ReceivePetVitals(byte[] payload)
    {
        BindPetVitalsReceiveOwner(_worldReceiveOwner as IWorldPetVitalsReceiveOwner);
        PrivateServerWorldStateDecoder.DecodePetVitals(payload, StorePetVital);
        return true;
    }

    private WorldPlayerPetStatusState ReceivePetSnapshotVitals(byte[] payload)
    {
        // Compatibility for custom owners without IWorldPetSnapshotReceiveOwner. The actual
        // live owners use ReceivePetSnapshot and write their full native prefix directly.
        if (!PrivateServerWorldStateDecoder.TryDecodePlayerPetStatus(payload, out var pet))
            return null;
        BindPetVitalsReceiveOwner(_worldReceiveOwner as IWorldPetVitalsReceiveOwner);
        // Successful-full order: level/experience/threshold, hunger/trust/points, HP/SP.
        StorePetVital(WorldPetVital.Level, pet.Level);
        StorePetVital(WorldPetVital.Experience, pet.Detail.Experience);
        StorePetVital(WorldPetVital.ExperienceToNext, pet.Detail.ExperienceToNext);
        StorePetVital(WorldPetVital.Starve, pet.Detail.Starve);
        StorePetVital(WorldPetVital.Trust, pet.Detail.Trust);
        StorePetVital(WorldPetVital.FreePoints, pet.Detail.FreePoints);
        StorePetVital(WorldPetVital.HitPoints, pet.HitPoints);
        StorePetVital(WorldPetVital.MaximumHitPoints, pet.MaximumHitPoints);
        StorePetVital(WorldPetVital.MagicPoints, pet.MagicPoints);
        StorePetVital(WorldPetVital.MaximumMagicPoints, pet.MaximumMagicPoints);
        return pet;
    }
}
}
