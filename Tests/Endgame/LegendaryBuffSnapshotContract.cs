using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Executes the native production snapshot reader, not a duplicate reconciliation model.
static class LegendaryBuffSnapshotContract
{
    sealed class Reader : IWorldBuffReader
    {
        readonly BinaryReader reader;
        public Reader(byte[] bytes) { reader = new BinaryReader(new MemoryStream(bytes), Encoding.UTF8); }
        public int I32() => reader.ReadInt32();
        public int U8() => reader.ReadByte();
        public int U16() => reader.ReadUInt16();
        public string Str() => reader.ReadString();
        public int Remaining => (int)(reader.BaseStream.Length - reader.BaseStream.Position);
    }
    static byte[] Snapshot(int count, int remaining = 5, int icon = 7100, string name = "蓄势 ×2", string desc = "再次施法后触发；2层", bool future = false)
    {
        using (var bytes = new MemoryStream())
        {
            var w = new BinaryWriter(bytes, Encoding.UTF8);
            w.Write((ushort)count);
            for (int i = 0; i < count; i++)
            {
                w.Write(400001 + i); w.Write(icon); w.Write(remaining); w.Write(name); w.Write(desc);
                w.Write((byte)1); w.Write((byte)255); w.Write((byte)0); w.Write((byte)0);
            }
            w.Write((byte)0);
            if (future) { w.Write(12345); w.Write("future optional presentation"); }
            return bytes.ToArray();
        }
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main()
    {
        var owner = new ReferenceWorldReceiveOwner();
        owner.ReceiveBuffState(new Reader(Snapshot(2)), () => 10, () => false);
        Check(CharData.buffs.Count == 2 && CharData.buffs[0].iconId == 7100, "add icons");
        Check(CharData.buffs[0].name == "蓄势 ×2" && CharData.buffs[0].desc.Contains("2层"), "server stack/name/description");
        Check(CharData.buffs[0].posMode == -1 && CharData.buffs[0].recvTime == 10, "no invented world effect or clock");
        owner.ReceiveBuffState(new Reader(Snapshot(1, 3, 7200, "蓄势 ×1", "剩余1次", true)), () => 12, () => false);
        Check(CharData.buffs.Count == 1 && CharData.buffs[0].iconId == 7200 && CharData.buffs[0].remainSec == 3, "replace stale rows and icon; tolerate optional tail");
        Check(CharData.buffs[0].name == "蓄势 ×1", "refresh stack presentation");
        owner.ReceiveBuffState(new Reader(Snapshot(1, 2)), () => 20, () => true);
        Check(CharData.buffs[0].remainSec == 2 && CharData.buffs[0].recvTime == 20, "map snapshot keeps authoritative remaining duration");
        owner.ReceiveBuffState(new Reader(Snapshot(1, -2, 0)), () => 21, () => false);
        Check(CharData.buffs[0].remainSec == -2 && CharData.buffs[0].iconId == 0, "source-bound and authored zero remain intact");
        Check(!owner.ReceiveBuffState(new Reader(Snapshot(0)), () => 22, () => false) && CharData.buffs.Count == 0, "consume/expire/dispel/remove snapshot");
        Console.WriteLine("PASS native legendary buff replacement, stack text, icons, map duration, optional tail, source lifetime and removal");
    }
}
namespace UnityEngine { public static class Time { public static float realtimeSinceStartup; } }
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class ReferenceWorldReceiveOwner { }
    static class CustomMaps { public static bool IsCustom(int id) => false; }
    static class GameBootstrap { public static int CurMapId; }
    static class CharData
    {
        public sealed class BuffView { public int skillId, iconId, remainSec, posMode; public string name, desc; public bool good; public float recvTime; }
        public static List<BuffView> buffs; public static bool invisible, doubleExp; public static byte immobile;
    }
}
