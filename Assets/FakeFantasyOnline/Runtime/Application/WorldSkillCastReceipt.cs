using System;
using System.Threading;

namespace FakeFantasyOnline.Application
{

/// <summary>
/// Bounded one-shot provenance shared by a decoded cast and all its immutable observations.
/// Its lifetime is the existing retained event's lifetime, not a map window or a second queue.
/// Payload values, actor identity and clocks are never cached in this receipt.
/// </summary>
public sealed class WorldSkillCastReceipt
{
    // 0 = unhandled, 1 = executing, 2 = applied, 3 = failed after an application attempt.
    private int applicationState;

    public bool NativeApplicationAttempted => Volatile.Read(ref applicationState) != 0;
    public bool NativeOwnerApplied => Volatile.Read(ref applicationState) == 2;
    public bool NativeOwnerFailed => Volatile.Read(ref applicationState) == 3;

    public void ApplyNativeOwner(Action apply)
    {
        if (apply == null) throw new ArgumentNullException(nameof(apply));
        if (Interlocked.CompareExchange(ref applicationState, 1, 0) != 0) return;
        try
        {
            apply();
            Volatile.Write(ref applicationState, 2);
        }
        catch
        {
            // Native bodies can already have changed pose, cooldown or effect state. A failed
            // attempt is never turned back into deferred work, nor described as successful.
            Volatile.Write(ref applicationState, 3);
            throw;
        }
    }
}

}
