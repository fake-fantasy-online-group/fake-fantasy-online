using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Bounded source recovery of the official 1.5.8 NetClient transport/session cone.
/// </summary>
public sealed partial class NetClient : IDisposable
{
    // Official 1.6.0 Login callback 06000888 uses this value for payload and auth-tag inputs.
    public const string ClientVersion = "1.6.0";
    private const int ReceiveTimeoutMilliseconds = 30000;
    private const int SendTimeoutMilliseconds = 15000;
    private const int MaximumFrameBody = 1 << 20;
    private const int MinimumFrameBody = 64;
    private const int MaximumTransmitsPerSecond = 45;
    private const int TransmitQueueCapacity = 1024;
    private const float PingIntervalSeconds = 5f;
    // Official static NetClient rows: BacklogWarn (System.Int32) and BacklogWarnCooldownSec
    // (System.Single). Both architectures compare `299 < count` and re-arm with
    // `Time.realtimeSinceStartup + 10.0` (x86_64 DAT_01117c9c holds the 10.0f addend).
    private const int BacklogWarn = 299;
    private const float BacklogWarnCooldownSec = 10f;
    // metadata_usages ordinal 14157, kind 5, decode_state=exact. Update formats it with
    // (count, _inboxPeak) and hands the result to the host warning sink unchanged.
    private const string BacklogWarningFormat =
        "[net] 接收积压 {0} 条(峰值 {1}) —— 主线程派发跟不上入包速率, 表现为'游戏变慢'但不掉线";
    // metadata_usages ordinal 16954, kind 5, decode_state=exact. RawTx (0x06000776) enqueues
    // it and stores it in LastDropReason when the bounded transmit queue rejects the frame.
    private const string BackpressureDropReason = "上行积压超限(网络长时间发不出去)";
    // metadata_usages ordinal 18148, kind 5, decode_state=exact. RawTx (0x06000776) uses it in
    // its catch (Exception) instead of the backpressure literal.
    private const string ClosedDropReason = "连接已关闭, 发送失败";
    // metadata_usages ordinal 17179, kind 5, decode_state=exact. TxLoop (0x06000777) prefixes
    // the caught exception message with it: "发送中断: " + ex.Message.
    private const string TransmitInterruptedPrefix = "发送中断: ";
    // metadata_usages ordinal 17665, kind 5, decode_state=exact. RxLoop (0x06000778) stores it
    // when the four byte length prefix cannot be filled (arm64 0x272af0 loads the literal slot
    // 0x14aa00a8, x86_64 the matching slot).
    private const string ServerClosedReason = "服务器已关闭连接";
    // metadata_usages ordinal 18073, kind 5, decode_state=exact. RxLoop (0x06000778) stores it
    // when the frame body read cannot be filled (arm64 0x272b8c loads 0x14aa0d68).
    private const string BodyReadInterruptedReason = "读帧体时连接中断";
    // metadata_usages ordinal 17572, kind 5, decode_state=exact. RxLoop (0x06000778) formats it
    // with the offending length (arm64 0x272bbc / x86_64 0x28222e, single argument).
    // The boxed argument is System.Int32, not UInt32, so 0xffffffff is shown as -1:
    //   * arm64: 06000778.c lines 129-134 take `iVar5 = *(int *)(lVar7 + 0x20)`, store it in
    //     the low 32 bits of local_78 and call thunk_FUN_001b835c(DAT_015175a0, &local_78).
    //     The same DAT_015175a0 boxes both arguments of the backlog warning in Update
    //     (0x0600077d), i.e. ConcurrentQueue.Count (System.Int32) and the field at +0x120,
    //     whose atlas row is NetClient._inboxPeak @288 System.Int32.
    //   * x86_64 repeats it: DAT_0150b848 boxes the same Update backlog pair and the RxLoop
    //     frame length, while the UInt64 sequence pair uses DAT_0150b870 (arm64
    //     DAT_015175c8), so the pointer really discriminates primitive types and the frame
    //     length is the signed 32-bit one.
    //   * The bound is still unsigned: arm64 0x272890 "sub w8,w26,#0x40" + "cmp w8,#0xfffc1 /
    //     b.hs" and x86_64 0x281f52 "add eax,-0x40 / cmp eax,0xfffc1 / jae", both rendered by
    //     Ghidra as `0xfffc0 < (int)len - 0x40U`. Range technique and display type are
    //     therefore separate facts; the product keeps both.
    private const string IllegalFrameLengthFormat =
        "收到非法帧长 {0}(超上限或流已错位)";
    // metadata_usages ordinal 17383, kind 5, decode_state=exact. RxLoop (0x06000778) formats it
    // with (expected, received) in that order (arm64 0x272c0c / x86_64 0x28226c, two arguments).
    private const string SequenceRegressionFormat =
        "帧序号回退(期望≥{0}实收{1}, 疑似重放或流错位)";
    // metadata_usages ordinal 18002, kind 5, decode_state=exact. The inner catch (Exception)
    // around NetCrypto.DecryptBody in RxLoop (0x06000778) prefixes the caught message with it
    // (arm64 0x272d08 loads the literal slot 0x14aa0b30, x86_64 0x28241b the same ordinal).
    private const string DecryptFailedPrefix = "解密失败: ";
    // metadata_usages ordinal 17553, kind 5, decode_state=exact. The outer catch (Exception)
    // around the receive loop in RxLoop (0x06000778) prefixes the caught message with it
    // (arm64 0x272e18 loads the literal slot 0x14a90d28, x86_64 0x282506 the same ordinal).
    private const string ReceiveInterruptedPrefix = "接收中断: ";

    private readonly PrivateServerOptions _options;
    private readonly NetClientLoginEnvironment _loginEnvironment;

    // Host clock injected by the composition root. Official 1.5.8 reads
    // UnityEngine.Time.realtimeSinceStartup inside PaceTx/DrainTx/TakeTxToken
    // (arm64 0x271eb4 / 0x2721b8 / 0x271f84, x86_64 0x281520 / 0x281830), and Protocol
    // stays Unity-free, so the provider is the only adapter here.
    private readonly Func<float> _realtimeSeconds;
    // The product-only _sendSync lock is gone: PaceTx, DrainTx and the Close tail it used to
    // wrap are all recovered official bodies now and carry no Monitor in either
    // architecture, and every real caller runs on one host executor (see
    // netclient-host-execution-boundary-1-5-8, WB-010).
    private readonly ConcurrentQueue<ProtocolPacket> _inbox;
    private readonly ConcurrentQueue<string> _errs;
    private readonly Queue<byte[]> _txPending;
    private readonly Stopwatch _dispatchSw;
    private TcpClient _tcp;
    private TcpClient _connectingTcp;
    private NetworkStream _ns;
    private Thread _rx;
    private Thread _tx;
    private volatile bool _run;
    private volatile bool _dropped;
    private int _gen;
    private byte[] _aesKey;
    private byte[] _macKey;
    private ulong _sendSeq;
    private BlockingCollection<byte[]> _txQ;
    private int _txSecCount;
    // Official NetClient._txSecStart is System.Single at +0x114 (atlas field row).
    private float _txSecStart;
    // Official NetClient._nextPingAt is System.Single at +0x100 (atlas field row).
    private float _nextPingAt;
    // Official NetClient._inboxPeak is System.Int32 at +0x120 and _nextBacklogWarnAt is
    // System.Single at +0x124 (atlas field rows). EstablishConnection clears both with a
    // single 8-byte zero store, Update raises the peak before the backlog warning and arms the
    // warning cooldown with a second clock read.
    private int _inboxPeak;
    private float _nextBacklogWarnAt;

    // Product-only host bindings read by the recovered Update() body. The official body calls
    // its own Dispatch (0x0600077F), UnityEngine.Time.realtimeSinceStartup,
    // UnityEngine.Debug.LogWarning and the static LoadTrace members directly; these four
    // adapters are what keeps Protocol Unity-free. None of them owns a loop, a threshold or a
    // protocol outcome: they only hand the callback, the clocks and the diagnostics through.
    private Action<ushort, byte[]> _hostDispatch;
    // Scheduling seam for the host's async entry callback. No packet leaves this same
    // inbox while that callback is suspended before its synchronous BuildWorld attempt.
    private Func<bool> _hostMayDequeue;
    private Action<string> _warningSink;
    private IWorldReceiveTracePort _receiveTrace;
    private Func<float> _dispatchMillisecondsPerFrame;
    // Product-only host binding for the error-queue drain. The official body invokes the OnError
    // field (+0x70), now bound by composition to the recovered title error consumer. The real
    // gateway Pump paths no longer supply this separate sink. This retained adapter sink is the
    // pre-WB-012 product wiring: the onError argument handed to one Update(...) call handles the
    // errors dequeued during that same call and nothing else. It is deliberately not the
    // official OnError consumer: no official writer of that field was found, so this binding is
    // scoped to the queue drain of this call and must not be reported as recovered 1.5.8
    // behaviour. It reaches neither Send (0x06000772) nor Connect/ConnectAsync
    // (0x060006BB/0x060006BC), because those read the field and never this sink.
    private Action<string> _hostErrorSink;

