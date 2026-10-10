using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    // Official 1.5.8 CharData..cctor (06000f2d). Keep these field initializers together:
    // their declaration order carries the native allocation/publication order across partials.
    // No explicit static constructor: the original type has BeforeFieldInit semantics.
    // Unlisted fields keep their zero/null defaults; no original cctor store targets them.

    internal static string name = string.Empty;

    internal static long charId = 0;

    internal static int race = 0;

    // Original 1.5.8 06000f2d four-choice vector is (race=0, gender=1, head=1, hair=1).
    internal static int gender = 1;

    // Original 1.5.8 06000f2d static initial choices; never reset by the creation draw.
    internal static int head = 1;

    internal static int hair = 1;

    internal static int level = 1;

    // Identity and initial value come from the frozen 1.5.8 field atlas (CharData.lastLogin,
    // System.String, static offset 40 = +0x28) and from CharData..cctor (0x06000f2d), which
    // sets +0x28 to the empty string literal metadata_usage_10335_kind5 (decode state exact,
    // value "") at arm64 line 56 / x86_64 line 65 of the frozen dual-architecture export
    // (wbb-060-chardata-cctor-{arm64,x86_64}/06000f2d.c). It is the character-selection
    // mirror target of TitleFlow.MirrorSlotToCharData (0x06000efb); no other official writer
    // reaches it before the enter-world request.
    internal static string lastLogin = string.Empty;

    /// <summary>
    /// Official 1.5.8 <c>CharData.title</c>: field token 67114201, System.String, static,
    /// atlas offset 48 - the <c>+0x30</c> static slot Dispatch case 0x804c stores into.
    /// </summary>
    internal static string title = string.Empty;

    /// <summary>
    /// Official 1.5.8 <c>CharData.ownedTitles</c>: field token 67114202,
    /// System.Collections.Generic.List&lt;System.String&gt;, static, atlas offset 56 - the
    /// <c>+0x38</c> static slot the same case empties and appends to.
    /// </summary>
    internal static List<string> ownedTitles = new List<string>();

    // Official 1.5.8 CharData..cctor (0x06000f2d) gives both skill dictionaries their initial
    // value: +0x48 skills and +0x50 skillDmgBonus each get a freshly allocated
    // Dictionary<int,int> (arm64 stores 0x395dc0 / 0x395de8 and x86_64 stores
    // 0x3a90e7 / 0x3a9118 in the exact frozen 06000f2d body). Every static access is preceded by the class-init gate
    // ([class+0xe4] -> il2cpp class-init helper, arm64 0x177938), so the initialiser has run
    // before any reader can reach the field: in the official client skills is never null, and a
    // "no 0x8015 received yet" reader takes the empty-dictionary path (TryGetValue false), never
    // a null-check path.
    internal static Dictionary<int, int> skills = new Dictionary<int, int>();

    internal static Dictionary<int, int> skillDmgBonus = new Dictionary<int, int>();

    // Official 040014e5/040014e6, static +0x78/+0x7c. The bounded 06000f2d
    // initializer store supplies 100 for both. These fields currently serve only
    // the owner-only GameHud.UpdateStaminaRing body; no receipt is invented here.
    internal static int stamina = 100;

    internal static int maxStamina = 100;

    internal static int nianli = 100;

    internal static int maxNianli = 100;

    internal static int naili = 100;

    internal static int maxNaili = 100;

    /// <summary>
    /// Official 0x8017 aggregate, field token 67114219, System.Boolean, <c>CharData</c> statics
    /// +0x90 (arm64 <c>strb w20,[x8,#0x90]</c> at 0x258e1c / x86_64 <c>movb %bl,0x90(%rax)</c>
    /// at 0x265411). It is the last write of the case and the only one that reads the wire after
    /// the rows: one byte is read when <c>Buf.Remaining &gt;= 1</c> and a packet that ends after
    /// the last row stores false instead of keeping the previous value.
    /// <c>CharData..cctor</c> clears it at arm64 <c>strb wzr,[x8,#0x90]</c> (0x395e0c).
    /// </summary>
    internal static bool doubleExp = false;

    /// <summary>
    /// Official 0x8017 list owner, field token 67114233, System.Collections.Generic.List&lt;CharData.BuffView&gt;,
    /// <c>CharData</c> statics +0xc0 (arm64 <c>str x20,[x0,#0xc0]!</c> at 0x258880 / x86_64
    /// <c>movq %rsi,0xc0(%rdi)</c> at 0x2653ad, both followed by the write barrier).
    /// <c>CharData..cctor</c> (0x06000f2d) allocates an empty list for it
    /// (arm64 <c>str x19,[x0,#0xc0]!</c> at 0x395e2c, the object created by the <c>object_new</c>
    /// immediately before), so the process-wide initial value is an empty list and not null.
    /// Every 0x8017 reply then replaces it wholesale with the list that read built.
    /// </summary>
    internal static List<BuffView> buffs = new List<BuffView>();

    /// <summary>
    /// Official 0x8017 aggregate, field token 67114234, System.Boolean, <c>CharData</c> statics
    /// +0xd8 (arm64 <c>strb w8,[x9,#0xd8]</c> at 0x258898 / x86_64
    /// <c>movb %r12b,0xd8(%rax)</c> at 0x2653ca). The case ORs every row's invisible byte as it
    /// reads them - including the rows the custom-map test later drops from the list - and writes
    /// the result once, after the whole list has been published.
    /// <c>CharData..cctor</c> (0x06000f2d) clears it with one 16-bit store at +0xd8
    /// (arm64 <c>strh wzr,[x8,#0xd8]</c> at 0x395e40), so the initial value is false.
    /// </summary>
    internal static bool invisible = false;

    /// <summary>
    /// Official 0x8017 aggregate, field token 67114235, System.Byte, <c>CharData</c> statics
    /// +0xd9 (arm64 <c>strb w24,[x9,#0xd9]</c> at 0x25889c / x86_64 <c>movb %bl,0xd9(%rax)</c>
    /// at 0x2653d1). The case keeps the unsigned maximum of every row's raw immobile byte, again
    /// before the custom-map test drops rows, so the raw byte survives instead of being folded
    /// into a flag. Cleared by the same cctor store as <see cref="invisible"/>.
    /// </summary>
    internal static byte immobile = 0;

    internal static int pkProtectLv = 20;

    internal static int glowMode = 1;

    internal static string avEffect = string.Empty;

    // Official 1.5.8 CharData.titleEffect, field token 67114263, static slot +0x148.
    // CharData..cctor (0x06000f2d) stores the empty string on both architectures.
    // This restores the owner initializer only; local title-effect rendering is not connected.
    internal static string titleEffect = string.Empty;

    internal static string petName = string.Empty;

    internal static List<int> petBag = new List<int>();

    internal static List<string> petBagNames = new List<string>();

    // Official static field +0x180. The list index, not PetBrief.slot, addresses a share.
    public static List<PetBrief> petSlots = new List<PetBrief>();

    internal static string petPrename = string.Empty;

    internal static int petStarve = 100;

    internal static int petTrust = 100;

    internal static bool petShow = true;

    internal static bool petShowTianmo = true;

    internal static bool showMountRing = true;

    private static readonly Dictionary<int, bool> equipShow = new Dictionary<int, bool>();

    internal static Dictionary<string, int> soulFxPart = new Dictionary<string, int>();

    private static readonly int[] FxParts = { 2, 6, 8, 9, 1, 3, 10, 7 };

    internal static bool riding = false;

    internal static int nameMode = 0;

    // Official static field +0x238 (568), public static readonly System.String[].
    // CharData..cctor 0x06000f2d allocates a 6-element array and stores metadata usages 17625,
    // 18241, 18242, 18238, 18240 and 18239 into elements 0..5 in that order.
    public static readonly string[] NAMEMODE_TIPS =
    {
        "显示其他玩家",
        "隐藏其他玩家名字",
        "隐藏其他玩家名字+血条",
        "隐藏其他玩家",
        "隐藏其他玩家(保留队友)",
        "隐藏其他玩家(保留摊位)"
    };

    /// <summary>
    /// Official 1.5.8 <c>CharData.showTitle</c>: field token 67114318, System.Boolean, static,
    /// atlas offset 584 - the <c>+0x248</c> byte the head name tag gates the title on.
    /// </summary>
    /// <remarks>
    /// The initial value is true because <c>CharData..cctor</c> stores it, not because of the
    /// C# default: 0x06000f2d (arm64 0x395b74 / x86_64 0x3a8ea0) writes NAMEMODE_TIPS into
    /// +0x238 at arm64 0x396158 and then <c>mov w9,#0x1</c> / <c>strb w9,[x8,#0x248]</c> at
    /// arm64 0x39616c / 0x396174.
    /// PlayerNameTag.LateUpdate (0x06000a33, arm64 0x2c9580) compares this static with its own
    /// cached copy (arm64 0x2c9980 <c>ldrb w8,[x8,#0x248]</c> / 0x2c9984
    /// <c>ldrb w9,[x19,#0x58]</c> / 0x2c9988 <c>cmp w8,w9</c>) and rebuilds on a difference;
    /// PlayerNameTag.Rebuild (0x06000a37, arm64 0x2c9d5c) copies the static into that cache
    /// (arm64 0x2c9ea8 / 0x2c9eb0) and bakes the title string only while it is true. Without
    /// this field the head title is never drawn at all: this byte decides the title text's
    /// visibility independently of the title-effect string at +0x148.
    /// </remarks>
    internal static bool showTitle = true;

    internal static int mountModel = 0;

    internal static int rideDir = 4;

    internal static bool rideFlip = false;

    internal static float mountSaddleX = 0f;

    internal static float mountSaddleY = 0f;

    internal static int riderSeat = 0;

    internal static bool mountTianmo = false;

    internal static int rideAnchorId = 0;

    internal static int rideSeats = 0;

    internal static int mountBoundTopPx = 0;

    internal static float mountNameOffXU = 0f;

    public static List<PetSkill> petSkills = new List<PetSkill>();

    public static Dictionary<int, int> petSkillIcons = new Dictionary<int, int>();

    internal static int petFightCap = 4;

    internal static int petLifeCap = 8;

    // Existing product-only preference identity storage. These empty strings have no
    // allocation, callback or dependency on the official initializer sequence above.

    private static string preferenceSuffix = string.Empty;

    private static string legacyPreferenceSuffix = string.Empty;
}

}
