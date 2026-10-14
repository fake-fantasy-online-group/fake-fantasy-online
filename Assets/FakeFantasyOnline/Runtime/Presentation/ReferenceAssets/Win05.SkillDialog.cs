using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the official Win05 skilldlg branch.
/// </summary>
public partial class Win05
{
    public const string SkillDialogName = "skilldlg";
    public const string SkillDialogBackgroundPath =
        "res/uires/skilldlg/skilldlgbk.gsa";
    public const string SkillButtonPath = "res/uires/skilldlg/skillbtn.gsa";
    public const string PlusButtonPath = "res/uires/common/plusbtn.gsa";
    public const string SkillFirePath = "res/uires/common/skillfire.gsa";
    public const float SkillDialogWidth = 283f;
    public const float SkillDialogHeight = 397f;
    public const int SkillDialogDepth = 10;

    // Native and adapter initializers, including the job/life tables, are in Win05.Initialization.cs.
    private static int skillTab;
    private static float skillScroll;
    public static bool lifeLearnMode;
    public static Win05 I;

    // Native and adapter field initializers are ordered in Win05.Initialization.cs.
    private ReferenceResourceLoader skillResources;
    private ReferenceWorldBitmapFont skillFont;
    private WorldEntryRecord skillCharacter;
    private Texture2D[] closeFr;
    private Texture2D[] helpFr;
    private string cur;
    // Official 06001035: live singleton and a non-null current window name.
    public static bool AnyOpen => I != null && I.cur != null;
    private float winY;
    private bool dragging;
    private Vector2 dragOff;
    private int playerLevel;
    private int heldSkillId => GameHud.DragSkillIdForIndependentRuntime;
    private int heldSkillIconId;
    private Texture2D heldSkillIcon => GameHud.DragSkillIconForIndependentRuntime;
    private WorldHotbarSlotKind heldSkillKind => (WorldHotbarSlotKind)GameHud.DragKindForIndependentRuntime;
    private bool skillDialogReady;

    public bool SkillDialogReady => skillDialogReady;
    public bool IsOpen => cur != null;
    public bool SkillDialogVisible => string.Equals(cur, SkillDialogName, StringComparison.Ordinal);
    public int SkillTab => skillTab;
    public int HeldSkillId => heldSkillId;
    public int HeldSkillIconId => heldSkillIconId;
    internal Texture2D HeldSkillTextureForIndependentRuntime => heldSkillIcon;
    public WorldHotbarSlotKind HeldSkillKind => heldSkillKind;
    public float SkillWindowX => winX;
    public float SkillWindowY => winY;
    private WorldSkillBookObservation skillBookObservation;
    public WorldSkillBookState SkillBook =>
        skillBook ?? (skillBook = skillBookObservation.ToSnapshot());
    public WorldLifeSkillBookState LifeSkillBook
    {
        get
        {
            var rows = new List<WorldLifeSkillState>(lifeData.Count);
            foreach (var row in lifeData)
                rows.Add(new WorldLifeSkillState(row.Key, row.Value.Item1, row.Value.Item2, false));
            return new WorldLifeSkillBookState(rows, lifeCanLearn);
        }
    }

    private void Awake()
    {
        I = this;
    }

    public bool TryInitializeSkillDialog(
        ReferenceResourceLoader resources,
        ReferenceFirstWorldUiTip uiTip,
        WorldEntryRecord character)
    {
        return TryInitializeSkillDialog(resources, uiTip, character, null);
    }

    public bool TryInitializeSkillDialog(
        ReferenceResourceLoader resources,
        ReferenceFirstWorldUiTip uiTip,
        WorldEntryRecord character,
        ReferenceSystemMessageBox messageBox)
    {
        if (resources == null || uiTip == null || !uiTip.Ready || character == null)
        {
            return false;
        }

        ReferenceWorldBitmapFont font;
        if (!ReferenceWorldBitmapFont.TryCreate(resources, out font) ||
            font == null)
        {
            return false;
        }

        // Official Win05 has no eager background load: OnGUI 06001042 asks Bk(def) on every draw
        // and falls back to PatchPanel + the Def title when that resolves null (arm64 :403-479,
        // x86_64 :415-480), so since WBB-052/053 the four window backgrounds are not assembly
        // conditions and a missing skilldlgbk can no longer reach
        // ReferenceFirstWorldWindowOwner.cs:1132 and release the whole world.
        // WBB-056 removed the three domain-data tests that used to stand here (skLayout != null,
        // skLayout.switches != null, skLayout.pointlabel != null && Length >= 4) and the
        // LoadSkillData 06001020 call in front of them. None of them has a native counterpart:
        // there is no official TryInitializeSkillDialog, and the loader has exactly two official
        // callers - SkillInfo 06001021 and DrawSkill 06001025 (index `calls`, one edge per
        // architecture) - so the real read point is the DrawSkill body, which is where this build
        // now calls it. Its official body (arm64 rva 0x3ad4f8 720 bytes / x86_64 rva 0x3c00b0)
        // also stores `skLoaded` (Win05 static +0x10) *before* the two GameTables.LoadText
        // 0600046f reads and never clears it, so a missing "skilldlg_layout" leaves skLayout null
        // once and for all: no retry, no throw, no failure. What the drawing pass needs is
        // smaller still - DrawSkill 06001025 only asks for `pointlabel != null &&
        // pointlabel.Length > 1` (arm64 :304-312, x86_64 :346-352; a length below two falls into
        // the non-returning bounds helper arm64 :319 / x86_64 :359), and a null `switches` is a
        // normal empty page because FindSwitch 06001024 returns null for it (x86_64 :39-41) and
        // the body answers with "(本页无技能)". A missing or short table is therefore a drawn state at the
        // DrawSkill point, never a reason to report this window failed.
        skillResources = resources;
        skillCharacter = character;
        skillFont = font;
        playerLevel = character.Level;
        // Rebinding a map adapter never resets the native owner, placement or input state.
        I = this;
        skillDialogReady = true;
        ReferenceSystemMessageBox wardrobeMessageBox = messageBox;
        bool ownsWardrobeMessageBox = false;
        if (wardrobeMessageBox == null)
        {
            if (!ReferenceSystemMessageBox.TryCreate(
                    resources,
                    font,
                    out wardrobeMessageBox))
            {
                skillDialogReady = false;
                return false;
            }
            ownsWardrobeMessageBox = true;
        }

        if (!TryInitializeMagicWardrobeDialog(
                wardrobeMessageBox,
                ownsWardrobeMessageBox) ||
            !TryInitializePetDialog(character) ||
            !TryInitializeChangeEquipmentDialog())
        {
            skillDialogReady = false;
            return false;
        }

        return true;
    }

    public bool ApplySkillBook(WorldSkillBookState state)
    {
        if (!skillDialogReady || state == null)
        {
            return false;
        }

        skillBookObservation = new WorldSkillBookObservation(state);
        skillBook = state;
        return true;
    }

    internal bool ApplySkillBookObservation(WorldSkillBookObservation observation)
    {
        if (!skillDialogReady || !observation.HasValue) return false;
        skillBookObservation = observation;
        skillBook = observation.MaterializedState;
        return true;
    }

