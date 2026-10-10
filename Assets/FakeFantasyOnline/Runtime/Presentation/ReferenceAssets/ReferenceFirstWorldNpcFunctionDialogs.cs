using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static class ReferenceNpcFunctionUi
{
    public static Texture2D[] LoadFrames(
        ReferenceResourceLoader resources,
        string path,
        int count)
    {
        Texture2D[] frames = new Texture2D[count];
        for (int index = 0; index < count; index++)
        {
            frames[index] = resources.LoadGsaFrame(path, index);
        }
        return frames;
    }

    public static Rect Center(float width, float height, float scale)
    {
        float w = width * scale;
        float h = height * scale;
        return new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
    }

    public static Rect L(Rect window, float x, float y, float width, float height, float scale)
    {
        return new Rect(
            window.x + x * scale,
            window.y + y * scale,
            width * scale,
            height * scale);
    }

    public static bool Contains(Rect rect, Vector2 point)
    {
        return point.x >= rect.xMin && point.x <= rect.xMax &&
            point.y >= rect.yMin && point.y <= rect.yMax;
    }

    public static bool Clicked(Rect rect, Event current)
    {
        return current != null && current.type == EventType.MouseDown &&
            current.button == 0 && Contains(rect, current.mousePosition);
    }

    public static void DrawButton(Rect rect, Texture2D[] frames, Event current)
    {
        if (frames == null || frames.Length == 0 || !frames[0])
        {
            return;
        }

        bool over = current != null && Contains(rect, current.mousePosition);
        int index = over && frames.Length > 1 ? 1 : 0;
        if (over && current.type == EventType.MouseDown && frames.Length > 2)
        {
            index = 2;
        }
        Texture2D frame = frames[Mathf.Min(index, frames.Length - 1)];
        if (frame)
        {
            GUI.DrawTexture(rect, frame, ScaleMode.StretchToFill, true);
        }
    }
}

/// <summary>
/// Application snapshot and Unity-lifecycle adapter for recovered TransportUI. The window's own
/// command hop is NetClient.I.Teleport, so this adapter carries no command writer of its own.
/// </summary>
public sealed class ReferenceTransportUI : IDisposable
{
    public const string BackgroundPath = "res/uires/transportdlg/transportdlg.gsa";
    public const string RowPath = "res/uires/transportdlg/transitem.gsa";
    public const string SelectionPath = "res/uires/transportdlg/checkcover.gsa";
    public const string ConfirmPath = "res/uires/transportdlg/transportbtn.gsa";
    public const string CancelPath = "res/uires/common/cancelbtn.gsa";
    public const string ClosePath = "ui/equipdlg05/miniclose_f0.png";
    public const float WindowWidth = 343f;
    public const float WindowHeight = 426f;

    private TransportUI core;
    private bool disposed;

    private ReferenceTransportUI() { }

    public bool Ready => !disposed && core != null && TransportUI.I == core;
    public bool Visible => core != null && core.VisibleForIndependentRuntime;
    public int TransportList => core == null ? 0 : core.TransportListForIndependentRuntime;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        out ReferenceTransportUI owner)
    {
        owner = null;
        ReferenceWorldBitmapFont font;
        if (resources == null)
        {
            return false;
        }
        ReferenceWorldBitmapFont.TryCreate(resources, out font);

        ReferenceTransportUI adapter = new ReferenceTransportUI();
        GameObject root = new GameObject("TransportUI") { hideFlags = HideFlags.HideAndDontSave };
        root.SetActive(false);
        adapter.core = root.AddComponent<TransportUI>();
        // TransportUI.DoTeleport (0x06000f96) reaches NetClient.I itself, so this adapter only owns
        // the component lifetime and the window state - there is no second command writer here and
        // nothing is queued between the row click and NetClient.Teleport (0x060006e9).
        adapter.core.BindForIndependentRuntime(font);
        root.SetActive(true);
        if (!UnityEngine.Application.isPlaying && TransportUI.I == null)
        {
            adapter.core.InitializeForIndependentRuntime();
        }
        if (!adapter.Ready)
        {
            adapter.Dispose();
            return false;
        }
        owner = adapter;
        return true;
    }

    public void Open(int transportList)
    {
        core.Open(null, transportList);
    }

    public bool ApplyState(WorldTransportState next)
    {
        if (next == null || core == null)
        {
            return false;
        }
        List<TeleportData.Dest> destinations = new List<TeleportData.Dest>(next.Destinations.Count);
        foreach (WorldTransportDestinationState row in next.Destinations)
        {
            destinations.Add(new TeleportData.Dest
            {
                name = row.Name,
                map = row.MapId,
                x = row.X,
                y = row.Y,
                price = row.Price
            });
        }
        core.Open(destinations, next.TransportList);
        return true;
    }

    public Rect WindowRect(float scale) =>
        ReferenceNpcFunctionUi.Center(WindowWidth, WindowHeight, scale);

    public bool MouseOver(Vector2 pointer, float scale) => core != null && core.MouseOver();

    public void Close()
    {
        if (core != null)
        {
            core.Close();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        if (core != null)
        {
            // The original component owns visibility and its singleton until Unity destroys it.
            // Do not close or clear I early during the deferred-destruction interval.
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(core.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(core.gameObject);
            }
            core = null;
        }
    }
}

