using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Bounded source recovery of the official 1.5.8 connection owner and connect-completion
/// queue: 0x060006BB Connect(), 0x060006BC ConnectAsync(Action&lt;bool&gt;), the 0x06000782
/// connect worker and 0x060006BD EstablishConnection().
/// Only the background completion path is consumed by Update (0x0600077D); the
/// already-connected and the address-not-ready paths call the callback synchronously on the
/// calling thread.
/// </summary>
public sealed partial class NetClient
{
    // Official public state (atlas fields, owner=NetClient). The official member names are
    // kept so the ledger keeps a 1:1 identity: host (+0x20, String) and port (+0x28, Int32)
    // are plain public fields, AddressReady (+0x2c) and AddressError (+0x30) are
    // auto-properties (only their backing fields appear in the atlas), OnError (+0x70) is a
    // public Action<string> field. The address coroutine owns their startup publication;
    // the existing login bridge reads those outputs. Direct Connect/ConnectAsync frontend
    // routing remains separate from that address-resolution recovery.
    public string host;
    public int port;

    public bool AddressReady { get; private set; }

    public string AddressError { get; private set; }

    // Official SetupNet result slots, +0x40 and +0x48. Dispatch reads each live slot once.
    public Action<int, string> OnRegisterResult;
    public Action<int, string> OnResetResult;

    public Action<string> OnError;

    // Official NetClient.OnNotice is a public System.Action<string> instance field at +0x78
    // (120 decimal, atlas field row, token 0x040009c6), sitting between OnError (+0x70) and
    // OnDisconnected (+0x80). Dispatch (0x0600077f) reads it exactly once inside the 0x8008
    // case, at arm64 0x24f8d4 / x86_64 0x25de97, i.e. after the case's own Str read and after
    // its U8 display-mode read, and the very next test is the null check: when the field is
    // set, the decoded string is handed to the delegate and the case returns, so no display
    // mode is switched on, nothing reaches ChatUI/CancelWork and the packet is not published
    // to the dialog-text edge window. The read is a plain field load at that address, not a
    // value captured when some subscriber registered, so the product reads the field at the
    // same point; a cached delegate or a mirrored bool would change which packets take the
    // delegate arm after a late assignment. The title now installs its netMsg-only callback
    // through the composition root and reinstalls it on title reconstruction. Original
    // TitleFlow.OnDestroy (06000ec3) clears seven other slots but leaves +0x78 untouched;
    // neither title destruction nor world entry clears it here. A non-null live slot still
    // consumes the notice before the default mode tails, including while a world is active.
    public Action<string> OnNotice;

    // Official NetClient.OnDisconnected, public System.Action at +0x80 (atlas offset 128); Update
    // (0x0600077D) reads it once per detected drop, right after Close(), and invokes it with no
    // argument; never routed to OnError and never cleared by Close. The official assignment site is
    // the tail of GameBootstrap.Start (0x060003ef, arm64 060003ef.c:268-282 / x86_64 :231-243), a
    // single store after a NetClient.I null test; TitleFlow.SetupNet (0x06000ebe) is not that site
    // (it touches only +0x70/+0x78 and six call slots, 06000ebe.c:70-102). WBC-119 restores that
    // one store as a product-independent binding in Bootstrap/ClientBootstrap.cs, so this field now
    // has a real subscriber instead of staying null.
    public Action OnDisconnected;

    // +0xe8 / +0xf0 / +0xf8.
    // Volatile semantics: every native access to _connecting (and to _run / _dropped, see
    // NetClient.cs) is wrapped by thunk_FUN_001844d8 (arm64 0x1844d8: `dmb ish; ret`) in the
    // load-then-barrier / barrier-then-store form, i.e. the IL2CPP volatile access barrier,
    // while ordinary fields such as _nextPingAt get no barrier at all. The recovered
    // Assembly-CSharp.dll cannot confirm the C# spelling either way: Cpp2IL rebuilds field
    // signatures from Il2CppType, which does not carry a custom modifier, so scanning those
    // signatures for modreq(IsVolatile) proves nothing. The two-argument helper used after
    // reference stores (thunk_FUN_00178e5c / FUN_0017777c, arm64 0x1f9534) is the IL2CPP GC
    // card-marking write barrier, so no Volatile.Write/Thread.MemoryBarrier is emitted here
    // for _connectCb.
    private volatile bool _connecting;
    private readonly ConcurrentQueue<bool> _connectDone;
    private Action<bool> _connectCb;

