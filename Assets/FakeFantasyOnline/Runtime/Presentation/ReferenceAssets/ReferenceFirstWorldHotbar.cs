using System;
using System.Collections.Generic;
using System.Text;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Historical icon-resolver compatibility surface. Live slots use the original GameHud.HotIcon.
/// </summary>
public sealed class ReferenceFirstWorldHotbarIconCatalog
{
    public const string SkillsTablePath = "tables/skills.json";

    private readonly ReferenceResourceLoader resources;
    private readonly ReferenceItemIconCatalog itemIcons;
    private readonly Dictionary<int, Texture2D> sourceIconCache =
        new Dictionary<int, Texture2D>();
    private readonly Dictionary<int, int> skillIcons = new Dictionary<int, int>();
    private bool skillIconsLoaded;

    public ReferenceFirstWorldHotbarIconCatalog(ReferenceResourceLoader resources)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        itemIcons = new ReferenceItemIconCatalog(resources);
    }

    public Texture2D Load(WorldHotbarSlotState slot)
    {
        if (slot == null || !slot.Occupied)
        {
            return null;
        }

        // GameHud.HotIcon first honors the non-zero icon supplied by the authoritative
        // twenty-slot snapshot. The official branch formats that value through the
        // unpadded item path and does not reinterpret it as an item identity.
        if (slot.IconId > 0)
        {
            return LoadSourceIcon(slot.IconId);
        }

        // With no server icon, kind 2 calls NetClient.LoadItemIcon and therefore consumes
        // the complete item/mount mapping tables rather than treating the item id as art.
        if (slot.Kind == WorldHotbarSlotKind.Item)
        {
            return itemIcons.Load(slot.Id);
        }

        // Character and pet skill fallbacks both resolve a skill identity to the icon
        // carried by the official skill data before using the same source-icon cache.
        // The current official table includes both families reached by our private server.
        EnsureSkillIcons();
        int skillIconId;
        return skillIcons.TryGetValue(slot.Id, out skillIconId) && skillIconId > 0
            ? LoadSourceIcon(skillIconId)
            : null;
    }

    private Texture2D LoadSourceIcon(int iconId)
    {
        Texture2D cached;
        if (sourceIconCache.TryGetValue(iconId, out cached))
        {
            return cached;
        }

        Texture2D texture = resources.LoadItemIconFirstVisible(
            string.Format("item/item/item{0}.gso", iconId));
        sourceIconCache[iconId] = texture;
        return texture;
    }

    private void EnsureSkillIcons()
    {
        if (skillIconsLoaded)
        {
            return;
        }

        skillIconsLoaded = true;
        byte[] bytes = resources.ReadSourceOwned(SkillsTablePath);
        if (bytes == null || bytes.Length == 0)
        {
            return;
        }

        try
        {
            SkillTable table = JsonUtility.FromJson<SkillTable>(
                Encoding.UTF8.GetString(bytes));
            if (table == null || table.skills == null)
            {
                return;
            }

            for (int index = 0; index < table.skills.Length; index++)
            {
                SkillRow row = table.skills[index];
                if (row != null && row.id > 0 && row.icon > 0)
                {
                    skillIcons[row.id] = row.icon;
                }
            }
        }
        catch (ArgumentException)
        {
            skillIcons.Clear();
        }
    }

    [Serializable]
    private sealed class SkillTable
    {
        public SkillRow[] skills;
    }

    [Serializable]
    private sealed class SkillRow
    {
        public int id;
        public int icon;
    }
}

/// <summary>
/// Existing top-layout accelerator view over the thirty-slot hotbar state.
/// </summary>
public sealed class ReferenceFirstWorldHotbar : IDisposable
{
    public const bool ShowByDefault = true;
    public const int SlotsPerRow = 10;

