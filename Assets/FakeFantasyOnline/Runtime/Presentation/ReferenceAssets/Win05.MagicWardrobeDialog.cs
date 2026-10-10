using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the official Win05 magicstoredlg branch.
/// </summary>
public partial class Win05
{
    public const string MagicWardrobeDialogName = "magicstoredlg";
    public const string MagicWardrobeBackgroundPath =
        "res/uires/magicstore/magicstore.gsa";
    public const string MagicWardrobeTitle = "魔法衣橱";
    public const float MagicWardrobeDialogWidth = 540f;
    public const float MagicWardrobeDialogHeight = 302f;
    public const int MagicWardrobeVoucherItemId = 0x350c;

    private const int MagicWardrobeCategoryCount = 5;
    private const int MagicWardrobeSlotCount = 100;
    private const float MagicWardrobeSourceWidth = 448f;
    private const float MagicWardrobeCellPixels = 35f;
    private const float MagicWardrobeStridePixels = 38f;
    private const float MagicWardrobeDragThreshold = 6f;
    private const string MagicWardrobeNoVoucherMessage =
        "没有【魔法衣橱提取凭证】, 无法取出。\n请到神奇货架购买(功能道具组)。";
    private const string MagicWardrobeExtractPrefix = "取出【";
    private const string MagicWardrobeExtractSuffix =
        "】退回背包?\n需消耗 1 个魔法衣橱提取凭证(神奇货架有售)。";
    private const string MagicWardrobeWornSuffix = " (穿着中)";
    private const string MagicWardrobeInstruction =
        "\n左键穿/脱 · 右键取出(消耗1提取凭证)";



    private ReferenceSystemMessageBox magicMessageBox;
    private bool ownsMagicMessageBox;
    private ReferenceItemIconCatalog magicItemIcons;
    // WBD-068: magicCat and magicScroll are the third and fourth PRIVATE STATIC members of this
    // same official group and were the last two still declared instance here. Frozen index fields
    // rows (owner 'Win05', flags 17 / is_static 1): magicCat +0x70 field 0x040016df, magicScroll
    // +0x74 0x040016e0. The identical 0x70/0x74 numbers in the x86_64 image are NOT this pair - the
    // frozen rows put itemIconCache at +0x84 and petSkillIconCache at +0x8c with flags 33, and the
    // arm64/x86_64 field tables agree once those blocks are expanded. The bounded WBD-068 re-run of
    // the WBD-064 method-extent scanner with +0x70 added to its watch set
    // (Build/WBD-068/dis-arm64-win05-cat.txt, dis-x86-win05-cat.txt; frozen slices
    // GameAssembly.arm64 / GameAssembly.x86_64, rva == __TEXT file offset) still reports "scanned
    // 94 Win05* methods; 3 touch Win05 static drag/scroll fields" and adds only the +0x70 accesses
    // the WBD-064 watch set could not name. Every one of them is in DrawMagicStore 0600104f:
    // the category-selected Edge test reads magicCat (arm64 0x3be244 / 0x3be268, x86_64 0x3d2105 /
    // 0x3d2126), the MouseDown hover gate reads it again (arm64 0x3be4d4 / 0x3be4f0, x86_64
    // 0x3d23bd / 0x3d23ea), and the click arm stores it (arm64 0x3be344 / 0x3be36c, x86_64
    // 0x3d21ed / 0x3d2212). magicScroll is written in the same click arm as one eight-byte zero
    // (arm64 0x3be3a8, x86_64 0x3d2258), written back on every pass by GUI.BeginScrollView (arm64
    // 0x3be99c, x86_64 0x3d290b) and handed to NativeScroll.BarAt 060006ae as its ref scroll
    // (0600104f.c:675-677), which is why the product passing `ref magicScroll` into NativeScroll.BarAt
    // is C#-legal with a static field and needs no `this.`. No other Win05* method writes either
    // field. Win05..cctor 0600105e initialises neither: the one group store an exhaustive
    // field-instruction sweep of its 6804-byte arm64 body can find is the magicDragFrom = -1 that
    // WBD-065 registered (0x3c7a58), and the x86_64 body is the same, so both fields keep the CLR
    // zero default and there is no second initialiser to add.
    private static Vector2 magicScroll;
    // WBD-065: in official 1.5.8 these five are PRIVATE STATIC members of Win05, not instance
    // members. Frozen index fields rows (owner 'Win05', flags 17 / is_static 1): magicDragFrom
    // +0x7c 0x040016e1, magicDragId +0x80 0x040016e2, magicDragIcon +0x88 0x040016e3,
    // magicDragDown +0x90/+0x94 0x040016e4, magicDragMoved +0x98 0x040016e5. Win05..cctor
    // 0x0600105e (arm64 0x3c6720+6804, x86_64 0x3dae20+7408) initialises exactly one of them,
    // magicDragFrom = -1 (arm64 0x3c7a58, x86_64 0x3dc269), and the dual-architecture scan of all
    // 94 Win05* method extents (Build/WBD-064/dis-arm64-win05-static.txt and its x86_64 twin)
    // finds no other writer outside DrawMagicStore 0x0600104f: the MouseDown arm writes all five
    // and the two MouseUp clear blocks write three (From/Icon/Moved). Official Win05 owns no
    // OnDestroy at all - only Awake 0x06001038 ("Win05.I = this") - and nothing clears this group
    // when a world is released, so the values live for the process and survive every rebuilt
    // Win05 instance. Declaring them instance fields dropped them at each map change, which is a
    // reachable behavioural difference and not an implementation style choice.
    private static Vector2 magicDragDown;
    private static Texture2D magicDragIcon;
    private static int magicCat;
    private static int magicDragId;
    private static bool magicDragMoved;
    private bool magicWardrobeDialogReady;

