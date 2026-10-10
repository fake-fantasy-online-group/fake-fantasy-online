using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class NetClient
{
    // Official 06000781: all thirteen initialized fields belong to this one runtime
    // owner, in native order. Keep them together: ordering between partial files is
    // unspecified. Field initializers retain the original BeforeFieldInit boundary.
    // The Protocol resolver publishes the exact parsed array through the composition
    // binding; neither Protocol nor the address adapters allocate a second static root.
    public static ClientConfig.Srv[] Servers = new ClientConfig.Srv[0];

    // Official 1.5.8 NetClient.RttMs: System.Int32 static, atlas static offset 16 (0x10),
    // milliseconds. NetClient..cctor (0x06000781) writes -1 into it together with
    // NetClient.OnlineCount (static offset 20) with one eight-byte store - arm64 0x274194
    // `movi.2d v0,#-1` / 0x274198 `str d0,[x8,#0x10]`, x86_64 the matching pair - so -1 and not
    // zero is the official unmeasured root.
    // This slot is the single authority for the round-trip latency in this product. Protocol
    // decodes the 0x8001 wire and the TickCount subtraction and hands the resulting int to
    // IWorldReceiveOwner.ReceiveRoundTripLatency exactly once per received packet; nothing else
    // stores, mirrors, caches or recomputes the value, and Presentation never reads Protocol.
    // Until a real 0x8001 reply arrives the slot stays -1, which is what makes
    // TitleFlow.DrawServer (0x06000ef4) render usage 17394 "延迟 --" or usage 17396 "延迟 测量中…"
    // instead of a fabricated number.
    internal static int RttMs = -1;

    // Official 1.5.8 NetClient.OnlineCount: System.Int32 static, atlas static offset 20 (0x14),
    // written by NetClient.Dispatch (0x0600077f) case 0x805b and by nothing else in the frozen
    // Dispatch body - the linear store scan over all 22962 instructions of that body finds one
    // store to [[TypeInfo:NetClient] + 0xb8] + 0x14, at case 0x805b (arm64 0x24cd10
    // `str w0,[x8,#0x14]` / x86_64 0x25a70d `movl %eax,0x14(%rcx)`); the case reads one
    // Buf.U16() (0x06000793, arm64 0x26b690 / x86_64 0x27ad70) after the null-Buf raise, so the
    // wire value is a 16-bit count widened into a signed slot. NetClient..cctor (0x06000781)
    // covers this slot with the same eight-byte -1 store as RttMs, so -1 and not zero is the
    // official "never answered" root; no disconnect, close or stage change writes it again, and
    // a stale count staying on screen after a reconnect is official rather than a leak.
    internal static int OnlineCount = -1;

    private static readonly Dictionary<int, Texture2D> _grayIcon = new Dictionary<int, Texture2D>();
    private static readonly Dictionary<string, int> _equipAv = new Dictionary<string, int>();
    private static readonly Dictionary<string, int> _wardrobeAv = new Dictionary<string, int>();
    private static readonly HashSet<string> _wardrobeSlots = new HashSet<string>();
    private static readonly string[] _avCats = { "body", "cap", "face", "backpack", "weaponr", "weaponl" };

    private static readonly string[][] _wcatToSlots =
    {
        new[] { "cap" }, new[] { "face" }, new[] { "body" },
        new[] { "backpack" }, new[] { "weaponr", "weaponl" },
    };

    // Official 1.5.8 NetClient..cctor (0x06000781), static +0x68.
    public static int PendingCastSkill = -1;
    private static int _pendSelfAlive = -1;

    public static readonly List<(string, long, string, byte, string, int)> GmFeedbacks =
        new List<(string, long, string, byte, string, int)>();
    public static readonly List<(int, string, int, int, byte)> VoteOptions =
        new List<(int, string, int, int, byte)>();
}

}