    public NetClient(
        PrivateServerOptions options,
        Func<float> realtimeSecondsProvider,
        Action<string> warningSink = null,
        IWorldReceiveTracePort receiveTrace = null,
        Func<float> dispatchMillisecondsPerFrame = null,
        NetClientLoginEnvironment loginEnvironment = null)
    {
        // Official 06000780 initializes these seven slots in this order (arm64
        // 0x273ef8..0x273fdc; x86_64 0x2833d2..0x283519). Literal 10335 is empty,
        // not null. Keep the sequence together rather than relying on partial-file
        // field-initializer ordering; the Protocol owner supplies all live consumers.
        host = string.Empty;
        _lastDropReason = string.Empty;
        _inbox = new ConcurrentQueue<ProtocolPacket>();
        _errs = new ConcurrentQueue<string>();
        _connectDone = new ConcurrentQueue<bool>();
        _txPending = new Queue<byte[]>();
        _dispatchSw = new Stopwatch();

        // The independent host still needs its retirement lock. Allocate it after
        // the native instance initializers, before any injected capability is bound.
        _independentConnectionLifetimeSync = new object();

        _options = options ?? throw new ArgumentNullException(nameof(options));
        _realtimeSeconds = realtimeSecondsProvider
            ?? throw new ArgumentNullException(nameof(realtimeSecondsProvider));
        _warningSink = warningSink;
        _receiveTrace = receiveTrace;
        _dispatchMillisecondsPerFrame = dispatchMillisecondsPerFrame;
        _loginEnvironment = loginEnvironment
            ?? NetClientLoginEnvironment.FromDiagnosticOptions(_options);
    }

    // Official 1.5.8 NetClient.get_Connected (0x060006B7, arm64 0x2645f0 / 20 bytes,
    // x86_64 0x273a30 / 32 bytes). The two architectures are the same body:
    //   arm64 : ldr x0,[x0,#0x88]   _tcp (+0x88), one read
    //           cbz x0, 0x264600    -> ret with x0 still holding the null _tcp, i.e. false
    //           mov x1,#0; b 0xfcfa9c   tail call TcpClient.get_Connected, null methodInfo
    //   x86_64: mov rdi,[rdi+0x88]; test rdi,rdi; je 0x273a48 -> xor eax,eax; ret
    //           xor esi,esi; jmp 0xfcad30
    // So _tcp is read exactly once, the null path returns false and the non-null path
    // returns TcpClient.Connected unchanged. The single read is what the same source
    // shape produces elsewhere in this file: Connect (0x060006BB, arm64 0x2649c8) is the
    // restored `if (_tcp != null && _tcp.Connected)` and also has one ldr of +0x88, so the
    // read count does not argue for a cached local here - it is one load either way.
    // _run (+0xa8) is never read: the previous product conjunct `_run &&` has no
    // instruction in either body and is removed.
    // Consumer consequence, stated because the change is observable: while _run is already
    // false but _tcp is still the open client (after a drop published by RxLoop or RawTx
    // and before the Update _dropped branch calls Close) the official property returns
    // true. The existing product consumers PrivateServerGateway.Connected ->
    // ClientSessionCoordinator.Connected -> GameBootstrap.ReportPos / FlushMoveReport and
    // the movement-report Update therefore keep reporting during that window. That is the
    // official behaviour; no consumer was modified here and no gateway stage or cancel
    // logic is claimed complete by this recovery.
    public bool Connected => _tcp != null && _tcp.Connected;

    // Original public instance properties. Read the same live transport state used
    // by Update; neither getter caches, resets, clamps or adds a connection gate.
    public int InboxBacklog => _inbox.Count;
    public int InboxPeak => _inboxPeak;

    // Official NetClient.LastDropReason is a volatile String field at +0xb0. RawTx
    // (0x06000776) and TxLoop (0x06000777) both wrap its store with the IL2CPP volatile
    // barrier thunk (arm64 0x17784c, a branch to 0x1844d8 `dmb ish; ret`) followed by the GC
    // card-marking write barrier (0x17777c -> 0x178e5c); ordinary fields such as _nextPingAt
    // get neither. C# cannot mark an auto-property volatile, so the property is hand written
    // over a volatile backing field instead.
    private volatile string _lastDropReason;

    public string LastDropReason
    {
        get => _lastDropReason;
        private set => _lastDropReason = value;
    }

    // Product-only adapter over the official connection entry points. Official Connect
    // (0x060006BB) and ConnectAsync (0x060006BC) take no address arguments because the
    // address lives in the official host/port fields (see NetClient.Connection.cs); the
    // gateway still hands the resolved endpoint over explicitly, so this adapter only fills
    // those fields and runs the single official 0x060006BD body. It deliberately carries no
    // second algorithm: no parameter validation, no leading Close and no catch of its own.
    // The official body is the only connection implementation.
    public bool EstablishConnection(string host, int port)
    {
        this.host = host;
        this.port = port;
        return EstablishConnection();
    }