    public bool ApplyLifeSkillBook(WorldLifeSkillBookState state)
    {
        if (state == null)
        {
            return false;
        }

        // The original static SetLifeSkills call belongs to its actual 8022 receipt.
        // This window observes those roots; opening it must not replay an older snapshot.
        return true;
    }

    // Original SetLifeSkills 0600101d: replace each root in order, creating only
    // the collection whose argument is null. No instance/readiness or clear gate.
    public static void SetLifeSkills(Dictionary<int, (int, string)> d, HashSet<int> canLearn)
    {
        lifeData = d ?? new Dictionary<int, (int, string)>();
        lifeCanLearn = canLearn ?? new HashSet<int>();
    }

    // WBB-058: the skill page no longer keeps a task-log copy of its own. Official Win05.DrawSkill
    // 06001025 asks TaskClient.IsDone 06000e61, i.e. the TaskClient `Done` set the 0x8027 receive
    // path already fills (ReferenceWorldReceiveOwner.cs:141 TaskClient.SetState), not a snapshot
    // owned by this window, so nothing in Win05 reads WorldTaskLogState any more. The entry point
    // stays because ReferenceFirstWorldWindowOwner.cs:6261 and the existing skill-dialog audit
    // (Editor/ReferenceFirstWorldSkillDialogAudit.cs:64) still feed it, and it keeps the same
    // contract as the other Apply* entry points: false for !skillDialogReady or a null snapshot,
    // true otherwise.
    public bool ApplyTaskLog(WorldTaskLogState state)
    {
        if (!skillDialogReady || state == null)
        {
            return false;
        }

        return true;
    }

    public void ApplyPlayerLevel(int level)
    {
        playerLevel = Math.Max(0, level);
    }

    // Official 0x06001032, static System.Void Win05.Toggle(System.String).
    // arm64 rva 0x3b2958 (496 bytes) and x86_64 rva 0x3c5a30 (432 bytes) decode to one body:
    //   * load the static instance Win05.I (Win05 class static field +0x50);
    //   * UnityEngine.Object.op_Equality(I, null) - `I == null` - is the only guard, and on that
    //     branch the body builds the singleton itself: new GameObject("Win05") (StringLiteral
    //     usage 14048) then GameObject.AddComponent<Win05>() (usage 07696; CoreModule
    //     UnityEngine.GameObject.AddComponent, arm64 0x5fbb64), stored back into Win05.I.
    //     Official Win05.Awake 06001038 is the single store "Win05.I = this", so mounting the
    //     component already publishes the instance and the store is the same object;
    //   * System.String.op_Equality(I.cur, name) picks the new value of I.cur (+0x28): the same
    //     name closes the window (null), any other name switches the window to it;
    //   * I.placed (+0x68, this build names the field I.positioned) is written false, which is
    //     exactly the flag Win05.OnGUI 06001042 reads to recentre (06001042.c:182-192);
    //   * nothing else is read or written: no resource load, no Def lookup, no skillDialogReady
    //     test, no logging and no winX/drag reset - official Win05.Open 06001033 has the same
    //     two-instance-write shape with the same "build the singleton when I is null" step.
    //     A still-null instance falls into the non-returning null helper (arm64 0x3b2b44 /
    //     x86_64 0x3c5bd2), i.e. the official throw, never a silent return.
    // WBB-050 removed the product-only `I.winX = -1f`, `I.dragging = false` and
    // `I.ClearMagicWardrobeDrag()` that used to follow the official two writes: recentring is
    // back on the official `!positioned` test (the four per-window Ensure*WindowPosition helpers
    // now read it instead of the `winX >= 0f` sentinel), and the drag fields (+0x5c dragging,
    // +0x60 dragOff) are written only by Win05.OnGUI 06001042 (06001042.c:346/353/367/401).
    public static void Toggle(string name)
    {
        if (I == null)
        {
            I = new GameObject("Win05").AddComponent<Win05>();
        }

        I.cur = string.Equals(I.cur, name, StringComparison.Ordinal) ? null : name;
        I.positioned = false;
        I.mobileSkills.Reset();
    }

    // Official 0x06001033, static System.Void Win05.Open(System.String).
    // arm64 rva 0x3ad160 (644 bytes) and x86_64 rva 0x3bfdc0 (560 bytes) decode to one body:
    //   * `I == null` builds the singleton itself - new GameObject("Win05") (StringLiteral usage
    //     14048) then GameObject.AddComponent<Win05>() (usage 07696) - exactly as Toggle does;
    //   * System.String.op_Inequality(I.cur, name): only a differing name writes I.cur (+0x28)
    //     and I.placed (+0x68) = false; the same-name path writes neither, so re-Open of the
    //     window that is already current keeps its position;
    //   * still inside that branch, System.String.op_Equality(name, "historybar") (StringLiteral
    //     usage 15027) resets the static _histSig (+0xc4, 0xffffffff) and _histMaxY (+0xcc, -1f);
    //   * the static lifeLearnMode (+0x48) is cleared to false on every path, before each return;
    //   * no resource/readiness guard and no I.winX write exist. A still-null instance reaches
    //     the non-returning null helper (arm64 0x3ad3e0 / x86_64 0x3bffdd), i.e. the official
    //     throw, never a silent return.
    public static void Open(string name)
    {
        if (I == null)
        {
            I = new GameObject("Win05").AddComponent<Win05>();
        }

        if (!string.Equals(I.cur, name, StringComparison.Ordinal))
        {
            I.cur = name;
            I.positioned = false;
            I.mobileSkills.Reset();
            if (string.Equals(name, "historybar", StringComparison.Ordinal))
            {
                _histSig = -1;
                _histMaxY = -1f;
            }
        }

        lifeLearnMode = false;
    }

