using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // 06000454. Keep every native instance initializer in this one declaration so C#
    // cannot order the allocating operations by an unrelated partial file. The implicit
    // constructor still performs these writes before the unchanged MonoBehaviour base call.
    internal int headPortVer = -2;
    private Dictionary<int, Texture2D> ringMaskCache = new Dictionary<int, Texture2D>();
    public bool showAvatar = true, showHotbar = true, showMinimap = true, showChat = true,
        showExp = true, showMenu = true, showTarget = true;
    private int petHeadModel = -1;
    private string sketchName = "";
    internal string mapName = "";
    private bool miniOpen = true;
    internal float miniZoom = 2f;
    internal bool miniDirty = true;
    internal int lastSx = -9999;
    internal readonly List<GateMark> gateMarks = new List<GateMark>();
    private int ringFracQ = -1;
    internal float hpAimVal = -1f;

    // 06000455. Allocation, initializer completion, then publication in native order.
    // Keep field initializers (no explicit static constructor) to preserve BeforeFieldInit.
    internal static readonly List<TgSt> tgStatus = new List<TgSt>();
    private static bool hotbarExpanded = false;
    private static int hotbarRow = 0;
    internal static readonly int[] hotSkill = new int[30];
    private static readonly Texture2D[] hotIcon = new Texture2D[30];
    internal static readonly int[] hotKind = new int[30];
    private static readonly int[] hotServerIcon = new int[30];
    private static readonly Rect[] hotSlotRect = new Rect[30];
    private static readonly Dictionary<int, Vector2> skillCd = new Dictionary<int, Vector2>();
    private static int dragFromSlot = -1;
    private static int pressSlot = -1;
    private static readonly Dictionary<int, Texture2D> buffIcons = new Dictionary<int, Texture2D>();
    internal static readonly List<CharData.BuffView> EmptyBuffs = new List<CharData.BuffView>();
    private static readonly float[] BarProf =
    {
        0.07f, 0.56f, 0.12f, 0.99f, 1f, 1f,
        1f, 1f, 0.97f, 0.1f, 0.53f, 0.07f
    };

    // The complete nine-element array is published only after all tuple elements exist.
    private static readonly (string, float, string, string)[] CtrlBtns =
    {
        ("statusbtn", 6f, "快捷键:C", "装备"),
        ("warebtn", 48f, "快捷键:I", null),
        ("teambtn", 90f, "快捷键:Q", null),
        ("familybtn", 132f, "快捷键:F", null),
        ("skillbtn", 174f, "快捷键:T", null),
        ("taskbtn", 216f, "快捷键:N", "任务"),
        ("emailbtn", 258f, "快捷键:L", "邮件"),
        ("rackbtn", 300f, "快捷键:Alt+G", "交易"),
        ("optionbtn", 342f, "快捷键:O", null)
    };
}

}
