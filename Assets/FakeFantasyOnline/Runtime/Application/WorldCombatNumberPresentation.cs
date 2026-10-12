using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{

// Capable gateways identify the exact journal reset, including a disconnect while no frame
// is polling. A reused authenticated socket must not reset successful consumption at login.
public interface IWorldCombatNumberStreamSource
{
    long CombatNumberStreamGeneration { get; }
}

/// <summary>Connection-owned one-shot numbers and retryable, unpresented experience edges.</summary>
public sealed class WorldCombatNumberPresentation
{
    public const float RetryIntervalSeconds = 0.25f;
    public const int RetryBudget = 16;
    public const int MaximumPendingNumbers = 256;
    private readonly object sync = new object();
    private readonly LinkedList<WorldCombatNumberState> pending = new LinkedList<WorldCombatNumberState>();
    private LinkedListNode<WorldCombatNumberState> retryCursor;
    public int DroppedPresentationCount { get; private set; }
    private IReadOnlyList<WorldCombatNumberState> observedNumbers;
    private long observedRevision;
    private float nextRetry;
    private bool backlogWarningRaised;
    private bool backlogWarningPending;

    public int PendingCount { get { lock (sync) return pending.Count; } }
    public long ConsumedRevision
    {
        get
        {
            lock (sync)
            {
                long consumed = observedRevision;
                foreach (WorldCombatNumberState number in pending)
                    consumed = Math.Min(consumed, number.Revision - 1);
                return consumed;
            }
        }
    }

    public bool TryPresent(IReadOnlyList<WorldCombatNumberState> numbers, float now,
        Func<WorldCombatNumberState, bool> present)
    {
        if (numbers == null) throw new ArgumentNullException(nameof(numbers));
        if (present == null) throw new ArgumentNullException(nameof(present));
        lock (sync)
        {
            // A failed target must not monopolize presentation or allocate/copy the complete
            // backlog every frame. Round-robin retries are bounded and use unscaled time.
            if (pending.Count > 0 && now >= nextRetry)
            {
                nextRetry = now + RetryIntervalSeconds;
                int attempts = Math.Min(RetryBudget, pending.Count);
                for (int index = 0; index < attempts; index++)
                {
                    LinkedListNode<WorldCombatNumberState> node = retryCursor ?? pending.First;
                    LinkedListNode<WorldCombatNumberState> next = node.Next ?? pending.First;
                    bool shown = present(node.Value); // Keep the edge if the callback throws.
                    if (shown) pending.Remove(node);
                    retryCursor = pending.Count == 0 ? null :
                        (next.List == pending ? next : pending.First);
                }
            }

            // Gateway snapshots are immutable. Observe a replacement only once, and use
            // revisions (never amounts) to distinguish repeated equal-valued rewards.
            if (!ReferenceEquals(numbers, observedNumbers))
            {
                foreach (WorldCombatNumberState number in numbers)
                {
                    if (number == null || number.Revision <= observedRevision) continue;
                    bool shown = present(number);
                    if (!shown && number.Type == WorldCombatNumberState.ExperienceType)
                    {
                        if (pending.Count == 0) nextRetry = now + RetryIntervalSeconds;
                        // This is a bounded visual backlog, not the authoritative XP balance.
                        // Only pathological saturation drops the oldest unshown popup.
                        if (pending.Count == MaximumPendingNumbers)
                        {
                            if (retryCursor == pending.First) retryCursor = pending.First.Next;
                            pending.RemoveFirst();
                            DroppedPresentationCount++;
                            if (!backlogWarningRaised)
                            {
                                backlogWarningRaised = true;
                                backlogWarningPending = true;
                            }
                        }
                        pending.AddLast(number);
                    }
                    observedRevision = number.Revision;
                }
                observedNumbers = numbers;
            }
            return pending.Count == 0;
        }
    }

    public bool TakeBacklogWarning()
    {
        lock (sync)
        {
            bool result = backlogWarningPending;
            backlogWarningPending = false;
            return result;
        }
    }

    public void Clear()
    {
        lock (sync)
        {
            pending.Clear();
            retryCursor = null;
            DroppedPresentationCount = 0;
            observedNumbers = null;
            observedRevision = 0;
            nextRetry = 0f;
            backlogWarningRaised = false;
            backlogWarningPending = false;
        }
    }
}

}