    // Official 0x06001036, static System.Void Win05.CloseNow(): arm64 rva 0x3b2c70 (276 bytes)
    // and x86_64 rva 0x3c5cb0 (208 bytes) are one short body - type-initialise Win05 (usage
    // 02686) and CoreModule (02095), read the static Win05.I (+0x50), Object.op_Inequality(I,0)
    // and, only inside that arm, `*(I + 0x28) = 0; write-barrier; return;` (arm64 :53-56,
    // x86_64 :36-40). That single store is the whole method: official never writes placed(+0x68),
    // dragging(+0x5c), dragOff(+0x60) or the wardrobe drag statics here, and it touches no other
    // component. When the instance is still null the second raw null test falls into the
    // non-returning helper (arm64 FUN_00177a74 at :59, x86_64 FUN_0016c1b0 at :43), i.e. the
    // official throw, never a silent return.
    // WBB-051 removed the three product-only writes that used to follow the official store:
    //   * UiInput.Close() - Win05 is not the menu's owner. The official UiInput static owner is
    //     closed in two places, neither of them here: UiInput.DrawMenu (06000fd2) closes itself on
    //     a row click, on a MouseDown outside the menu rect and on an Escape KeyDown
    //     (UiInput.cs:78-105), and UiEsc.CloseTop (06000fca) has a global input-menu slot that
    //     reads UiInput.get_MenuOpen (06000fcb) and answers it with UiInput.Close (06000fcc)
    //     before returning true - the direct reader is
    //     ReferenceFirstWorldWindowOwner.CloseChosenEscapeWindow's InputMenu arm. Its phase is
    //     earlier than the Win05 close slot and only that: inside the same escape chain
    //     ReferenceFirstWorldEscapeDispatch.Choose returns InputMenu before it ever tests
    //     skillDialogVisible, so one Escape closes the menu and nothing else, and the Win05 slot is
    //     reached only on a later Escape, when the menu already reports closed. That is a relative
    //     order inside this chain, not a claim about the whole host pass - the host still has its
    //     own front guards ahead of Choose. The menu is also drawn only by
    //     the control whose name equals the stored owner (UiInput.Draw 06000fd1 ends with
    //     `if (owner == name) DrawMenu(...)`), so an owner left behind by a closed window is
    //     invisible and only reappears when that control is drawn again. UiInput stays an
    //     independent owner; no replacement cleanup and no cross-component de-duplication.
    //   * I.dragging = false - official OnGUI 06001042 returns at 06001042.c:103-105 before the
    //     drag arms whenever cur is null, so official never clears the flag on close; MouseUp
    //     (type == 1, arm64 :399-402 / x86_64 :411-413) is its only clearer.
    //   * I.ClearMagicWardrobeDrag() - the official wardrobe drag fields are static and are
    //     cleared only inside DrawMagicStore 0600104f's MouseUp arm.
    // Later packets narrowed the product-only clears this member left untouched: WBD-064 removed
    // the ApplyMagicWardrobeState one and WBD-065 removed the world-release one in
    // DisposeMagicWardrobeDialog - both erased state official keeps, and WBD-065 also moved the
    // five drag fields onto official static storage. WBD-067 then read the two remaining callsites
    // themselves on the frozen slices: the wardrobe close button (Win05.OnGUI 0x06001042's close
    // arm stores only cur) and the category switch (DrawMagicStore 0x0600104f's category arm
    // stores only magicCat +0x70 and magicScroll +0x74) have no drag-field store either, so both
    // product-only clears were removed and every remaining ClearMagicWardrobeDrag() call now sits
    // in an official MouseUp block of DrawMagicStore - the two inside DrawMagicWardrobeGrid.
    // CloseNow itself was last changed by WBB-051; this comment only tracks the clear call sites.
    public static void CloseNow()
    {
        if (I != null)
        {
            I.cur = null;
            I.mobileSkills.Reset();
        }
    }

    public static void OpenLifeLearn()
    {
        Open(SkillDialogName);
        lifeLearnMode = true;
        skillTab = 2;
        skillScroll = 0f;
        RequestLifeRefresh();
    }

    private static void RequestLifeRefresh()
    {
        if (NetClient.I != null) NetClient.I.LearnLifeForIndependentRuntime(0);
    }

    public void ClearHeldSkill()
    {
        GameHud.ClearDragForIndependentRuntime();
        heldSkillIconId = 0;
    }

    public bool PickupCharacterSkill(int skillId, Texture2D heldIcon)
    {
        SkInfo info = SkillInfo(skillId);
        if (!skillDialogReady || !(UsesGrowthSkill(skillId) ? EndgamePanel.GrowthState.SkillRank(skillId)?.CanCast == true : SkillBook.LevelOf(skillId) > 0) ||
            !SkillUsableForHotbar(info))
        {
            return false;
        }

        GameHud.PickupSkill(skillId, heldIcon, false);
        heldSkillIconId = info.icon;
        return true;
    }

    // Original 06001045 / 06001046; life capacity is the first tuple member.
    private static (bool, bool) PetSkillExt() => (CharData.petLifeCap > 8, CharData.petFightCap > 4);

    private static float PetSkillExtraH()
    {
        var ext = PetSkillExt();
        float extra = ext.Item1 ? 40f : 0f;
        if (ext.Item2) extra += 40f;
        return extra;
    }

    // Compatibility entry for existing source fixtures. Production uses Unity OnGUI only.
    public bool DrawSkillDialog(float scale)
    {
        OnGUI();
        return true;
    }

    // Original 06001025 keeps the current OnGUI layout delegate, Def and event as arguments.
    private void DrawSkill(Func<float, float, float, float, Rect> L, Def d, Event ev)
    {
        LoadSkillData();
        if (skLayout == null)
        {
            DrawSkillLayoutMissing(L, d);
        }
        else
        {
            int career = CareerIndex();
            DrawTabs(L, ev);
            if (skillTab == 2) DrawLifeSkills(L, d);
            else DrawCombatSkills(L, d, ev, career);
        }
    }

    // Official 0x0600102c is static; every helper it uses is static too, so no instance state is
    // involved. GameHud.HotSlotTip calls it from its own static body.
    public static string SkillTipText(int skillId, int currentLevel)
    {
        SkInfo info = SkillInfo(skillId);
        if (info == null) return null;
        if (UsesGrowthSkill(info.id)) return BuildGrowthSkillTip(info, false);
        var rows = new List<(string, char, bool)>();
        AppendLevelBlock(rows, info, Mathf.Max(1, currentLevel),
            currentLevel > 0 ? string.Format("等级: {0}", currentLevel) : "等级: 未学习", info.name);
        AppendDmgBonusRow(rows, info);
        if (SkillUsableForHotbar(info)) rows.Add((MobileClientPresentation.Enabled ? "轻点查看；点放入快捷栏选择位置" : "双击或拖曳至快捷栏以使用这个技能", (char)6, false));
        return ComposeTip(rows);
    }

    public bool CanShowUpgrade(int skillId)
    {
        SkInfo info = SkillInfo(skillId);
        if (UsesGrowthSkill(skillId)) return EndgamePanel.GrowthState.CanInvestSkill(skillId);
        if (info == null || CharData.skillPoints <= 0) return false;
        CharData.skills.TryGetValue(skillId, out int currentLevel);
        if (currentLevel >= info.maxlv) return false;
        bool prerequisite = info.pre == 0 ||
            (CharData.skills.TryGetValue(info.pre, out int previous) && previous > 0);
        SkLevel next = TipLevel(info, currentLevel + 1);
        bool levelAllowed = next == null || next.lreq <= 0 || next.lreq == 999 || CharData.level >= next.lreq;
        return levelAllowed && (currentLevel > 0 || prerequisite);
    }

    public void DisposeSkillDialog()
    {
        // Compatibility adapter release, not an original Win05 lifecycle callback.
        // Native cur, placement, drag, resource caches and shared held items remain owned
        // by their original owners. Scene-root destruction is the final lifetime boundary.
        skillDialogReady = false;
        skillResources = null;
        skillCharacter = null;
        skillFont = null;
        if (ownsMagicMessageBox) magicMessageBox?.Dispose();
        magicMessageBox = null;
        ownsMagicMessageBox = false;
    }

