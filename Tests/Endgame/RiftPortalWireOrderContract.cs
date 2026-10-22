using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;

// The runner adds unchanged production movement/session wrappers and native
// Send/PaceTx/DrainTx methods. Crypto and final socket writes alone are captured.
namespace FakeFantasyOnline.Protocol
{
    public enum GatewayStage { InWorld }
    public static class ProtocolOpcodes { public const ushort PlayerMoveRequest = 0x1017; }
    public static class NetCrypto
    {
        public static byte[] EncryptFrame(byte[] frame, byte[] aes, byte[] mac) => frame;
    }
    public sealed partial class NetClient
    {
        private sealed class SocketState { public bool Connected = true; }
        private readonly SocketState _tcp = new SocketState();
        private readonly byte[] _aesKey = new byte[0], _macKey = new byte[0];
        private ulong _sendSeq;
        private readonly Queue<byte[]> _txPending = new Queue<byte[]>();
        private const int MaximumTransmitsPerSecond = 45;
        private int _txSecCount;
        private float _txSecStart;
        private float now;
        private float _realtimeSeconds() => now;
        public Action<string> OnError;
        public readonly List<byte[]> Frames = new List<byte[]>();
        private void RawTx(byte[] frame) => Frames.Add(frame);
        internal partial class Buf { private byte[] _b; private int _pos; }
        public void SeedBudget(int used) => _txSecCount = used;
        public void DrainNextSecond() { now = 1; DrainTx(); }
        public int PendingFrames => _txPending.Count;
    }
    public sealed partial class PrivateServerGateway
    {
        private GatewayStage _stage = GatewayStage.InWorld;
        private readonly NetClient _netClient;
        private readonly IHostExecutor _host;
        private object _worldEntryAdmission = null;
        private static readonly EndgameRetryJournal endgameRetryJournal = new EndgameRetryJournal();
        private string EndgameScope => "portal-wire-test";
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld => true;
        // Occupied throughout each test. The actual movement wrapper must not
        // wait behind this unrelated gateway operation before queuing position.
        private readonly SemaphoreSlim _sendOperation = new SemaphoreSlim(0, 1);
        private static bool IsWireFailure(Exception error) => error is IOException;
        public PrivateServerGateway(NetClient wire, IHostExecutor host)
        { _netClient = wire; _host = host; }
    }
}
namespace FakeFantasyOnline.Application
{
    public enum ClientSessionState { InWorld }
    public sealed partial class ClientSessionCoordinator
    {
        private ClientSessionState _state = ClientSessionState.InWorld;
        private readonly PrivateServerGateway _gateway;
        public ClientSessionCoordinator(PrivateServerGateway gateway) { _gateway = gateway; }
    }
}
internal static class RiftPortalWireOrderContract
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private sealed class NoPostContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object state)
        { throw new Exception("Main-thread position unexpectedly posted instead of executing inline."); }
    }
    private static ushort Opcode(byte[] frame) => BitConverter.ToUInt16(frame, 8);
    public static void Main()
    {
        foreach (int usedBudget in new[] { 0, 44, 45 })
        {
            using var host = new SynchronizationContextHostExecutor(new NoPostContext(), Thread.CurrentThread.ManagedThreadId);
            var wire = new NetClient(); wire.SeedBudget(usedBudget);
            var gateway = new PrivateServerGateway(wire, host);
            var session = new ClientSessionCoordinator(gateway);
            gateway.Endgame.Begin(new EndgameRequest("hello"));
            gateway.Endgame.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"));
            Task<Result> movement = session.ReportPlayerMoveAsync(245, 300, CancellationToken.None);
            Check(movement.IsCompletedSuccessfully && movement.Result.IsSuccess,
                "Actual session/gateway movement executes synchronously on host, despite occupied operation semaphore.");
            Check(wire.Frames.Count + wire.PendingFrames == 1,
                "Movement has already entered this native connection before advance is called.");
            Check(gateway.SendEndgame(new EndgameRequest("rift.advance", runID: "same-connection", floorIndex: 1)),
                "Actual gateway accepts the portal mutation.");
            Check(wire.Frames.Count + wire.PendingFrames == 2, "Both frames belong to the same native sender.");
            wire.DrainNextSecond();
            Check(wire.Frames.Count == 2 && wire.PendingFrames == 0, "Native pacing queue drains both frames.");
            Check(Opcode(wire.Frames[0]) == 0x1017 && Opcode(wire.Frames[1]) == 0x10d0,
                "Real Send/PaceTx/DrainTx preserve movement before advance under budget " + usedBudget + ".");
            Check(BitConverter.ToUInt64(wire.Frames[0], 0) == 0 && BitConverter.ToUInt64(wire.Frames[1], 0) == 1,
                "Both packets use one monotonic connection sequence.");
            Check(BitConverter.ToInt32(wire.Frames[0], 10) == 245 && BitConverter.ToInt32(wire.Frames[0], 14) == 300,
                "Production native buffer carries exact current position.");
        }
        Console.WriteLine("PASS " + checks + " actual session/gateway/host/native FIFO contracts; crypto/socket substituted, no live network.");
    }
}