/// <summary>
/// Source translation of the official ShopDlg open and authoritative item-list surface.
/// </summary>
public sealed class ReferenceShopDlg : IDisposable
{
    public const string BackgroundPath = "res/uires/tradedlg/tradedlgbk.gsa";
    public const string RowPath = "res/uires/tradedlg/tradeiteml.gsa";
    public const string ClosePath = "res/uires/common/miniclose.gsa";
    public const float WindowWidth = 569f;
    public const float WindowHeight = 426f;

    private readonly ReferenceWorldBitmapFont font;
    private readonly ReferenceItemIconCatalog icons;
    private readonly Texture2D background;
    private readonly Texture2D row;
    private readonly Texture2D[] close;
    private WorldShopState state = WorldShopState.Empty;
    private bool disposed;

    private ReferenceShopDlg(
        ReferenceWorldBitmapFont font,
        ReferenceItemIconCatalog icons,
        Texture2D background,
        Texture2D row,
        Texture2D[] close)
    {
        this.font = font;
        this.icons = icons;
        this.background = background;
        this.row = row;
        this.close = close;
        Ready = background;
    }

    public bool Ready { get; private set; }
    public bool Visible { get; private set; }
    public int ShopId { get; private set; }
    public bool SellMode { get; private set; }

    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceShopDlg owner)
    {
        owner = null;
        ReferenceWorldBitmapFont font;
        if (resources == null ||
            !ReferenceWorldBitmapFont.TryCreate(resources, out font) ||
            font == null || !font.Ready)
        {
            return false;
        }

        owner = new ReferenceShopDlg(
            font,
            new ReferenceItemIconCatalog(resources),
            resources.LoadGsa(BackgroundPath),
            resources.LoadGsa(RowPath),
            ReferenceNpcFunctionUi.LoadFrames(resources, ClosePath, 4));
        return owner.Ready;
    }

    public void Open(int shopId, bool sell)
    {
        ShopId = shopId;
        SellMode = sell;
        state = WorldShopState.Empty;
        Visible = shopId > 0;
    }

    public bool ApplyState(WorldShopState next)
    {
        if (next == null || next.ShopId != ShopId)
        {
            return false;
        }
        state = next;
        return true;
    }

    public Rect WindowRect(float scale) =>
        ReferenceNpcFunctionUi.Center(WindowWidth, WindowHeight, scale);

    public bool MouseOver(Vector2 pointer, float scale)
    {
        return Visible && ReferenceNpcFunctionUi.Contains(WindowRect(scale), pointer);
    }

    public bool Draw(float scale)
    {
        if (!Ready || disposed || !Visible)
        {
            return !disposed && Ready;
        }

        Rect win = WindowRect(scale);
        Event current = Event.current;
        GUI.DrawTexture(win, background, ScaleMode.StretchToFill, true);
        Rect closeRect = ReferenceNpcFunctionUi.L(win, 536f, 8f, 20f, 19f, scale);
        ReferenceNpcFunctionUi.DrawButton(closeRect, close, current);
        if (ReferenceNpcFunctionUi.Clicked(closeRect, current))
        {
            current.Use();
            Close();
            return true;
        }

        font.Draw(win.x + 25f * scale, win.y + 10f * scale,
            SellMode ? "出售物品" : "购买物品", Color.white, scale);
        int rows = Mathf.Min(state.Items.Count, 8);
        for (int index = 0; index < rows; index++)
        {
            float x = 22f + (index % 2) * 268f;
            float y = 50f + (index / 2) * 82f;
            Rect rowRect = ReferenceNpcFunctionUi.L(win, x, y, 254f, 72f, scale);
            if (row)
            {
                GUI.DrawTexture(rowRect, row, ScaleMode.StretchToFill, true);
            }
            WorldShopItemState item = state.Items[index];
            Texture2D icon = icons.Load(item.ItemId);
            if (icon)
            {
                ReferenceItemIconCatalog.Draw(
                    new Rect(rowRect.x + 5f * scale, rowRect.y + 5f * scale,
                        36f * scale, 36f * scale),
                    icon,
                    scale);
            }
            Color color = item.Blocked ? new Color(1f, 0.3f, 0.3f, 1f) : Color.white;
            font.Draw(rowRect.x + 48f * scale, rowRect.y + 8f * scale,
                item.Name, color, scale);
            font.Draw(rowRect.x + 48f * scale, rowRect.y + 28f * scale,
                item.Price.ToString(), color, scale);
        }
        return true;
    }

    public void Close()
    {
        Visible = false;
    }

    public void Dispose()
    {
        disposed = true;
        Ready = false;
        Close();
    }
}

