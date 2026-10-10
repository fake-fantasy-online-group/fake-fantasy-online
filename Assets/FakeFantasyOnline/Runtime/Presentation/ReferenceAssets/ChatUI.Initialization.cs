using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // 1.6.0 06000101: keep the allocating initializers in this declaration, in native
    // publication order. Field initializers retain the original BeforeFieldInit flag;
    // an explicit static constructor would change that initialization contract.
    private static readonly List<(byte, byte, int, string)> pendingRefs =
        new List<(byte, byte, int, string)>();

    // Unlike the other roots, this slot is mutable: TakeOutgoing and the send mapper
    // replace it. It nevertheless starts as a constructed empty list, not null.
    public static List<(byte, byte, int)> OutgoingShares = new List<(byte, byte, int)>();

    private static readonly List<Line> chatLines = new List<Line>();
    private static readonly List<Banner> bcQueue = new List<Banner>();

    private static readonly (string Name, float Offset, string Tooltip)[] SideBtns =
    {
        ("viewbtn", -120f, "聊天信息查看面板"),
        ("filterbtn", -96f, "过滤聊天信息: 勾选要显示的频道"),
        ("clearbtn", -72f, "清空聊天窗口信息"),
        ("lockbtn", -48f, "聊天背景"),
        ("sizebtn", -24f, "缩进/弹出聊天窗口")
    };

    private static readonly List<Line> _sysTmp = new List<Line>();
    private static readonly List<Line> _chatTmp = new List<Line>();

    // Original static slot +0x60. The manual channel renderer borrows this array,
    // just as it borrows the original message, share and side-button roots.
    internal static readonly string[] Channels = { "附近", "队伍", "家族", "世界" };

    // Official 1.6.0 static +0x68; this table's order defines the persisted mask bits.
    private static readonly (string ch, string cap)[] FilterChans =
    {
        ("世界", "世界消息"),
        ("附近", "附近消息"),
        ("队伍", "队伍消息"),
        ("家族", "家族消息")
    };
}
}