    private readonly ReferenceFirstWorldHudResources resources;
    private WorldHotbarState state = WorldHotbarState.Empty;
    private WorldHotbarState visualState = WorldHotbarState.Empty;
    private WorldPlayerPetStatusState countPet;
    private bool countPetBound;
    private GameHud hudOwner;
    private bool detachedShowHotbar = ShowByDefault;
    private bool showHotbar
    {
        get => hudOwner != null ? hudOwner.ShowHotbarForIndependentRuntime : detachedShowHotbar;
        set
        {
            if (hudOwner != null) hudOwner.ShowHotbarForIndependentRuntime = value;
            else detachedShowHotbar = value;
        }
    }
    internal void BindHudOwnerForIndependentRuntime(GameHud owner)
    {
        hudOwner = owner;
    }
    private Action activationObserver;
    internal void BindActivationObserverForIndependentRuntime(Action observer)
        => activationObserver = observer;

    /// <summary>
    /// The official GameHud.OnGUI (0x06000414) hotbar-body door: the body is entered only for
    /// showHotbar (+0x45) == 1 with accelbarbk (+0x248) non-null (arm64 lines 1457-1466; a failing
    /// test branches to LAB_0042c3a4 at line 1866, past both Tab arms). Every key decision inside
    /// the body inherits it, including the bottom Tab cycle arm inside DrawAt. Captured at
    /// the same read point, i.e. before
    /// the hide/plus/minus buttons can write showHotbar later in the same GUI pass, exactly as the
    /// native body reads the field once before those buttons run.
    /// </summary>
    internal bool NativeHotbarBodyForIndependentRuntime { get; private set; }
    private bool expanded { get => GameHud.HotbarExpandedForIndependentRuntime; set => GameHud.HotbarExpandedForIndependentRuntime = value; }
    private int hotbarRow { get => GameHud.HotbarRowForIndependentRuntime; set => GameHud.HotbarRowForIndependentRuntime = value; }
    private int pressedSlot { get => GameHud.PressSlotForIndependentRuntime; set => GameHud.PressSlotForIndependentRuntime = value; }
    private int heldSlotId => heldSourceSlot >= 0 ? GameHud.DragSkillIdForIndependentRuntime : 0;
    // Official GameHud.OnGUI (0x06000414) global release gate reads the raw dragSkillId
    // (GameHud static slot +0x68) and nothing else: arm64 06000414.c:1938-1944 `iVar5 =
    // *(int *)(...+0x68) ... if (iVar5 != 0)`, x86_64 06000414.c:2338 the same read whose zero
    // arm jumps to LAB_00442d02. It is not the source-slot-qualified projection heldSlotId,
    // because an externally picked-up skill carries dragFromSlot == -1 and is still cleared by
    // this tail (arm64 :1960-1965, x86_64 :2352-2358) while skipping SyncHotbar.
    private int heldDragSkillId => GameHud.DragSkillIdForIndependentRuntime;
    private int heldSlotIconId;
    private WorldHotbarSlotKind heldSlotKind => (WorldHotbarSlotKind)GameHud.DragKindForIndependentRuntime;
    private int heldSourceSlot => GameHud.DragFromSlotForIndependentRuntime;
    private Texture2D heldSlotIcon => GameHud.DragSkillIconForIndependentRuntime;
    private Texture2D externalHeldSkillIcon;
    internal void BindExternalHeldTextureForIndependentRuntime(Texture2D texture) => externalHeldSkillIcon = texture;
    private bool disposed;

    private ReferenceFirstWorldHotbar(ReferenceFirstWorldHudResources resources)
    {
        this.resources = resources;
        Ready = true;
    }

    internal static ReferenceFirstWorldHotbar CreateNativeView(ReferenceFirstWorldHudResources resources)
        => new ReferenceFirstWorldHotbar(resources);

    public bool Ready { get; private set; }

    public bool Visible => showHotbar;

    public bool Expanded => expanded;

    public int ActiveRow => hotbarRow;

    public WorldHotbarState State => state;
    internal WorldHotbarState VisibleStateForIndependentRuntime => visualState;

    public string HoverTooltip { get; private set; } = string.Empty;