    // Official 0x060006BB Connect() (arm64 0x2649c8 / x86_64 0x273d50, 180 bytes). Both
    // architectures have no LSDA index entry for this start address, so the body has no
    // exception region. Synchronous: everything runs on the calling thread.
    //   if (_tcp != null && _tcp.Connected) return true;                     0x2649f8
    //   if (AddressReady) return EstablishConnection();                      0x264a1c (tail)
    //   OnError?.Invoke(string.IsNullOrEmpty(AddressError)
    //       ? "正在获取服务器地址, 请稍候…"                        usage 17719 @0x14aa258
    //       : AddressError);                                                  0x264a34
    //   return false;                                                         0x264a6c
    // The delegate call is null-conditional in the native body (cbz x20 -> return false), so
    // OnError?.Invoke is the official form and not a product softening.
    public bool Connect()
    {
        if (_tcp != null && _tcp.Connected)
        {
            return true;
        }

        if (AddressReady)
        {
            return EstablishConnection();
        }

        OnError?.Invoke(
            string.IsNullOrEmpty(AddressError) ? "正在获取服务器地址, 请稍候…" : AddressError);
        return false;
    }

    // Official 0x060006BC ConnectAsync(Action<bool> onDone) (arm64 0x265008 / x86_64
    // 0x2743d0, 464 bytes; no LSDA index entry in either architecture -> no exception
    // region). The official signature has no CancellationToken.
    public void ConnectAsync(Action<bool> onDone)
    {
        if (_tcp != null && _tcp.Connected)
        {
            onDone?.Invoke(true);                                          // 0x265084, w1 = 1
            return;
        }

        // _connecting is read before the barrier (0x2650a8 ldrb / 0x2650ac barrier). When a
        // connect attempt is already in flight the official body returns without ever
        // calling onDone (0x2650b4).
        if (_connecting)
        {
            return;
        }

        if (!AddressReady)
        {
            OnError?.Invoke(
                string.IsNullOrEmpty(AddressError) ? "正在获取服务器地址, 请稍候…" : AddressError);
            onDone?.Invoke(false);                                         // 0x2651c0, w1 = 0
            return;
        }

        // 0x2650cc barrier -> 0x2650d4 _connecting = 1 -> 0x2650dc _connectCb = onDone
        // (followed by the GC write barrier 0x17777c).
        _connecting = true;
        _connectCb = onDone;

        // ThreadStart(this, usage 8151 = NetClient.<ConnectAsync>b__55_0 = token 0x06000782),
        // then Thread(ts), IsBackground = true, Name = usage 12919 "NetClient-Connect",
        // Start(). Official keeps no field for the thread handle.
        Thread thread = new Thread(ConnectWorker)
        {
            IsBackground = true,
            Name = "NetClient-Connect"
        };
        thread.Start();
    }

    // Read-only adapter for the Task-based login caller: distinguish the native silent return
    // from a retained callback without duplicating ConnectAsync's Connected/connecting tests.
    // Neither the native latch nor its single callback is written by this projection.
    internal bool OwnsConnectCallbackForIndependentRuntime(Action<bool> callback) =>
        ReferenceEquals(_connectCb, callback);

    // Official 0x06000782 NetClient.<ConnectAsync>b__55_0() (arm64 0x2745a4 / x86_64
    // 0x283b60, 96 bytes; no LSDA index entry -> no exception region). The worker only runs
    // EstablishConnection and enqueues the result. It never calls onDone, never clears
    // _connecting, never calls Close, never touches _errs and never touches the transmit
    // rate window; _connectDone is a readonly field, so its null guard (0x274600) is
    // unreachable in the product as well.
    private void ConnectWorker()
    {
        // The native body calls EstablishConnection first and only then loads _connectDone
        // (arm64 0x2745a4: bl EstablishConnection, afterwards the +0xf0 load and Enqueue), so
        // the result is computed before the queue receiver is read. C# would otherwise
        // evaluate the receiver expression first.
        bool result = EstablishConnection();
        _connectDone.Enqueue(result);
    }

