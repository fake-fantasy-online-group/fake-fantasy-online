using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source restoration of the official Win05 petdlg owner and its local presentation helpers.
/// </summary>
public partial class Win05
{
    public const string PetDialogName = "petdlg";
    public const string PetDialogBackgroundPath =
        "res/uires/petdlg/petdlgbk.gsa";
    public const string PetSkillBackgroundPath =
        "res/uires/petdlg/petskilldlg.gsa";
    public const float PetDialogWidth = 257f;
    public const float PetDialogHeight = 542f;
    public const float PetSkillPanelExtraWidth = 166f;

    // Pet constants and native static/adapter instance initializers are ordered in Win05.Initialization.cs.
    // Win05.DrawPet 06001043 gives every pet command its own drawing arm. WBD-121 verified and
    // restored the display-toggle arm - NetClient.PetShow 0x0600073b - and only that arm; the
    // other arms keep the state their own packages reached. The only intent this dialog still
    // holds for a later pass is the pet view mask, which is a host view update, not a pet command.
    private Texture2D petSlotIcon;
    private static bool petSkillPanelOpen;
    private static bool petRenaming;
    private bool petDialogReady;

    // The host consumes only the last view mask at its existing observation point.
    // Intermediate masks only overwrote the same bool; publishing these scalars cannot
    // allocate or fail between the native preference callback and Event.Use.
    private int pendingPetViewMask;
    private bool hasPendingPetViewMask;

    public bool PetDialogReady => petDialogReady;
    public bool PetDialogVisible =>
        string.Equals(cur, PetDialogName, StringComparison.Ordinal);

    public WorldPlayerPetStatusState PetStatus => petStatus;
    internal Texture2D PetNicknameEditTexture => UiFrame("res/uires/petdlg/nicknameedit.gsa", 0);
    // InventoryDlg owns the held pet across pickup, close, sort and ClearHeld.
    // A copied index can outlive the native close/sort stores and mask host input.
    public bool HasHeldPet => InventoryDlg.HasHeldBarPet;

    private bool TryInitializePetDialog(WorldEntryRecord character)
    {
        petStatus = WorldPlayerPetStatusState.FromEntry(character.Pet);
        petDialogReady = true;
        return true;
    }

    private bool petResourcesBound;
    private void EnsurePetResourcesForIndependentRuntime()
    {
        if (petResourcesBound) return;
        ReferenceResourceLoader resources = skillResources ?? ReferenceResourceLoader.OpenStreamingAssets();
        petResourcesBound = true;
        // Existing item3385 resource supplier is preserved; no item decoder or ownership change.
        petSlotIcon = resources.LoadItemIconFirstVisible("item/item/item3385.gso");
    }

    // Original0600103c: an owner cache stores both the converted texture and a null miss.
    private Texture2D IronBg(string path)
    {
        if (ironCache.TryGetValue(path, out Texture2D texture)) return texture;
        Texture2D source = ResLoader.I != null ? ResLoader.I.LoadGsa2021First(path) : null;
        texture = source != null ? ToBlackIron(source) : null;
        ironCache[path] = texture;
        return texture;
    }

    public bool ApplyPetStatus(WorldPlayerPetStatusState status)
    {
        if (!petDialogReady || status == null)
        {
            return false;
        }

        petStatus = status;
        return true;
    }

    // Original Win05.DrawPet06001043. The caller supplies the same pre-drag localization
    // closure and event as every other native Win05 content owner.
    private void DrawPet(Func<float, float, float, float, Rect> L, Def d, Event current)
    {
        // Original <>c__DisplayClass87_0: allocate before any pet-page work, then
        // publish L, receiver and event in that order. curSkillId stays zero until
        // the existing selected-skill read; every original helper shares this capture.
        var capture = new PetDrawCapture();
        capture.L = L;
        capture.owner = this;
        capture.ev = current;
        int activePet = CharData.petActive;
        int model = CharData.petModel;
        DrawCurrentPet(capture, model, s, current);
        DrawPetRename(capture, model, s, current);
        DrawPetPointControls(L, s, current);
        int selectedSkill = DrawCurrentPetSkill(capture, s, current);
        if (petSkillPanelOpen && CharData.petSkills != null)
            DrawPetSkillPanel(capture, selectedSkill, s, current);
        DrawPetPreferences(capture, s, current);
        DrawPetShowToggle(L, s, current);
        DrawPetSlots(L, activePet, s, current);
    }

