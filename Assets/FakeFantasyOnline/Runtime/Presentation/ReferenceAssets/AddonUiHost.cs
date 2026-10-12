using System;
using System.Collections.Generic;
using System.Threading;
using FakeFantasyOnline.PluginApi;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Main-thread, bounded addon UI rendered with the client's shared artwork skin.</summary>
public sealed partial class AddonUiHost : MonoBehaviour, IPluginWidgetUi, IDisposable
{
    private const int MaxPanels = 64;
    private const int MaxWidgets = 64;
    private const int MaxEvents = 128;
    private readonly Dictionary<string, Panel> panels = new Dictionary<string, Panel>(StringComparer.Ordinal);
    private readonly List<Panel> order = new List<Panel>();
    private readonly Queue<PendingEvent> events = new Queue<PendingEvent>();
    private static readonly List<AddonUiHost> hosts = new List<AddonUiHost>();
    private int ownerThread;
    private GUIStyle labelStyle;
    private bool disposed;
    private Panel dragging;
    private Vector2 dragOffset;
    private int dragControl;
    public event Action<PluginUiEvent> EventRaised;

    private sealed class PendingEvent
    {
        internal Panel Owner;
        internal PluginUiEvent Event;
    }

    private sealed class Widget
    {
        internal PluginWidgetDefinition Definition;
        internal string Text;
        internal double Progress;
    }

    private sealed class Panel
    {
        internal PluginPanelDefinition Definition;
        internal readonly List<Widget> Widgets = new List<Widget>();
        internal bool Visible = true;
        internal bool Moved;
        internal float X, Y;
        internal string Text;
        internal Rect Rect;
        internal int PointerCapture, KeyboardFocus;
    }

    private sealed class Registration : IDisposable
    {
        private Action release;
        internal Registration(Action release) { this.release = release; }
        public void Dispose() { Action action = release; release = null; action?.Invoke(); }
    }

    private void Awake() { ownerThread = Thread.CurrentThread.ManagedThreadId; }
    private void OnEnable() { if (!disposed && !hosts.Contains(this)) hosts.Add(this); }
    private void OnDisable()
    {
        hosts.Remove(this);
        ClearDrag();
        foreach (Panel panel in order) { WinZ.Forget(panel); ReleaseControls(panel); }
        ClearManagerControls();
        WinZ.Forget(this);
        WinZ.Forget(launcherOwner);
    }
    private void OnDestroy() { Dispose(); }

    public IDisposable RegisterPanel(PanelDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        IDisposable handle = RegisterWidgetPanel(new PluginPanelDefinition(definition.PanelId,
            definition.Title, 300f, 190f, PluginAnchor.TopLeft, 24f + order.Count % 8 * 18f,
            90f + order.Count % 8 * 18f, true));
        panels[definition.PanelId].Text = BoundText(definition.InitialText);
        return handle;
    }

    public IDisposable RegisterWidgetPanel(PluginPanelDefinition definition)
    {
        CheckThread();
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (panels.Count >= MaxPanels) throw new InvalidOperationException("Addon panel limit reached.");
        if (panels.ContainsKey(definition.PanelId)) throw new InvalidOperationException("Panel id already registered.");
        if (definition.Widgets.Count > MaxWidgets) throw new ArgumentException("Too many widgets.", nameof(definition));
        Panel panel = new Panel { Definition = definition, Text = string.Empty };
        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (PluginWidgetDefinition widget in definition.Widgets)
        {
            if (!ids.Add(widget.WidgetId)) throw new ArgumentException("Duplicate widget id.", nameof(definition));
            panel.Widgets.Add(new Widget { Definition = widget, Text = BoundText(widget.Text), Progress = widget.Progress });
        }
        panel.Rect = ResolveRect(panel);
        panels.Add(definition.PanelId, panel);
        order.Add(panel);
        return new Registration(() => RemovePanel(panel));
    }