    // Official 0x060006BE Close() (arm64 0x2655d8 / x86_64 0x274990, 484 / 528 bytes).
    // Frozen packet-NetClient/060006be.c, both architectures agree:
    //   _run = false;                                     +0xa8,  arm64 0x26561c
    //   _txPending.Clear();                               +0x108, arm64 0x265634
    //   _txSecCount = 0; _txSecStart = 0f;                one 8-byte zero store at +0x110
    //   Interlocked.Increment(ref _gen);                  +0xac,  arm64 0x265648
    //   try { if (_txQ != null) _txQ.CompleteAdding(); } catch { }
    //   try {
    //     if (_ns != null) _ns.Close();
    //     if (_tcp != null) _tcp.Close();
    //   } catch { }
    //   _tcp = null; _ns = null; _txQ = null;             +0x88 / +0x90 / +0xe0
    //
    // Exception regions, from the exact LSDA index entries (arm64 LSDA 0x12ea94c,
    // x86_64 0x12e5974; lsda_exact.py requires the entry to match the function start):
    //   * try 1 = arm64 [0x265660,0x265664) / x86_64 [0x274a1f,0x274a24): only the
    //     CompleteAdding call (arm64 0x265660 `bl 0xb5798c`, x86_64 0x274a1f
    //     `call 0xb59090`). The _txQ load and its null test run before it and are not
    //     protected.
    //   * try 2 = arm64 [0x26567c,0x265690) / x86_64 [0x274a3a,0x274a53): starts at the
    //     indirect _ns close (arm64 0x26567c `blr x9`, vtable slot +0x208; x86_64 0x274a3a
    //     `call qword ptr [rax + 0x208]`) and runs through the direct TcpClient.Close
    //     (arm64 0x26568c, x86_64 0x274a4e). The two closes therefore share ONE catch:
    //     their landing pads (arm64 0x265748 / x86_64 0x274b2b) end the catch and branch
    //     straight to the field nulling (arm64 0x265690 / x86_64 0x274a53), so an exception
    //     raised by the _ns close skips `_tcp.Close()` instead of falling through to it.
    //     A separate try per close would have continued into the tcp close instead.
    //   * Both landing pads are `catch` with an empty body, not a finally:
    //     __cxa_begin_catch -> is-inst (arm64 0x177f70 -> 0x1e2f3c, x86_64 0x16c6a0 ->
    //     0x1e8460) -> on a match __cxa_end_catch and continue, on a mismatch
    //     __cxa_allocate_exception(8) + __cxa_throw, i.e. a rethrow. Landing pad 1
    //     continues into the _ns close (arm64 0x265708/0x26570c, x86_64 0x274ae9/0x274af3),
    //     so a failing CompleteAdding still closes the transport.
    //   * The _ns close is a virtual call and `Stream.Dispose()` is not virtual in the
    //     Mono BCL IL2CPP builds against (it forwards to the virtual Close()), so the
    //     source form is Close(). TcpClient.Close is not virtual and is emitted as the
    //     direct call arm64 0x26568c / x86_64 0x274a4e with a null methodInfo.
    //
    // Faithful points kept on purpose, do not "fix":
    //   * No Interlocked.Exchange, no Dispose, no Join/Wait and no lock: the native closes
    //     both handles while they are still readable and then stores null into them.
    //   * Close never clears _connecting, _connectCb or _connectDone, never drains _errs,
    //     never resets _aesKey, _macKey, _sendSeq or _nextPingAt, never drains _inbox and
    //     never invokes OnDisconnected. The product still needs those resets for its own
    //     teardown, so they live in CloseIndependentRuntime(), which only the gateway
    //     teardown calls - never the official Update/OnDestroy intent.
    //   * The _txPending null guard (arm64 0x265624 `cbz` -> 0x177a74) is the unreachable
    //     readonly guard and is not reproduced.
    //   * The barrier in front of `_run = false` (arm64 0x265618 `bl 0x17784c` -> 0x1844d8
    //     `dmb ish; ret`; x86_64 0x2749c5 `call 0x16bf30`) is the volatile-store barrier of
    //     the volatile bool _run field, and the three barriers after the handle stores
    //     (arm64 0x17777c -> 0x178e5c -> 0x1f9534) are the GC card marking of a reference
    //     field write. C# emits both for a volatile field and a reference store, so no
    //     MemoryBarrier is added here.
    //
    // The caught class is System.Object, i.e. the official source form is a bare `catch`.
    // Three layers have to be kept apart here, they are not the same thing:
    //   * LSDA layer: `cmp w1, #1` (arm64 0x2656d4 / 0x26574c; x86_64 0x274ab7 / 0x274b2f)
    //     only compares the action index the personality routine supplies for the C++
    //     clause that was entered. The C++ object is the Il2CppExceptionWrapper:
    //     __cxa_begin_catch (arm64 0x1126454) returns it, `ldr x8,[x21]` takes its single
    //     field (the managed exception) and the mismatch path allocates 8 bytes, copies
    //     exactly that field back (`ldr x8,[x21]; str x8,[x0]`, arm64 0x265720 / 0x26578c)
    //     and rethrows with the static C++ typeinfo 0x1394928. This layer carries no
    //     managed class identity and must not be read as the C# catch type.
    //   * is-inst layer: arm64 0x2656e8 / 0x265760 `adrp x8,#0x1517000; add x8,x8,#0x558;
    //     ldr x0,[x8,#0x10]` and x86_64 0x274aca / 0x274b3e `lea rax,[rip+...]` ->
    //     0x150B800 with `mov rdi,[rax+0x10]` load the class argument; the instance
    //     argument is the klass of the wrapper's managed object (`ldr x8,[x21]; ldr x1,[x8]`).
    //     Helper: arm64 0x177f70 -> 0x1e2f3c, x86_64 0x16c6a0 -> 0x1e8460.
    //   * slot identity: base+0x10 is written exactly once, by the runtime defaults
    //     initialiser arm64 0x1de060. It memsets the 0x300 byte table first
    //     (`mov w1,#0x300; bl 0x1126604`), stores two image/assembly pointers at +0x0
    //     (0x1de0ac) and +0x8 (0x1de0b8), and then fills the class slots with
    //     Class::FromName (0x1e4e88): +0x10 = System.Object (name string 0x1354f7c =
    //     "Object", namespace 0x1354210 = "System", store at 0x1de0d4), +0x20 = Void
    //     (0x1355d03), +0x28 = Boolean (0x1354f3f), +0x48 = Int32, +0x70 = UInt64,
    //     +0x90 = String, +0x250 = ValueType - the Il2CppDefaults order, and +0x48 /
    //     +0x70 are the Int32 / UInt64 boxing slots already pinned for this build. The
    //     base lives in __DATA,__common (zero filled, no file bytes), which is why it
    //     can only be read through its writer and not from the frozen image.
    //   * the helper's own fast path proves the consequence: arm64 0x1e2f98
    //     `ldr x8,[x21,#0x10]; cmp x19,x8; b.eq 0x1e3124` -> 0x1e3124 `mov w0,#1; ret`,
    //     i.e. as soon as the class argument is System.Object the test returns true, so
    //     both landing pads swallow every managed throw and the rethrow path is dead.
    //   * `catch (Exception)` is therefore excluded: the already verified handlers of
    //     RawTx (0x06000776, arm64 handler 0x2720dc), EstablishConnection (0x060006BD)
    //     and RxLoop materialise their class from metadata usage 0x148aba8, a different
    //     path. C# emits System.Object as the handler class of a bare `catch`, and
    //     `catch (object)` is not legal C#, so the official form is `catch { }`.
    public void Close()
    {
        _run = false;
        _txPending.Clear();
        // Official 0x060006BE: _txSecCount = 0; _txSecStart = 0f; emitted as one 8-byte
        // zero store at +0x110 (arm64 0x265638, x86_64 0x2749f0), in that source order.
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
            // Official landing pad 1 (arm64 0x2656d0 / x86_64 0x274ab3): bare catch over
            // System.Object, swallow and continue with the transport close.
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
            // Official landing pad 2 (arm64 0x265748 / x86_64 0x274b2b): bare catch over
            // System.Object, swallow and continue with the field nulling. A throw here
            // leaves _tcp unclosed, which is the native behaviour, not an accident of this
            // shape. This is official behaviour, not a product obligation: the product
            // teardown below cannot reach the handle any more because the official body
            // nulls _tcp and _ns before it returns, so closing a TcpClient skipped by the
            // _ns throw is a separate gateway-contract item, not something to patch here.
        }