    public static bool TryCreate(
        ReferenceResourceLoader loader,
        ReferenceFirstWorldHudResources resources,
        ReferenceGameHudItemCooldowns itemCooldowns,
        ReferenceGameHudSkillCooldowns skillCooldowns,
        out ReferenceFirstWorldHotbar owner)
    {
        owner = null;
        if (loader == null || resources == null || !resources.Ready ||
            itemCooldowns == null || !itemCooldowns.Ready ||
            skillCooldowns == null || !skillCooldowns.Ready)
        {
            return false;
        }

        owner = new ReferenceFirstWorldHotbar(resources);
        return true;
    }

    /// <summary>
    /// Binds the immutable UI mirror to the last published snapshot and nothing else.
    /// </summary>
    /// <remarks>
    /// This used to perform the official 0x8016 stores - the <c>CharData.petSkillIcons</c>
    /// <c>set_Item</c> gate and the <c>GameHud.LoadHotbar</c> tail call - and it no longer does.
    /// Official 0x8016 performs both inside its case body, once per received packet, and its
    /// dictionary is never cleared, so the owner of those stores is
    /// <c>IWorldReceiveOwner.ReceiveHotbar</c>, which the gateway calls at the packet's own
    /// arrival. A per-frame projection cannot stand in for it: this member is version-gated on the
    /// published snapshot, so two 0x8016 in one frame reach it once with the second packet only,
    /// and a row that the first packet carried would never be recorded. It also used to project
    /// the "no packet yet" sentinel: <c>WorldHotbarState.Empty</c> is the initial mirror value, so
    /// a forced sync with no 0x8016 ever received called <c>LoadHotbar</c> with thirty zeros and
    /// stored a hotbar the official client never stores.
    /// <para>
    /// The mirror is an observation of the received and drawn state. Drawing and drag
    /// decisions read the <c>GameHud</c> statics directly; a successful draw refreshes the
    /// public projection only after the native input and held-icon sequence finishes.
    /// </para>
    /// </remarks>
    public bool ApplyState(WorldHotbarState next)
    {
        if (!Ready || disposed || next == null ||
            next.Slots.Count != WorldHotbarState.SlotCount)
        {
            return false;
        }

        state = next;
        visualState = next;
        return true;
    }

    // Retain the existing host/Editor admission and pet-skill publication used by count and
    // availability. Tooltip content itself reads the recovered live owners; it retains no
    // inventory, pet or skill-window snapshot here.
    public bool ApplyTooltipSources(
        WorldInventoryState inventory,
        WorldPlayerPetStatusState pet,
        Win05 skills)
    {
        if (!Ready || disposed || inventory == null || skills == null ||
            !skills.SkillDialogReady)
        {
            return false;
        }

        BindCountInputs(pet);
        return true;
    }

    // Skill inputs for the recovered count/availability owners. Inventory fields are bound by
    // the real inventory view's ApplyState before GUI and item-use callbacks.
    private void BindCountInputs(WorldPlayerPetStatusState pet)
    {
        // Character skills already belong to the receipt owner. Tooltip snapshots can lag a
        // receipt and must not replace its complete or partially written native roots here.
        if (!countPetBound || !ReferenceEquals(countPet, pet))
        {
            CharData.BindPetSkillsForIndependentRuntime(pet);
            countPet = pet;
            countPetBound = true;
        }
    }

    /// <summary>
    /// Draws the recovered rows and invokes native UseHotSlot at the click/key before Event.Use.
    /// The world observes activation for its existing targeting/facing projections.
    /// </summary>
    public bool Draw(
        float elapsedSeconds,
        out int activatedSlot,
        out WorldHotbarState saveState)
    {
        bool releasedHeldSkill;
        return Draw(
            elapsedSeconds,
            0,
            0,
            out activatedSlot,
            out saveState,
            out releasedHeldSkill);
    }

