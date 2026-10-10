using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IWorldSkillBookObservationSource
{
    private ISkillBookReceiveOwner _skillBookReceiveOwner;
    // Optional/headless construction is supported. Keep only the latest reached value of
    // each of the three roots until a genuine owner appears, not packets or replay actions.
    // Null collections preserve the as-yet-unobserved native roots on that first binding.
    private WorldSkillBookReceipt _unboundSkillBookReceipt;

    private void BindSkillBookReceiveOwner(ISkillBookReceiveOwner owner)
    {
        if (_skillBookReceiveOwner != null || owner == null) return;
        _skillBookReceiveOwner = owner;
        if (_unboundSkillBookReceipt == null) return;
        WorldSkillBookState actual = owner.ReceiveSkillBook(_unboundSkillBookReceipt);
        PublishSkillBookState(actual);
        _unboundSkillBookReceipt = null;
    }

    private WorldSkillBookObservation _skillBookObservation =
        new WorldSkillBookObservation(WorldSkillBookState.Empty);

    public WorldSkillBookObservation SkillBookObservation
    {
        get { lock (_worldStateSync) return _skillBookObservation; }
    }

    private void PublishSkillBookObservation(WorldSkillBookObservation observation)
    {
        lock (_worldStateSync)
        {
            _skillBookObservation = observation;
            _skillBookState = observation.MaterializedState;
            System.Threading.Interlocked.Increment(ref _skillBookStateVersion);
        }
    }

    private WorldSkillBookObservation ReceiveSkillBookObservation(byte[] payload)
    {
        if (!(_skillBookReceiveOwner is ISkillBookStreamReceiveOwner owner))
            return new WorldSkillBookObservation(ReceiveSkillBook(payload));

        var reader = new NetClient.Buf(payload);
        WorldSkillBookObservation observation = default;
        try
        {
            owner.ReceiveSkillBook(reader, out observation);
        }
        finally
        {
            // Prefix failures publish only reached roots, with no projection which could
            // replace the native exception. A missing first I32 publishes nothing.
            if (observation.HasValue) PublishSkillBookObservation(observation);
        }
        return observation;
    }

    // Both Pump entry points apply reached roots before a required-read failure escapes.
    // Owner/tip failures never enter the decoder's compatibility catch or a replay marker.
    private WorldSkillBookState ReceiveSkillBook(byte[] payload)
    {
        WorldSkillBookReceipt receipt = null;
        WorldSkillBookState decoded = null;
        WorldSkillBookState actual = null;
        bool complete = false;
        try
        {
            decoded = PrivateServerSkillBookCodec.DecodeSkillBook(payload, out receipt,
                projectSnapshot: _skillBookReceiveOwner == null);
            complete = true;
        }
        finally
        {
            if (receipt != null)
            {
                if (_skillBookReceiveOwner != null)
                {
                    actual = _skillBookReceiveOwner.ReceiveSkillBook(receipt);
                }
                else
                {
                    WorldSkillBookReceipt previous = _unboundSkillBookReceipt;
                    _unboundSkillBookReceipt = new WorldSkillBookReceipt(receipt.SkillPoints,
                        receipt.Skills ?? previous?.Skills,
                        receipt.DamageBonuses ?? previous?.DamageBonuses);
                    actual = complete ? decoded : receipt.ApplyToSnapshot(SkillBookState);
                }
                // No cancellation gate or queue can undo the reached stores and their mirror.
                PublishSkillBookState(actual);
            }
        }
        return actual;
    }
}
}