    public void SetPanelText(string panelId, string text) { GetPanel(panelId).Text = BoundText(text); }
    public void SetWidgetText(string panelId, string widgetId, string text) { GetWidget(panelId, widgetId).Text = BoundText(text); }
    public void SetWidgetProgress(string panelId, string widgetId, double progress)
    {
        if (double.IsNaN(progress) || double.IsInfinity(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
        GetWidget(panelId, widgetId).Progress = Math.Max(0d, Math.Min(1d, progress));
    }
    public void SetPanelVisible(string panelId, bool visible)
    {
        Panel panel = GetPanel(panelId);
        panel.Visible = visible;
        if (!visible)
        {
            WinZ.Forget(panel);
            ReleaseControls(panel);
            if (ReferenceEquals(dragging, panel)) ClearDrag();
        }
    }

    private Panel GetPanel(string id)
    {
        CheckThread();
        if (id == null || !panels.TryGetValue(id, out Panel panel)) throw new KeyNotFoundException("Unknown addon panel.");
        return panel;
    }
    private Widget GetWidget(string panelId, string widgetId)
    {
        foreach (Widget widget in GetPanel(panelId).Widgets)
            if (string.Equals(widget.Definition.WidgetId, widgetId, StringComparison.Ordinal)) return widget;
        throw new KeyNotFoundException("Unknown addon widget.");
    }
    private void RemovePanel(Panel panel)
    {
        if (disposed) return;
        CheckThread();
        if (!panels.TryGetValue(panel.Definition.PanelId, out Panel current) || !ReferenceEquals(current, panel)) return;
        panels.Remove(panel.Definition.PanelId);
        order.Remove(panel);
        WinZ.Forget(panel);
        ReleaseControls(panel);
        if (ReferenceEquals(dragging, panel)) ClearDrag();
    }
    private static string BoundText(string text)
    {
        text = text ?? string.Empty;
        return LimitText(text, 8192);
    }
    private static string LimitText(string text, int maximum)
    {
        if (text.Length <= maximum) return text;
        // Never split a UTF-16 surrogate pair when bounding observation or rendered text.
        if (maximum > 0 && char.IsHighSurrogate(text[maximum - 1]) && char.IsLowSurrogate(text[maximum])) maximum--;
        return text.Substring(0, maximum);
    }

    private void CheckThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != ownerThread) throw new InvalidOperationException("Addon UI requires the Unity main thread.");
        if (disposed) throw new ObjectDisposedException(nameof(AddonUiHost));
    }

    // Invoked by PluginBootstrap.Update before the Lua tick. Script callbacks never run inside OnGUI.
    public void DrainEvents(int maximum = 64)
    {
        CheckThread();
        for (int i = 0; i < Math.Max(0, Math.Min(MaxEvents, maximum)) && events.Count > 0; i++)
        {
            PendingEvent pending = events.Dequeue();
            PluginUiEvent item = pending.Event;
            if (!panels.TryGetValue(item.PanelId, out Panel current) || !ReferenceEquals(current, pending.Owner)) continue;
            Action<PluginUiEvent> callback = EventRaised;
            if (callback == null) continue;
            foreach (Action<PluginUiEvent> subscriber in callback.GetInvocationList())
            {
                try { subscriber(item); }
                catch (Exception exception) { Debug.LogWarning("Addon UI event failed: " + exception.Message); }
            }
        }
    }
    private void Emit(Panel panel, string widgetId, string name)
    {
        if (events.Count < MaxEvents) events.Enqueue(new PendingEvent { Owner = panel, Event = new PluginUiEvent(panel.Definition.PanelId, widgetId, name) });
    }

    private void OnGUI()
    {
        if (disposed) return;
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        try
        {
            // Fixed controls only: no native resource ownership is transferred to addons.
            foreach (Panel panel in order.ToArray())
                if (panel.Visible) DrawPanel(panel);
            DrawManager();
        }
        finally { GUI.depth = previousDepth; GUI.color = previousColor; }
    }

    private float Scale => Mathf.Clamp(UIScale.Factor, 1f, 2f);
    private Rect ResolveRect(Panel panel)
    {
        float scale = Scale;
        PluginPanelDefinition definition = panel.Definition;
        float width = Mathf.Min(Mathf.Clamp(definition.Width, 120f, 1200f) * scale, Mathf.Max(120f, Screen.width));
        float height = Mathf.Min(Mathf.Clamp(definition.Height, 70f, 900f) * scale, Mathf.Max(70f, Screen.height));
        float x = definition.X * scale, y = definition.Y * scale;
        if (panel.Moved) { x = panel.X; y = panel.Y; }
        else
        {
            switch (definition.Anchor)
            {
                case PluginAnchor.TopRight: x = Screen.width - width - x; break;
                case PluginAnchor.BottomLeft: y = Screen.height - height - y; break;
                case PluginAnchor.BottomRight: x = Screen.width - width - x; y = Screen.height - height - y; break;
                case PluginAnchor.Center: x += (Screen.width - width) * .5f; y += (Screen.height - height) * .5f; break;
            }
        }
        return new Rect(Mathf.Clamp(x, 0f, Mathf.Max(0f, Screen.width - width)),
            Mathf.Clamp(y, 0f, Mathf.Max(0f, Screen.height - height)), width, height);
    }