    public bool Draw(
        float elapsedSeconds,
        int heldSkillId,
        int heldSkillIconId,
        out int activatedSlot,
        out WorldHotbarState saveState,
        out bool releasedHeldSkill)
    {
        return Draw(
            elapsedSeconds,
            heldSkillId,
            heldSkillIconId,
            WorldHotbarSlotKind.CharacterSkill,
            out activatedSlot,
            out saveState,
            out releasedHeldSkill);
    }

    public bool Draw(
        float elapsedSeconds,
        int heldSkillId,
        int heldSkillIconId,
        WorldHotbarSlotKind heldSkillKind,
        out int activatedSlot,
        out WorldHotbarState saveState,
        out bool releasedHeldSkill,
        int? viewportWidth = null,
        int? viewportHeight = null,
        bool observeVisibleState = true,
        bool nativeInvocation = false)
    {
        activatedSlot = -1;
        saveState = null;
        releasedHeldSkill = false;
        HoverTooltip = string.Empty;
        // The automatic GameHud caller retains native show/resource/row gates in DrawAt.
        // View/viewport/frame-time admission belongs only to explicit compatibility draws.
        if (!nativeInvocation && (!Ready || disposed || (viewportWidth ?? Screen.width) < 1 ||
            (viewportHeight ?? Screen.height) < 1 ||
            float.IsNaN(elapsedSeconds) || float.IsInfinity(elapsedSeconds) ||
            elapsedSeconds < 0f))
        {
            return false;
        }

        float scale = hudOwner != null ? hudOwner.s : UIScale.Factor;

        // These are stages of one native OnGUI call: preserve incoming GUI state and
        // each reached leaf's own normal/exceptional restoration behavior.
        DrawAt(
            viewportWidth ?? Screen.width,
            viewportHeight,
            scale,
            elapsedSeconds,
            heldSkillId,
            heldSkillIconId,
            heldSkillKind,
            nativeInvocation,
            out activatedSlot,
            out saveState,
            out releasedHeldSkill);
        GameHud.DrawDragIconForIndependentRuntime(hudOwner != null ? hudOwner.s : scale);

        // The explicit draw API retains its existing snapshot result. The original GUI
        // reads native slots and discards this DTO, so its caller does not request the
        // non-native allocation between DrawDragIcon and the following minimap stage.
        if (observeVisibleState)
        {
            ObserveVisibleState();
            if (saveState != null) saveState = visualState;
        }
        return true;
    }

    private void ObserveVisibleState()
    {
        for (int i = 0; i < WorldHotbarState.SlotCount; i++)
        {
            var slot = GameHud.HotSlotStateForIndependentRuntime(i);
            WorldHotbarSlotState shown = visualState.Slots[i];
            if (shown.Id != slot.id || (int)shown.Kind != slot.kind || shown.IconId != slot.serverIcon)
                visualState = visualState.WithSlot(i, slot.id, (WorldHotbarSlotKind)slot.kind, slot.serverIcon);
        }
        visualState = visualState.WithExpanded(GameHud.HotbarExpandedForIndependentRuntime);
    }

    /// <summary>
    /// Source translation of the GameHud.OnGUI held-icon release guard. Event type alone owns
    /// release; the original branch does not inspect the mouse button or require a valid slot.
    /// </summary>
    public static bool ShouldReleaseHeldSkill(int heldSkillId, EventType eventType)
    {
        return heldSkillId != 0 && eventType == EventType.MouseUp;
    }

    public bool MouseOver(
        Vector2 topOriginPointer,
        int screenWidth,
        float scale)
    {
        GameSettings.EnsureLoaded();
        return GameHud.HotbarContainsForIndependentRuntime(topOriginPointer, screenWidth, Screen.height,
            scale, GameSettings.HotbarBottom, showHotbar, expanded);
    }

    public static float HotbarLogicalX(int screenWidth, float scale) =>
        GameHud.HotbarXForIndependentRuntime(scale, screenWidth);

    public static Rect BackgroundRect(
        int screenWidth,
        float logicalY,
        float scale)
    {
        float x = HotbarLogicalX(screenWidth, scale);
        return new Rect(
            x * scale,
            logicalY * scale,
            428f * scale,
            45f * scale);
    }

