using System;
using System.Threading;
using System.Threading.Tasks;

namespace FakeFantasyOnline.Foundation
{

/// <summary>
/// Minimal host-thread execution capability. The host is the single thread that owns the
/// state a caller must not touch concurrently (the Unity main thread in the product).
/// The implementation never blocks a caller on a cross-thread hand-off: either the action
/// runs inline because the caller already is the host, or it is posted and observed through
/// the returned task.
/// </summary>
public interface IHostExecutor
{
    /// <summary>True when the calling thread is the captured host thread.</summary>
    bool IsHostThread { get; }

    /// <summary>
    /// Runs <paramref name="action"/> on the host thread and completes when it really ran.
    /// A token that is already cancelled, or that is cancelled before the action starts, keeps
    /// the action from running and completes the task as cancelled with that token. Once the
    /// action has started its real outcome is reported: a faulted action completes as faulted,
    /// an action that throws <see cref="OperationCanceledException"/> completes as cancelled
    /// with the token carried by that exception, and a completed action completes successfully
    /// even if the token is cancelled afterwards.
    /// </summary>
    Task RunAsync(Action action, CancellationToken cancellationToken);
}

/// <summary>
/// <see cref="IHostExecutor"/> over an explicitly supplied <see cref="SynchronizationContext"/>
/// and host thread id. Both are captured by the composition root (the Unity main thread) and
/// injected; nothing here discovers a context by itself and nothing here is a global static.
/// <para>
/// There is exactly one start decision, shared by the host path and by the posted path: under
/// one lock an entry is looked up, and while that lock is still held the executor state and the
/// caller token are read, the entry is removed and the outcome is fixed - not started and
/// cancelled, or started. Whoever wins that decision is the only one that completes the task, so
/// an action that started always reports its real outcome and an action that did not start never
/// runs. No foreign <see cref="Action"/> runs while the lock is held and no
/// <see cref="CancellationTokenRegistration"/> is disposed while the lock is held, because a
/// cancellation callback needs that same lock.
/// </para>
/// </summary>
public sealed class SynchronizationContextHostExecutor : IHostExecutor, IDisposable
{
    private readonly SynchronizationContext _context;
    private readonly int _hostThreadId;
    private readonly object _sync = new object();
    // Cancelled once by Dispose and never disposed itself: its token is the cancel identity
    // handed to every later caller, and a disposed source could no longer hand out a token.
    private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
    private readonly System.Collections.Generic.Dictionary<TaskCompletionSource<object>, CancellationTokenRegistration> _queued =
        new System.Collections.Generic.Dictionary<TaskCompletionSource<object>, CancellationTokenRegistration>();
    private bool _closed;

    public SynchronizationContextHostExecutor(SynchronizationContext context, int hostThreadId)
    {
        _context = context;
        _hostThreadId = hostThreadId;
    }

    public bool IsHostThread => Thread.CurrentThread.ManagedThreadId == _hostThreadId;

    public Task RunAsync(Action action, CancellationToken cancellationToken)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (cancellationToken.IsCancellationRequested)
        {
            // Pre-cancelled: the action never reaches the queue and the token really is
            // cancelled, so Task.FromCanceled is valid here.
            return Task.FromCanceled(cancellationToken);
        }

        TaskCompletionSource<object> completion =
            new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_sync)
        {
            // One linearization point, shared by the host path and the off-host path: the
            // closed check and the queue insertion happen together, so "in the queue" means
            // exactly "not started yet". No second window is opened after this check.
            if (_closed)
            {
                return Task.FromCanceled(_shutdown.Token);
            }

            _queued.Add(completion, default(CancellationTokenRegistration));
        }

        if (IsHostThread)
        {
            // Host path: the same start decision as the posted path, only without a post.
            StartQueued(completion, action, cancellationToken);
            return completion.Task;
        }

        if (_context == null)
        {
            ReportUnstarted(
                completion,
                new InvalidOperationException(
                    "The host thread dispatcher is unavailable: no synchronization context was supplied."));
            return completion.Task;
        }

        CancellationTokenRegistration registration = cancellationToken.Register(
            () => CancelQueued(completion, cancellationToken));
        bool retained;
        lock (_sync)
        {
            retained = _queued.ContainsKey(completion);
            if (retained) _queued[completion] = registration;
        }

        if (!retained)
        {
            // The cancel callback already won the entry: that path owns the completion.
            registration.Dispose();
            return completion.Task;
        }

        try
        {
            _context.Post(ignored => StartQueued(completion, action, cancellationToken), null);
        }
        catch (Exception exception)
        {
            // Posting failed: the action never started, so this is reported as a fault of the
            // dispatch itself, not as a cancellation.
            ReportUnstarted(completion, exception);
        }

