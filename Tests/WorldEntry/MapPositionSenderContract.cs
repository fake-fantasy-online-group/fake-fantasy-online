using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// The runner compiles the production portal/iterator, bootstrap reporting, session
// forwarding, gateway wrappers and native payload writers. Only host/transport and
// Unity are substituted; a queued socket write is not server acknowledgement.
namespace FakeFantasyOnline.Foundation
{
    public sealed class Result
    {
        public static Result Success() => new Result();
        public static Result Failure(string code, string reason) => new Result();
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public enum ClientSessionState { InWorld }
    public enum GatewayStage { InWorld }
    public static class ProtocolOpcodes
    {
        public const ushort SavePositionRequest = 0x100a, PlayerMoveRequest = 0x1017;
    }
    public sealed class PositionPacket
    {
        public ushort Opcode;
        public byte[] Body;
        public int I32(int offset) => BitConverter.ToInt32(Body, offset);
        public bool Entered => Body.Length == 13 && Body[12] != 0;
    }
    public sealed class PositionPayload
    {
        public readonly MemoryStream Bytes = new MemoryStream();
        public void I32(int value) { byte[] bytes = BitConverter.GetBytes(value); Bytes.Write(bytes, 0, bytes.Length); }
        public void U8(byte value) => Bytes.WriteByte(value);
    }
    public sealed partial class NativePositionSender
    {
        public static readonly List<PositionPacket> Packets = new List<PositionPacket>();
        void Send(ushort opcode, Action<PositionPayload> write)
        {
            var payload = new PositionPayload(); write(payload);
            Packets.Add(new PositionPacket { Opcode = opcode, Body = payload.Bytes.ToArray() });
        }
    }
    public sealed class TestPositionHost
    {
        public Task RunAsync(Action action, CancellationToken token) { token.ThrowIfCancellationRequested(); action(); return Task.CompletedTask; }
    }
    public sealed partial class TestGateway
    {
        GatewayStage _stage = GatewayStage.InWorld;
        NativePositionSender _netClient = new NativePositionSender();
        TestPositionHost _host = new TestPositionHost();
        public static readonly List<int> Publications = new List<int>();
        void BeginWorldScenePublication(int mapId, CancellationToken token) => Publications.Add(mapId);
        Task SendAsync(Action action, CancellationToken token) => _host.RunAsync(action, token);
        bool IsWireFailure(Exception exception) => exception is IOException;
    }
    public sealed partial class TestSession
    {
        ClientSessionState _state = ClientSessionState.InWorld;
        TestGateway _gateway = new TestGateway();
    }
    public static class ArtUploadClock { public static void BeginFrame() { } }
    public partial class GameBootstrap
    {
        public static GameBootstrap I;
        CancellationTokenSource positionReportLifetime = new CancellationTokenSource();
        int _lastReportX = int.MinValue, _lastReportY = int.MinValue;
        void UpdateTraceDiagnosticsForIndependentRuntime() { }
        void UpdateFade() { }
        public void PumpPositionForTest(float seconds) { UnityEngine.Time.deltaTime = seconds; Update(); }
        public void ForcePeriodicSaveForTest() => ReportPos(false);
    }
}
static class MapPositionSenderContract
{
    static int checks;
    static void Require(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    static void Main()
    {
        NativePositionSender.Packets.Clear(); TestGateway.Publications.Clear();
        ResLoader.I = new ResLoader();
        var b = new GameBootstrap(); GameBootstrap.I = b;
        // Original ordinary gate has no Flush or request send. A caller may have
        // sent movement earlier, but the final source-polygon position is optional.
        b.HitPortalForTest(285, 100, 100);
        Require(b.Started == 1 && b.Builds.Count == 0, "native gate started local prebuild");
        Require(NativePositionSender.Packets.Count == 0, "portal trigger emits no request, entered=0 or forced movement");
        Require(GameBootstrap.CurMapId == 1, "pending map did not replace current source map before construction");
        b.ForcePeriodicSaveForTest();
        PositionPacket first = NativePositionSender.Packets.Single();
        Require(first.Opcode == 0x100a && first.Body.Length == 13 && first.I32(0) == 1 && !first.Entered,
            "periodic save while prebuilding still carries source map entered=0");
        Require(first.I32(4) == 10 && first.I32(8) == 20, "prebuild periodic save uses old player position");
        b.PumpPositionForTest(.3f);
        PositionPacket movement = NativePositionSender.Packets.Last();
        Require(movement.Opcode == 0x1017 && movement.Body.Length == 8 && movement.I32(0) == 10 && movement.I32(4) == 20,
            "movement payload contains coordinates only, no destination or source tag");
        while (b.Builds.Count == 0)
        {
            Require(!NativePositionSender.Packets.Any(p => p.Opcode == 0x100a && p.I32(0) == 285),
                "no target-map save precedes construction");
            b.Step();
        }
        PositionPacket ready = NativePositionSender.Packets.Last();
        Require(b.Built && ready.Opcode == 0x100a && ready.I32(0) == 285 && ready.Entered,
            "first target-map notification is entered=1 after complete production build");
        Require(ready.I32(4) == 100 && ready.I32(8) == 100, "ready identifies exact configured landing");
        Require(TestGateway.Publications.SequenceEqual(new[] { 285 }), "only built target ready starts gateway scene publication");
        int count = NativePositionSender.Packets.Count; b.Drain();
        Require(NativePositionSender.Packets.Count == count, "fade and settled tail emit no extra confirmation");
        Require(NativePositionSender.Packets.Count(p => p.Opcode == 0x100a && p.Entered) == 1,
            "ordinary local first build emits one native ready");
        b.ForcePeriodicSaveForTest();
        PositionPacket saved = NativePositionSender.Packets.Last();
        Require(saved.I32(0) == 285 && !saved.Entered, "target entered=0 appears only after target was built and ready was issued");
        Console.WriteLine("PASS " + checks + " actual position-sender checks: no prebuild target/portal notification; one post-build native ready; host and transport substituted");
    }
}