    public bool MagicWardrobeDialogReady => magicWardrobeDialogReady;
    public bool MagicWardrobeDialogVisible =>
        string.Equals(cur, MagicWardrobeDialogName, StringComparison.Ordinal);
    public WorldWardrobeState MagicWardrobeState => magicWardrobeState?.State;

    private bool TryInitializeMagicWardrobeDialog(
        ReferenceSystemMessageBox messageBox,
        bool ownsMessageBox)
    {
        if (messageBox == null || !messageBox.Ready)
        {
            return false;
        }

        // magicstoredlg's background is resolved on every draw by Bk(def) (g2021 ->
        // LoadGsa2021First) with the official PatchPanel + Def title fallback, and official never
        // compares its size: OnGUI 06001042 draws it stretched into L(0, 0, def.w, def.h), so the
        // 448x302 source check and the null check are both already dropped here.
        // WBB-054 drops the last art condition on this step, the focus texture. Official
        // DrawMagicStore 0600104f owns no such field and asks at the draw site instead:
        // `InventoryDlg.get_FocusBk()` (0600050c, arm64 :609) on every pass, then
        // `Object.op_Inequality(texture, null)` with `Texture2D.whiteTexture` substituted when it
        // resolves null (arm64 :613-621). So a missing res/effect/focusbk0.gsa draws a white box
        // under the worn item and never reaches this step - before this packet it made
        // TryInitializeSkillDialog fail, which reaches ReferenceFirstWorldWindowOwner.cs:1132 and
        // ReleaseOwnedState(), i.e. it released the whole world.
        magicMessageBox = messageBox;
        ownsMagicMessageBox = ownsMessageBox;
        magicItemIcons = new ReferenceItemIconCatalog(skillResources);
        magicWardrobeDialogReady = true;
        return true;
    }

    public bool ApplyMagicWardrobeState(WorldWardrobeState state) =>
        ApplyMagicWardrobeObservation(state);

    internal bool ApplyMagicWardrobeObservation(IWorldWardrobeObservation state)
    {
        if (state == null)
        {
            return false;
        }

        // WBD-064 removed the product-only arm that used to follow this assignment:
        // `if (magicDragId > 0 && state.Find(magicDragId) == null) ClearMagicWardrobeDrag();`.
        // Official 1.5.8 has no receive-side drag test at all. DrawMagicStore 0600104f reads
        // Win05's static wardrobe list itself on every pass (0600104f.c:228, Win05 static +0x68)
        // and the only receiver, Win05.SetWardrobe 06001039, writes just wardrobeCap (+0x60) and
        // that list (+0x68) - no drag field is touched when a snapshot arrives
        // (NetClient.Dispatch is its single caller, arm64 0x256fa0 / x86_64 0x260717, case
        // 0x8038). The bounded dual-architecture scan of all 94 Win05* methods settles the chain:
        // magicDragId (+0x80 Win05 static) is written once, in the MouseDown arm
        // (arm64 0x3bf3e8 / x86_64 0x3d33fa), and read only for the two mouse-up sends
        // (arm64 0x3bf984 Move / 0x3bfacc Wear, x86_64 0x3d39e4 / 0x3d3ba1), both behind the
        // magicDragFrom (+0x7c) >= 0 gate. So an id that leaves the snapshot stays referenced
        // until the drag ends - which is exactly the official behaviour, where the server alone
        // decides a worn, blocked or vanished item.
        magicWardrobeState = state;
        return true;
    }