    private void DrawPanel(Panel panel)
    {
        panel.Rect = ResolveRect(panel);
        Rect rect = panel.Rect;
        float scale = Scale;
        Event current = Event.current;
        var frame = WinZ.Frame(panel, rect, 8);
        GUI.depth = frame.Item1;
        Vector2 previousMouse = current.mousePosition;
        int previousHot = GUIUtility.hotControl, previousKeyboard = GUIUtility.keyboardControl;
        if (frame.Item2 && !ReferenceEquals(dragging, panel)) current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            ArtworkSkin.Panel(rect, scale);
            Rect close = new Rect(rect.xMax - 26f * scale, rect.y + 5f * scale, 20f * scale, 20f * scale);
            Label(new Rect(rect.x + 10f * scale, rect.y + 6f * scale, rect.width - 45f * scale, 20f * scale), panel.Definition.Title, scale, false);
            if (ArtworkSkin.Close(close, scale))
            {
                panel.Visible = false;
                WinZ.Forget(panel);
                ReleaseControls(panel);
                if (ReferenceEquals(dragging, panel)) ClearDrag();
                Emit(panel, string.Empty, "close");
                return;
            }
            HandleDrag(panel, new Rect(rect.x, rect.y, rect.width - 30f * scale, 30f * scale));
            Rect body = new Rect(rect.x + 8f * scale, rect.y + 32f * scale, rect.width - 16f * scale, rect.height - 40f * scale);
            GUI.BeginGroup(body);
            try
            {
                if (!string.IsNullOrEmpty(panel.Text)) Label(new Rect(0, 0, body.width, body.height), panel.Text, scale, true);
                foreach (Widget widget in panel.Widgets)
                {
                    PluginWidgetDefinition definition = widget.Definition;
                    Rect bounds = new Rect(Mathf.Clamp(definition.X, -1200f, 1200f) * scale,
                        Mathf.Clamp(definition.Y, -900f, 900f) * scale,
                        Mathf.Clamp(definition.Width, 1f, 1200f) * scale, Mathf.Clamp(definition.Height, 1f, 900f) * scale);
                    switch (definition.Kind)
                    {
                        case PluginWidgetKind.Label: Label(bounds, widget.Text, scale, true); break;
                        case PluginWidgetKind.Button:
                            if (ArtworkSkin.Button(bounds, widget.Text, scale, definition.Enabled)) Emit(panel, definition.WidgetId, "click");
                            break;
                        case PluginWidgetKind.Progress:
                            ArtworkSkin.Panel(bounds, scale);
                            Fill(new Rect(bounds.x + 3f * scale, bounds.y + 3f * scale,
                                Mathf.Max(0f, bounds.width - 6f * scale) * (float)Math.Max(0d, Math.Min(1d, widget.Progress)),
                                Mathf.Max(0f, bounds.height - 6f * scale)), new Color(.23f, .51f, .34f, 1f));
                            Label(bounds, widget.Text, scale, false);
                            break;
                    }
                }
            }
            finally { GUI.EndGroup(); }
        }
        finally
        {
            current.mousePosition = previousMouse;
            if (GUIUtility.hotControl != 0 && GUIUtility.hotControl != previousHot) panel.PointerCapture = GUIUtility.hotControl;
            if (GUIUtility.keyboardControl != 0 && GUIUtility.keyboardControl != previousKeyboard) panel.KeyboardFocus = GUIUtility.keyboardControl;
        }
    }

    private static void ReleaseControls(Panel panel)
    {
        if (panel.PointerCapture != 0 && GUIUtility.hotControl == panel.PointerCapture) GUIUtility.hotControl = 0;
        if (panel.KeyboardFocus != 0 && GUIUtility.keyboardControl == panel.KeyboardFocus) GUIUtility.keyboardControl = 0;
        panel.PointerCapture = panel.KeyboardFocus = 0;
    }

    private void HandleDrag(Panel panel, Rect title)
    {
        int id = GUIUtility.GetControlID(panel.Definition.PanelId.GetHashCode(), FocusType.Passive, title);
        Event current = Event.current;
        if (panel.Definition.Draggable && current.type == EventType.MouseDown && current.button == 0 && title.Contains(current.mousePosition))
        {
            dragging = panel;
            dragOffset = current.mousePosition - panel.Rect.position;
            dragControl = id;
            GUIUtility.hotControl = id;
            current.Use();
        }
        if (!ReferenceEquals(dragging, panel)) return;
        if (current.type == EventType.MouseDrag && GUIUtility.hotControl == dragControl)
        {
            panel.Moved = true;
            panel.X = current.mousePosition.x - dragOffset.x;
            panel.Y = current.mousePosition.y - dragOffset.y;
            panel.Rect = ResolveRect(panel);
            current.Use();
        }
        if (current.type == EventType.MouseUp && current.button == 0)
        {
            ClearDrag();
            Emit(panel, string.Empty, "moved");
            current.Use();
        }
    }
    private void ClearDrag()
    {
        if (dragControl != 0 && GUIUtility.hotControl == dragControl) GUIUtility.hotControl = 0;
        dragControl = 0;
        dragging = null;
    }
    private void Label(Rect rect, string text, float scale, bool wrap)
    {
        if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label) { font = NpcChatter.CnFont(), richText = false, clipping = TextClipping.Clip };
        labelStyle.fontSize = Mathf.RoundToInt(12f * scale);
        labelStyle.wordWrap = wrap;
        labelStyle.alignment = wrap ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
        labelStyle.normal.textColor = new Color(1f, .94f, .78f, 1f);
        GUI.Label(rect, BoundText(text), labelStyle);
    }
    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    public static bool MouseOver()
    {
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        foreach (AddonUiHost host in hosts)
        {
            if (host == null || host.disposed || !host.isActiveAndEnabled) continue;
            if (host.readAddons != null && (host.LauncherRect.Contains(pointer) || host.managerVisible && host.ManagerRect.Contains(pointer))) return true;
            foreach (Panel panel in host.order)
                if (panel.Visible && host.ResolveRect(panel).Contains(pointer)) return true;
        }
        return false;
    }

    public static IReadOnlyDictionary<string, object> ObserveState(int maxPanels = 32)
    {
        int maximum = Math.Max(0, Math.Min(MaxPanels, maxPanels));
        List<object> result = new List<object>();
        bool available = false, manager = false, truncated = false;
        AddonBridgeStatus bridge = new AddonBridgeStatus(false, 0, 0, "Unavailable");
        foreach (AddonUiHost host in hosts)
        {
            if (host == null || host.disposed || !host.isActiveAndEnabled) continue;
            host.CheckThread();
            available = true;
            manager |= host.managerVisible;
            bridge = host.readBridge?.Invoke() ?? bridge;
            foreach (Panel panel in host.order)
            {
                if (result.Count >= maximum) { truncated = true; break; }
                Rect rect = host.ResolveRect(panel);
                List<object> widgets = new List<object>();
                foreach (Widget widget in panel.Widgets)
                {
                    if (widgets.Count >= 16) break;
                    string text = LimitText(widget.Text, 256);
                    widgets.Add(new Dictionary<string, object> {
                        { "id", widget.Definition.WidgetId }, { "kind", widget.Definition.Kind.ToString().ToLowerInvariant() },
                        { "text", text }, { "progress", widget.Progress }, { "enabled", widget.Definition.Enabled } });
                }
                string panelText = LimitText(panel.Text, 1024);
                result.Add(new Dictionary<string, object> {
                    { "panelId", panel.Definition.PanelId }, { "title", LimitText(panel.Definition.Title, 256) }, { "text", panelText },
                    { "visible", panel.Visible }, { "x", rect.x }, { "y", rect.y }, { "width", rect.width }, { "height", rect.height }, { "widgets", widgets }, { "widgetsTruncated", panel.Widgets.Count > widgets.Count } });
            }
        }
        return new Dictionary<string, object> {
            { "available", available }, { "managerVisible", manager }, { "panels", result }, { "truncated", truncated },
            { "bridge", new Dictionary<string, object> { { "enabled", bridge.Enabled }, { "port", bridge.Port }, { "clients", bridge.Clients }, { "status", bridge.Status } } } };
    }

    public void Dispose()
    {
        if (disposed) return;
        CheckThread();
        OnDisable();
        disposed = true;
        panels.Clear();
        order.Clear();
        events.Clear();
        EventRaised = null;
        readAddons = null;
        addonCommand = null;
        discover = null;
        readBridge = null;
        setBridge = null;
    }
}
}
