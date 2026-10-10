using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Unity can destroy a duplicate or scene component before BindOwner runs.
    // Every constructed component therefore owns the initialized-unbound Close
    // state before Awake; this never resolves I or borrows another owner's core.
    private readonly UnboundCloseState unboundCloseForIndependentRuntime = new UnboundCloseState();
    private Action closeNativeForIndependentRuntime;

    internal void BindCloseForIndependentRuntime(Action close) =>
        closeNativeForIndependentRuntime = close;

    // Actual 1.6.0 060006c9 directly enters this owner's Close (060006c8).
    private void OnDestroy()
    {
        Close();
    }

    private void Close()
    {
        if (closeNativeForIndependentRuntime != null)
        {
            // Keep the bound host's native-Close-first cleanup and idempotence.
            closeNativeForIndependentRuntime();
            return;
        }

        unboundCloseForIndependentRuntime.Close();
    }

    // Before binding there is no transport writer. The native constructor 0600078a
    // initializes _txPending; the remaining Close fields have their zero defaults.
    // Retain the full 060006c8 body, including its exception boundaries and null tail,
    // rather than treating destruction of an unbound owner as a no-op.
    // Live binding selects the existing immutable Protocol core; this state never
    // receives a connection or any transport-writing capability.
    private sealed class UnboundCloseState
    {
        private readonly Queue<byte[]> _txPending = new Queue<byte[]>();
        private volatile bool _run;
        private int _txSecCount;
        private float _txSecStart;
        private int _gen;
        private BlockingCollection<byte[]> _txQ;
        private NetworkStream _ns;
        private TcpClient _tcp;

        public void Close()
        {
            _run = false;
            _txPending.Clear();
            _txSecCount = 0;
            _txSecStart = 0f;
            Interlocked.Increment(ref _gen);
            try
            {
                if (_txQ != null)
                {
                    _txQ.CompleteAdding();
                }
            }
            catch
            {
            }
            try
            {
                if (_ns != null)
                {
                    _ns.Close();
                }
                if (_tcp != null)
                {
                    _tcp.Close();
                }
            }
            catch
            {
            }
            _tcp = null;
            _ns = null;
            _txQ = null;
        }
    }
}
}