/// <summary>Type and lifetime adapter for the original StoreDlg owner.</summary>
public sealed class ReferenceStoreDlg : IDisposable
{
    public const string BackgroundPath = "ui/storedlg05/storedlgbk100.png";
    public const string SlotPath = "ui/waredlg05/slot.png";
    public const string ClosePath = "res/uires/common/miniclose.gsa";
    public const float WindowWidth = 423f, WindowHeight = 501f;
    private bool disposed;
    private ReferenceStoreDlg() { }
    public bool Ready => !disposed;
    public bool Visible => !disposed && StoreDlg.Visible;
    internal int CurrentTabForIndependentRuntime => StoreDlg.CurTab;
    public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceStoreDlg owner)
    {
        owner = resources == null ? null : new ReferenceStoreDlg();
        return owner != null;
    }
    public void Open() => StoreDlg.OpenNow();
    internal void Toggle() => StoreDlg.Toggle();
    public void Close() { if (StoreDlg.Visible) StoreDlg.Toggle(); }
    public bool ApplyState(WorldWarehouseState next)
    {
        if (disposed || next == null) return false;
        var items = new System.Collections.Generic.List<StoreDlg.WItem>(next.Items.Count);
        foreach (WorldWarehouseItemState item in next.Items) items.Add(Project(item, true));
        StoreDlg.SetData(next.PageCount, next.Money, items);
        return true;
    }
    internal void Synchronize(IWarehouseUpdateSource source)
    {
        if (disposed || source == null) return;
        while (source.TryDequeueWarehouseUpdate(out WorldWarehouseUpdate update))
        {
            switch (update.Kind)
            {
                case WorldWarehouseUpdateKind.SetData: ApplyState(update.State); break;
                case WorldWarehouseUpdateKind.PatchHeader: StoreDlg.PatchHeader(update.PageCount, update.Money); break;
                case WorldWarehouseUpdateKind.PatchSlot:
                    var item = Project(update.Item, false);
                    StoreDlg.PatchSlot(item.tab, item.slot, item.id, item); break;
            }
        }
    }
    private static StoreDlg.WItem Project(WorldWarehouseItemState item, bool full) => new StoreDlg.WItem
    {
        tab = item.Tab, slot = item.Slot, id = item.ItemId, count = item.Count,
        name = item.Name, desc = item.Description, curDur = item.CurrentDurability,
        maxDur = item.MaximumDurability, blocked = item.BlockedByte, quality = item.Quality,
        icon = full || item.ItemId > 0 ? NetClient.LoadItemIcon(item.ItemId) : null
    };
    public Rect WindowRect(float scale) => StoreDlg.I != null
        ? StoreDlg.I.WindowRectForIndependentRuntime(scale)
        : new Rect((Screen.width - WindowWidth * scale) * .5f, (Screen.height - WindowHeight * scale) * .5f, WindowWidth * scale, WindowHeight * scale);
    public bool MouseOver(Vector2 pointer, float scale) => Visible && WindowRect(scale).Contains(pointer);
    public void Dispose() { disposed = true; }
}
}