    // Independent-host lifetime only. The native worker still publishes its one result to
    // Update and has no cancellation/Join. Final host shutdown can outlive the last Pump,
    // so retain the actual attempt's resources separately from fields Close may clear.
    // All resource writes happen inside EstablishConnection; Completed is published under
    // this lock before a terminal owner may read them. No native state uses this lock.
    private readonly object _independentConnectionLifetimeSync;
    private IndependentConnectionResources _independentConnectionResources;
    private bool _independentHostShutdown;

    private sealed class IndependentConnectionResources
    {
        internal TcpClient Client;
        internal NetworkStream Stream;
        internal BlockingCollection<byte[]> TransmitQueue;
        internal bool Completed;

        internal void Release()
        {
            // Complete, never Dispose: TxLoop may still be enumerating this exact queue.
            // Each release is attempted even if the other one throws. These terminal-only
            // attempts must not replace the native worker's bool outcome or invoke a callback.
            try { TransmitQueue?.CompleteAdding(); }
            catch { }
            try { Stream?.Close(); }
            catch { }
            try { Client?.Close(); }
            catch { }
            TransmitQueue = null;
            Stream = null;
            Client = null;
        }
    }

    // Called only after the real host has attempted its normal session disposal, before
    // closing its executor. If the attempt is still running, its own finally owns release;
    // otherwise this handoff takes the already-completed resources. Neither path waits.
    internal void RetireConnectionResourcesForIndependentRuntime()
    {
        IndependentConnectionResources completed;
        lock (_independentConnectionLifetimeSync)
        {
            _independentHostShutdown = true;
            completed = _independentConnectionResources;
            _independentConnectionResources = null;
            if (completed != null && !completed.Completed) completed = null;
        }
        completed?.Release();
    }