    public bool ApplyMagicWardrobeInventory(WorldInventoryState state)
    {
        if (!magicWardrobeDialogReady || state == null)
        {
            return false;
        }

        return true;
    }

    public bool TryConsumeWardrobeStore(out byte bagTab, out int bagSlot)
    {
        // The removed adapter queue had no producer; preserve its empty result.
        bagTab = 0;
        bagSlot = -1;
        return false;
    }

    // The wardrobe wear and move actions are not dequeued here: the official grid input branch
    // (Win05.DrawMagicStore 0x0600104f) sends NetClient.WardrobeWear / NetClient.WardrobeMove
    // itself inside the mouse-up handling, so a dequeue arm would send a second time.
    private void DrawMagicStore(Func<float, float, float, float, Rect> L, Def d, Event ev)
    {
        float contentScale = d.w / MagicWardrobeSourceWidth;
        DrawMagicWardrobeCategories(L, contentScale, ev);
        List<WardrobeItem> items = BuildMagicWardrobeCategory();
        DrawMagicWardrobeSummary(L, contentScale, items.Count);
        DrawMagicWardrobeGrid(L, contentScale, ev, items);
    }

    private void DrawMagicWardrobeCategories(
        Func<float, float, float, float, Rect> L,
        float contentScale,
        Event current)
    {
        for (int index = 0; index < MagicWardrobeCategoryCount; index++)
        {
            Rect rect = L(
                contentScale * 27f,
                51f + index * 40f,
                contentScale * 36f,
                38f);
            if (index == magicCat)
            {
                Edge(rect, new Color(1f, 0.85f, 0.30f, 1f));
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                // WBD-067: the official category click arm of DrawMagicStore 0x0600104f writes
                // exactly two statics and then eats the click, in this order - magicCat (+0x70):
                // arm64 0x3be344 / 0x3be36c (`str w21,[x8,#0x70]`), x86_64 0x3d21ed / 0x3d2212
                // (`mov dword ptr [rax+0x70],r13d`); magicScroll (+0x74) = zero read from that
                // type's static: arm64 0x3be3a8 `stur d0,[x8,#0x74]`, x86_64 0x3d2258
                // `movsd qword ptr [rax+0x74],xmm0`; Event.Use() after the event's own null throw:
                // arm64 0x3be3b8, x86_64 0x3d226c. The arm contains no store to +0x7c, +0x80,
                // +0x88, +0x90 or +0x98 (read fully: arm64 0x3be328-0x3be3bc, x86_64
                // 0x3d21d3-0x3d226c), so the product-only ClearMagicWardrobeDrag() is removed and
                // magicCat/magicScroll stay official: switching category does not abandon a drag,
                // the drag survives until DrawMagicStore's own MouseUp arm clears it.
                // WBD-068: both targets are static now, matching the frozen fields rows, so this
                // click also no longer writes a per-instance copy that official keeps for the
                // process; the values persist across a rebuilt world root exactly as they do there.
                magicCat = index;
                magicScroll = Vector2.zero;
                current?.Use();
            }
        }
    }

    private static List<WardrobeItem> BuildMagicWardrobeCategory()
    {
        var items = new List<WardrobeItem>();
        foreach (WardrobeItem item in wardrobe)
            if (item.cat == magicCat) items.Add(item);
        return items;
    }

    private void DrawMagicWardrobeSummary(
        Func<float, float, float, float, Rect> L, float contentScale, int itemCount)
    {
        // DrawMagicStore0600104f gates both counters and the total measurement together.
        if (!BitmapFont.Ready) return;
        string current = string.Format("当前收纳 {0}/{1}", itemCount,
            wardrobeCap > 0 ? wardrobeCap : 100);
        string total = string.Format("总收纳 {0} 件", wardrobe.Count);
        Rect currentRect = L(contentScale * 90f, 43f, 0f, 0f);
        BitmapFont.Draw(currentRect.x, currentRect.y, current, new Color(1f, 0.85f, 0.30f, 1f), s);
        Rect totalRect = L(contentScale * 412f, 43f, 0f, 0f);
        float totalWidth = BitmapFont.Measure(total, s);
        BitmapFont.Draw(totalRect.x - totalWidth, totalRect.y, total,
            new Color32(200, 195, 175, 255), s);
    }

