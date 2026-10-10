using System;

namespace FakeFantasyOnline.PluginApi
{

public sealed class PanelDefinition
{
    public PanelDefinition(string panelId, string title, string initialText)
    {
        PanelId = panelId ?? throw new ArgumentNullException(nameof(panelId));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        InitialText = initialText ?? string.Empty;
    }

    public string PanelId { get; }

    public string Title { get; }

    public string InitialText { get; }
}

public interface IPluginUi
{
    IDisposable RegisterPanel(PanelDefinition panel);

    void SetPanelText(string panelId, string text);
}
}