    public static Rect ActiveEffectRect(
        int screenWidth,
        float logicalY,
        float scale)
    {
        float x = HotbarLogicalX(screenWidth, scale);
        return new Rect(
            (x - 3f) * scale,
            (logicalY - 2f) * scale,
            434f * scale,
            49f * scale);
    }

    public static Rect SlotRect(
        int column,
        int screenWidth,
        float logicalY,
        float scale)
    {
        if (column < 0 || column >= SlotsPerRow)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        float x = HotbarLogicalX(screenWidth, scale);
        return new Rect(
            (x + 5f + column * 40f) * scale,
            (logicalY + 3f) * scale,
            40f * scale,
            40f * scale);
    }

    public static Rect IconRect(
        int column,
        int screenWidth,
        float logicalY,
        float scale)
    {
        if (column < 0 || column >= SlotsPerRow)
        {
            throw new ArgumentOutOfRangeException(nameof(column));
        }

        float x = HotbarLogicalX(screenWidth, scale);
        return new Rect(
            (x + 10f + column * 40f) * scale,
            (logicalY + 7f) * scale,
            30f * scale,
            30f * scale);
    }

    public static Rect FunctionLabelsRect(
        int screenWidth,
        float logicalY,
        float scale,
        float textureHeight)
    {
        float x = HotbarLogicalX(screenWidth, scale);
        return new Rect(
            (x + 5f) * scale,
            logicalY * scale,
            400f * scale,
            textureHeight * scale);
    }

    public static Rect ButtonRect(
        int screenWidth,
        float scale,
        float logicalY)
    {
        float x = HotbarLogicalX(screenWidth, scale);
        return new Rect(
            (x + 403f) * scale,
            logicalY * scale,
            20f * scale,
            19f * scale);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Ready = false;
        state = WorldHotbarState.Empty;
        visualState = WorldHotbarState.Empty;
        countPet = null;
        countPetBound = false;
        externalHeldSkillIcon = null;
        activationObserver = null;
        HoverTooltip = string.Empty;
        pressedSlot = -1;
        ClearHeldHotbarSlot();
        disposed = true;
    }