    private void DrawMagicWardrobeGrid(
        Func<float, float, float, float, Rect> L,
        float contentScale,
        Event current,
        List<WardrobeItem> items)
    {
        Rect viewport = L(
            contentScale * 81f,
            58f,
            contentScale * 340f,
            196f);
        int columns = Math.Max(
            9,
            (int)((contentScale * 340f - 18f) / 38f));
        int rows = (MagicWardrobeSlotCount + columns - 1) / columns;
        float viewScale = s;
        float contentHeight = rows * MagicWardrobeStridePixels * viewScale;

        magicScroll = GUI.BeginScrollView(
            viewport,
            magicScroll,
            new Rect(0f, 0f, viewport.width - 16f * viewScale, contentHeight),
            GUIStyle.none,
            GUIStyle.none);
        // Visible-row hover is tested in BeginScrollView's transformed event space.
        // It is distinct from the later SlotUnder hit test after the scroll bar.
        int hovered = -1;
        for (int index = 0; index < MagicWardrobeSlotCount; index++)
        {
            int column = index % columns;
            int row = index / columns;
            float cellScale = s;
            float cell = MagicWardrobeCellPixels * cellScale;
            Rect rect = new Rect(column * MagicWardrobeStridePixels * cellScale,
                row * MagicWardrobeStridePixels * cellScale, cell, cell);
            // Original culls the entire row before border, item lookup or hover.
            if (rect.yMax < magicScroll.y || rect.y > magicScroll.y + viewport.height)
                continue;
            bool isHovered = rect.Contains(current.mousePosition);
            Edge(rect, new Color(1f, 1f, 1f, 0.13f));
            if (index >= items.Count)
            {
                continue;
            }

            WardrobeItem item = items[index];
            if (item.worn)
            {
                FillMagicWardrobeRect(rect, new Color(1f, 0.82f, 0.25f, 0.30f));
            }
            else if (isHovered)
            {
                FillMagicWardrobeRect(rect, new Color(1f, 1f, 1f, 0.16f));
            }

            Texture2D icon = item.icon;
            float inset = rect.width * 4f / 36f;
            Rect iconRect = new Rect(rect.x + inset, rect.y + inset,
                rect.width - inset * 2f, rect.height - inset * 2f);
            if (icon)
            {
                ReferenceItemIconCatalog.Draw(iconRect, icon, s);
            }
            else if (BitmapFont.Ready && !string.IsNullOrEmpty(item.name))
            {
                float textScale = s;
                string fallback = item.name.Substring(
                    0,
                    Math.Min(2, item.name.Length));
                BitmapFont.Draw(
                    rect.x + 4f * textScale,
                    rect.y + 10f * textScale,
                    fallback,
                    Color.white,
                    s);
            }

            if (item.blocked)
            {
                Texture2D focus = InventoryDlg.FocusBk;
                Color saved = GUI.color;
                GUI.color = InventoryDlg.ItemFocusBkColor;
                GUI.DrawTexture(iconRect, focus != null ? focus : Texture2D.whiteTexture);
                GUI.color = saved;
            }
            if (item.worn)
            {
                Edge(rect, new Color(1f, 0.85f, 0.30f, 1f));
                Edge(new Rect(rect.x + 1f, rect.y + 1f,
                    rect.width - 2f, rect.height - 2f), new Color(1f, 0.85f, 0.30f, 1f));
            }
            if (isHovered) hovered = index;
        }
        GUI.EndScrollView();

        // Official Win05.DrawMagicStore 0x0600104f (arm64 0x3bf044-0x3bf108, x86_64
        // 0x3d303f-0x3d3109): after EndScrollView it localises the track itself with
        // L(contentScale*432.5-7.5, 58, 15, 196) and calls NativeScroll.BarAt(060006ae) with
        // the grid viewport height and rows*38*scale, not a wider 198-tall area box through
        // NativeScroll.Bar. Both parametrisations make the same track rectangle, but Bar hands
        // BarAt the area height while official hands it the viewport height, and BarAt's
        // `maximum = contentH - viewH` (early return `contentH <= viewH + 1f`) makes that
        // two-scale difference observable in the clamp, the jump target and the thumb travel.
        Rect scrollTrack = L(
            contentScale * 432.5f - 7.5f,
            58f,
            15f,
            196f);
        NativeScroll.BarAt(
            this,
            scrollTrack,
            viewport.height,
            contentHeight,
            ref magicScroll);

        Vector2 pointer = current.mousePosition;
        HandleMagicWardrobeInput(
            current,
            pointer,
            viewport,
            columns,
            items);
        DrawMagicWardrobeDrag(pointer, viewport, columns, MagicWardrobeCellPixels * s, s);
        if (hovered >= 0 && hovered < items.Count)
        {
            WardrobeItem item = items[hovered];
            string title = item.name +
                (item.worn ? MagicWardrobeWornSuffix : string.Empty);
            RequestSkillTipForIndependentRuntime(
                TooltipContent.Fmt(title, item.desc) + MagicWardrobeInstruction, s);
        }
    }