    private void DrawCurrentPet(PetDrawCapture capture, int model, float scale, Event current)
    {
        Func<float, float, float, float, Rect> L = capture.L;
        WorldPlayerPetDetailState detail = petStatus.Detail;
        if (model <= 0)
        {
            // DrawPet06001043 has no substitute current-pet label on this path.
            return;
        }

        Texture2D pet = PetTex(CharData.petModel);
        if (pet)
        {
            GUI.DrawTexture(Fit(L(47f, 119f, 83f, 96f), pet), pet);
        }

        DrawMountRingToggle(L, scale, current);
        string tier = CharData.PetTierName(CharData.petGrowTier);
        capture.Lbl(21f, 49f, CharData.petPrename +
            (tier.Length > 0 ? "·" + tier : string.Empty), new Color(1f, 0.85f, 0.3f, 1f));
        capture.Lbl(65f, 249f, detail.MaximumAttack.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(155f, 249f, detail.MinimumAttack.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(65f, 262f, detail.Defense.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(155f, 262f, detail.MagicAttack.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(65f, 275f, detail.Hit.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(155f, 275f, detail.MagicDefense.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));

        capture.Lbl(59f, 74f, CharData.petCurHp.ToString(CultureInfo.CurrentCulture) + "/" +
            CharData.petMaxHp.ToString(CultureInfo.CurrentCulture), Color.white);
        capture.Lbl(163f, 74f, CharData.petCurSp.ToString(CultureInfo.CurrentCulture) + "/" +
            CharData.petMaxSp.ToString(CultureInfo.CurrentCulture), Color.white);
        capture.Lbl(176f, 98f, CharData.petLevel.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        if (BitmapFont.Ready && CharData.petArousal > 0)
        {
            int arousal = CharData.petArousal;
            int stars = Mathf.Clamp(arousal, 0, 10);
            BitmapFont.CenterOutline(L(164f, 180f, 62f, 12f),
                new string('★', Math.Min(stars, 5)), new Color(1f, 0.85f, 0.3f, 1f), scale);
            if (arousal > 5)
                BitmapFont.CenterOutline(L(164f, 192f, 62f, 12f),
                    new string('★', stars - 5), new Color(1f, 0.85f, 0.3f, 1f), scale);
        }
        capture.Lbl(184f, 117f, CharData.petStarve.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(184f, 136f, CharData.petTrust.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));

        capture.Lbl(51f, 298f, detail.Vitality.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(169f, 298f, detail.Intelligence.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(51f, 316f, detail.Strength.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(169f, 316f, detail.Agility.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(51f, 335f, detail.Dexterity.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        capture.Lbl(169f, 335f, detail.Spirit.ToString(
            CultureInfo.CurrentCulture), new Color(0.86f, 0.8f, 0.72f, 1f));
        if (CharData.petPoints > 0)
        {
            Texture2D fire = UiFrame("res/uires/common/pointfire.gsa",
                (int)(Time.unscaledTime * 8f) % 8);
            if (fire != null) GUI.DrawTexture(L(33f, 341f, 30f, 30f), fire);
        }
        capture.Lbl(55f, 357f, CharData.petPoints.ToString(
            CultureInfo.CurrentCulture), new Color(1f, 0.85f, 0.3f, 1f));
        // DrawPet 06001043 has a separate Ready gate for the experience line.
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(
                L(121f, 357f, 125f, 12f),
                CharData.petExp.ToString(CultureInfo.CurrentCulture) + "/" +
                CharData.petExpUp.ToString(CultureInfo.CurrentCulture),
                new Color(0.86f, 0.8f, 0.72f, 1f),
                scale);
        }

        Texture2D habit = UiFrame("res/uires/petdlg/habit.gsa", Math.Max(0, detail.Habit - 1));
        Texture2D gender = UiFrame("res/uires/petdlg/gender.gsa", detail.Gender);
        if (habit != null) GUI.DrawTexture(L(200f, 95f, 16f, 16f), habit);
        if (gender != null) GUI.DrawTexture(L(218f, 95f, 16f, 16f), gender);

    }

    private int DrawCurrentPetSkill(PetDrawCapture capture, float scale, Event current)
    {
        Func<float, float, float, float, Rect> L = capture.L;
        Rect button = L(210f, 251f, 30f, 30f);
        capture.curSkillId = CharData.petActiveSkill;
        Texture2D icon = null;
        if (CharData.petSkills != null)
        {
            CharData.PetSkill selected = CharData.petSkills.Find(capture.IsCurrentSkill);
            if (selected.id != 0) icon = PetSkillIcon(selected.id, selected.icon);
        }
        if (icon != null) GUI.DrawTexture(Fit(button, icon), icon);
        if (GUI.Button(button, GUIContent.none, GUIStyle.none))
        {
            petSkillPanelOpen = !petSkillPanelOpen;
            current.Use();
        }
        return capture.curSkillId;
    }

    private void DrawMountRingToggle(Func<float, float, float, float, Rect> L, float scale, Event current)
    {
        Rect box = L(40f, 206f, 14f, 14f);
        Rect hit = L(36f, 202f, 22f, 22f);
        Texture2D frame = UiFrame("res/uires/common/checkbox.gsa", CharData.ShowMountRing ? 1 : 0);
        if (frame != null) GUI.DrawTexture(box, frame);
        else
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, .5f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = old;
            if (CharData.ShowMountRing && BitmapFont.Ready)
                BitmapFont.Draw(box.x + scale + scale, box.y, "√", Color.white, scale);
        }
        if (hit.Contains(current.mousePosition)) UITip.Show("是否展示天魔形象", scale);
        if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
        {
            CharData.ShowMountRing = !CharData.ShowMountRing;
            current.Use();
        }
    }

    private void DrawPetRename(PetDrawCapture capture, int model, float scale, Event current)
    {
        Func<float, float, float, float, Rect> L = capture.L;
        Texture2D edit = UiFrame("res/uires/petdlg/nicknameedit.gsa", 0);
        if (edit != null)
        {
            float width = TexScale.W(edit);
            float widthScale = s;
            float height = TexScale.H(edit);
            GUI.DrawTexture(L(94f, 45f, width / widthScale, height / s), edit);
        }
        if (petRenaming)
        {
            var style = new GUIStyle(GUI.skin.textField);
            style.fontSize = Mathf.RoundToInt(12f * scale);
            style.normal.textColor = Color.white;
            petRenameText = UiInput.Field(L(100f, 49f, 92f, 14f),
                "petNick", petRenameText ?? string.Empty, 10, style, scale);
            GUI.FocusControl("petNick");
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Return)
            {
                SubmitPetRename();
                current.Use();
            }
        }
        else if (model > 0)
            capture.Lbl(100f, 49f, CharData.petName ?? string.Empty, Color.white);

        Texture2D buttonTexture = UiFrame("res/uires/petdlg/nicknamebtn.gsa", 0);
        Rect button = L(201f, 43f, 44f, 22f);
        if (buttonTexture != null) GUI.DrawTexture(button, buttonTexture);
        if (GUI.Button(button, buttonTexture != null ? GUIContent.none : new GUIContent("更改"),
            buttonTexture != null ? GUIStyle.none : GUI.skin.button))
        {
            if (petRenaming) SubmitPetRename();
            else
            {
                petRenaming = true;
                petRenameText = CharData.petName ?? string.Empty;
            }
            current.Use();
        }
    }

    // Official Win05.<DrawPet>g__SubmitName|87_1 0x06001060, arm64 0x3c01bc (516 bytes) and
    // x86_64 0x3d4250 (464 bytes), both six steps in order:
    // the rename field with the empty-string substitution, one String.Trim(), `Length > 0`, one
    // NetClient.I read compared with Object.op_Inequality, then a second read for the call whose
    // null reaches the non-returning helper at arm64 0x3c03bc / x86_64 0x3d441a, PetRename(text),
    // and finally the local echo into CharData static +0x158 (petName) through the write barrier.
    // Evidence: Build/ReferenceReverseGhidra-1.5.8/queries/WBD-119-06001060-{arm64,x86_64}.
    // The flag Win05 static +0x9a (petRenaming) is cleared on the shared
    // tail, so empty text and a null first I read both exit edit mode without sending; only the
    // second read's null leaves it set by throwing. The send itself is not same-frame synchronous
    // in the product (see ReferenceCharacterSkillUsePort) - that gap is the recorded partial.
    private void SubmitPetRename()
    {
        string value = petRenameText == null ? string.Empty : petRenameText.Trim();
        if (value.Length > 0 && NetClient.I != null)
        {
            NetClient.I.PetRenameForIndependentRuntime(value);
            CharData.petName = value;
        }
        petRenaming = false;
    }

    private void DrawPetPointControls(Func<float, float, float, float, Rect> L, float scale, Event current)
    {
        if (CharData.petPoints <= 0 || NetClient.I == null) return;
        Texture2D plus = UiFrame("res/uires/common/plusbtn.gsa", 0);
        // Six native point rows; keep the values at their existing consumer without heap arrays.
        for (int index = 0; index < 6; index++)
        {
            Rect rect = L(index % 2 == 0 ? 106f : 223f, 294f + (index / 2) * 19f, 20f, 19f);
            if (plus != null) GUI.DrawTexture(rect, plus);
            if (GUI.Button(rect, plus != null ? GUIContent.none : new GUIContent("+"),
                plus != null ? GUIStyle.none : GUI.skin.button))
            {
                // The outer I admission belongs to DrawPet; the WBD-119 click arm still
                // directly re-reads I and throws before Use if that later receiver is null.
                NetClient.I.PetAddPointForIndependentRuntime(index switch
                {
                    0 => 5, 1 => 3, 2 => 1, 3 => 7, 4 => 9, 5 => 61,
                    _ => throw new IndexOutOfRangeException()
                });
                current.Use();
            }
        }
    }

    private void DrawPetShowToggle(Func<float, float, float, float, Rect> L, float scale, Event current)
    {
        // Official Win05.DrawPet 06001043 draws this checkbox from the official display root
        // CharData.petShow (CharData static +0x1f0), not from the panel's own copy of the
        // snapshot value: arm64 :2175-2180 reads that byte, then Win05.UiFrame 06001050 at :2183
        // and GUI.DrawTexture at :2200; x86_64 :2344-2349, :2353 and :2373. Both frozen exports
        // put the whole block at depth 1 of DrawPet - the branch reads and the click arm, arm64
        // :2175-2245 and x86_64 :2344-2424, contain no enclosing conditional - so no control gate
        // dominates the show/hide control. In particular neither body tests a current-pet model
        // here: the product guard this method used to start with (`petStatus.Model <= 0`) has no
        // native counterpart on this arm and is gone since WBD-121. The byte still only chooses
        // the checked texture, arm64 :2182 and x86_64 :2351.
        if (CharData.petShow)
        {
            Texture2D checkedFrame = UiFrame("res/uires/common/checkbox.gsa", 1);
            if (checkedFrame != null) GUI.DrawTexture(L(18f, 521f, 12f, 12f), checkedFrame);
        }
        Rect hit = L(17f, 520f, 97f, 14f);
        if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
        {
            // Official click arm, arm64 :2224-2245 and x86_64 :2402-2424: the byte at CharData
            // static +0x1f0 is XORed with 1 first, then NetClient.I is read once into a local and
            // tested with a raw pointer compare - arm64 :2233-2234 and x86_64 :2411-2412,
            // `lVar = **(long **)(usage_02073 + 0xb8)` then `if (lVar != 0)` - not with
            // UnityEngine.Object.op_Inequality, which is what `NetClient.I != null` binds to on
            // the Presentation MonoBehaviour and which also reads the static a second time. So
            // the instance is captured once and compared with ReferenceEquals here: a Unity object
            // that is destroyed but not CLR null is still called, exactly as the frozen body does.
            // PetShow 0x0600073b then runs on the captured instance with a second read of +0x1f0 -
            // the byte after the XOR, arm64 :2239 and x86_64 :2417 - and Event.Use() runs last,
            // outside the instance test: with NetClient.I null the echo and the event consumption
            // still happen and only the send is skipped. The frozen Use call keeps its own null
            // check, arm64 :2241 and x86_64 :2420, and both go to the throw helper, so a null
            // event is an exception and not a silent skip. So the echo is one write of the
            // official root before the send, and the next 0x800b snapshot overwrites it.
            CharData.petShow = !CharData.petShow;
            var net = NetClient.I;
            if (!ReferenceEquals(net, null))
            {
                net.PetShowForIndependentRuntime(CharData.petShow);
            }
            current.Use();
        }
    }

    private void DrawPetSlots(Func<float, float, float, float, Rect> L, int activePet, float scale, Event current)
    {
        int hovered = -1;
        for (int index = 0; index < 10; index++)
        {
            Rect rect = L(50f + (index % 5) * 40f, index < 5 ? 377f : 414f, 32f, 32f);
            bool occupied = CharData.petSlots != null && index < CharData.petSlots.Count &&
                CharData.petSlots[index].model > 0;
            if (!occupied)
            {
                Edge(rect, new Color(1f, 1f, 1f, .15f));
                continue;
            }
            EnsurePetResourcesForIndependentRuntime();
            if (petSlotIcon != null) GUI.DrawTexture(Fit(rect, petSlotIcon), petSlotIcon);
            if (CharData.petSlots[index].locked) InventoryDlg.DrawLockBadge(rect, scale);
            if (index == activePet) Edge(rect, new Color(1f, 0.85f, 0.3f, 1f));
            if (rect.Contains(current.mousePosition) && !InventoryDlg.HasHeldAny) hovered = index;
            if (!rect.Contains(current.mousePosition) || current.type != EventType.MouseDown) continue;

            // Existing WBD-076/077/078 closure: commands use the row index and run in this pass.
            if (current.button == 0 && current.clickCount == 2)
            {
                CharData.SwitchPet(index);
                InventoryDlg.ClearHeld();
                current.Use();
            }
            else if (current.button == 1)
            {
                if (NetClient.I != null)
                {
                    NetClient.I.PetRecallForIndependentRuntime(index);
                    current.Use();
                }
            }
            else if (current.button == 0 && InventoryDlg.ShareModHeld(current))
            {
                PetBrief row = CharData.petSlots[index];
                string name = string.IsNullOrEmpty(row.nickname) ? row.species : row.nickname;
                InventoryDlg.ShareToChat(1, 0, index, name);
                current.Use();
            }
            else if (current.button == 0 && !InventoryDlg.HasHeldAny)
            {
                InventoryDlg.PickUpBarPet(index, petSlotIcon, CharData.petSlots[index].nickname);
                current.Use();
            }
        }
        if (hovered >= 0 && hovered < CharData.petSlots.Count)
        {
            string instruction = hovered == activePet && CharData.petModel >= 1
                ? "双击收起(不出战)\n右击/拖进背包=收回\n拖到别格=排序"
                : "双击出战\n右击/拖进背包=收回\n拖到别格=排序";
            PetInfoTooltip.Show(CharData.petSlots[hovered], current.mousePosition, scale, instruction);
        }
        if (InventoryDlg.HasHeldBarPet && current.type == EventType.MouseDown)
        {
            PlayerController.SuppressWorldClick();
            int held = InventoryDlg.HeldBarPet;
            if (current.button == 1)
            {
                InventoryDlg.ClearHeld();
                current.Use();
            }
            else if (current.button == 0)
            {
                // Reuse the restored canonical helper; it uses the live window position.
                int target = HitPetSlotImpl(current.mousePosition);
                if (target >= 0)
                {
                    if (target != held && NetClient.I != null) NetClient.I.PetSwapForIndependentRuntime(held, target);
                }
                else if (InventoryDlg.OverWindow(current.mousePosition))
                {
                    if (NetClient.I != null) NetClient.I.PetRecallForIndependentRuntime(held);
                }
                InventoryDlg.ClearHeld();
                current.Use();
            }
        }
    }

    // Official Win05.DrawTooltip 0600105a (arm64 rva 0x3c0974 / x86_64 0x3d4970; frozen exports
    // Build/ReferenceReverseGhidra-1.5.8/queries/skill-window-{arm64,x86_64}-20260910/0600105a.c
    // on the shared Build root; official caller Win05.DrawPet 06001043, arm64 06001043.c:2067).
    // name and desc stay apart: one title line then WrapText 06001044(desc, 14); empty pair returns.
    // Size = widest BitmapFont.Measure 060000b4 line + 2*pad; fill (16,16,16,196)/255,
    // PatchPanel 0600105b overlay, then DrawOutline 060000b6: line 0 (1,.85,.3,1), rest
    // (225,220,195)/255, later lines stepped down by s*13 + s*4.
    private void DrawTooltip(string name, string desc, Vector2 mouse)
    {
        var lines = new List<string>();
        if (!string.IsNullOrEmpty(name))
        {
            lines.Add(name);
        }
        if (!string.IsNullOrEmpty(desc))
        {
            lines.AddRange(WrapText(desc, 14));
        }
        if (lines.Count == 0)
        {
            return;
        }
        float pad = s * 6f;
        float width = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            float measured = BitmapFont.Measure(lines[i], s);
            if (width <= measured)
            {
                width = measured;
            }
        }
        width = pad + pad + width;
        float height = pad + pad + s * 13f * lines.Count + s * 4f * (lines.Count - 1);
        float x = mouse.x + s * 14f;
        float y = mouse.y + s * 8f;
        if (Screen.width < width + x)
        {
            x = (mouse.x - width) + s * -6f;
        }
        if (Screen.height < y + height)
        {
            y = (Screen.height - height) + -2f;
        }
        x = Mathf.Max(0f, x);
        y = Mathf.Max(0f, y);
        GUI.color = new Color(16f / 255f, 16f / 255f, 16f / 255f, 196f / 255f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        PatchPanel(new Rect(x, y, width, height));
        float textX = x + pad;
        float textY = y + pad;
        for (int i = 0; i < lines.Count; i++)
        {
            if (i != 0)
            {
                textY = textY + s * 13f + s * 4f;
            }
            BitmapFont.DrawOutline(textX, textY, lines[i],
                i == 0 ? new Color(1f, 0.85f, 0.3f, 1f) : (Color)new Color32(225, 220, 195, 255), s);
        }
    }

    private void DrawPetSkillPanel(PetDrawCapture capture,
        int selectedSkillId, float scale, Event current)
    {
        Func<float, float, float, float, Rect> L = capture.L;
        Texture2D background = IronBg(PetSkillBackgroundPath);
        var ext = PetSkillExt();
        if (background != null)
        {
            float drawY = 120f;
            capture.Band(0f, 104f, background, ref drawY);
            if (ext.Item1) capture.Band(64f, 104f, background, ref drawY);
            capture.Band(104f, 109f, background, ref drawY);
            capture.Band(109f, 132f, background, ref drawY);
            capture.Band(132f, 172f, background, ref drawY);
            if (ext.Item2) capture.Band(132f, 172f, background, ref drawY);
            capture.Band(172f, 177f, background, ref drawY);
        }

        Texture2D close = UiFrame("res/uires/common/miniclose.gsa", 0);
        Rect closeRect = L(398f, 126f, 20f, 19f);
        if (close != null) GUI.DrawTexture(closeRect, close);
        if (GUI.Button(closeRect, close != null ? GUIContent.none : new GUIContent("✕"),
            close != null ? GUIStyle.none : GUI.skin.button))
        {
            petSkillPanelOpen = false;
            current.Use();
        }

        float extraLife = ext.Item1 ? 40f : 0f;
        var positions = new (float x, float y)[]
        {
            (5f, 28f), (45f, 28f), (85f, 28f), (125f, 28f),
            (5f, 68f), (45f, 68f), (85f, 68f), (125f, 68f),
            (5f, 108f), (45f, 108f), (85f, 108f), (125f, 108f),
            (5f, 136f + extraLife), (45f, 136f + extraLife),
            (85f, 136f + extraLife), (125f, 136f + extraLife),
            (5f, 176f + extraLife), (45f, 176f + extraLife),
            (85f, 176f + extraLife), (125f, 176f + extraLife)
        };
        int lifeCap = Math.Min(ext.Item1 ? 12 : 8, Math.Max(1, CharData.petLifeCap));
        int fightCap = Math.Min(ext.Item2 ? 8 : 4, Math.Max(1, CharData.petFightCap));
        var fight = new List<CharData.PetSkill>();
        var life = new List<CharData.PetSkill>();
        foreach (CharData.PetSkill skill in CharData.petSkills)
        {
            if (skill.level < 1) continue;
            (skill.fight ? fight : life).Add(skill);
        }
        var placed = new List<(CharData.PetSkill skill, int cell)>();
        for (int index = 0; index < fight.Count && index < fightCap; index++)
            placed.Add((fight[index], 12 + index));
        int lifeIndex = 0;
        foreach (CharData.PetSkill skill in life)
        {
            if (lifeIndex < lifeCap) placed.Add((skill, lifeIndex++));
        }

        bool hovered = false;
        string tipName = string.Empty, tipDescription = string.Empty;
        foreach (var item in placed)
        {
            CharData.PetSkill skill = item.skill;
            var position = positions[item.cell];
            Rect rect = L(257f + position.x, 120f + position.y, 36f, 36f);
            Texture2D icon = PetSkillIcon(skill.id, skill.icon);
            if (icon != null)
            {
                if (skill.level < 1) GUI.color = new Color(1f, 1f, 1f, .35f);
                GUI.DrawTexture(Fit(rect, icon), icon);
                GUI.color = Color.white;
            }
            if (skill.id == selectedSkillId) Edge(rect, new Color(1f, 0.85f, 0.3f, 1f));
            if (icon == null && BitmapFont.Ready)
                BitmapFont.CenterOutline(rect, skill.name, skill.level > 0
                    ? new Color32(220, 210, 140, 255) : new Color32(110, 110, 110, 255), scale);
            if (skill.active)
            {
                GUI.color = new Color(.4f, 1f, .4f, 1f);
                GUI.DrawTexture(new Rect(rect.xMax - 6f * scale, rect.y + 2f * scale,
                    4f * scale, 4f * scale), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (skill.level > 0 && BitmapFont.Ready)
                BitmapFont.CenterOutline(new Rect(rect.x, rect.yMax - 11f * scale,
                    rect.width, 10f * scale), "Lv." + skill.level.ToString(CultureInfo.CurrentCulture),
                    new Color32(255, 245, 200, 255), scale);
            if (!rect.Contains(current.mousePosition)) continue;
            hovered = true;
            tipName = (skill.active ? "[主动] " : "[被动] ") + skill.name +
                (skill.level > 0 ? "  Lv." + skill.level.ToString(CultureInfo.CurrentCulture) + "/" +
                    skill.max.ToString(CultureInfo.CurrentCulture) : "  (未领悟)");
            tipDescription = skill.desc ?? string.Empty;
            if (current.type == EventType.MouseDown && skill.level > 0 && skill.active)
            {
                if (current.button == 1)
                {
                    // Reuse the closed WBD-120 call/echo/Use order, including its two I reads.
                    if (NetClient.I != null)
                    {
                        if (skill.id == 13002) NetClient.I.PetUseSkillForIndependentRuntime(13002);
                        else if (skill.id == CharData.petActiveSkill)
                        {
                            NetClient.I.PetSetSkillForIndependentRuntime(0);
                            CharData.petActiveSkill = 0;
                        }
                        else
                        {
                            NetClient.I.PetUseSkillForIndependentRuntime(skill.id);
                            CharData.petActiveSkill = skill.id;
                        }
                        current.Use();
                    }
                }
                else if (current.button == 0)
                {
                    GameHud.PickupSkill(skill.id, icon, true);
                    heldSkillIconId = skill.icon;
                    current.Use();
                }
            }
        }
        // Reuse WBC-134's post-loop owner tooltip, with the existing native font gate.
        if (hovered && BitmapFont.Ready)
            DrawTooltip(tipName, tipDescription, current.mousePosition);
    }

    public bool TryConsumePetViewMask(out int mask)
    {
        if (!hasPendingPetViewMask)
        {
            mask = 0;
            return false;
        }

        mask = pendingPetViewMask;
        hasPendingPetViewMask = false;
        return true;
    }

    public bool TryGetPetSlotAt(int index, out WorldPlayerPetSlotState state)
    {
        state = null;
        if (petStatus == null || index < 0 || index >= 10)
        {
            return false;
        }
        IReadOnlyList<WorldPlayerPetSlotState> slots = petStatus.Slots;
        if (slots == null || index >= slots.Count)
        {
            return false;
        }
        state = slots[index];
        return true;
    }

    private static bool TryDequeueSlot(Queue<int> queue, out int slot)
    {
        if (queue.Count == 0)
        {
            slot = -1;
            return false;
        }
        slot = queue.Dequeue();
        return true;
    }

    private void DrawPetPreferences(PetDrawCapture capture, float scale, Event current)
    {
        GameSettings.EnsureLoaded();
        Texture2D checkedFrame = UiFrame("res/uires/common/checkbox.gsa", 1);
        Texture2D uncheckedFrame = UiFrame("res/uires/common/checkbox.gsa", 0);
        Color32 gold = new Color32(221, 198, 149, 255);
        Color32 gray = new Color32(120, 110, 92, 255);

        capture.PerfRow(453f, GameSettings.HideOtherPets, "不显示他人宠物/坐骑(最省)", true, () =>
        {
            bool old = GameSettings.HideOtherPets;
            GameSettings.HideOtherPets = !old;
            PerfApply(!old);
        }, checkedFrame, uncheckedFrame, gold, gray);
        capture.PerfRow(475f, GameSettings.HideMyPetGlow, "不显示自己宠物/坐骑光效", true, () =>
        {
            GameSettings.HideMyPetGlow = !GameSettings.HideMyPetGlow;
            PerfApply(false);
        }, checkedFrame, uncheckedFrame, gold, gray);
        capture.PerfRow(497f, GameSettings.HideOtherPetGlow, "不显示他人宠物/坐骑光效", true, () =>
        {
            GameSettings.HideOtherPetGlow = !GameSettings.HideOtherPetGlow;
            PerfApply(false);
        }, checkedFrame, uncheckedFrame, gold, gray);
    }

    // The original four-field reference capture is also the real Find predicate target.
    // There are no field initializers or custom constructor effects.
    private sealed class PetDrawCapture
    {
        public Func<float, float, float, float, Rect> L;
        public Win05 owner;
        public int curSkillId;
        public Event ev;

        public bool IsCurrentSkill(CharData.PetSkill skill) => skill.id == curSkillId;

        public void Lbl(float x, float y, string value, Color color)
        {
            // Native Lbl06001077 returns before localization or drawing when unready.
            if (!BitmapFont.Ready) return;
            Rect point = L(x, y, 0f, 0f);
            BitmapFont.DrawOutline(point.x, point.y, value, color, owner.s);
        }

        public void Band(float from, float to, Texture2D background, ref float drawY)
        {
            const float sourceHeight = 177f;
            float height = to - from;
            GUI.DrawTextureWithTexCoords(L(257f, drawY, 166f, height), background,
                new Rect(0f, 1f - to / sourceHeight, 1f, height / sourceHeight));
            drawY += height;
        }

        // Original0600107a, including the disabled branch. Current DrawPet calls are enabled.
        public void PerfRow(float y, bool value, string label, bool enabled, Action onClick,
            Texture2D checkedFrame, Texture2D uncheckedFrame, Color32 gold, Color32 gray)
        {
            Rect check = L(18f, y, 12f, 12f);
            Texture2D frame = value ? checkedFrame : uncheckedFrame;
            if (frame != null)
            {
                GUI.color = new Color(1f, 1f, 1f, enabled ? 1f : .45f);
                GUI.DrawTexture(check, frame);
                GUI.color = Color.white;
            }
            Lbl(34f, y - 1f, label, enabled ? gold : gray);
            if (enabled && GUI.Button(L(16f, y - 2f, 200f, 15f),
                GUIContent.none, GUIStyle.none))
            {
                onClick();
                // Existing view-only bridge: the native callback already owns Save/send/effects.
                owner.pendingPetViewMask = GameSettings.PetViewMask;
                owner.hasPendingPetViewMask = true;
                ev.Use();
            }
        }
    }

    private static Texture2D ToBlackIron(Texture2D source)
    {
        if (!source)
        {
            return null;
        }

        Color32[] pixels = source.GetPixels32();
        for (int index = 0; index < pixels.Length; index++)
        {
            Color32 value = pixels[index];
            float light = (value.b * 0.114f + value.r * 0.299f + value.g * 0.587f) /
                255f;
            pixels[index] = new Color32(
                (byte)(light * 158f),
                (byte)(light * 148f),
                (byte)(light * 132f),
                value.a);
        }

        Texture2D result = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false);
        result.name = "RecoveredWin05PetIron";
        result.SetPixels32(pixels);
        result.Apply(false, false);
        result.filterMode = FilterMode.Point;
        result.wrapMode = TextureWrapMode.Clamp;
        return result;
    }

    private void DisposePetDialog()
    {
        petDialogReady = false;
    }

    private static void DestroyOwnedTexture(Texture2D value)
    {
        if (!value)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

}