        _tcp = null;
        _ns = null;
        _txQ = null;
    }

    // Product-only lifecycle adapter, no official token: the operations official 0x060006BE
    // does not perform and that only the independent gateway teardown depends on. It is
    // deliberately not part of Close(), so the official Update `_dropped` branch and
    // OnDestroy keep running the official body alone:
    //   * closing the in-flight client: official NetClient keeps no handle to a connect
    //     that is still under way, so an official Close cannot abort one. This gateway
    //     starts EstablishConnection on a pool thread (Task.Run) and needs Close() to
    //     interrupt a blocking TcpClient.Connect, so the handle stays here.
    //   * dropping the Tx/Rx thread fields, the session keys, _sendSeq and _nextPingAt,
    //     and draining _inbox: official Close leaves all of them alone; the gateway
    //     session teardown (DisposeConnectionCore) resets them so a following connect
    //     cannot dispatch packets of the previous session.
    // Only PrivateServerGateway.DisposeConnectionCore and PrivateServerGateway
    // .CloseTransportAsync call it, both on the host thread, right after Close().
    // Boundary, stated as it is: Close() followed by this helper inside one host action
    // is ordered, not mutually exclusive with a background EstablishConnection. That
    // worker publishes _ns / _run / _txQ / _tcp and clears _connectingTcp on its own
    // thread, so it can publish between the two calls or after both of them; the helper
    // then clears the session keys of a connection that is still live, and a late
    // established connection keeps handles that neither call closed. The same window
    // already existed before this split (the previous Close exchanged the handles in one
    // step and cleared the keys in a later step of the same call), so the split does not
    // add it; closing it needs the gateway connection contract (generation aware
    // abandoning of a late connection), which is out of scope here. Do not "fix" it by
    // waiting for the worker, changing the gateway timeout result or closing _tcp again
    // inside Close(): all three would change behaviour the official body does not have.
    public void CloseIndependentRuntime()
    {
        TcpClient connectingClient = Interlocked.Exchange(ref _connectingTcp, null);
        connectingClient?.Close();

        _rx = null;
        _tx = null;
        _aesKey = null;
        _macKey = null;
        _sendSeq = 0;
        _nextPingAt = 0f;

        while (_inbox.TryDequeue(out _))
        {
        }
    }

    // Official 1.5.8 NetClient.Login (0x060006c0, arm64 0x2657c0) allocates the display class,
    // stores account, password and this, then hands <Login>b__0 (0x0600087e, arm64 0x277574)
    // to Send(0x1002, ...). The frozen lambda body proves the four string writes are
    // NetClient.Buf.Str (0x0600078e) and not another writer:
    //   b.Str(account); b.Str(password);
    //   b.Str(SystemInfo.deviceUniqueIdentifier ?? "");   // usage 10335 = StringLiteral:""
    //   b.Str("1.5.8");                                   // usage 11123 = StringLiteral:1.5.8
    //   b.U8(VmDetect.Detect(out local)); b.U8(DebugDetect.Detect(out local));
    //   b.Bytes(NetCrypto.ComputeAuthTag(_aesKey, _macKey, "1.5.8"));
    // Str is therefore the official semantics for these four writes: a null string becomes ""
    // instead of throwing, the length prefix is U16 and truncates above 65535 while the payload
    // is still copied in full (see NetClient.Buf.Write.cs). Bootstrap supplies the real device
    // getter and recovered detector methods through decision-free host capabilities. Each
    // runs here, at its original write position after Send's connected gate; no environment
    // snapshot or options value supplies the production title path. The reason local is unused.
    public void Login(string account, string password)
    {
        Send(
            ProtocolOpcodes.LoginRequest,
            payload =>
            {
                payload.Str(account);
                payload.Str(password);
                payload.Str(_loginEnvironment.DeviceUniqueIdentifier() ?? string.Empty);
                payload.Str(ClientVersion);
                payload.U8(_loginEnvironment.DetectVm(out string reason) ? (byte)1 : (byte)0);
                payload.U8(_loginEnvironment.DetectDebug(out reason) ? (byte)1 : (byte)0);
                payload.Bytes(
                    NetCrypto.ComputeAuthTag(_aesKey, _macKey, ClientVersion));
            });
    }

    public void SendClientCaps()
    {
        Send(
            ProtocolOpcodes.ClientCapabilities,
            payload => payload.I32(31));
    }

    public void SetPetView(int mask)
    {
        Send(ProtocolOpcodes.SetPetViewRequest, payload => payload.I32(mask));
    }

    public void SavePos(int mapId, int x, int y, bool entered = false)
    {
        Send(
            ProtocolOpcodes.SavePositionRequest,
            payload =>
            {
                payload.I32(mapId);
                payload.I32(x);
                payload.I32(y);
                payload.U8(entered ? (byte)1 : (byte)0);
            });
    }

    public void PlayerMove(int x, int y)
    {
        Send(
            ProtocolOpcodes.PlayerMoveRequest,
            payload =>
            {
                payload.I32(x);
                payload.I32(y);
            });
    }

    internal void WriteUseSkillForIndependentRuntime(int skillId, int targetId)
    {
        // Recovered <UseSkill>b__0 writer. The Presentation facet owns the preceding decisions.
        Send(ProtocolOpcodes.UseSkillRequest, payload =>
        {
            payload.I32(skillId);
            payload.U8(0);
            payload.I32(0);
            payload.I32(0);
            payload.I32(targetId);
        });
    }

    public void BeginWork(int skill) => Send(ProtocolOpcodes.BeginWorkRequest, b => b.I32(skill));
    public void Gather(int skill) => Send(ProtocolOpcodes.GatherWorkRequest, b => b.I32(skill));

    public void EndWork()
    {
        // Original <EndWork>b__135_0 is an empty writer; Send still emits the packet.
        Send(ProtocolOpcodes.EndWorkRequest, payload => { });
    }

    public void Sit(bool on)
    {
        Send(ProtocolOpcodes.SittingRequest, payload => payload.U8(on ? (byte)1 : (byte)0));
    }

    internal void WriteUseSkillAtForIndependentRuntime(int skillId, int x, int y, int targetId)
    {
        Send(ProtocolOpcodes.UseSkillRequest, payload =>
        {
            payload.I32(skillId);
            payload.U8(1);
            payload.I32(x);
            payload.I32(y);
            payload.I32(targetId);
        });
    }

    private void Ping(long tick)
    {
        Send(0x1001, payload => payload.I64(tick));
    }

    internal void SendPacket(ushort opcode, byte[] payload)
    {
        Send(opcode, writer => writer.Bytes(payload));
    }

    // Official 1.5.8 NetClient.Send (0x06000772, arm64 0x2658c0 / x86_64 0x274cb0; frozen
    // packet-NetClient/06000772.c, both architectures agree and no Monitor/LSDA appears in
    // either body):
    //   if (_tcp != null && _tcp.Connected) {
    //     Buf b = new Buf(64);                       // ctor 0x06000787, [opt] cap = 64
    //     body(b);                                   // null body -> NullReferenceException
    //     byte[] payload = b.ToArray();              // 0x06000790, length is _pos
    //     byte[] frame = new byte[payload.Length + 10];
    //     ulong seq = _sendSeq; _sendSeq++;          // read (+0xc8) then increment
    //     frame[0..7] = seq little-endian; frame[8..9] = id little-endian;   // inline stores
    //     Array.Copy(payload, 0, frame, 10, payload.Length);
    //     PaceTx(NetCrypto.EncryptFrame(frame, _aesKey, _macKey));
    //     return;
    //   }
    //   OnError?.Invoke("未连接服务器");               // usage 17676, verified in the frozen index
    //
    // Faithful points kept on purpose, do not "fix":
    //   * The connected test is _tcp != null && _tcp.Connected only. It never reads _run, so
    //     the product Connected property is not used here.
    //   * A disconnected send reports through OnError and returns; it does not throw, does not
    //     synthesize a Result and does not fabricate a stage/transport outcome. OnError is a
    //     public field with no product subscriber yet: the official null-callback semantics are
    //     kept as-is and binding a real subscriber stays PENDING (see the WB-011 report).
    //   * A null body is not pre-checked: the delegate call is what raises the exception.
    //   * This body carries no lock. The product-only lock (_sendSync) that used to wrap it
    //     is gone and the field itself is gone with it: PaceTx, DrainTx and Close are all
    //     recovered official bodies now and none of them carry a Monitor.
    //   * The single send path still runs through the host executor (PrivateServerGateway);
    //     nothing here hands the frame to a thread-pool queue.
    private void Send(ushort id, Action<Buf> body)
    {
        if (_tcp != null && _tcp.Connected)
        {
            Buf buffer = new Buf();
            body(buffer);
            byte[] payload = buffer.ToArray();
            byte[] frame = new byte[payload.Length + 10];
            ulong sequence = _sendSeq;
            _sendSeq = unchecked(sequence + 1);
            frame[0] = (byte)sequence;
            frame[1] = (byte)(sequence >> 8);
            frame[2] = (byte)(sequence >> 16);
            frame[3] = (byte)(sequence >> 24);
            frame[4] = (byte)(sequence >> 32);
            frame[5] = (byte)(sequence >> 40);
            frame[6] = (byte)(sequence >> 48);
            frame[7] = (byte)(sequence >> 56);
            frame[8] = (byte)id;
            frame[9] = (byte)(id >> 8);
            Array.Copy(payload, 0, frame, 10, payload.Length);
            byte[] encrypted = NetCrypto.EncryptFrame(frame, _aesKey, _macKey);
            PaceTx(encrypted);
            return;
        }

        OnError?.Invoke("未连接服务器");
    }

    // Official 1.5.8 NetClient.PaceTx (0x06000774, arm64 0x271eb4 / x86_64 0x281520):
    //   if (_txPending.Count != 0) { _txPending.Enqueue(frame); return; }
    //   float now = Time.realtimeSinceStartup;
    //   if (1.0f <= now - _txSecStart) { _txSecStart = now; _txSecCount = 0; }
    //   else if (TxMaxPerSec < _txSecCount) { _txPending.Enqueue(frame); return; }
    //   _txSecCount++; RawTx(frame);
    // 0x2c (44) < _txSecCount == _txSecCount >= 45 == MaximumTransmitsPerSecond.
    // No Monitor/LSDA in either architecture: the official body is lock-free.
    private void PaceTx(byte[] frame)
    {
        if (_txPending.Count != 0)
        {
            _txPending.Enqueue(frame);
            return;
        }

        float now = _realtimeSeconds();
        if (now - _txSecStart >= 1f)
        {
            _txSecStart = now;
            _txSecCount = 0;
        }
        else if (_txSecCount >= MaximumTransmitsPerSecond)
        {
            _txPending.Enqueue(frame);
            return;
        }

        _txSecCount++;
        RawTx(frame);
    }

    // Official 1.5.8 NetClient.DrainTx (0x06000775, arm64 0x2721b8 / x86_64 0x281830):
    //   while (_txPending.Count > 0) {
    //     float now = Time.realtimeSinceStartup;
    //     if (1.0f <= now - _txSecStart) { _txSecStart = now; _txSecCount = 0; }
    //     else if (TxMaxPerSec < _txSecCount) { return; }
    //     _txSecCount++; RawTx(_txPending.Dequeue());
    //   }
    // The window reset sits inside the loop, and the body is lock-free in both
    // architectures (no Monitor call, no LSDA landing pad).
    // The product-only lock (_sendSync) that used to wrap this body was removed to match the
    // official body, so _txPending / _txSecCount / _txSecStart carry no mutual exclusion of
    // their own. They no longer need one: netclient-host-execution-boundary-1-5-8 (WB-010)
    // routed every real send onto one host executor, so PaceTx, DrainTx and the Update call
    // that reaches them all run on that single host thread. Close is the third writer and it
    // is the recovered official body now, so it carries no lock of its own either.
    private void DrainTx()
    {
        while (_txPending.Count > 0)
        {
            float now = _realtimeSeconds();
            if (now - _txSecStart >= 1f)
            {
                _txSecStart = now;
                _txSecCount = 0;
            }
            else if (_txSecCount >= MaximumTransmitsPerSecond)
            {
                return;
            }

            _txSecCount++;
            RawTx(_txPending.Dequeue());
        }
    }

    // Official 1.5.8 NetClient.TakeTxToken (0x06000773, arm64 0x271f84 / 104 bytes,
    // x86_64 0x2815f0 / 96 bytes). The recovered managed source
    // (Build/ReferenceManagedRecovery-1.5.8/source/NetClient.cs:16253) and both frozen
    // architectures agree:
    //   float now = Time.realtimeSinceStartup;   arm64 bl 0x104dde0 / x86_64 call 0x10402e0,
    //                                            methodInfo = 0 (mov x0,#0 / xor edi,edi)
    //   float delta = now - _txSecStart;         +0x114, arm64 fsub s1,s0,s1 /
    //                                            x86_64 subss xmm1,[rbx+0x114]
    //   if (delta < 1f) {                        arm64 fmov s2,#1.0 + fcmp s1,s2 + b.ge ->
    //                                            the reset path; x86_64 ucomiss xmm1,
    //                                            [rip+0xe96666] = the float 1.0 at
    //                                            0x1117C7C + jae -> the reset path
    //     count = _txSecCount;                   +0x110, ldr w8 / mov r14d
    //     if (_txSecCount > 44) return false;    cmp w8,#0x2c + b.le, cmp r14d,0x2c + jle:
    //                                            signed compare, 0 = false (mov w0,#0 /
    //                                            xor eax,eax)
    //   } else { _txSecStart = now; count = 0; } arm64 str s0,[x19,#0x114]; x86_64 movss
    //                                            [rbx+0x114],xmm0
    //   _txSecCount = count + 1;                 add w8,w8,#1 / inc r14d, str to +0x110
    //   return true;                             mov w0,#1 / mov al,1
    // The threshold is the same one PaceTx (0x06000774) and DrainTx (0x06000775) use:
    // 0x2c (44) < _txSecCount == _txSecCount > 44, i.e. 45 takes per one second window.
    // The compare is single precision on both architectures (s registers with fmov s2,#1.0
    // and ucomiss against the float constant), so the literal is 1f and not the double 1.0
    // that the decompiler dump prints.
    // The clock is the injected _realtimeSeconds() provider, the same adapter PaceTx and
    // DrainTx already use; no lock and no exception region: neither architecture has a
    // Monitor call or an LSDA entry for this start address.
    // Caller: the atlas lists none, and the recovered PaceTx / DrainTx carry this token
    // window inlined instead of calling it, so the member stays owner-only in the product
    // as well. It is recovered because it is the official member, not because anything
    // should call it: no caller was invented and PaceTx / DrainTx were left alone.
    private bool TakeTxToken()
    {
        float now = _realtimeSeconds();
        int count;
        if (now - _txSecStart < 1f)
        {
            count = _txSecCount;
            if (_txSecCount > 44)
            {
                return false;
            }
        }
        else
        {
            _txSecStart = now;
            count = 0;
        }

        _txSecCount = count + 1;
        return true;
    }

    // Official 1.5.8 NetClient.RawTx (0x06000776, arm64 0x271fec / 460 bytes, x86_64
    // 0x281650 / 480 bytes). Both architectures agree instruction by instruction:
    //   BlockingCollection<byte[]> queue = _txQ;   // one read, +0xe0, arm64 0x272040
    //   try {
    //     if (queue != null && queue.TryAdd(frame)) return;      // usage 3368, bl 0xb56c0c
    //     if (!_run) return;                                     // +0xa8 volatile read
    //     _errs.Enqueue("上行积压超限(网络长时间发不出去)");       // +0xd8, usage 16954 / 3410
    //     LastDropReason = "上行积压超限(网络长时间发不出去)";     // +0xb0
    //     _run = false;                                          // +0xa8
    //     _dropped = true;                                       // +0xa9
    //   }
    //   catch (Exception) {                                      // is-inst usage 1633
    //     if (!_run) return;
    //     _errs.Enqueue("连接已关闭, 发送失败");                  // usage 18148
    //     LastDropReason = "连接已关闭, 发送失败";
    //     _run = false;
    //     _dropped = true;
    //   }
    // The exception region is real and does not come from a missing call in Ghidra's C:
    // Build/WB013-tx-lifecycle-20260927/lsda-arm64-exact.txt and lsda-x86_64-exact.txt both
    // report an LSDA index entry for the method start, with landing pads 0x2720d0 / 0x2720d4 /
    // 0x2720d8 (arm64) and 0x281738 / 0x28173a / 0x28173c (x86_64) that all branch into one
    // handler: __cxa_begin_catch -> is-inst System.Exception (usage 1633) -> __cxa_end_catch,
    // then the same drop sequence with the second literal. The arm64 handler sits at 0x2720dc
    // and the x86_64 one at 0x28173e.
    // Faithful points kept on purpose, do not "fix":
    //   * The only guard is _run, not _dropped: a second congestion is silent because the first
    //     one already cleared _run, so the drop is reported exactly once per connection.
    //   * The try covers the drop writes as well (arm64 call-site [0x272090,0x2720b0), x86_64
    //     [0x2816f0,0x28171c)), so a throw raised inside the drop still lands in the catch and
    //     reports "连接已关闭, 发送失败".
    //   * No IOException and no rethrow: the product status code
    //     "transport.transmit.backpressure" was not official and is gone.
    //   * The frame travels by reference into the queue; official never copies or clears it.
    private void RawTx(byte[] frame)
    {
        BlockingCollection<byte[]> queue = _txQ;
        try
        {
            if (queue != null && queue.TryAdd(frame))
            {
                return;
            }

            if (!_run)
            {
                return;
            }

            _errs.Enqueue(BackpressureDropReason);
            LastDropReason = BackpressureDropReason;
            _run = false;
            _dropped = true;
        }
        catch (Exception)
        {
            if (!_run)
            {
                return;
            }

            _errs.Enqueue(ClosedDropReason);
            LastDropReason = ClosedDropReason;
            _run = false;
            _dropped = true;
        }
    }

    // Official 1.5.8 NetClient.TxLoop (0x06000777, arm64 0x272288 / 1088 bytes, x86_64
    // 0x281900 / 1152 bytes). Both architectures agree:
    //   try {
    //     foreach (byte[] frame in queue.GetConsumingEnumerable())   // usage 3367, bl 0xb5807c
    //       stream.Write(frame, 0, frame.Length);                    // vtable +0x2f8, w2 = 0
    //   }
    //   catch (Exception ex) {                                       // is-inst usage 1633
    //     if (!_run) return;                                         // +0xa8 volatile read
    //     if (_gen != generation) return;                            // +0xac volatile read
    //     string reason = "发送中断: " + ex.Message;                  // usage 17179, Concat
    //     _errs.Enqueue(reason); LastDropReason = reason; _run = false; _dropped = true;
    //   }
    // The two exception regions are proved from the LSDA and the landing pads, not from the
    // absence of a catch in Ghidra's C: Build/WB013-tx-lifecycle-20260927/lsda-arm64-exact.txt
    // and lsda-x86_64-exact.txt both report an LSDA index entry for the method start.
    //   * foreach finally: arm64 pads 0x272570..0x272580 all branch to 0x272584, which runs
    //     __cxa_begin_catch, saves the exception object, runs __cxa_end_catch, then performs
    //     the IDisposable.Dispose call (usage 1828) at 0x2724a4 and rethrows through 0x177a68;
    //     x86_64 does the same at 0x281b56 -> 0x281b7b/0x281bd4 -> 0x281d0c.
    //   * catch: arm64 pads 0x272558/0x27255c/0x272560 branch to 0x272564, which saves the
    //     exception and reaches 0x2725c8, the is-inst System.Exception test followed by the
    //     handler above; x86_64 matches at 0x281d61 -> 0x281c06.
    // The interface dispatch sequence `klass + 0x138 + slot * 16` pins the vtable base, and the
    // exception member read at vtable +0x188 is the very slot 0x060006BD uses for
    // "连接失败: " + ex.Message, so ex.Message is the official spelling, not ToString().
    // Faithful points kept on purpose, do not "fix":
    //   * The loop body carries no _run / _gen check: the official Tx thread writes every frame
    //     the consuming enumeration yields and only stops when it completes or throws.
    //   * A stale generation publishes nothing: the catch returns before touching _errs,
    //     LastDropReason, _run or _dropped, and _run is tested before _gen.
    //   * This body does not close the stream, does not call Close and does not touch
    //     _txPending or the rate window, and it inlines its own four writes instead of going
    //     through a helper, exactly like the official body does.
    // WB-014 restored the Rx worker (0x06000778), which likewise inlines its publish block, so
    // the product-only DropGeneration helper lost its last caller and was removed.
    private void TxLoop(
        BlockingCollection<byte[]> queue,
        NetworkStream stream,
        int generation)
    {
        try
        {
            foreach (byte[] frame in queue.GetConsumingEnumerable())
            {
                stream.Write(frame, 0, frame.Length);
            }
        }
        catch (Exception ex)
        {
            if (!_run)
            {
                return;
            }

            if (Volatile.Read(ref _gen) != generation)
            {
                return;
            }

            string reason = TransmitInterruptedPrefix + ex.Message;
            _errs.Enqueue(reason);
            LastDropReason = reason;
            _run = false;
            _dropped = true;
        }
    }

    // Official 1.5.8 NetClient.RxLoop(NetworkStream ns, byte[] aesKey, byte[] macKey,
    // int myGen) (0x06000778, arm64 0x2726c8 / 1960 bytes, x86_64 0x281d80 / 2032 bytes).
    // Both architectures agree on every branch below. The parameter names come from the atlas
    // row (parameter_names_json ["ns","aesKey","macKey","myGen"]) and the call order is proved
    // by the Rx thread carrier 0x0600087c (arm64 0x27754c): ldr x8,[x0,#0x10] (<>4__this),
    // ldr w4,[x0,#0x28] (myGen), ldp x2,x3,[x0,#0x38] (rxAes, rxMac), ldr x1,[x0,#0x30]
    // (rxns) then b 0x2726c8. DecryptBody receives (body, aesKey, macKey) in that order:
    // arm64 0x27293c-0x27294c sets x0=body, x1=aesKey and x2=[sp,#0x10] (macKey spill from
    // 0x2727a8); x86_64 0x282005-0x282014 does the same.
    //   * One lengthPrefix of four bytes is allocated once before the try (arm64 0x272790,
    //     x86_64 0x281dd6) and expectedSequence is zeroed there too (arm64 0x2727ac,
    //     "str xzr,[sp]"), so both live outside the protected region: the first LSDA call-site
    //     with a landing pad starts at arm64 0x2727c0 / x86_64 0x281e6e, the loop condition.
    //   * The loop condition is _run first, then _gen == myGen, each read followed by the
    //     IL2CPP volatile barrier stub (arm64 0x17784c -> 0x1844d8 is "dmb ish; ret").
    //     arm64 0x2727bc-0x2727d4, x86_64 0x281e65-0x281e83.
    //   * The frame length is the four prefix bytes combined into a 32-bit value
    //     (arm64 0x272874-0x27288c, x86_64 0x281f2b-0x281f4c) and is guarded by a single
    //     unsigned range test: arm64 0x272890-0x27289c is "sub w8,w26,#0x40" then
    //     "cmp w8,#0xfffc1 / b.hs", x86_64 0x281f52-0x281f5a is "add eax,-0x40 /
    //     cmp eax,0xfffc1 / jae", rendered by Ghidra on both architectures as
    //     `0xfffc0 < (int)len - 0x40U`. The product keeps that exact test instead of two
    //     signed comparisons. The value itself is the signed Int32 the official body boxes
    //     (see IllegalFrameLengthFormat), so the two named constants bound the range while
    //     a high-bit length is still displayed as a negative number.
    //   * Both read loops are the inlined body of the official ReadExact (0x06000779): the
    //     constant 4 argument and the missing "count < 1" early-out match IL2CPP having
    //     inlined it, which is also why that method has no recorded caller in the index.
    //     Their failure literals differ: length prefix -> ServerClosedReason, body ->
    //     BodyReadInterruptedReason.
    //   * NetCrypto.DecryptBody sits in its own try. arm64 LSDA call-site
    //     [0x27293c,0x272950) -> pads 0x272ca0/0x272cb0, x86_64 [0x282005,0x282019) ->
    //     pad 0x2823bf, and both handlers test is-inst System.Exception, read Message through
    //     vtable slot +0x188 and String.Concat it onto DecryptFailedPrefix. The outer try is
    //     the rest of the loop plus the two string.Format branches and concatenates onto
    //     ReceiveInterruptedPrefix instead (arm64 pads 0x272d70-0x272db8 -> 0x272dc0,
    //     x86_64 pads 0x282372-0x2824a4 -> 0x2824aa). A non-Exception selector rethrows.
    //   * A decrypted body shorter than ten bytes is skipped, not reported: arm64 0x272958-
    //     0x272960 branches straight back to the loop head 0x2727bc, x86_64 0x282025-0x28202b
    //     to 0x281e65. There is no inner_length failure literal anywhere in the method.
    //   * The sequence is System.UInt64 read from the first eight decrypted bytes
    //     (arm64 0x272964, x86_64 0x282031) and only a strictly smaller value is rejected
    //     (arm64 "cmp x25,x8 / b.lo", x86_64 "cmp r12,rcx / jb"); the literal formats
    //     (expected, received) in that order. expectedSequence becomes received + 1 only
    //     after the queue insert (arm64 0x272a04, after the Enqueue at 0x272a00) and the
    //     official increment is a plain wrapping add (arm64 0x272a08 "add x8,x8,#1"), which
    //     the product states as unchecked.
    //   * opcode is System.UInt16 at decrypted offset 8 (arm64 0x27297c, x86_64 0x282045)
    //     and the payload is one new byte[len - 10] plus one System.Array.Copy
    //     (plain, 10, payload, 0, len - 10) - arm64 0x272980-0x2729a8, x86_64 0x28204d-
    //     0x28207f, rendered as `Array.Copy(lVar,10,uVar,0,*(int*)(lVar+0x18) + -10,0)` in
    //     both frozen exports. No reader object, no length cache and no extra bounds guard
    //     is involved: the >= 10 test above already covers offsets 0..9.
    //   * The second generation check exits the loop silently: arm64 0x2729ac-0x2729bc
    //     branches to 0x272c2c ("mov x21,#0" -> publish block), x86_64 0x282103-0x282113 to
    //     0x282283. It is not a per-packet filter that keeps looping.
    //   * The publish block is outside every try (arm64 [0x272ae4,0x272bb0) and x86_64
    //     [0x282283,0x282328) are "lp=none action=0"). It re-reads _run and _gen, returns
    //     without touching anything when either fails, and otherwise, for a non-null failure,
    //     runs _errs.Enqueue -> LastDropReason -> _run = false -> _dropped = true in that
    //     order (arm64 0x272b28-0x272b68). _run = false and _dropped = true are written even
    //     when there is no failure, but that is unreachable: a null failure only arrives from
    //     the loop condition or the generation break, both of which fail the re-read.
    // Kept on purpose, do not "fix": no Unity time, no Close, no _txPending and no rate
    // window is touched from this thread. The queue element stays the product ProtocolPacket
    // because the official element type is ValueTuple<UInt16, Byte[]> consumed by Dispatch
    // (0x0600077f), which is out of scope for this package: the container is an accepted
    // inbound boundary, its own payload null guard is a product property of that container
    // and not a claim that the official tuple behaves the same way.
    private void RxLoop(
        NetworkStream stream,
        byte[] aesKey,
        byte[] macKey,
        int generation)
    {
        byte[] lengthPrefix = new byte[4];
        ulong expectedSequence = 0;
        string failure = null;
        try
        {
            while (_run && generation == Volatile.Read(ref _gen))
            {
                if (!ReadExact(stream, lengthPrefix, lengthPrefix.Length))
                {
                    failure = ServerClosedReason;
                    break;
                }

                // Signed 32-bit decode, unsigned range test. Both are official: the boxed
                // length is System.Int32 (see IllegalFrameLengthFormat), while the bound is
                // `0xfffc0 < (uint)(len - 0x40)`, which rejects everything below 64 and above
                // 1 << 20 for every int input, including negatives, and accepts exactly
                // 1 << 20. The allocation below therefore only sees a legal length.
                int bodyLength = lengthPrefix[0]
                    | (lengthPrefix[1] << 8)
                    | (lengthPrefix[2] << 16)
                    | (lengthPrefix[3] << 24);
                if (unchecked((uint)(bodyLength - MinimumFrameBody)
                    > (uint)(MaximumFrameBody - MinimumFrameBody)))
                {
                    failure = string.Format(IllegalFrameLengthFormat, bodyLength);
                    break;
                }

                byte[] body = new byte[bodyLength];
                if (!ReadExact(stream, body, body.Length))
                {
                    failure = BodyReadInterruptedReason;
                    break;
                }

                byte[] plain;
                try
                {
                    plain = NetCrypto.DecryptBody(body, aesKey, macKey);
                }
                catch (Exception ex)
                {
                    failure = DecryptFailedPrefix + ex.Message;
                    break;
                }

                if (plain.Length < 10)
                {
                    continue;
                }

                // Little endian field reads straight from the plaintext, no parser object:
                // the frozen bodies are one 8-byte load at data offset 0 (arm64 06000778.c:166
                // `uVar8 = *(ulong *)(lVar10 + 0x20)`, x86_64:140 the same) and one 2-byte load
                // at data offset 8 (arm64:174 `uVar4 = *(undefined2 *)(lVar10 + 0x28)`,
                // x86_64:150), and no BitConverter call appears in the decompilation of either
                // architecture, so the product states the little-endian composition explicitly
                // instead of assuming an SDK helper.
                ulong receivedSequence = (ulong)plain[0]
                    | ((ulong)plain[1] << 8)
                    | ((ulong)plain[2] << 16)
                    | ((ulong)plain[3] << 24)
                    | ((ulong)plain[4] << 32)
                    | ((ulong)plain[5] << 40)
                    | ((ulong)plain[6] << 48)
                    | ((ulong)plain[7] << 56);
                if (receivedSequence < expectedSequence)
                {
                    failure = string.Format(
                        SequenceRegressionFormat,
                        expectedSequence,
                        receivedSequence);
                    break;
                }

                ushort opcode = (ushort)(plain[8] | (plain[9] << 8));
                byte[] payload = new byte[plain.Length - 10];
                Array.Copy(plain, 10, payload, 0, payload.Length);
                if (generation != Volatile.Read(ref _gen))
                {
                    break;
                }

                _inbox.Enqueue(new ProtocolPacket(opcode, payload));
                expectedSequence = unchecked(receivedSequence + 1);
            }
        }
        catch (Exception ex)
        {
            failure = ReceiveInterruptedPrefix + ex.Message;
        }

        if (_run && generation == Volatile.Read(ref _gen))
        {
            if (failure != null)
            {
                _errs.Enqueue(failure);
                LastDropReason = failure;
            }

            _run = false;
            _dropped = true;
        }
    }

    // Official 1.5.8 NetClient.ReadExact(NetworkStream ns, byte[] b, int n)
    // (0x06000779, arm64 0x272e70 / 220 bytes, x86_64 0x282570 / 144 bytes). The atlas row
    // gives the parameter names ["ns","b","n"]. Both architectures agree:
    //   * n < 1 returns true before anything else (arm64 0x272e84 "cmp w2,#1 / b.lt",
    //     x86_64 0x28257e "test edx,edx / jle").
    //   * ns == 0 raises NullReferenceException through the IL2CPP noreturn helper
    //     (arm64 0x272e90 -> 0x272f48 -> 0x177a74; x86_64 0x282585 -> 0x2825f2 -> 0x16c1b0),
    //     which is what "ns.Read(...)" already does, so no guard is written here.
    //   * The first Read asks for the whole n at offset 0 (arm64 0x272eb4, x86_64 0x2825a3)
    //     and a value < 1 returns false (arm64 0x272eb8, x86_64 0x2825a9).
    //   * The refill loop reads at the filled offset for the remaining count and returns
    //     false as soon as a read is not > 0 (arm64 0x272ed0-0x272ef4, x86_64 0x2825bb-
    //     0x2825e6); it completes when the remaining count is <= 0 and returns true.
    // The LSDA index reports no exception region for this method on either architecture
    // (Build/WB014-rx-lifecycle-20260928/lsda-arm64-exact.txt:58 and
    // lsda-x86_64-exact.txt:58), so the official body carries no try/catch and no finally:
    // the stream is borrowed, never closed or disposed here. RxLoop (0x06000778) is the only
    // caller in the product, and in the official build its call sites were inlined, which is
    // why the call index records no caller for this token.
    private static bool ReadExact(NetworkStream stream, byte[] buffer, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read < 1)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }

    // Official 1.5.8 NetClient.Update() (0x0600077D, arm64 0x273664 / 1536 bytes, x86_64
    // 0x282b60 / 1616 bytes). Both architectures agree. No exception region comes from the
    // native LSDA index, not from an absent call: Build/WB009-connect-owner-20260927/
    // lsda-arm64-exact.txt:71-72 reports "arm64 func 0x273664..0x273c64 / NO LSDA index entry
    // for 0x273664" and lsda-x86_64-exact.txt:71-72 the same for "x86_64 func 0x282b60..
    // 0x2831b0". The 58-entry call list agrees (no __cxa_begin_catch, no is-inst
    // System.Exception) but is only supporting evidence:
    //   DrainTx();                                                    0x06000775
    //   while (_errs.TryDequeue(out string e)) OnError?.Invoke(e);     +0xd8 queue, +0x70 field
    //     // product-only, not official: the same message also goes to the per-call
    //     // _hostErrorSink that Update(dispatch, onError, budget) bound for this call
    //   while (_connectDone.TryDequeue(out bool ok)) {                 +0xf0 queue
    //     _connecting = false;                                         +0xe8
    //     if (ok) _nextPingAt = Time.realtimeSinceStartup + 5f;        +0x100
    //     Action<bool> cb = _connectCb; _connectCb = null; cb?.Invoke(ok);   +0xf8
    //   }
    //   if (_dropped) { _dropped = false; Close(); OnDisconnected?.Invoke(); }  +0xa9/+0x80
    //   if (_tcp != null && _tcp.Connected && _nextPingAt <= Time.realtimeSinceStartup) {
    //     _nextPingAt = Time.realtimeSinceStartup + 5f;                // second clock read
    //     Ping(Environment.TickCount);                                 0x06000732
    //   }
    //   int count = _inbox.Count;                                      +0xd0
    //   if (_inboxPeak < count) _inboxPeak = count;                    +0x120
    //   if (BacklogWarn < count && _nextBacklogWarnAt <= Time.realtimeSinceStartup) {
    //     _nextBacklogWarnAt = Time.realtimeSinceStartup + 10f;        // second clock read
    //     Debug.LogWarning(string.Format(usage 14157, count, _inboxPeak));
    //   }
    //   if (LoadTrace.On) LoadTrace.Backlog(count);                    0x060001F0
    //   _dispatchSw.Restart();                                         +0x118
    //   int processed = 0;                     // native iVar2 enters the loop as 1 and is
    //   while (_inbox.TryDequeue(out packet)) { // incremented at the top of each next turn
    //     processed++;
    //     long started = LoadTrace.Now;                                0x06000204, before Dispatch
    //     Dispatch(packet.Opcode, packet.Payload);                     0x0600077F
    //     if (LoadTrace.On) {
    //       int len = payload == null ? 0 : payload.Length;  // resolved BEFORE Ms, see below
    //       LoadTrace.Net(opcode, len, LoadTrace.Ms(started));        0x060001EF
    //     }
    //     if (2 < processed) {                      // at least three dispatched this frame
    //       double elapsed = _dispatchSw.Elapsed.TotalMilliseconds;
    //       float budget = Mathf.Max(2f, GameSettings.TargetFrameMs * 0.36f);
    //       if (budget <= elapsed) return;
    //     }
    //   }
    //
    // Faithful points kept on purpose, do not "fix":
    //   * The ping guard is the official `_tcp != null && _tcp.Connected` test; it never reads
    //     _run, so the product Connected property is not used, and there is no `_nextPingAt <= 0`
    //     seeding branch. The comparison and the re-arm read the host clock twice.
    //   * OnError and OnDisconnected are read at these two points only, once each, from the
    //     official fields; OnDisconnected gets no argument and is not routed to OnError. The
    //     extra _hostErrorSink call next to the OnError read is product-only wiring for the
    //     error-queue drain and is not an official consumer: it is scoped to one Update(...) call
    //     and never writes the field, so a caller-assigned OnError still behaves as before.
    //   * The drain does not swallow: every dequeued error reaches the official field read and
    //     the per-call host sink, so removing the WB-012 global assignment loses no delivery.
    //   * `2 < processed` is a lower bound, not a cap: the budget is only consulted after the
    //     third dispatched packet, so a frame always dispatches at least three, and any number
    //     beyond that while the budget holds.
    //   * LoadTrace.Now is read unconditionally before the dispatch, even while tracing is off;
    //     a null payload contributes length 0 instead of throwing.
    //   * The third Net argument is the float returned by LoadTrace.Ms, not a constant 0.
    //     Ghidra's prototype for 0x060001EF omits the float parameter, so both the decompiled C
    //     (Net(uVar5 & 0xffff, uVar9, 0)) and the high pcode input list end with the trailing
    //     methodInfo 0 and never show ms. The instructions do: arm64 0x273b3c `bl 0x364600`
    //     (Ms) returns the float in s0 and nothing between it and 0x273b4c `bl 0x35d45c` (Net)
    //     writes a V register - only `mov x0, x24` (msgId), `mov x1, x23` (len) and
    //     `mov x2, #0x0` (methodInfo) run in between - and Net reads that register at entry
    //     (0x35d478 `mov.16b v8, v0`). x86_64 mirrors it: 0x28307e `callq 0x377310` (Ms), then
    //     `movl %r13d, %edi` / `movl %r15d, %esi` / `xorl %edx, %edx` and 0x28308b
    //     `callq 0x36f520` (Net), whose entry spills the argument at 0x36f53d
    //     `movss %xmm0, -0x2c(%rbp)`. Ms is therefore called for its value, not for effect.
    //   * len is resolved before that Ms call on both architectures and on both payload paths:
    //     arm64 0x273b20 `ldr w23, [x8, #0x18]` / 0x273bd4 `mov w23, #0x0` for a null payload,
    //     x86_64 0x283061 `movl 0x18(%rax), %r15d` / 0x283122 `xorl %r15d, %r15d`. The product
    //     source keeps that order by evaluating the length into a local first.
    //   * The budget is read from the host provider at every check; no frame time is cached.
    //   * `_inbox`, `_errs`, `_connectDone` and `_dispatchSw` are readonly fields, so the
    //     native null guards around them are unreachable and are not reproduced.
    public void Update()
    {
        DrainTx();
        while (_errs.TryDequeue(out string error))
        {
            // Official field read, unchanged: a real caller-assigned subscriber still fires.
            OnError?.Invoke(error);
            // Product-only per-call consumer; see _hostErrorSink. Not the official OnError.
            _hostErrorSink?.Invoke(error);
        }

        while (_connectDone.TryDequeue(out bool connectOk))
        {
            _connecting = false;
            if (connectOk)
            {
                _nextPingAt = _realtimeSeconds() + PingIntervalSeconds;
            }

            Action<bool> callback = _connectCb;
            _connectCb = null;
            callback?.Invoke(connectOk);
        }

        if (_dropped)
        {
            _dropped = false;
            Close();
            OnDisconnected?.Invoke();
        }

        if (_tcp != null && _tcp.Connected && _nextPingAt <= _realtimeSeconds())
        {
            _nextPingAt = _realtimeSeconds() + PingIntervalSeconds;
            Ping(Environment.TickCount);
        }

        int count = InboxBacklog;
        if (InboxPeak < count)
        {
            _inboxPeak = count;
        }

        if (BacklogWarn < count && _nextBacklogWarnAt <= _realtimeSeconds())
        {
            _nextBacklogWarnAt = _realtimeSeconds() + BacklogWarnCooldownSec;
            _warningSink?.Invoke(string.Format(BacklogWarningFormat, count, InboxPeak));
        }

        if (_receiveTrace.On)
        {
            _receiveTrace.Backlog(count);
        }

        _dispatchSw.Restart();
        int processed = 0;
        while ((_hostMayDequeue == null || _hostMayDequeue()) &&
               _inbox.TryDequeue(out ProtocolPacket packet))
        {
            processed++;
            long started = _receiveTrace.Now;
            _hostDispatch(packet.Opcode, packet.Payload);
            if (_receiveTrace.On)
            {
                // Native argument order, both architectures:
                //   len is resolved first - arm64 0x273b20 `ldr w23, [x8, #0x18]` /
                //   x86_64 0x283061 `movl 0x18(%rax), %r15d`, and both null paths set it to
                //   zero before the call (arm64 0x273bd4 `mov w23, #0x0`,
                //   x86_64 0x283122 `xorl %r15d, %r15d`) - then LoadTrace.Ms returns the
                //   elapsed time in the float return register and LoadTrace.Net receives it
                //   as its third argument.
                int length = packet.Payload == null ? 0 : packet.Payload.Length;
                _receiveTrace.Net(packet.Opcode, length, _receiveTrace.Ms(started));
            }

            if (2 < processed)
            {
                double elapsed = _dispatchSw.Elapsed.TotalMilliseconds;
                if (_dispatchMillisecondsPerFrame() <= elapsed)
                {
                    return;
                }
            }
        }
    }

    // Product-only host binding adapter. The official Update() takes no arguments, so this
    // overload only binds the host capabilities the official body reads and then runs that one
    // body: there is no second loop, no second error path and no protocol decision here.
    //   * dispatch stands in for the official Dispatch body (0x0600077F), which is not
    //     restored; it is the same handler the gateway already handed over before this package
    //     and it is still called once per dequeued packet.
    //   * onError is NOT assigned to the official OnError field (+0x70). WB-012 did that and
    //     WB-012-R2 reverts it: the field also feeds the disconnected send branch of 0x06000772
    //     and the address-not-ready branches of 0x060006BB / 0x060006BC, where the gateway
    //     handler would run DisposeConnectionCore (stage = Disconnected plus the
    //     connection-lifetime cancel). No official writer of that field was found:
    //       - NetClient.Awake (0x060006B8) and NetClient..ctor (0x06000780) never store to
    //         +0x70; the type has no Start and no OnEnable, so no lifecycle hook assigns it.
    //       - Connect (0x060006BB) and ConnectAsync (0x060006BC) only read +0x70
    //         (060006bb.c:37 / 060006bc.c:59). The only official callers of those two methods
    //         are TitleFlow.DrawLogin (0x06000EE6), DoRecover (0x06000EEC), DoAppeal
    //         (0x06000EEE) and DoRegister (0x06000EF0), and neither architecture stores to
    //         NetClient.I + 0x70 in any of them.
    //       - Across every exported body the only store to a +0x70 slot near NetClient is
    //         NetClient.ChangeSetSwap (0x06000743, 06000743.c:51-53), and that write targets
    //         NetClient+<>c.<>9__217_0 - the cached Action<NetClient.Buf> delegate at offset
    //         0x70 of the <>c static block (field 0x4000A49), not NetClient.OnError.
    //     That historical revert did not equate OnTransportError with official OnError. The
    //     address-resolution integration now binds the independently recovered title consumer
    //     outside this overload; the two real gateway Pump calls pass no product error sink.
    //   * onError instead binds the per-call product sink _hostErrorSink, which the error-queue
    //     drain of Update() reads alongside the official field. Scope is exactly one Update(...)
    //     call: the value is captured before the body runs and restored afterwards, so a
    //     re-entrant Pump cannot replace the outer call's callback, and because it is a separate
    //     field it never reaches 0x06000772 or 0x060006BB / 0x060006BC. The official OnError
    //     field is left untouched, so a caller that really assigns it keeps working and nothing
    //     here erases or fakes that subscription. No swap, no second Update loop, no swallowed
    //     error and no fabricated OnDisconnected subscriber.
    //   * dispatchMillisecondsPerFrame is the host budget provider; there is no 6f fallback,
    //     so a caller that pumps without a bound provider is not a supported product path.
    // Both OnError and OnDisconnected now have explicit composition-root assignments. For
    // OnDisconnected (+0x80),
    // GameBootstrap.Start (0x060003ef, arm64 060003ef.c:268-282 / x86_64 :231-243) has a recovered
    // assignment for it, and WBC-119 bound that subscriber once from the composition root, so this
    // invoke is live. The product anchor is Awake rather than Start - see NetClient.Connection.cs -
    // and nothing inside this body decides anything about it.
    public void Update(
        Action<ushort, byte[]> dispatch,
        Action<string> onError,
        Func<float> dispatchMillisecondsPerFrame = null,
        Func<bool> mayDequeue = null)
    {
        if (dispatch == null)
        {
            throw new ArgumentNullException(nameof(dispatch));
        }

        _hostDispatch = dispatch;
        Func<bool> previousMayDequeue = _hostMayDequeue;
        _hostMayDequeue = mayDequeue;
        Action<string> previousErrorSink = _hostErrorSink;
        _hostErrorSink = onError;
        _dispatchMillisecondsPerFrame = dispatchMillisecondsPerFrame;
        try
        {
            Update();
        }
        finally
        {
            // Per-call snapshot, not a global override: a nested Pump must not leave its own
            // sink behind for the outer call. The official OnError field is never touched here.
            _hostErrorSink = previousErrorSink;
            _hostMayDequeue = previousMayDequeue;
        }
    }

    public void Dispose()
    {
        Close();
    }
}
}
