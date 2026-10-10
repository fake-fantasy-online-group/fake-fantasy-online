using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Original 0600105e: all native static initializers are in this one declaration,
    // preserving allocation/publication order independently of partial-file ordering.
    // Array and dictionary initializers publish only after every element/Add succeeds.
    // No explicit static constructor: retain the original BeforeFieldInit (0x100000).
    private static readonly string[] SkillTabName = { "主修", "辅助", "生活" };

    private static readonly string[] SkillTabKey = { "maintab", "othertab", "lifetab" };

    private static readonly (int Id, string Name, int IconId)[] LifeSkillList =
    {
        (11007, "挖矿", 5069),
        (11008, "伐木", 5070),
        (11006, "钓鱼", 5068),
        (11004, "矿石合成", 5066),
        (11002, "武器工匠", 5064),
        (11003, "防具工匠", 5065),
        (11005, "炼丹士", 5067),
        (11001, "商人", 5063)
    };

    private static Dictionary<int, (int, string)> lifeData = new Dictionary<int, (int, string)>();

    private static HashSet<int> lifeCanLearn = new HashSet<int>();

    // Official Win05..cctor 0600105e, arm64 rva 0x3c6720 (6804 bytes) / x86_64 rva 0x3dae20 (7408 bytes):
    // the static `Dictionary<string, Def> Defs` (Win05 class static +0x58) is filled with nineteen
    // `new Def()` + `Defs[key] = def` pairs in one fixed order. Every Def reads its four geometry
    // floats out of one sixteen-byte frozen block: arm64 _DAT_01127b20 (+0x18 w, +0x1c h, +0x20 cx,
    // +0x24 cy) stepping by 0x10, x86_64 _DAT_0111d070 with the same layout. Values below are read
    // straight out of the frozen GameAssembly-arm64.dylib (__DATA_CONST, file offset 0x1127b20),
    // and the x86_64 export assigns the same blocks to the same keys in the same order - the
    // sixteenth entry (studydlg) reuses the skilldlg block on both architectures
    // (arm64 _DAT_01127b30 / x86_64 _DAT_0111d080). Flags are the byte stores next to each Def:
    // +0x30 nohelp, +0x32 iron, +0x33 g2021 (petdlg stores both +0x32 and +0x33 in one
    // `*(undefined2 *)0x32 = 0x101`). The four gsa paths that already exist as product constants
    // (skilldlg/studydlg, petdlg, changeequipdlg, magicstoredlg) reuse those constants so the table
    // and the adapters cannot drift; every geometry value stays the frozen literal.
    private static readonly Dictionary<string, Def> Defs = new Dictionary<string, Def>
    {
        { "historybar", new Def { gsa = "res/uires/chatbar/historybar.gsa", w = 556f, h = 521f,
            cx = 531f, cy = 17f, title = "聊天信息", nohelp = true } },
        { "skilldlg", new Def { gsa = SkillDialogBackgroundPath, w = 283f, h = 397f,
            cx = 256f, cy = 17f, title = "技能" } },
        { "familylistdlg", new Def { gsa = "res/uires/family/familylistdlg.gsa", w = 494f,
            h = 325f, cx = 469f, cy = 10f, title = "家族" } },
        { "sysconfigdlg", new Def { gsa = "res/uires/sysoption/sysconfigdlg.gsa", w = 267f,
            h = 445f, cx = -1f, cy = 0f, title = "系统设置" } },
        { "petdlg", new Def { gsa = PetDialogBackgroundPath, w = 257f, h = 542f,
            cx = 230f, cy = 17f, title = "QQ宝贝宠", iron = true, g2021 = true } },
        { "taskdlg", new Def { gsa = "res/uires/taskdlg/taskbk.gsa", w = 495f, h = 397f,
            cx = 469f, cy = 18f, title = "任务" } },
        { "maillistdlg", new Def { gsa = "res/uires/maildlg/maillistdlg.gsa", w = 418f, h = 325f,
            cx = -1f, cy = 0f, title = "信件", nohelp = true } },
        { "teamdlg", new Def { gsa = null, w = 187f, h = 374f, cx = 162f, cy = 15f,
            title = "交友", nohelp = true } },
        { "storedlg", new Def { gsa = "res/uires/storedlg/storedlgbk.gsa", w = 223f, h = 301f,
            cx = 198f, cy = 17f, title = "仓库" } },
        { "composedlg", new Def { gsa = "res/uires/composedlg/composedlg.gsa", w = 450f, h = 484f,
            cx = 340f, cy = 40f, title = "合成" } },
        { "repairdlg", new Def { gsa = "res/uires/repairdlg/repairdlg.gsa", w = 193f, h = 213f,
            cx = 169f, cy = 17f, title = "修理" } },
        { "figuredlg", new Def { gsa = "res/uires/figuredlg/figuredlg.gsa", w = 345f, h = 476f,
            cx = 276f, cy = 34f, title = "人物" } },
        { "grocerystatusdlg", new Def { gsa = "res/uires/grocerydlg/grocerystatus.gsa", w = 291f,
            h = 443f, cx = 266f, cy = 22f, title = "杂货" } },
        { "tradedlg", new Def { gsa = "res/uires/tradedlg/tradedlgbk.gsa", w = 569f, h = 426f,
            cx = 543f, cy = 21f, title = "交易" } },
        { "sysoptiondlg", new Def { gsa = "res/uires/sysoption/sysoptiondlg.gsa", w = 192f,
            h = 207f, cx = -1f, cy = 0f, title = "系统选项", nohelp = true } },
        { "studydlg", new Def { gsa = SkillDialogBackgroundPath, w = 283f, h = 397f,
            cx = 256f, cy = 17f, title = "学习技能" } },
        { "changeequipdlg", new Def { gsa = ChangeEquipmentBackgroundPath, w = 257f,
            h = 329f, cx = 230f, cy = 18f, title = "换装方案", g2021 = true } },
        { "magicstoredlg", new Def { gsa = MagicWardrobeBackgroundPath, w = 540f,
            h = 302f, cx = 510f, cy = 11f, title = "魔法衣橱", g2021 = true } },
        { "fosterdlg", new Def { gsa = "res/uires/foster/fosterdlgbk.gsa", w = 307f, h = 466f,
            cx = 283f, cy = 18f, title = "培养", g2021 = true } },
    };

    private static readonly List<WardrobeItem> wardrobe = new List<WardrobeItem>();

    private static int magicDragFrom = -1;

    private static string petRenameText = string.Empty;

    private static readonly List<ChgItem> changeSet = new List<ChgItem>();

    private static readonly (float x, float y)[] ChgSlotPos =
    {
        (20f, 80f), (20f, 120f), (20f, 160f), (20f, 200f), (18f, 252f),
        (212f, 80f), (212f, 120f), (212f, 160f), (212f, 200f), (213f, 252f), (116f, 250f)
    };

    private static int chatTab = 7;

    private static int _histSig = -1;

    private static int _histSigTab = -1;

    private static float _histMaxY = -1f;

    private static readonly List<(string, Color, string, string, bool, string,
        List<Share>)> _histRows =
        new List<(string, Color, string, string, bool, string, List<Share>)>();

    private static int _histRowsVer = -1;

    private static int _histRowsTab = -1;

    private static float _histRowsS = -1f;

    private static readonly (string, string)[] ChatTabs =
    {
        ("systemtab", "系统"), ("gmtab", "GM"), ("", "投票"),
        ("teamtab", "队伍"), ("broadtab", "广播"), ("uniontab", "家族"),
        ("whispertab", "私聊"), ("nearbytab", "附近"), ("worldtab", "世界"), ("", "公告")
    };

    private static string voteInput = "";

    private static int voteCacheVer = -1;

    private static float voteCacheS = -1f;

    private static readonly List<(int, List<string>, int, int, byte, float)> voteRows =
        new List<(int, List<string>, int, int, byte, float)>();

    private static readonly List<(string, bool)> _ntBlocks = new List<(string, bool)>();

    private static int _ntStamp = -1;

    private static float _ntW = -1f;

    private static string fbInput = "";

    private static int gmTypeFrame = -10;

    private static readonly List<(string, int, List<string>, List<string>, float)> fbRows =
        new List<(string, int, List<string>, List<string>, float)>();

    private static int fbCacheVer = -1;

    private static float fbCacheS = -1f;

    private static int fbCacheSub = -1;

    // Original 0600105d: keep the six cache allocations and intervening winX store
    // in native order. The compiler still calls MonoBehaviour..ctor after field initializers.
    private readonly Dictionary<int, Texture2D> skillIconCache =
        new Dictionary<int, Texture2D>();

    private readonly Dictionary<string, Texture2D> bkCache =
        new Dictionary<string, Texture2D>();

    private float winX = -1f;

    private readonly Dictionary<string, Texture2D> ironCache =
        new Dictionary<string, Texture2D>();

    private readonly Dictionary<int, Texture2D> petSkillIconCache =
        new Dictionary<int, Texture2D>();

    private readonly Dictionary<int, Texture2D> itemIconCache =
        new Dictionary<int, Texture2D>();

    private readonly Dictionary<string, Texture2D[]> uiFrameCache =
        new Dictionary<string, Texture2D[]>();

    private IWorldWardrobeObservation magicWardrobeState;

    private WorldPlayerPetStatusState petStatus;

    private WorldSkillBookState skillBook;

    public Win05()
    {
        // The seven native initializers and implicit MonoBehaviour base call finish
        // before these adapter observations. Preserve their values and relative order.
        // Extra observer effects and DTO type-initialization timing remain qualified;
        // this does not make the whole constructor equivalent to original 0600105d.
        magicWardrobeState = WorldWardrobeState.Empty;
        petStatus = new WorldPlayerPetStatusState(0, string.Empty, 0, 0, 0, 0, 0, false);
        skillBook = WorldSkillBookState.Empty;
    }

}
}
