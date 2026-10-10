using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.PluginApi
{

public enum PluginWidgetKind { Label, Button, Progress }
public enum PluginAnchor { TopLeft, TopRight, BottomLeft, BottomRight, Center }

/// <summary>Additive widget surface. Existing text-only plugins retain IPluginUi compatibility.</summary>
public interface IPluginWidgetUi : IPluginUi
{
    IDisposable RegisterWidgetPanel(PluginPanelDefinition panel);
    void SetWidgetText(string panelId, string widgetId, string text);
    void SetWidgetProgress(string panelId, string widgetId, double progress);
    void SetPanelVisible(string panelId, bool visible);
    event Action<PluginUiEvent> EventRaised;
}

public sealed class PluginPanelDefinition
{
    public PluginPanelDefinition(string id, string title, float width, float height,
        PluginAnchor anchor = PluginAnchor.TopLeft, float x = 24, float y = 90,
        bool draggable = true, IReadOnlyList<PluginWidgetDefinition> widgets = null)
    {
        PanelId = GameApiData.RequireName(id, nameof(id));
        Title = GameApiData.CheckText(title, nameof(title)) ?? string.Empty;
        Width = Positive(width, nameof(width));
        Height = Positive(height, nameof(height));
        if (!Enum.IsDefined(typeof(PluginAnchor), anchor)) throw new ArgumentOutOfRangeException(nameof(anchor));
        Anchor = anchor;
        X = Finite(x, nameof(x));
        Y = Finite(y, nameof(y));
        Draggable = draggable;
        var copy = new List<PluginWidgetDefinition>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (widgets != null)
        {
            if (widgets.Count > 256) throw new ArgumentException("A panel may contain at most 256 widgets.", nameof(widgets));
            foreach (var widget in widgets)
            {
                if (widget == null || !names.Add(widget.WidgetId)) throw new ArgumentException("Widget IDs must be unique and widgets must not be null.", nameof(widgets));
                copy.Add(widget);
            }
        }
        Widgets = new ReadOnlyCollection<PluginWidgetDefinition>(copy);
    }
    internal static float Finite(float value, string parameter)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameter);
        return value;
    }
    internal static float Positive(float value, string parameter)
    {
        if (Finite(value, parameter) <= 0) throw new ArgumentOutOfRangeException(parameter);
        return value;
    }
    public string PanelId { get; }
    public string Title { get; }
    public float Width { get; }
    public float Height { get; }
    public PluginAnchor Anchor { get; }
    public float X { get; }
    public float Y { get; }
    public bool Draggable { get; }
    public IReadOnlyList<PluginWidgetDefinition> Widgets { get; }
}

public sealed class PluginWidgetDefinition
{
    public PluginWidgetDefinition(string id, PluginWidgetKind kind, string text, float x, float y,
        float width, float height, double progress = 0, bool enabled = true)
    {
        WidgetId = GameApiData.RequireName(id, nameof(id));
        if (!Enum.IsDefined(typeof(PluginWidgetKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        Kind = kind;
        Text = GameApiData.CheckText(text, nameof(text)) ?? string.Empty;
        X = PluginPanelDefinition.Finite(x, nameof(x));
        Y = PluginPanelDefinition.Finite(y, nameof(y));
        Width = PluginPanelDefinition.Positive(width, nameof(width));
        Height = PluginPanelDefinition.Positive(height, nameof(height));
        if (double.IsNaN(progress) || double.IsInfinity(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
        Progress = progress;
        Enabled = enabled;
    }
    public string WidgetId { get; }
    public PluginWidgetKind Kind { get; }
    public string Text { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public double Progress { get; }
    public bool Enabled { get; }
}

public sealed class PluginUiEvent
{
    public PluginUiEvent(string panelId, string widgetId, string name)
    {
        PanelId = GameApiData.RequireName(panelId, nameof(panelId));
        WidgetId = GameApiData.CheckText(widgetId, nameof(widgetId));
        Name = GameApiData.RequireName(name, nameof(name));
    }
    public string PanelId { get; }
    public string WidgetId { get; }
    public string Name { get; }
}
}
