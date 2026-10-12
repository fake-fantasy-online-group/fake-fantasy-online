using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;

struct Color32 { }
class Frame { public WorldEntryRecord Applied; public bool ApplyLocalPlayerAppearance(WorldEntryRecord value, Color32 tint) { Applied = value; return true; } }
class Equipment { public bool CanApplyAppearance(WorldEntryRecord value) => true; public bool ApplyAppearance(WorldEntryRecord value) => true; }
class SessionView {
    readonly PrivateServerGateway gateway;
    public SessionView(PrivateServerGateway value) { gateway = value; }
    public long PlayerAppearanceStateVersion => gateway.PlayerAppearanceStateVersion;
    public WorldPlayerAppearanceState PlayerAppearanceState => gateway.PlayerAppearanceState;
}
partial class WorldOwner {
    SessionView session;
    WorldEntryRecord character;
    Frame frame = new Frame();
    Equipment localEquipmentDialog = new Equipment();
    long playerAppearanceStateVersion = long.MinValue;
    public WorldOwner(PrivateServerGateway gateway, WorldEntryRecord value) { session = new SessionView(gateway); character = value; }
    public bool Run() => SyncPlayerAppearance(true, default(Color32));
    public int Body => character.Appearance.Body;
}
class Host : IHostExecutor {
    public bool IsHostThread => true;
    public Task RunAsync(Action action, CancellationToken token) { token.ThrowIfCancellationRequested(); action(); return Task.CompletedTask; }
}
class AppearanceHandoffContract {
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Require(bool value, string label) { if (!value) throw new Exception(label); }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    static void Set(object target, string name, object value) => target.GetType().GetField(name, Flags).SetValue(target, value);
    static void Str(BinaryWriter writer, string value) { byte[] bytes = Encoding.UTF8.GetBytes(value); writer.Write((ushort)bytes.Length); writer.Write(bytes); }
    static void Appearance(BinaryWriter writer, int body) { writer.Write((ushort)body); for (int i = 0; i < 5; i++) writer.Write((ushort)0); writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)0); }
    static byte[] AppearancePacket(int entity, int body) {
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream)) {
            writer.Write(entity); Appearance(writer, body); Str(writer, ""); Str(writer, ""); return stream.ToArray();
        }
    }
    static byte[] SelfData(int entity, string name, int body) {
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream)) {
            writer.Write(entity); writer.Write((byte)0); Str(writer, name); writer.Write(58); writer.Write(7581f); writer.Write(5236f);
            writer.Write((byte)1); writer.Write((byte)1); writer.Write((byte)1); writer.Write((byte)1);
            writer.Write((ushort)7); foreach (int value in new[] {60, 0, 100, 100, 100, 50, 50}) writer.Write(value);
            Str(writer, ""); Appearance(writer, body); writer.Write(0); Str(writer, ""); writer.Write(0); writer.Write((long)183);
            return stream.ToArray();
        }
    }
    static WorldOwner Enter(PrivateServerGateway gateway, int entity, string name, int body) {
        WorldOwner owner = null;
        gateway.BindWorldEntryReceipt(entry => {
            owner = new WorldOwner(gateway, entry);
            // Real 0x8003 receipt decoded this identity, map and appearance before the gate.
            Require(entry.EntityId == entity && entry.MapId == 58 && entry.Appearance.Body == body, "self-data receipt");
            gateway.CaptureWorldEntryCompletion(entry)();
        });
        Require((bool)Call(gateway, "TryReceiveWorldEntryWithoutWaiter", (ushort)0x8003, SelfData(entity, name, body)), "0x8003 handled");
        return owner;
    }
    static void Publish(PrivateServerGateway gateway, int entity, int body) {
        WorldPlayerAppearanceState value;
        Require(PrivateServerWorldStateDecoder.TryDecodePlayerAppearance(AppearancePacket(entity, body), out value), "0x800a decoder");
        Call(gateway, "PublishPlayerAppearanceState", value, CancellationToken.None);
    }
    static void LeaveBoundary(PrivateServerGateway gateway) {
        // Exercise the exact cleanup used by LeaveWorldForTitleAsync. Socket/roster IO is
        // deliberately excluded; transport flags model the stopped drain at that boundary.
        Set(gateway, "_worldDrainActive", false);
        Call(gateway, "ClearDepartedPlayerSnapshots", CancellationToken.None);
    }
    static PrivateServerGateway Gateway() {
        var gateway = new PrivateServerGateway(PrivateServerOptions.LocalDefault(), new Host(), () => 0f, worldReceiveOwner: new ReceiveOwner());
        Set(gateway, "_connectionLifetime", new CancellationTokenSource());
        return gateway;
    }
    static void Main(string[] args) {
        bool expectBug = args.Length > 0 && args[0] == "--expect-stale-bug";
        foreach (var scenario in new[] { (5, 6, "刺客春霞"), (7, 8, "药师骚骚霞") }) {
            var gateway = Gateway();
            Require(Enter(gateway, scenario.Item1, scenario.Item3, 100).Run(), "first character ready");
            Publish(gateway, scenario.Item1, 101);
            long version = gateway.PlayerAppearanceStateVersion;
            LeaveBoundary(gateway);
            WorldOwner next = Enter(gateway, scenario.Item2, "术士姐姐", 200);
            if (expectBug) { Require(!next.Run(), "old entity must reproduce InvalidPlayerAppearance gate"); continue; }
            Require(gateway.PlayerAppearanceState == null, "departed appearance retired");
            Require(gateway.PlayerAppearanceStateVersion == version + 1, "retirement invalidates observation version");
            Require(next.Run() && next.Body == 200, "new character retains self-data appearance");
            Publish(gateway, scenario.Item2, 201);
            Require(next.Run() && next.Body == 201, "current character subsequent 0x800a applies");
            Call(gateway, "BeginWorldScenePublication", 59, CancellationToken.None);
            Require(gateway.PlayerAppearanceState.Appearance.Body == 201, "same character map transition preserves appearance");
            LeaveBoundary(gateway);
            WorldOwner same = Enter(gateway, scenario.Item2, "术士姐姐", 202);
            Require(same.Run() && same.Body == 202, "same character reentry uses new self-data");
            Publish(gateway, 0, 203);
            Require(same.Run() && same.Body == 203, "local alias appearance still applies");
            LeaveBoundary(gateway);
            Require(Enter(gateway, scenario.Item1, scenario.Item3, 300).Run(), "alias cannot leak across character switch");
            long emptyVersion = gateway.PlayerAppearanceStateVersion;
            LeaveBoundary(gateway);
            Require(gateway.PlayerAppearanceStateVersion == emptyVersion, "empty cleanup is idempotent");
        }
        Console.WriteLine(expectBug ? "REPRODUCED stale appearance rejects both assassin->warlock and healer->warlock" : "PASS appearance handoff: two professions, reentry, current updates, zero alias, map preservation, versioning, idempotence");
    }
}