    private void DrawAt(
        int screenWidth,
        int? viewportHeight,
        float scale,
        float elapsedSeconds,
        int heldSkillId,
        int heldSkillIconId,
        WorldHotbarSlotKind heldSkillKind,
        bool nativeInvocation,
        out int activatedSlot,
        out WorldHotbarState saveState,
        out bool releasedHeldSkill)
    {
        activatedSlot = -1;
        saveState = null;
        Event current = Event.current;
        releasedHeldSkill = false;
        bool droppedHeldSlot = false;
        GameSettings.EnsureLoaded();
        bool drewRows = showHotbar && resources.AccelBarBackground != null;
        NativeHotbarBodyForIndependentRuntime = drewRows;
        bool bottom = false;
        if (drewRows)
        {
            GameHud.BeginHotbarRowsForIndependentRuntime();
            bottom = GameSettings.HotbarBottom && resources.BottomHotbarBackground != null;
            float sh = bottom ? GameHud.BottomHotbarScaleForIndependentRuntime(scale, screenWidth) : scale;
            float x = bottom ? GameHud.BottomHotbarXForIndependentRuntime(sh, screenWidth) : HotbarLogicalX(screenWidth, sh);
            float y0 = bottom ? (viewportHeight ?? Screen.height) + sh * -50f : 0f;
            float y1 = bottom ? y0 + sh * -45f : 45f * sh;
            float buttonX = (x + (bottom ? 815f : 403f)) * sh;
            float hideY = bottom ? y0 + sh * -21f : 4f * sh;
            float plusY = bottom ? y0 + sh * -40f : 23f * sh;
            DrawRow(0, y0, screenWidth, sh, bottom, x, nativeInvocation,
                ref activatedSlot, ref saveState, ref droppedHeldSlot);
            if (GameHud.HotbarButtonForIndependentRuntime(resources.AccelToggle,
                new Rect(buttonX, hideY, 20f * sh, 19f * sh))) showHotbar = false;
            if ((!bottom || !expanded) && GameHud.HotbarButtonForIndependentRuntime(resources.AccelPlus,
                new Rect(buttonX, plusY, 20f * sh, 19f * sh)))
            {
                expanded = true;
                saveState = visualState;
                GameHud.SyncHotbarForIndependentRuntime();
            }
            if (expanded)
            {
                DrawRow(1, y1, screenWidth, sh, bottom, x, nativeInvocation,
                    ref activatedSlot, ref saveState, ref droppedHeldSlot);
                if (GameHud.HotbarButtonForIndependentRuntime(resources.AccelMinus,
                    new Rect(buttonX, bottom ? plusY : y1 + 4f * sh, 20f * sh, 19f * sh)))
                {
                    expanded = false;
                    hotbarRow = 0;
                    saveState = visualState;
                    GameHud.SyncHotbarForIndependentRuntime();
                }
                if (!bottom)
                {
                    GameHud.HotbarButtonForIndependentRuntime(resources.AccelHelp,
                        new Rect(buttonX, y1 + 23f * sh, 20f * sh, 19f * sh));
                    if (current != null && current.type == EventType.KeyDown && current.keyCode == KeyCode.Tab)
                    {
                        hotbarRow = 1 - hotbarRow;
                        current.Use();
                    }
                }
            }
            else hotbarRow = 0;
        }
        else if (resources.AccelShowButton != null)
        {
            bool bottomToggle = GameSettings.HotbarBottom && resources.BottomHotbarBackground != null;
            float sh = bottomToggle ? GameHud.BottomHotbarScaleForIndependentRuntime(scale, screenWidth) : scale;
            float x = bottomToggle ? GameHud.BottomHotbarXForIndependentRuntime(sh, screenWidth) : HotbarLogicalX(screenWidth, sh);
            Rect toggle = new Rect((x + (bottomToggle ? 815f : 403f)) * sh,
                bottomToggle ? (viewportHeight ?? Screen.height) - 21f * sh : 4f * sh,
                20f * sh, 19f * sh);
            if (GameHud.HotbarButtonForIndependentRuntime(resources.AccelShowButton, toggle)) showHotbar = true;
        }

        // Preserve the original hotbar-body tail: pickup, press reset, bottom Tab,
        // F-key action/Use, then the unconditional held-release and DrawDragIcon tail.
        if (drewRows)
        {
            BeginHeldHotbarSlotDrag(current, heldSkillId);
            if (current != null && current.type == EventType.MouseUp) pressedSlot = -1;
            if (current != null && current.type == EventType.KeyDown)
            {
                if (bottom && hudOwner != null)
                    hudOwner.HandleTabCycleKeyForIndependentRuntime();
                bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                int keySlot = GameHud.HotbarKeySlotForIndependentRuntime(current.keyCode, bottom, alt);
                if (keySlot >= 0)
                {
                    activatedSlot = keySlot;
                    GameHud.UseHotSlot(keySlot);
                    current.Use();
                    if (!nativeInvocation) activationObserver?.Invoke();
                }
            }
        }

        releasedHeldSkill = heldSkillId != 0 && (droppedHeldSlot ||
            current != null && ShouldReleaseHeldSkill(heldSkillId, current.type));
        if (current != null && current.type == EventType.MouseUp && heldDragSkillId != 0)
        {
            if (heldSourceSlot >= 0)
            {
                saveState = ReleaseHeldHotbarSlot();
                GameHud.SyncHotbarForIndependentRuntime();
            }
            else ClearHeldHotbarSlot();
        }
    }

