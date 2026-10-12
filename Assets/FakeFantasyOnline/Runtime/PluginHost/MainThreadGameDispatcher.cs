using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FakeFantasyOnline.PluginHost
{

/// <summary>
/// Bounded handoff to the constructing thread. Only Pump invokes callbacks.
/// Cancellation can withdraw queued work; it cannot preempt an action that already started.
/// </summary>
public sealed class MainThreadGameDispatcher : IDisposable
{
    private readonly object gate = new object();
    private readonly LinkedList<Work> queue = new LinkedList<Work>();
    private readonly int ownerThreadId;
    private readonly int maxPending;
    private bool disposed;

    public MainThreadGameDispatcher(int maxPending = 1024)
    {
        if (maxPending <= 0) throw new ArgumentOutOfRangeException(nameof(maxPending));
        this.maxPending = maxPending;
        ownerThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public int PendingCount { get { lock (gate) return queue.Count; } }

    public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken token = default(CancellationToken))
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (token.IsCancellationRequested) return Task.FromCanceled<T>(token);
        var work = new Work<T>(this, action, token);
        lock (gate)
        {
            if (disposed) return Task.FromException<T>(new ObjectDisposedException(nameof(MainThreadGameDispatcher)));
            if (queue.Count >= maxPending) return Task.FromException<T>(new InvalidOperationException("The main-thread dispatch queue is full."));
            work.Node = queue.AddLast(work);
        }
        work.RegisterCancellation();
        return work.Task;
    }

    /// <returns>Number of dequeued jobs, including jobs cancelled before their callback could run.</returns>
    public int Pump(int maxCount = 64)
    {
        if (Thread.CurrentThread.ManagedThreadId != ownerThreadId)
            throw new InvalidOperationException("Only the dispatcher's owning thread can pump game actions.");
        if (maxCount < 0) throw new ArgumentOutOfRangeException(nameof(maxCount));
        int count = 0;
        while (count < maxCount)
        {
            Work work;
            bool cancelled;
            lock (gate)
            {
                if (disposed || queue.Count == 0) break;
                work = queue.First.Value;
                queue.RemoveFirst();
                work.Node = null; // Claim execution atomically against Cancel and Dispose.
                cancelled = work.Token.IsCancellationRequested;
            }
            count++;
            if (cancelled) work.Cancel();
            else work.Execute();
        }
        return count;
    }

    private void CancelQueued(Work work)
    {
        lock (gate)
        {
            if (work.Node == null) return;
            queue.Remove(work.Node);
            work.Node = null;
        }
        work.Cancel();
    }

    public void Dispose()
    {
        List<Work> pending;
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            pending = new List<Work>(queue.Count);
            foreach (var work in queue) { work.Node = null; pending.Add(work); }
            queue.Clear();
        }
        // Never wait for a token callback while holding the queue lock.
        foreach (var work in pending) work.Fail(new ObjectDisposedException(nameof(MainThreadGameDispatcher)));
    }

    private abstract class Work
    {
        private readonly MainThreadGameDispatcher owner;
        private readonly object registrationGate = new object();
        private CancellationTokenRegistration registration;
        private bool hasRegistration;
        private bool completed;
        protected Work(MainThreadGameDispatcher owner, CancellationToken token) { this.owner = owner; Token = token; }
        public CancellationToken Token { get; }
        public LinkedListNode<Work> Node { get; set; }
        public void RegisterCancellation()
        {
            if (!Token.CanBeCanceled) return;
            // Register may synchronously invoke the callback if cancellation won the enqueue race.
            var created = Token.Register(() => owner.CancelQueued(this));
            bool release;
            lock (registrationGate)
            {
                release = completed;
                if (!release) { registration = created; hasRegistration = true; }
            }
            if (release) created.Dispose();
        }
        protected void Complete()
        {
            CancellationTokenRegistration toDispose = default(CancellationTokenRegistration);
            bool release;
            lock (registrationGate)
            {
                completed = true;
                release = hasRegistration;
                if (release) { toDispose = registration; hasRegistration = false; }
            }
            if (release) toDispose.Dispose();
        }
        public abstract void Execute();
        public abstract void Cancel();
        public abstract void Fail(Exception exception);
    }

    private sealed class Work<T> : Work
    {
        private readonly TaskCompletionSource<T> completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        private Func<T> action;
        public Work(MainThreadGameDispatcher owner, Func<T> action, CancellationToken token) : base(owner, token) { this.action = action; }
        public Task<T> Task { get { return completion.Task; } }
        public override void Execute()
        {
            try
            {
                var result = action();
                action = null;
                Complete();
                completion.TrySetResult(result);
            }
            catch (Exception error) { Fail(error); }
        }
        public override void Cancel()
        {
            action = null;
            Complete();
            completion.TrySetCanceled(Token);
        }
        public override void Fail(Exception exception)
        {
            action = null;
            Complete();
            completion.TrySetException(exception);
        }
    }
}
}