    private void HandleMagicWardrobeInput(
        Event current,
        Vector2 pointer,
        Rect viewport,
        int columns,
        List<WardrobeItem> items)
    {
        if (current == null)
        {
            return;
        }

        // Official Win05.DrawMagicStore 0x0600104f takes the slot index inside the MouseDown arm
        // itself (arm64 0x3bf17c-0x3bf238, x86_64 0x3d3173-0x3d3249): the viewport Contains test
        // and SlotUnder run after GUI.EndScrollView and after NativeScroll.Bar has written this
        // frame's scroll, so the index is taken against the scroll of the current frame, not
        // against the value the scroll view started with.
        if (current.type == EventType.MouseDown)
        {
            int hovered = SlotUnder(pointer, viewport, columns);
            if (hovered >= 0 && hovered < items.Count)
            {
                WardrobeItem item = items[hovered];
                if (current.button == 0)
                {
                    magicDragFrom = hovered;
                    magicDragId = item.id;
                    magicDragIcon = item.icon;
                    magicDragDown = pointer;
                    magicDragMoved = false;
                    current.Use();
                }
                else if (current.button == 1)
                {
                    BeginMagicWardrobeExtract(item);
                    current.Use();
                }
                return;
            }
        }

        if (magicDragFrom >= 0 && !magicDragMoved &&
            current.type == EventType.MouseDrag &&
            Vector2.Distance(pointer, magicDragDown) > s * MagicWardrobeDragThreshold)
        {
            magicDragMoved = true;
        }

        if (magicDragFrom >= 0 && current.type == EventType.MouseUp &&
            current.button == 0)
        {
            int target = SlotUnder(pointer, viewport, columns);
            // Official Win05.DrawMagicStore 0x0600104f: both grid actions read NetClient.I once and
            // send from the mouse-up branch itself, before the drag state is cleared and before
            // Event.Use. There is no deferred queue, no dialog-readiness test and no id/range test;
            // worn, blocked and cross-category outcomes are decided by the server.
            int capturedItemId = magicDragId;
            if (magicDragMoved)
            {
                if (target >= 0 && target != magicDragFrom)
                {
                    NetClient.I?.WardrobeMoveForIndependentRuntime(capturedItemId, target);
                }
            }
            else
            {
                NetClient.I?.WardrobeWearForIndependentRuntime(capturedItemId);
            }

            ClearMagicWardrobeDrag();
            current.Use();
        }
        else if (magicDragFrom >= 0 && current.rawType == EventType.MouseUp)
        {
            ClearMagicWardrobeDrag();
        }
    }

    // Official Win05+<>c__DisplayClass105_1.<DrawMagicStore>b__2 (0x06001071, arm64 0x3d2be0 /
    // x86_64 0x3e6eb0) is this affirmative callback and nothing else: read NetClient.I once and,
    // only when it is non-null, send the captured item id. There is no dialog-readiness test, no
    // id-range test and no deferred hand-off, so the send stays inside the confirmation.
    private void BeginMagicWardrobeExtract(WardrobeItem item)
    {
        if (InventoryDlg.ItemCount(MagicWardrobeVoucherItemId) < 1)
        {
            ShowMagicMessageForIndependentRuntime(MagicWardrobeNoVoucherMessage, null, null);
            return;
        }

        int capturedItemId = item.id;
        ShowMagicMessageForIndependentRuntime(
            MagicWardrobeExtractPrefix + item.name +
                MagicWardrobeExtractSuffix,
            () => NetClient.I?.WardrobeRemoveForIndependentRuntime(capturedItemId),
            null);
    }

