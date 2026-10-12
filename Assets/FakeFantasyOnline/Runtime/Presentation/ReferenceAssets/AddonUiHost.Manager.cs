using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// These presentation-only DTOs keep PluginHost out of the Presentation assembly.
public sealed class AddonManagerEntry
{
    public AddonManagerEntry(string id, string name, string version, string state, string error, bool active)
    { Id = id; Name = name; Version = version; State = state; Error = error; Active = active; }
    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string State { get; }
    public string Error { get; }
    public bool Active { get; }
}

public sealed class AddonBridgeStatus
{
    public AddonBridgeStatus(bool enabled, int port, int clients, string status)
    { Enabled = enabled; Port = port; Clients = clients; Status = status ?? string.Empty; }
    public bool Enabled { get; }
    public int Port { get; }
    public int Clients { get; }
    public string Status { get; }
}

public sealed partial class AddonUiHost
{
    private readonly object launcherOwner = new object();
    private Func<IReadOnlyList<AddonManagerEntry>> readAddons;
    private Action<string, string> addonCommand;
    private Action discover;
    private Func<AddonBridgeStatus> readBridge;
    private Action<bool, int> setBridge;
    private bool managerVisible;
    private int managerPointer, managerKeyboard;
    private int managerPage;
    private string portText = "17845";
    private string managerError = string.Empty;