    // Official 0x060006BD EstablishConnection() (arm64 0x264a7c / x86_64 0x273e00, 1420
    // bytes). The whole body shares one landing pad (arm64 0x264ec0, 28 branches) whose
    // handler (0x264f30) runs __cxa_begin_catch -> is-inst System.Exception (usage 1633) ->
    // __cxa_end_catch -> _errs.Enqueue("连接失败: " + ex.Message) (usage 18147) and returns
    // false: the only catch is `catch (Exception ex)`, and it neither closes the socket nor
    // calls Close. The official body also writes nothing at +0x108/+0x110/+0x114, so it does
    // not clear _txPending and does not reset the transmit rate window. The resource captures
    // and completion finally below are independent-host ownership, not recovered native code.
    private bool EstablishConnection()
    {
        var resources = new IndependentConnectionResources();
        lock (_independentConnectionLifetimeSync)
            _independentConnectionResources = resources;
        try
        {
            TcpClient client = new TcpClient();
            // Capture before the ordered option setters: an option failure still returns
            // native false, but final retirement must own the already-created client.
            resources.Client = client;
            client.NoDelay = true;
            client.ReceiveTimeout = ReceiveTimeoutMilliseconds;
            client.SendTimeout = SendTimeoutMilliseconds;

            // Product-only handles: ordinary gateway disposal keeps its existing in-flight
            // abort; terminal retirement retains this exact client even if Close clears the
            // published fields before the worker returns. Neither changes native callbacks.
            _connectingTcp = client;
            client.Connect(host, port);
            NetworkStream stream = client.GetStream();
            resources.Stream = stream;
            byte[] aesKey;
            byte[] macKey;
            byte[] handshake;
            using (RSA serverPublic = NetCrypto.CreateServerPub(
                       _options.GetRsaModulus(),
                       _options.GetRsaExponent()))
            {
                handshake = NetCrypto.ClientHandshake(serverPublic, out aesKey, out macKey);
            }

            stream.Write(handshake, 0, handshake.Length);

            int generation = Interlocked.Increment(ref _gen);
            _dropped = false;
            LastDropReason = string.Empty;
            _aesKey = aesKey;
            // Official EstablishConnection clears both backlog diagnostics with one 8-byte zero
            // store at +0x120, sitting between the +0xb8 _aesKey store and the +0xc0 _macKey
            // store: _inboxPeak = 0 followed by _nextBacklogWarnAt = 0f, in that source order.
            _inboxPeak = 0;
            _nextBacklogWarnAt = 0f;
            _macKey = macKey;
            _ns = stream;
            _sendSeq = 0;
            _run = true;
            _txQ = resources.TransmitQueue = new BlockingCollection<byte[]>(TransmitQueueCapacity);

            // Official thread names: usage 12921 "NetClient-Tx" and usage 12920
            // "NetClient-Rx" (the product used "FFO NetClient Tx" / "FFO NetClient Rx").
            // Tx is created and started before Rx, both IsBackground.
            //
            // Official carrier of the Tx thread: EstablishConnection (0x060006BD) allocates one
            // NetClient+<>c__DisplayClass56_0, writes <>4__this (+0x10) = this, txq (+0x18) =
            // the BlockingCollection it just stored into _txQ, txns (+0x20) = the stream and
            // myGen (+0x28) = the value returned by Interlocked.Increment(_gen), then builds
            // ThreadStart(displayClass, metadata usage 9655) = 0x0600087b and starts the thread
            // before it fills rxns/rxAes/rxMac (+0x30/+0x38/+0x40) for usage 9656 = 0x0600087c.
            // 0x0600087b (arm64 0x277528 / 36 bytes, x86_64 0x286600 / 48 bytes, no LSDA index
            // entry in either architecture) reads those captured fields back - it never re-reads
            // _txQ, _ns or _gen - null-checks <>4__this (arm64 0x27752c cbz -> 0x177a74) and
            // tail-calls TxLoop(this, txq, txns, myGen). The three locals captured here are
            // exactly that display class: queue, stream and generation are values, so this
            // lambda is the official capture, not a product reshaping of it.
            BlockingCollection<byte[]> queue = _txQ;
            _tx = new Thread(() => TxLoop(queue, stream, generation))
            {
                IsBackground = true,
                Name = "NetClient-Tx"
            };
            _tx.Start();
            // Official carrier of the Rx thread: usage 9656 = 0x0600087c, the second
            // ThreadStart built from the very same display class after the Tx thread has been
            // started. 0x0600087c (arm64 0x27754c / 40 bytes, x86_64 0x286630 / 48 bytes, no
            // LSDA index entry in either architecture) reads <>4__this (+0x10, arm64 0x27754c
            // ldr x8,[x0,#0x10]), myGen (+0x28, 0x277554 ldr w4), rxAes and rxMac (+0x38/+0x40,
            // 0x277558 ldp x2,x3) and rxns (+0x30, 0x27755c ldr x1), null-checks <>4__this
            // (0x277550 cbz -> 0x177a74) and tail-calls RxLoop(this, rxns, rxAes, rxMac, myGen)
            // at 0x277564 b 0x2726c8; x86_64 0x286630 mirrors it and ends with jmp 0x281d80.
            // So 9656 belongs to Rx alone and is never the Tx 9655 / 0x0600087b lambda, and the
            // stream, both keys and the generation are captured values written here before the
            // thread starts - the worker never re-reads _ns, _aesKey, _macKey or _gen for them.
            _rx = new Thread(() => RxLoop(stream, aesKey, macKey, generation))
            {
                IsBackground = true,
                Name = "NetClient-Rx"
            };
            _rx.Start();
            _tcp = client;
            _connectingTcp = null;
            return true;
        }
        catch (Exception ex)
        {
            _errs.Enqueue("连接失败: " + ex.Message);
            return false;
        }
        finally
        {
            bool release;
            lock (_independentConnectionLifetimeSync)
            {
                resources.Completed = true;
                release = _independentHostShutdown;
                if (release && ReferenceEquals(_independentConnectionResources, resources))
                    _independentConnectionResources = null;
            }
            // Product-only final ownership release, not NetClient.Close: no host transmit
            // state, callback, connecting latch, completion queue or executor is touched.
            if (release) resources.Release();
        }
    }
}
}