        return completion.Task;
    }

    /// <summary>
    /// Closes the dispatcher. Queued actions that have not started are not executed: their
    /// tasks complete as cancelled with the shutdown token, so no caller is left awaiting a
    /// task that can never finish. Actions that already started keep their real outcome. The
    /// owner of the executor (the composition root that created it, or the QA entry that built
    /// the audited graph) calls this once, after the graph it served is disposed.
    /// </summary>
    public void Dispose()
    {
        System.Collections.Generic.KeyValuePair<TaskCompletionSource<object>, CancellationTokenRegistration>[] pending;
        lock (_sync)
        {
            if (_closed) return;
            _closed = true;
            // Cancel before any Task.FromCanceled(_shutdown.Token) can be produced, and only
            // here: no callback is registered on this source, so cancelling under the lock
            // cannot run foreign code.
            _shutdown.Cancel();
            pending = new System.Collections.Generic.KeyValuePair<TaskCompletionSource<object>, CancellationTokenRegistration>[_queued.Count];
            int index = 0;
            foreach (System.Collections.Generic.KeyValuePair<TaskCompletionSource<object>, CancellationTokenRegistration> entry in _queued)
            {
                pending[index++] = entry;
            }

            _queued.Clear();
        }

        foreach (System.Collections.Generic.KeyValuePair<TaskCompletionSource<object>, CancellationTokenRegistration> entry in pending)
        {
            // Outside the lock: disposing a registration waits for a callback that is already
            // running on another thread, and that callback needs the queue lock. The entries
            // were removed under the lock above, so no start can also complete one of them.
            entry.Value.Dispose();
            entry.Key.TrySetCanceled(_shutdown.Token);
        }
    }

    /// <summary>
    /// The single start decision. Under one lock: the entry must still be queued, then the
    /// executor state and the caller token decide whether it is cancelled or started, and the
    /// entry is removed inside that same critical section. Everything that can block or that
    /// runs foreign code happens after the lock.
    /// </summary>
    private void StartQueued(
        TaskCompletionSource<object> completion,
        Action action,
        CancellationToken cancellationToken)
    {
        CancellationTokenRegistration registration;
        QueuedOutcome outcome = BeginQueued(completion, cancellationToken, out registration);
        if (outcome == QueuedOutcome.NotQueued)
        {
            // A cancel callback or Dispose won the entry: the action does not run and that
            // path has already completed the task.
            return;
        }

        registration.Dispose();
        if (outcome == QueuedOutcome.CanceledByToken)
        {
            completion.TrySetCanceled(cancellationToken);
            return;
        }

        if (outcome == QueuedOutcome.CanceledByShutdown)
        {
            completion.TrySetCanceled(_shutdown.Token);
            return;
        }

        // Started: the action runs outside the lock and its own outcome is the task outcome.
        Complete(completion, action);
    }

    /// <summary>
    /// Cancellation of an action that has not started. It uses the same decision as
    /// <see cref="StartQueued"/>, so a cancellation and a start can never both win: whoever
    /// removes the entry under the lock completes the task, exactly once.
    /// </summary>
    private void CancelQueued(TaskCompletionSource<object> completion, CancellationToken cancellationToken)
    {
        CancellationTokenRegistration registration;
        QueuedOutcome outcome = BeginQueued(completion, cancellationToken, out registration);
        if (outcome != QueuedOutcome.CanceledByToken)
        {
            // NotQueued: the action already started, or Dispose already completed the task.
            return;
        }

        registration.Dispose();
        completion.TrySetCanceled(cancellationToken);
    }

    private void ReportUnstarted(TaskCompletionSource<object> completion, Exception exception)
    {
        CancellationTokenRegistration registration;
        // No token here: a failed dispatch is a fault and must not be reported as a
        // cancellation just because the caller token happens to be cancelled.
        if (BeginQueued(completion, CancellationToken.None, out registration) != QueuedOutcome.Started)
        {
            return;
        }

        registration.Dispose();
        completion.TrySetException(exception);
    }

    private QueuedOutcome BeginQueued(
        TaskCompletionSource<object> completion,
        CancellationToken cancellationToken,
        out CancellationTokenRegistration registration)
    {
        lock (_sync)
        {
            if (!_queued.TryGetValue(completion, out registration))
            {
                return QueuedOutcome.NotQueued;
            }

            _queued.Remove(completion);
            if (_closed)
            {
                return QueuedOutcome.CanceledByShutdown;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return QueuedOutcome.CanceledByToken;
            }

            return QueuedOutcome.Started;
        }
    }

    private static void Complete(TaskCompletionSource<object> completion, Action action)
    {
        try
        {
            action();
        }
        catch (OperationCanceledException exception)
        {
            // The action itself reports cancellation. TrySetCanceled takes the token as cancel
            // information and does not require it to be cancelled, so an action that throws
            // OperationCanceledException with CancellationToken.None still completes as
            // cancelled here instead of producing a second exception; the posted path and the
            // host path behave identically.
            completion.TrySetCanceled(exception.CancellationToken);
            return;
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
            return;
        }

        completion.TrySetResult(null);
    }

    private enum QueuedOutcome
    {
        NotQueued,
        Started,
        CanceledByToken,
        CanceledByShutdown,
    }

}
}