    public void ConfigureManager(Func<IReadOnlyList<AddonManagerEntry>> addons,
        Action<string, string> command, Action rescan,
        Func<AddonBridgeStatus> bridge, Action<bool, int> configureBridge)
    {
        CheckThread();
        readAddons = addons ?? throw new ArgumentNullException(nameof(addons));
        addonCommand = command ?? throw new ArgumentNullException(nameof(command));
        discover = rescan ?? throw new ArgumentNullException(nameof(rescan));
        readBridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        setBridge = configureBridge ?? throw new ArgumentNullException(nameof(configureBridge));
        portText = readBridge().Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public void ToggleManager() { CheckThread(); managerVisible = !managerVisible; if (!managerVisible) { WinZ.Forget(this); ClearManagerControls(); } }

    private Rect LauncherRect => new Rect(Mathf.Max(0f, Screen.width - 96f * Scale), 8f * Scale, 86f * Scale, 25f * Scale);
    private Rect ManagerRect
    {
        get
        {
            float width = Mathf.Min(570f * Scale, Mathf.Max(300f, Screen.width - 12f));
            float height = Mathf.Min(520f * Scale, Mathf.Max(200f, Screen.height - 20f));
            return new Rect(Mathf.Max(0f, (Screen.width - width) * .5f), Mathf.Max(0f, (Screen.height - height) * .5f), width, height);
        }
    }

    private void DrawManager()
    {
        if (readAddons == null) return;
        float scale = Scale;
        Event current = Event.current;
        int previousHot = GUIUtility.hotControl, previousKeyboard = GUIUtility.keyboardControl;
        var launcherFrame = WinZ.Frame(launcherOwner, LauncherRect, 5);
        GUI.depth = launcherFrame.Item1;
        if (ArtworkSkin.Button(LauncherRect, "插件", scale, !launcherFrame.Item2, managerVisible)) ToggleManager();
        RememberManagerControls(previousHot, previousKeyboard);
        if (!managerVisible) return;
        Rect rect = ManagerRect;
        var frame = WinZ.Frame(this, rect, 4);
        GUI.depth = frame.Item1;
        Vector2 previousMouse = current.mousePosition;
        if (frame.Item2) current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            ArtworkSkin.Panel(rect, scale);
            Label(new Rect(rect.x + 12f * scale, rect.y + 8f * scale, rect.width - 50f * scale, 22f * scale), "插件管理与本地 AI 接口", scale, false);
            if (ArtworkSkin.Close(new Rect(rect.xMax - 29f * scale, rect.y + 7f * scale, 20f * scale, 20f * scale), scale))
            { managerVisible = false; WinZ.Forget(this); ClearManagerControls(); return; }
            float left = rect.x + 12f * scale, top = rect.y + 38f * scale;
            if (ArtworkSkin.Button(new Rect(left, top, 112f * scale, 25f * scale), "重新扫描", scale)) RunManager(discover);
            Label(new Rect(left + 124f * scale, top, rect.width - 148f * scale, 25f * scale), "启用时加载 StreamingAssets/AddOns 中的代码", scale, false);

            IReadOnlyList<AddonManagerEntry> addons = readAddons() ?? Array.Empty<AddonManagerEntry>();
            float footer = 151f * scale;
            int perPage = Math.Max(1, (int)((rect.height - footer - 80f * scale) / (61f * scale)));
            int pages = Math.Max(1, (addons.Count + perPage - 1) / perPage);
            managerPage = Math.Max(0, Math.Min(pages - 1, managerPage));
            top += 35f * scale;
            if (addons.Count == 0) Label(new Rect(left, top, rect.width - 24f * scale, 50f * scale), "未发现插件。请在 StreamingAssets/AddOns 的插件目录中放入 addon.json 与 Lua 入口文件，然后重新扫描。", scale, true);
            for (int i = managerPage * perPage; i < addons.Count && i < (managerPage + 1) * perPage; i++)
            {
                AddonManagerEntry addon = addons[i];
                Rect row = new Rect(left, top, rect.width - 24f * scale, 57f * scale);
                ArtworkSkin.Panel(row, scale);
                float controlsWidth = 194f * scale;
                Label(new Rect(row.x + 6f * scale, row.y + 2f * scale, Mathf.Max(20f, row.width - controlsWidth), 23f * scale), addon.Name + "  " + addon.Version + "  [" + DisplayAddonState(addon.State) + "]", scale, false);
                string detail = string.IsNullOrEmpty(addon.Error) ? addon.Id : addon.Error;
                Label(new Rect(row.x + 6f * scale, row.y + 26f * scale, row.width - 12f * scale, 27f * scale), detail, scale, true);
                string id = addon.Id;
                if (ArtworkSkin.Button(new Rect(row.xMax - 184f * scale, row.y + 3f * scale, 85f * scale, 23f * scale), addon.Active ? "停用" : "启用", scale))
                    RunManager(() => addonCommand(id, addon.Active ? "disable" : "enable"));
                if (ArtworkSkin.Button(new Rect(row.xMax - 93f * scale, row.y + 3f * scale, 85f * scale, 23f * scale), "重新加载", scale))
                    RunManager(() => addonCommand(id, "reload"));
                top += 61f * scale;
            }
            top = rect.yMax - footer;
            if (ArtworkSkin.Button(new Rect(left, top, 60f * scale, 23f * scale), "上一页", scale, managerPage > 0)) managerPage--;
            Label(new Rect(left + 69f * scale, top, 140f * scale, 23f * scale), (managerPage + 1) + " / " + pages + "    " + addons.Count + " 个插件", scale, false);
            if (ArtworkSkin.Button(new Rect(left + 208f * scale, top, 60f * scale, 23f * scale), "下一页", scale, managerPage + 1 < pages)) managerPage++;
            if (ArtworkSkin.Button(new Rect(rect.xMax - 147f * scale, top, 135f * scale, 23f * scale), "显示插件面板", scale))
                foreach (Panel panel in order) panel.Visible = true;
            top += 32f * scale;
            AddonBridgeStatus bridge = readBridge();
            Label(new Rect(left, top, rect.width - 24f * scale, 22f * scale), "本地 AI 接口：" + bridge.Status + (bridge.Enabled ? "  (" + bridge.Clients + " 个连接)" : string.Empty), scale, false);
            top += 26f * scale;
            Label(new Rect(left, top, 123f * scale, 25f * scale), "127.0.0.1 :", scale, false);
            Rect port = new Rect(left + 99f * scale, top, 77f * scale, 25f * scale);
            ArtworkSkin.Input(port, scale);
            bool oldEnabled = GUI.enabled;
            GUI.enabled = oldEnabled && !bridge.Enabled;
            portText = GUI.TextField(port, portText, 5);
            GUI.enabled = oldEnabled;
            if (ArtworkSkin.Button(new Rect(left + 189f * scale, top, 123f * scale, 25f * scale), bridge.Enabled ? "停止接口" : "启动接口", scale))
            {
                if (!int.TryParse(portText, out int selectedPort) || selectedPort < 1024 || selectedPort > 65535)
                    managerError = "请输入 1024 至 65535 之间的端口号。";
                else RunManager(() => setBridge(!bridge.Enabled, selectedPort));
            }
            top += 30f * scale;
            Label(new Rect(left, top, rect.width - 24f * scale, 36f * scale),
                string.IsNullOrEmpty(managerError) ? "仅手动开启。开启期间，本机上的其他程序可以控制此客户端；使用完毕后请停止接口。" : managerError, scale, true);
        }
        finally
        {
            current.mousePosition = previousMouse;
            RememberManagerControls(previousHot, previousKeyboard);
        }
    }

    private static string DisplayAddonState(string state)
    {
        switch (state)
        {
            case "Discovered": return "待启用";
            case "Loading": return "加载中";
            case "Enabled": return "已启用";
            case "Disabled": return "已停用";
            case "Faulted": return "出错";
            case "Error": return "错误";
            default: return state;
        }
    }

    private void RememberManagerControls(int previousHot, int previousKeyboard)
    {
        if (GUIUtility.hotControl != 0 && GUIUtility.hotControl != previousHot) managerPointer = GUIUtility.hotControl;
        if (GUIUtility.keyboardControl != 0 && GUIUtility.keyboardControl != previousKeyboard) managerKeyboard = GUIUtility.keyboardControl;
    }

    private void ClearManagerControls()
    {
        if (managerPointer != 0 && GUIUtility.hotControl == managerPointer) GUIUtility.hotControl = 0;
        if (managerKeyboard != 0 && GUIUtility.keyboardControl == managerKeyboard) GUIUtility.keyboardControl = 0;
        managerPointer = managerKeyboard = 0;
    }

    private void RunManager(Action action)
    {
        try { action(); managerError = string.Empty; }
        catch (Exception exception) { managerError = BoundText(exception.Message); }
    }
}
}