    private void DrawRow(
        int row,
        float pixelY,
        int screenWidth,
        float scale,
        bool bottom,
        float x,
        bool nativeInvocation,
        ref int activatedSlot,
        ref WorldHotbarState saveState,
        ref bool droppedHeldSlot)
    {
        // The resource view already borrows the original GameHud used by this renderer.
        GameHud owner = resources.OwnerForIndependentRuntime;
        if (bottom)
            owner.DrawBottomHotbarChromeForIndependentRuntime(row, x, pixelY, scale);
        else
            owner.DrawTopHotbarChromeForIndependentRuntime(row, x, pixelY, scale);

        Event current = Event.current;
        int columns = bottom ? GameHud.BottomHotbarRowCountForIndependentRuntime(row) : SlotsPerRow;
        int firstSlot = row == 0 ? 0 : SlotsPerRow;
        for (int column = 0; column < columns; column++)
        {
            int slotIndex = firstSlot + column;
            Rect hit = bottom
                ? GameHud.BottomHotbarSlotRectForIndependentRuntime(row, column, x, pixelY, scale)
                : new Rect((x + 5f + column * 40f) * scale, pixelY + 3f * scale, 40f * scale, 40f * scale);
            GameHud.BindHotbarRectForIndependentRuntime(slotIndex, hit);
            Rect iconArea = bottom
                ? GameHud.BottomHotbarIconRectForIndependentRuntime(row, hit, scale)
                : new Rect(hit.x + 5f * scale, hit.y + 4f * scale, 30f * scale, 30f * scale);
            if (GameHud.hotSkill[slotIndex] != 0)
            {
                bool usable = GameHud.HotSlotUsableForIndependentRuntime(slotIndex);
                Texture2D icon = GameHud.HotIconForIndependentRuntime(slotIndex);
                owner.DrawHotSlotIconForIndependentRuntime(slotIndex, iconArea, icon, usable, scale);
                if (GameHud.hotKind[slotIndex] == 0 || GameHud.hotKind[slotIndex] == 2)
                    GameHud.DrawCdOverlay(iconArea, GameHud.hotSkill[slotIndex]);
                GameHud.DrawHotSlotCountForIndependentRuntime(slotIndex, iconArea, scale);
            }

            // 06000461 captures Event once per row, but reads its pointer after each
            // slot's render work, including empty slots. A reached null event throws.
            Vector2 pointer = current.mousePosition;
            if (hit.Contains(pointer) &&
                GameHud.hotSkill[slotIndex] != 0 && heldDragSkillId == 0)
            {
                // Official accelerator dragType 0 resolves the current slot and content at
                // the hover read, sharing the same live roots as count and availability.
                HoverTooltip = TooltipContent.Get(0, slotIndex);
                bool growthSkill = GameHud.hotKind[slotIndex] != 1 && GameHud.hotKind[slotIndex] != 2 &&
                    EndgamePanel.GrowthState != null && EndgameSkillRankView.IsProfessionSkill(GameHud.hotSkill[slotIndex]);
                if (growthSkill) UITip.ShowSkill(HoverTooltip, scale);
                else UITip.Show(HoverTooltip, scale);
            }

            if (hit.Contains(pointer))
            {
                if (current.type == EventType.MouseUp && heldDragSkillId != 0)
                {
                    // Read the raw native held state here. A previous row action can
                    // change it in this same GUI event; entry-time mirrors cannot decide.
                    int dropId = heldDragSkillId;
                    int dropKind = GameHud.DragKindForIndependentRuntime;
                    int source = heldSourceSlot;
                    GameHud.DropHotbarSlotForIndependentRuntime(slotIndex, dropId,
                        GameHud.DragSkillIconForIndependentRuntime, dropKind, source);
                    ClearHeldHotbarSlot();
                    GameHud.SyncHotbarForIndependentRuntime();
                    current.Use();

                    // Mark the existing save observation; its DTO is built after the
                    // complete native hotbar/held-icon sequence returns successfully.
                    saveState = visualState;
                    droppedHeldSlot = true;
                    continue;
                }

                if (current.type == EventType.MouseDown &&
                    GameHud.hotSkill[slotIndex] != 0 &&
                    (current.button == 0 || current.button == 1))
                {
                    bool activated = false;
                    if (current.button == 1)
                    {
                        activatedSlot = slotIndex;
                        GameHud.UseHotSlot(slotIndex);
                        activated = true;
                    }
                    else if (current.clickCount >= 2)
                    {
                        activatedSlot = slotIndex;
                        GameHud.UseHotSlot(slotIndex);
                        pressedSlot = -1;
                        activated = true;
                    }
                    else
                    {
                        pressedSlot = slotIndex;
                    }

                    current.Use();
                    // The explicit compatibility draw retains its successful-suffix mirror.
                    // Native Player animation already owns and observes facing separately.
                    if (activated && !nativeInvocation) activationObserver?.Invoke();
                }
            }
        }

        if (bottom)
            GameHud.DrawBottomHotbarLabelsForIndependentRuntime(row, x, pixelY, scale,
                row == 0 ? resources.BottomHotbarText : resources.BottomHotbarText2);
        else
            GameHud.DrawTopHotbarLabelsForIndependentRuntime(pixelY, screenWidth, scale, resources.AccelText);
    }

