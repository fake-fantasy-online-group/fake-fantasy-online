namespace FakeFantasyOnline.PluginApi
{

public static class PluginApiVersion
{
    public const int Major = 1;
    public const int Minor = 0;
    public const string Current = "1.0";
}

public static class PluginCapabilities
{
    public const string StateRead = "game.state.read";
    public const string EventsSubscribe = "game.events.subscribe";
    public const string ChatSend = "game.commands.chat";
    public const string UiPanel = "ui.panel";
    public const string LocalStorage = "storage.local";
}
}