    private void DrawMagicWardrobeDrag(
        Vector2 pointer,
        Rect viewport,
        int columns,
        float cell,
        float scale)
    {
        if (magicDragFrom < 0 || !magicDragMoved)
        {
            return;
        }

        int target = SlotUnder(pointer, viewport, columns);
        if (target >= 0)
        {
            int column = target % columns;
            int row = target / columns;
            Rect targetRect = new Rect(
                viewport.x + column * MagicWardrobeStridePixels * scale - magicScroll.x,
                viewport.y + row * MagicWardrobeStridePixels * scale - magicScroll.y,
                cell,
                cell);
            if (viewport.Contains(targetRect.center))
                Edge(targetRect, new Color(0.40f, 1f, 0.50f, 1f));
        }

        if (magicDragIcon)
        {
            Color saved = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.85f);
            ReferenceItemIconCatalog.Draw(
                new Rect(
                    pointer.x - 16f * scale,
                    pointer.y - 16f * scale,
                    32f * scale,
                    32f * scale),
                magicDragIcon,
                scale);
            GUI.color = saved;
        }
    }

    // Original 06001062: ascending rectangle scan, not a floor-division shortcut.
    private int SlotUnder(Vector2 pointer, Rect viewport, int columns)
    {
        if (!viewport.Contains(pointer)) return -1;
        Vector2 local = pointer - viewport.position + magicScroll;
        for (int index = 0; index < MagicWardrobeSlotCount; index++)
        {
            float scale = s;
            Rect rect = new Rect((index % columns) * MagicWardrobeStridePixels * scale,
                (index / columns) * MagicWardrobeStridePixels * scale,
                MagicWardrobeCellPixels * scale, MagicWardrobeCellPixels * scale);
            if (rect.Contains(local)) return index;
        }
        return -1;
    }

    private static void FillMagicWardrobeRect(Rect rect, Color color)
    {
        Color saved = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = saved;
    }

    // Official Win05.DrawMagicStore 0x0600104f clears exactly three static drag fields per clear
    // block - magicDragFrom (+0x7c) = -1, magicDragIcon (+0x88) = null, magicDragMoved (+0x98) =
    // false - and nothing else, in both clear blocks and both architectures:
    //   block A  arm64 0x3bf77c/0x3bf784/0x3bf7a0    x86_64 0x3d379c/0x3d3795/0x3d37c6
    //   block B  arm64 0x3bfb04/0x3bfb0c/0x3bfb28    x86_64 0x3d3be1/0x3d3bda/0x3d3c0b
    // Official never writes +0x80 (magicDragId) nor +0x90/+0x94 (magicDragDown) here; WBD-064
    // dropped the extra `magicDragId = 0;` and `magicDragDown = Vector2.zero;` so that the three
    // writes below are exactly the official ones. Both residues are unobservable: every reader of
    // magicDragId or magicDragDown sits behind the magicDragFrom (+0x7c) >= 0 gate
    // (drag threshold: arm64 0x3bf514-0x3bf608, x86_64 0x3d3547-0x3d3603; sends: arm64
    // 0x3bf984/0x3bfacc, x86_64 0x3d39e4/0x3d3ba1), which each clear already closes, and MouseDown
    // overwrites both fields before the next drag can read them.
    private void ClearMagicWardrobeDrag()
    {
        magicDragFrom = -1;
        magicDragIcon = null;
        magicDragMoved = false;
    }

    private void DisposeMagicWardrobeDialog()
    {
        if (ownsMagicMessageBox)
        {
            magicMessageBox?.Dispose();
        }
        magicWardrobeDialogReady = false;
        magicMessageBox = null;
        ownsMagicMessageBox = false;
        magicItemIcons = null;
        magicWardrobeState = WorldWardrobeState.Empty;
        // WBD-065 removed the product-only `ClearMagicWardrobeDrag();` that used to close this
        // method. The five drag fields are Win05 statics in official 1.5.8 and are cleared only
        // inside DrawMagicStore 0x0600104f's MouseUp arms; no Win05 method writes them on the way
        // down, official Win05 has no OnDestroy, and every world rebuild mounts a new Win05
        // instance, so a clear here erased a drag that official still carries over the map change
        // or the world exit. Everything else this disposes stays instance state, unchanged.
        // WBD-068 checked the same question for the two neighbours, magicCat (+0x70) and magicScroll
        // (+0x74): they are Win05 statics in the frozen rows, but this method already never touched
        // them, so no removal is needed here - and since they are statics now, a rebuilt world root
        // starts with the player's previous category and scroll instead of index 0 / (0, 0). The
        // category-click arm of DrawMagicStore 0x0600104f and GUI.BeginScrollView are the only
        // writers; neither the constructor nor the window Open/Toggle pair resets them in the
        // product, so nothing before the first read re-zeroes what official keeps either.
    }

}

}