    private void DrawTabs(Func<float, float, float, float, Rect> L, Event current)
    {
        for (int index = 0; index < SkillTabName.Length; index++)
        {
            Rect rect = L(14f + 56f * index, 40f, 56f, 23f);
            if (BitmapFont.Ready)
            {
                BitmapFont.CenterOutline(
                    rect,
                    SkillTabName[index],
                    index == skillTab ? new Color32(255, 230, 120, 255) : new Color32(190, 175, 140, 255),
                    s);
            }
            if (current != null && current.type == EventType.MouseDown &&
                rect.Contains(current.mousePosition))
            {
                skillTab = index;
                skillScroll = 0f;
                if (MobileClientPresentation.Enabled) mobileSkills.Reset();
                current.Use();
            }
        }
    }

    private void DrawCombatSkills(
        Func<float, float, float, float, Rect> L, Def def, Event current, int career)
    {
        // Official Win05.DrawSkill 06001025 draws the skill-point feedback *before* it resolves the
        // page: the skillfire frame leaves `CharData.skillPoints > 0` (arm64 :255-297,
        // x86_64 :297-336) and the "技能点" label follows straight after it (arm64 :299-333,
        // x86_64 :341-373), both ahead of the FindSwitch 06001024 call (arm64 :346, x86_64 :385)
        // whose null arm is what draws "(本页无技能)" (arm64 :349-362, x86_64 :386-394). A tab whose
        // page does not resolve therefore still shows the fire animation and the remaining skill
        // points under that line.
        DrawSkillPointFeedback(L);

        // Official Win05.DrawSkill 06001025 resolves the page exactly once, with key =
        // tabKeys[skillTab] and sw = career + 1 (arm64 0x3ae82c-0x3ae854 with the FindSwitch
        // 06001024 call at 0x3ae854; x86_64 key/sw setup before the call at 0x3c1409), inside the
        // body whose first test is `tabKeys != null` (arm64 0x3ae830 → the non-returning helper).
        // Only that single lookup exists: the atlas `calls` table lists exactly one 06001024 edge
        // out of this body. The product's `FindSkillSwitch(SkillTabKey[skillTab], 0)` fallback
        // therefore has no native counterpart and is gone - it used to turn "the career task is
        // not done" into "show another page".
        SkSwitch page = FindSkillSwitch(SkillTabKey[skillTab], career + 1);

        if (page == null || page.slots == null)
        {
            // Official Win05.DrawSkill 06001025, arm64 0x3ae930-0x3ae994 / x86_64
            // 0x3c15bb-0x3c1628: the null-page arm is `if (!BitmapFont.Ready) return;` and then
            // one centred line at `L(0, def.h * 0.5f, def.w, 12f)` - the same geometry this build
            // already draws - with the frozen fill (150, 150, 150, 255). Both architectures load
            // a grey of three equal channels: arm64 0x3ae97c-0x3ae990 builds 0x3f169697
            // (= 150/255) into s4 and then copies it with `mov v5.16b, v4.16b` /
            // `mov v6.16b, v4.16b` while `fmov s7, #1.0` sets the fourth; x86_64 loads
            // DAT_01118560 = {150/255, 150/255, 0, 0} into xmm2 and DAT_01117d90 =
            // {150/255, 1.0} into xmm3. The register order is the one the
            // sibling "技能布局缺失" arm pins down: there arm64 0x3ae508-0x3ae524 / x86_64
            // DAT_0111ce30 + DAT_0111cbc0 carry (200, 80, 80, 255), which is what
            // DrawSkillLayoutMissing already draws. This build used Color.white, i.e. 105/255 more
            // on every channel. WBB-056's residue note called this fill (255, 230, 150); the two
            // frozen slices above do not contain a 255 or a 230 channel for this call, and the
            // corrected value is recorded in the 06001025 note.
            if (!BitmapFont.Ready) return;
            BitmapFont.CenterOutline(
                L(0f, def.h * 0.5f, def.w, 12f),
                "(本页无技能)",
                new Color32(150, 150, 150, 255),
                s);
            return;
        }

        // Official job-task gate, Win05.DrawSkill 06001025 arm64 0x3ae868-0x3aecfc / x86_64
        // 0x3c1440-0x3c1971: the page has already resolved with slots, and then
        // TaskClient.IsDone 06000e61(JobTaskIds[career]) decides what the body draws. False means
        // the whole skill tree is replaced by the two-line hint and the body leaves through the
        // shared epilogue without drawing anything else for this page (arm64 0x3aed00 / x86_64
        // 0x3c2ac0); in this file that single draw path is DrawCombatSkills returning right after
        // the hint. - no tree, no slot icons, no upgrade plus buttons and no tooltip request. The career index is recomputed from CharData.race
        // immediately before the call (arm64 0x3ae880-0x3ae890, x86_64 0x3c144a-0x3c145c: clamp to
        // 0..4) and again inside the false arm for the two list indexes (arm64 0x3aeb7c-0x3aeb8c,
        // x86_64 0x3c17f2-0x3c180b), never from this window's own state.
        int jobTaskId = CareerIndex() switch
        {
            0 => 403, 1 => 404, 2 => 402, 3 => 400, 4 => 401,
            _ => throw new IndexOutOfRangeException()
        };
        if (!TaskClient.IsDone(jobTaskId))
        {
            DrawJobTaskHint(L, def);
            return;
        }

        float visibleHeight = page.containH > 0 ? page.containH : 299f;
        float visibleWidth = page.containW > 0 ? page.containW : 230f;
        // DrawSkill reads the live loader on each pass; it owns no extra tree cache.
        Texture2D tree = ResLoader.I != null && !string.IsNullOrEmpty(page.tree)
            ? ResLoader.I.LoadGsa(page.tree) : null;
        float treeHeight;
        if (tree)
        {
            treeHeight = GsaTextureScale.Height(tree);
        }
        else
        {
            // The fallback field is observed only after the texture load/test fails.
            treeHeight = page.treeH > 0 ? page.treeH : visibleHeight;
        }

        float maximumScroll = Mathf.Max(0f, treeHeight - visibleHeight);
        // Unlike the captured L used by the window frame and tabs, the original tree
        // viewport follows the live owner position after this pass's header drag.
        Rect viewport = new Rect(winX + 18f * s, winY + 65f * s,
            visibleWidth * s, visibleHeight * s);
        bool mobile = MobileClientPresentation.Enabled;
        if (mobile && mobileSkills.Skill > 0)
        {
            DrawMobileSkillActions(L, def);
            return;
        }
        bool mobileTap = mobile && MobileSkillTreeInput(viewport, current, maximumScroll);
        if (viewport.Contains(current.mousePosition) && current.type == EventType.ScrollWheel)
        {
            // An oversized hovered growth card owns its wheel input. Short cards
            // keep the original tree scrolling behavior and exact native layout.
            bool tipScrolled = false;
            foreach (SkSlot slot in page.slots)
            {
                Rect icon = new Rect(viewport.x + slot.x * s,
                    viewport.y + (slot.y - skillScroll) * s, 36f * s, 36f * s);
                if (icon.Contains(current.mousePosition) && UsesGrowthSkill(slot.id) &&
                    skInfo.TryGetValue(slot.id, out SkInfo scrollInfo))
                {
                    tipScrolled = UITip.ScrollSkill(BuildGrowthSkillTip(scrollInfo, true), s, current.delta.y);
                    if (tipScrolled) break;
                }
            }
            if (!tipScrolled)
                skillScroll = Mathf.Clamp(skillScroll + current.delta.y * 12f, 0f, maximumScroll);
            current.Use();
        }
        skillScroll = Mathf.Clamp(skillScroll, 0f, maximumScroll);

        SkInfo hoveredInfo = null;
        int hoveredLevel = 0;
        int requestedUpgrade = 0;
        // The native owner rereads its position/scale after wheel processing.
        GUI.BeginGroup(new Rect(winX + 18f * s, winY + 65f * s,
            visibleWidth * s, visibleHeight * s));
        if (tree)
        {
            GUI.DrawTexture(
                new Rect(
                    0f,
                    -skillScroll * s,
                    GsaTextureScale.Width(tree) * s,
                    GsaTextureScale.Height(tree) * s),
                tree);
        }

        Vector2 localPointer = current == null
            ? new Vector2(float.NegativeInfinity, float.NegativeInfinity)
            // Event.mousePosition is already group-local after BeginGroup.
            : current.mousePosition;
        Texture2D plus = UiFrame(PlusButtonPath, 0);
        SkSlot[] slots = page.slots;
        for (int index = 0; index < slots.Length; index++)
        {
            SkSlot slot = slots[index];
            // Official 06001025, arm64 :621-623 / x86_64 :745-748: the row is read unguarded and
            // a null row goes to the non-returning helper (LAB_003afb48 / LAB_003c2ad2), i.e. a
            // NullReferenceException. Capture its geometry before any dictionary/icon call.
            int slotX = slot.x;
            int slotY = slot.y;
            float slotScale = s;
            Rect iconRect = new Rect(
                slotX * slotScale,
                (slotY - skillScroll) * slotScale,
                36f * slotScale,
                36f * slotScale);
            // Official 06001025 clips every slot before it reads or draws anything for it:
            // `0.0 <= bottom` (arm64 :639, x86_64 :786) and `top <= visibleHeight * s`
            // (arm64 :647, x86_64 :791, fVar27/uVar21 resolved at arm64 :505), both inclusive,
            // so an out-of-view slot draws no icon, no level number and no plus, and cannot take
            // a click, a hover, an upgrade request or a pickup.
            if (!(0f <= iconRect.yMax && iconRect.y <= visibleHeight * s))
            {
                continue;
            }

            SkInfo info = SkillInfo(slot.id);
            CharData.skills.TryGetValue(slot.id, out int currentLevel);
            int iconId = info == null ? 0 : info.icon;
            Texture2D icon = LoadSkillIcon(iconId);
            EndgameSkillRankView rank = UsesGrowthSkill(slot.id) ? EndgamePanel.GrowthState.SkillRank(slot.id) : null;
            bool canUse = UsesGrowthSkill(slot.id) ? rank?.CanCast == true : currentLevel > 0;
            string levelLabel = rank == null ? currentLevel.ToString(CultureInfo.CurrentCulture) : rank.EffectiveRank;
            if (levelLabel.Length > 5) levelLabel = levelLabel.Substring(0, 4) + "…";
            DrawSkillIcon(iconRect, icon, canUse, tree != null);
            if (canUse && BitmapFont.Ready)
            {
                BitmapFont.DrawOutline(
                    iconRect.xMax - 12f * s,
                    iconRect.yMax - 12f * s,
                    levelLabel,
                    new Color32(255, 230, 120, 255),
                    s);
            }

            Rect plusRect = new Rect(
                iconRect.x - 8f * s,
                iconRect.y + 24f * s,
                20f * s,
                18f * s);
            if (CanShowUpgrade(slot.id))
            {
                if (plus != null) GUI.DrawTexture(plusRect, plus);
                else
                {
                    GUI.color = new Color(0.2f, 0.6f, 0.2f, 0.9f);
                    GUI.DrawTexture(plusRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (!mobile && current != null && current.type == EventType.MouseDown &&
                    plusRect.Contains(localPointer))
                {
                    requestedUpgrade = slot.id;
                    current.Use();
                }
            }

            if (info == null || !iconRect.Contains(localPointer))
            {
                continue;
            }

            if (mobile && mobileTap) { mobileSkills.Select(slot.id); mobileSkillFirstRow = 0; }
            if (!mobile && current != null && current.type == EventType.MouseDown &&
                current.button == 0 && canUse && SkillUsableForHotbar(info))
            {
                GameHud.PickupSkill(slot.id, icon, false);
                heldSkillIconId = iconId;
                current.Use();
            }
            hoveredInfo = info;
            hoveredLevel = currentLevel;
        }
        GUI.EndGroup();

        if (mobile)
        {
            DrawMobileSkillActions(L, def);
            return;
        }
        if (requestedUpgrade != 0 && NetClient.I != null) NetClient.I.UpgradeSkillForIndependentRuntime(requestedUpgrade);
        if (hoveredInfo != null)
        {
            string text = BuildSkillTip(hoveredInfo, hoveredLevel);
            if (UsesGrowthSkill(hoveredInfo.id)) UITip.ShowSkill(text, s);
            else RequestSkillTipForIndependentRuntime(text, s);
        }
    }

    // Official Win05.DrawSkill 06001025, arm64 :255-333 / x86_64 :297-373: the skillfire frame
    // needs `CharData.skillPoints > 0` (arm64 :255-297, x86_64 :297-336) and the label needs
    // `pointlabel != null && pointlabel.Length > 1 && BitmapFont.Ready` (arm64 :304-312,
    // x86_64 :346-352). Only pointlabel[0] and pointlabel[1] are read - the official floor is two
    // entries, not the four this build used to demand before it would draw at all - and the fill
    // is the frozen (255, 240, 160) (arm64 0x3f800000/0x3f70f0f1/0x3f20a0a1/0x3f800000,
    // x86_64 DAT_0111cdb0 + DAT_01119140).
    private void DrawSkillPointFeedback(Func<float, float, float, float, Rect> L)
    {
        if (DisplayedSkillPoints > 0)
        {
            UiFrame(SkillFirePath, 0);
            uiFrameCache.TryGetValue(SkillFirePath, out Texture2D[] frames);
            if (frames != null && frames.Length > 0)
            {
                Texture2D texture = frames[(int)(Time.time * 10f) % frames.Length];
                if (texture != null)
                    GUI.DrawTexture(L(91f, 359f, TexScale.W(texture), TexScale.H(texture)), texture);
            }
        }
        if (skLayout.pointlabel != null && skLayout.pointlabel.Length > 1 && BitmapFont.Ready)
        {
            var growth = EndgamePanel.GrowthState;
            bool retry = growth?.PendingRequest?.Op == "skill.upgrade";
            bool refresh = growth != null && !growth.Pending && (!growth.HasSkillRanks || growth.SkillRanksRevision != growth.Revision);
            string text = retry ? "查询原请求" : refresh ? "刷新技能" : "技能点 " + DisplayedSkillPoints;
            BitmapFont.DrawOutline(winX + skLayout.pointlabel[0] * s,
                winY + skLayout.pointlabel[1] * s, text, new Color32(255, 240, 160, 255), s);
            Event current = Event.current;
            if ((retry || refresh) && current != null && current.type == EventType.MouseDown && current.button == 0 &&
                L(skLayout.pointlabel[0], skLayout.pointlabel[1], 80f, 12f).Contains(current.mousePosition))
            {
                if (retry) EndgamePanel.RetryGrowthRequest(); else EndgamePanel.RefreshGrowthSkills();
                current.Use();
            }
        }
    }

    // Official Win05.DrawSkill 06001025, arm64 :114-127 / x86_64 :159-171: a null skLayout is a
    // drawn state, not a failure - the body only tests BitmapFont.Ready and then returns once the
    // centred line is out. The native static BitmapFont is also the supplier for an owner
    // created by Open/Toggle before any map adapter has been bound.
    private void DrawSkillLayoutMissing(Func<float, float, float, float, Rect> L, Def def)
    {
        if (!BitmapFont.Ready)
        {
            return;
        }

        BitmapFont.CenterOutline(
            L(0f, def.h * 0.5f, def.w, 12f),
            "技能布局缺失",
            new Color32(200, 80, 80, 255),
            s);
    }

    // Official Win05.DrawSkill 06001025, the JobTaskDone-false arm: arm64 0x3aeb90-0x3aecfc,
    // x86_64 0x3c1810-0x3c1971 - the shared return is arm64 0x3aed00 / x86_64 0x3c2ac0, so
    // nothing else of the skill page is drawn after these two lines. Both lines come from the
    // same local delegate `L(0, def.h * 0.5f +/- 8, def.w, 12f)` (arm64 0x3aeba4-0x3aebcc and
    // 0x3aec58-0x3aec7c; x86_64 0x3c1831-0x3c1862 and 0x3c18d7-0x3c1908) and then
    // BitmapFont.CenterOutline 060000ba with a frozen fill:
    //   * first line, y = def.h * 0.5 - 8, text = StringLiteral 17342 "尚未就职 —— 完成【" +
    //     JobTaskNames[career] + StringLiteral 16913 "】后开放职业技能" (arm64 0x3aebec-0x3aec0c,
    //     x86_64 0x3c1889-0x3c18a3), fill (210, 160, 90, 255) built into s4/s5/s6/s7 at arm64
    //     0x3aec18-0x3aec3c (0x3f52d2d3 / 0x3f20a0a1 / 0x3eb4b4b5 / 1.0) and loaded from
    //     DAT_0x111cdc0 = {210/255, 160/255} + DAT_0x1118280 = {90/255, 1.0} on x86_64
    //     (0x3c18b2 / 0x3c18ba);
    //   * second line, y = def.h * 0.5 + 8, text = StringLiteral 17161 "去【龙城】找【" +
    //     JobTaskNpcNames[career] + StringLiteral 16920 "】接取" (arm64 0x3aec9c-0x3aecbc,
    //     x86_64 0x3c192a-0x3c1944), fill (255, 230, 150, 255) - arm64 0x3aecc8-0x3aece4 loads
    //     0x3f66e6e7 (230/255) into s5, 0x3f169697 (150/255) into s6 and 1.0 into s4 and s7, x86_64
    //     DAT_0x111cdd0 = {1.0, 230/255} + DAT_0x1117d90 = {150/255, 1.0} (0x3c194f / 0x3c1957);
    //   * before either line the body re-tests BitmapFont.Ready (arm64 0x3aeb90-0x3aeb98, x86_64
    //     0x3c1810-0x3c1817) and returns without drawing when it is false, and a null Def reaches
    //     the non-returning helper (arm64 0x3aeba0, x86_64 0x3c182b). Keep the passed Def
    //     reference and read h/w at each line, even though the current skill/study rows agree.
    private void DrawJobTaskHint(Func<float, float, float, float, Rect> L, Def def)
    {
        if (!BitmapFont.Ready)
        {
            return;
        }

        int career = CareerIndex();
        string jobTaskName = career switch
        {
            0 => "战士就职", 1 => "剑客就职", 2 => "刺客就职", 3 => "药师就职", 4 => "术士就职",
            _ => throw new IndexOutOfRangeException()
        };
        string jobTaskNpcName = career switch
        {
            0 => "钟离门人裴谌", 1 => "吕仙门人许栖岩", 2 => "仙姑门人萼绿华",
            3 => "铁拐门人蓟子训", 4 => "果老门人寒山子",
            _ => throw new IndexOutOfRangeException()
        };
        BitmapFont.CenterOutline(
            L(0f, def.h * 0.5f - 8f, def.w, 12f),
            "尚未就职 —— 完成【" + jobTaskName + "】后开放职业技能",
            new Color32(210, 160, 90, 255),
            s);
        BitmapFont.CenterOutline(
            L(0f, def.h * 0.5f + 8f, def.w, 12f),
            "去【龙城】找【" + jobTaskNpcName + "】接取",
            new Color32(255, 230, 150, 255),
            s);
    }

    // Original 06001026 receives the native L closure and reads Event.current itself.
    private void DrawLifeSkills(Func<float, float, float, float, Rect> L, Def d)
    {
        Event current = Event.current;
        Texture2D button = UiFrame(SkillButtonPath, 0);
        Texture2D plus = UiFrame(PlusButtonPath, 0);
        int requested = 0;
        (int Id, string Name) hovered = (0, null);
        bool hoveredLearned = false;
        (int, string) hoveredState = default;
        for (int index = 0; index < LifeSkillList.Length; index++)
        {
            var definition = LifeSkillList[index];
            float x = 22f + (index % 4) * 54f;
            float y = 70f + (index / 4) * 54f;
            Rect iconRect = L(x, y, 36f, 36f);
            bool learned = lifeData.TryGetValue(definition.Id, out var state);
            bool canLearn = lifeLearnMode && !learned && lifeCanLearn.Contains(definition.Id);
            if (button != null)
                GUI.DrawTexture(L(x - 3f, y - 3f, GsaTextureScale.Width(button), GsaTextureScale.Height(button)), button);
            Texture2D icon = LoadSkillIcon(definition.IconId);
            if (icon != null)
            {
                if (!learned) GUI.color = new Color(1, 1, 1, 0.4f);
                GUI.DrawTexture(Fit(iconRect, icon), icon);
                GUI.color = Color.white;
            }
            if (canLearn && plus != null)
            {
                Rect plusRect = new Rect(iconRect.xMax - 12f * s, iconRect.yMax - 12f * s,
                    GsaTextureScale.Width(plus) * s, GsaTextureScale.Height(plus) * s);
                GUI.DrawTexture(plusRect, plus);
                if (current.type == EventType.MouseDown && current.button == 0 && plusRect.Contains(current.mousePosition))
                {
                    current.Use();
                    requested = definition.Id;
                }
            }
            if (iconRect.Contains(current.mousePosition))
            {
                hovered = (definition.Id, definition.Name);
                hoveredLearned = learned;
                hoveredState = state;
            }
        }
        if (requested != 0 && NetClient.I != null) NetClient.I.LearnLifeForIndependentRuntime(requested);
        if (hovered.Name == null) return;
        string tip = hoveredLearned
            ? string.Format("{0}\n{1}  熟练度 {2}", hovered.Name, hoveredState.Item2, hoveredState.Item1)
            : hovered.Name + (lifeCanLearn.Contains(hovered.Id)
                ? "\n可学习 (点右下角 + 学习)" : "\n未学习 (需先到鬼谷购买对应技能书)");
        UITip.Show(tip, s);
    }

    // Original Fit 06001052, used by the native life-skill icon path.
    private static Rect Fit(Rect area, Texture2D texture)
    {
        float factor = Mathf.Min(area.width / texture.width, area.height / texture.height);
        float width = texture.width * factor, height = texture.height * factor;
        return new Rect(area.x + (area.width - width) * 0.5f,
            area.y + (area.height - height) * 0.5f, width, height);
    }

    // Original ShowSkillTip row construction (06001028); existing caller owns UITip request.
    private string BuildSkillTip(SkInfo info, int currentLevel)
    {
        if (UsesGrowthSkill(info.id)) return BuildGrowthSkillTip(info, true);
        var rows = new List<(string, char, bool)>();
        AppendLevelBlock(rows, info, Mathf.Max(1, currentLevel),
            currentLevel > 0 ? string.Format("等级: {0}", currentLevel) : "等级: 未学习", info.name);
        if (currentLevel == 0)
        {
            SkLevel first = TipLevel(info, 1);
            if (first != null && first.lreq > 0 && first.lreq != 999)
                rows.Add((string.Format("需要等级: {0}", first.lreq), (char)1, false));
        }
        int nextLevel = currentLevel + 1;
        if (currentLevel > 0 && nextLevel <= info.maxlv)
        {
            rows.Add((string.Empty, (char)0, false));
            AppendLevelBlock(rows, info, nextLevel, null, "下一等级");
            SkLevel next = TipLevel(info, nextLevel);
            if (next != null && next.lreq > 0 && next.lreq != 999)
                rows.Add((string.Format("需要等级: {0}", next.lreq), (char)1, false));
        }
        AppendDmgBonusRow(rows, info);
        rows.Add((string.Empty, (char)0, false));
        rows.Add((string.Format("您现在的剩余技能点数为{0}", CharData.skillPoints), (char)6, false));
        bool prerequisite = info.pre == 0 ||
            (CharData.skills.TryGetValue(info.pre, out int priorLevel) && priorLevel > 0);
        SkLevel requirement = TipLevel(info, nextLevel);
        if (CharData.skillPoints <= 0 || currentLevel >= info.maxlv ||
            (currentLevel <= 0 && !prerequisite) ||
            (requirement != null && requirement.lreq > 0 && requirement.lreq != 999 && CharData.level < requirement.lreq))
            rows.Add(("您现在还无法升级这个技能", (char)6, false));
        if (SkillUsableForHotbar(info)) rows.Add((MobileClientPresentation.Enabled ? "轻点查看；点放入快捷栏选择位置" : "双击或拖曳至快捷栏以使用这个技能", (char)6, false));
        return ComposeTip(rows);
    }

    private static SkLevel TipLevel(SkInfo info, int level)
    {
        if (info.levels != null)
            foreach (SkLevel row in info.levels)
                if (row.lv == level) return row;
        return null;
    }

    private static SkLevel LevelOf(SkInfo info, int level)
    {
        if (info == null || info.levels == null)
        {
            return null;
        }

        for (int index = 0; index < info.levels.Length; index++)
        {
            SkLevel row = info.levels[index];
            if (row != null && row.lv == level)
            {
                return row;
            }
        }

        return null;
    }

    private static bool SkillUsableForHotbar(SkInfo info)
    {
        return info != null && info.tgt != 18 && info.type != 19 && info.type != 20;
    }

    // Official 06001025 has no skill-id filter in the slot loop (arm64 :621-639, x86_64 :745-786:
    // null row → helper, then geometry); 13601/13602 are selected out by the page itself, because
    // the only FindSwitch 06001024 call (arm64 :346-348, x86_64 :385) asks for `sw == career + 1`
    // and both ids live in the `maintab` sw=0 row, which that lookup never resolves.
    private int CareerIndex()
    {
        return Mathf.Clamp(CharData.race, 0, 4);
    }

    // Official 0x06001024, instance Win05.SkSwitch Win05.FindSwitch(System.String, System.Int32)
    // (arm64 rva 0x3ade34 336 bytes, x86_64 rva 0x3c0990 320 bytes). Both bodies are one loop:
    //   * `skLayout == null` (Win05 static +0x08) returns null without throwing (arm64 :36,
    //     x86_64 :33) and `skLayout.switches == null` (SkLayout +0x28) returns null too
    //     (arm64 :42-43, x86_64 :39-40) - the guard above is that pair;
    //   * each row is read as `switches[i]` (+0x20 + i*8) and compared with
    //     `String.op_Equality(row.tab /* +0x10 */, key) && row.sw /* +0x20 */ == value`
    //     (arm64 :59-65, x86_64 :56-62) - tab FIRST, sw second. The body below writes that same
    //     order; the two comparisons are pure, but the official order is writable as-is, so it is
    //     restored rather than kept reversed on a single-thread-equivalence argument;
    //   * a null row is NOT skipped: arm64 :60 `if (lVar3 == 0) goto LAB_003adf80` and x86_64 :57
    //     `if (lVar1 == 0) goto LAB_003c0ac3` both land on the non-returning helper
    //     (arm64 FUN_00177a74, x86_64 FUN_0016c1b0). The `row != null &&` that used to stand here
    //     translated that official NullReferenceException into a silent skip, so it is gone and
    //     the row read is left unguarded. Reachability is NOT asserted here: no experiment was
    //     run on whether JsonUtility can emit a null element for a [Serializable] class array,
    //     so this note claims only "no longer rewritten into a skip", never "unreachable".
    private SkSwitch FindSkillSwitch(string key, int value)
    {
        if (skLayout == null || skLayout.switches == null)
        {
            return null;
        }

        for (int index = 0; index < skLayout.switches.Length; index++)
        {
            SkSwitch row = skLayout.switches[index];
            if (string.Equals(row.tab, key, StringComparison.Ordinal) &&
                row.sw == value)
            {
                return row;
            }
        }

        return null;
    }

    private Texture2D LoadSkillIcon(int iconId) => SkillIcon(iconId);

    // Original SkillIcon 06001022.
    private Texture2D SkillIcon(int iconId)
    {
        if (iconId < 1) return null;
        if (skillIconCache.TryGetValue(iconId, out Texture2D cached)) return cached;
        Texture2D texture = null;
        if (ResLoader.I != null)
        {
            byte[] data = ResLoader.I.ReadRes(string.Format("item/item/item{0}.gso", iconId));
            if (data != null)
            {
                DimgDecoder.Result decoded = DimgDecoder.Decode(data, 0);
                if (decoded.ok)
                {
                    texture = decoded.tex;
                    TexScale.Register(texture, decoded.scale);
                }
            }
        }
        skillIconCache[iconId] = texture;
        return texture;
    }

    // Tree resources contain obsolete inactive pictures as well as their frames.
    // Keep only the original outer rim: the current skill's icon is authoritative
    // for both ranks. Cover the inner well so transparent pixels, fitted margins,
    // and a missing icon cannot expose a different skill from the tree below.
    private static void DrawSkillIcon(Rect area, Texture2D icon, bool learned, bool hasTreeArtwork)
    {
        float insetX = hasTreeArtwork ? area.width * (2f / 36f) : 0f;
        float insetY = hasTreeArtwork ? area.height * (2f / 36f) : 0f;
        Rect well = new Rect(area.x + insetX, area.y + insetY,
            area.width - 2f * insetX, area.height - 2f * insetY);
        GUI.color = new Color(17f / 255f, 17f / 255f, 17f / 255f, 1f);
        GUI.DrawTexture(well, Texture2D.whiteTexture);
        if (icon != null)
        {
            // Brightness, not opacity: an unlearned skill must still be one image.
            GUI.color = learned ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            GUI.DrawTexture(Fit(well, icon), icon);
        }

        GUI.color = Color.white;
    }

    private static void DrawPackageButton(
        Rect rect,
        IReadOnlyList<Texture2D> frames,
        Event current)
    {
        if (frames == null || frames.Count == 0)
        {
            return;
        }

        bool hovered = current != null && rect.Contains(current.mousePosition);
        int frame = hovered ? 1 : 0;
        if (hovered && Input.GetMouseButton(0))
        {
            frame = 2;
        }
        frame = Mathf.Clamp(frame, 0, frames.Count - 1);
        if (frames[frame])
        {
            GUI.DrawTexture(rect, frames[frame], ScaleMode.StretchToFill, true);
        }
    }

    private static bool DrawFrameButton(
        Rect rect,
        IReadOnlyList<Texture2D> frames,
        Event current)
    {
        DrawPackageButton(rect, frames, current);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    // Official Win05.OnGUI 06001042 close arm, arm64 :500-528 / x86_64 :515-540.
    // Three different states and only one of them draws art:
    //   * `plVar11 = param_1 + 0x38` (the shared close frames) == null -> the arm jumps straight to
    //     `GUI.Button(rect, StringLiteral usage 16870)`; no DrawTexture happens at all. The literal
    //     is metadata_usage_16870_kind5 -> StringLiteral index 6540 -> the UTF-8 bytes
    //     e2 9c 95 read out of the frozen 1.5.8 global-metadata.dat (sha256 941a6105...), i.e. one
    //     character U+2715 "✕". It is the two-argument `GUI.Button(Rect, String)` overload, so the
    //     missing-art case is the stock skin button with that glyph, not an invisible button;
    //   * a zero-length array falls into the non-returning helper first (arm64 :502 reads
    //     `*(int *)(arr + 0x18) == 0` before reading slot 0) - that is exactly the
    //     IndexOutOfRangeException a C# `frames[0]` raises, and the index below keeps it;
    //   * `frames[0]` Unity-null (`Object.op_Inequality(uVar8, 0)`) takes the same ✕ arm as a null
    //     array, so a partially loaded set (EnsureUiFrames leaves a null slot rather than shifting
    //     later frames down) is still a clickable ✕ button.
    // Pressing it stores `cur = null` plus one write barrier (arm64 :513-514 / *plVar12 = 0) and
    // skips the whole content dispatch; there is no Event.Use() in either native arm, GUI.Button
    // consumes the click itself. The four window close callers already answer that with CloseNow()
    // and return, which is the same single store.
    private static bool DrawWindowCloseButton(
        Rect rect,
        Texture2D[] frames,
        Event current)
    {
        if (frames == null || frames[0] == null)
        {
            return GUI.Button(rect, "✕");
        }

        DrawPackageButton(rect, frames, current);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    // Official Win05.OnGUI 06001042 help arm, arm64 :481-498 / x86_64 :480-497. This is NOT the
    // close arm with another rectangle: the tests are `param_1 + 0x40` (the shared help frames)
    // != null followed by `Object.op_Inequality(slot0, 0)`, and a failure of either one draws
    // nothing at all - there is no ✕ text fallback and there is no button to press. Copying the
    // close arm's default glyph here would invent a help button official does not have. The third
    // test is part of the callers, not this helper, because the rectangle itself is only built
    // inside that arm: `if ((def.nohelp) || (def.cx < 0f)) <no help>` (bVar15 at arm64 :252-274,
    // bVar3 on x86_64), i.e. help is only ever reached while `!def.nohelp && def.cx >= 0f`.
    private static bool DrawWindowHelpButton(Rect rect, Texture2D[] frames, Event current)
    {
        return frames != null && frames[0] != null && Btn(rect, frames, current);
    }

    // Official Win05.OnGUI 06001042 first-draw branch, arm64 :117-176 / x86_64 :124-188 (both
    // architectures carry the same body). It is the only place official ever fills the two shared
    // frame arrays, and it sits between the Def lookup and UIScale.Factor - before anything that
    // draws, so every later consumer reads whatever this left behind:
    //   * ResLoader.I is read once out of its static slot (metadata_usage_02298 static field
    //     +0xb8) and compared with `Object.op_Equality(I, null)`. A still-null owner returns from
    //     OnGUI without drawing, so the next frame re-enters here: that is the whole retry story,
    //     this gate belongs to the common OnGUI entry, before this helper;
    //   * `if (this[+0x38] == 0)` is the single guard, on the close array alone. The two arrays
    //     are instance fields (+0x38 closeFr, +0x40 helpFr) shared by every Win05 window, so one
    //     window's first frame binds them for all five. Nothing else reloads them;
    //   * both arrays are allocated `new Texture2D[3]` and stored into their fields BEFORE the
    //     first ResLoader.LoadUI (06000c15) call, so the fields are already non-null if the loop
    //     were ever interrupted - there is no second pass and no later retry of a failed path;
    //   * the loop writes with the raw index - `arr[i] = LoadUI(...)` (arm64 :161/172) - so a
    //     path that does not resolve leaves that slot null instead of shifting later frames down;
    //   * the names are Concat(metadata_usage_16361 "ui/equipdlg05/miniclose_f", i.ToString(),
    //     metadata_usage_10983 ".png") and Concat(metadata_usage_16359 "ui/equipdlg05/helpicon_f",
    //     ..., ".png"), already the official constants this build uses as
    //     ReferenceFirstWorldLocalEquipmentDialog.ClosePathBase / .HelpPathBase.
    // Textures stay owned by ResLoader's own LoadUI cache; the arrays only hold references.
    private void EnsureUiFrames(ResLoader loader)
    {
        if (closeFr != null) return;
        closeFr = new Texture2D[3];
        helpFr = new Texture2D[3];
        for (int index = 0; index < 3; index++)
        {
            closeFr[index] = loader.LoadUI("ui/equipdlg05/miniclose_f" + index.ToString() + ".png");
            helpFr[index] = loader.LoadUI("ui/equipdlg05/helpicon_f" + index.ToString() + ".png");
        }
    }

    private static Texture2D[] LoadPackageFrames(
        ReferenceResourceLoader resources,
        string path,
        int maximum)
    {
        List<Texture2D> frames = new List<Texture2D>();
        for (int index = 0; index < maximum; index++)
        {
            Texture2D texture = resources.LoadGsaFrame(path, index);
            if (!texture)
            {
                break;
            }

            frames.Add(texture);
        }

        return frames.ToArray();
    }

}

}