    private void BeginHeldHotbarSlotDrag(Event current, int externalHeldSkillId)
    {
        if (current == null || current.type != EventType.MouseDrag ||
            externalHeldSkillId != 0 || heldSlotId != 0 || pressedSlot < 0 ||
            pressedSlot >= WorldHotbarState.SlotCount)
        {
            return;
        }

        if (PickupHeldHotbarSlot(pressedSlot))
        {
            current.Use();
        }
    }

    private bool PickupHeldHotbarSlot(int source)
    {
        if (source < 0 || source >= WorldHotbarState.SlotCount || heldSlotId != 0)
        {
            return false;
        }

        if (GameHud.hotSkill[source] == 0)
        {
            return false;
        }

        GameHud.BeginSlotDragForIndependentRuntime(source);
        heldSlotIconId = GameHud.HotSlotStateForIndependentRuntime(source).serverIcon;
        return true;
    }

    private WorldHotbarState DropHeldSlot(
        int target,
        int heldId,
        int heldIconId,
        WorldHotbarSlotKind heldKind,
        int source)
    {
        GameHud.DropHotbarSlotForIndependentRuntime(target, heldId,
            source >= 0 ? heldSlotIcon : externalHeldSkillIcon, (int)heldKind, source);
        WorldHotbarSlotState replaced = visualState.Slots[target];
        WorldHotbarState next = visualState.WithSlot(
            target,
            heldId,
            heldKind,
            heldIconId);
        if (source >= 0 && source != target && replaced.Occupied)
        {
            next = next.WithSlot(
                source,
                replaced.Id,
                replaced.Kind,
                replaced.IconId);
        }

        return next;
    }

    private WorldHotbarState ReleaseHeldHotbarSlot()
    {
        WorldHotbarState save = heldSourceSlot < 0 ? null : visualState;
        ClearHeldHotbarSlot();
        return save;
    }

    private void ClearHeldHotbarSlot()
    {
        GameHud.ClearDragForIndependentRuntime();
        heldSlotIconId = 0;
    }

    internal bool PickupHeldHotbarSlotForAudit(int source)
    {
        bool pickedUp = PickupHeldHotbarSlot(source);
        if (pickedUp) ObserveVisibleState();
        return pickedUp;
    }

    internal WorldHotbarState DropHeldHotbarSlotForAudit(int target)
    {
        if (heldSlotId <= 0)
        {
            return null;
        }

        WorldHotbarState next = DropHeldSlot(
            target,
            heldSlotId,
            heldSlotIconId,
            heldSlotKind,
            heldSourceSlot);
        visualState = next;
        ClearHeldHotbarSlot();
        return next;
    }

    internal WorldHotbarState ReleaseHeldHotbarSlotForAudit()
    {
        return heldSlotId <= 0 ? null : ReleaseHeldHotbarSlot();
    }

    internal string HotSlotTipForAudit(int index)
    {
        return TooltipContent.Get(0, index);
    }
}

}
