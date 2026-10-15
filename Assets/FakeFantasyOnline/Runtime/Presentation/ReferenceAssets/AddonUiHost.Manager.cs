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

public sealed partial class AddonUiHost
{
    private readonly object launcherOwner = new object();
    private Func<IReadOnlyList<AddonManagerEntry>> readAddons;
    private Action<string, string> addonCommand;
    private Action discover;
    private Action<string> installSource;
    private Func<string> readInstallStatus;
    private Func<string> readInstallAction;
    private bool managerVisible;
    private int managerPointer, managerKeyboard;
    private int managerPage;
    private string sourceText = string.Empty;
    private string managerError = string.Empty;
    private string displayedInstallStatus;
    private Vector2 installStatusScroll;
    private GUIStyle installStatusStyle;

    public void ConfigureManager(Func<IReadOnlyList<AddonManagerEntry>> addons,
        Action<string, string> command, Action rescan,
        Action<string> install = null, Func<string> installStatus = null, Func<string> installAction = null)
    {
        CheckThread();
        readAddons = addons ?? throw new ArgumentNullException(nameof(addons));
        addonCommand = command ?? throw new ArgumentNullException(nameof(command));
        discover = rescan ?? throw new ArgumentNullException(nameof(rescan));
        installSource = install;
        readInstallStatus = installStatus;
        readInstallAction = installAction;
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
            Label(new Rect(rect.x + 12f * scale, rect.y + 8f * scale, rect.width - 50f * scale, 22f * scale), "插件管理", scale, false);
            if (ArtworkSkin.Close(new Rect(rect.xMax - 29f * scale, rect.y + 7f * scale, 20f * scale, 20f * scale), scale))
            { managerVisible = false; WinZ.Forget(this); ClearManagerControls(); return; }
            float left = rect.x + 12f * scale, top = rect.y + 38f * scale;
            if (ArtworkSkin.Button(new Rect(left, top, 112f * scale, 25f * scale), "重新扫描", scale)) RunManager(discover);
            Label(new Rect(left + 124f * scale, top, rect.width - 148f * scale, 25f * scale), "安装后需手动启用；启用将运行插件代码", scale, false);

            IReadOnlyList<AddonManagerEntry> addons = readAddons() ?? Array.Empty<AddonManagerEntry>();
            float footer = 151f * scale;
            int perPage = Math.Max(1, (int)((rect.height - footer - 80f * scale) / (61f * scale)));
            int pages = Math.Max(1, (addons.Count + perPage - 1) / perPage);
            managerPage = Math.Max(0, Math.Min(pages - 1, managerPage));
            top += 35f * scale;
            if (addons.Count == 0) Label(new Rect(left, top, rect.width - 24f * scale, 50f * scale), "未发现插件。可从源地址安装，或将 Lua 插件放入 AddOns 目录后重新扫描。", scale, true);
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
            if (installSource != null)
            {
                Rect source = new Rect(left, top, Mathf.Max(70f, rect.width - 185f * scale), 25f * scale);
                ArtworkSkin.Input(source, scale);
                sourceText = GUI.TextField(source, sourceText, 2048);
                string action = readInstallAction?.Invoke() ?? "安装 / 更新";
                if (ArtworkSkin.Button(new Rect(source.xMax + 6f * scale, top, 150f * scale, 25f * scale), action, scale))
                    RunManager(() => installSource(sourceText));
                top += 30f * scale;
                string status = string.IsNullOrEmpty(managerError) ? readInstallStatus?.Invoke() : managerError;
                DrawInstallStatus(new Rect(left, top, rect.width - 24f * scale, 68f * scale),
                    status ?? "输入插件源地址。安装与更新不会自动启用插件。", scale);
            }
            else if (!string.IsNullOrEmpty(managerError))
                Label(new Rect(left, top, rect.width - 24f * scale, 36f * scale), managerError, scale, true);
        }
        finally
        {
            current.mousePosition = previousMouse;
            RememberManagerControls(previousHot, previousKeyboard);
        }
    }

    private void DrawInstallStatus(Rect bounds, string status, float scale)
    {
        if (!string.Equals(displayedInstallStatus, status, StringComparison.Ordinal))
        {
            displayedInstallStatus = status;
            installStatusScroll = new Vector2(0f, 0f);
        }
        if (installStatusStyle == null)
            installStatusStyle = new GUIStyle(GUI.skin.textArea)
            {
                font = NpcChatter.CnFont(), richText = false, wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
        installStatusStyle.fontSize = Mathf.RoundToInt(12f * scale);
        float width = Mathf.Max(20f, bounds.width - 22f * scale);
        float height = Mathf.Max(bounds.height, installStatusStyle.CalcHeight(new GUIContent(status), width));
        installStatusScroll = GUI.BeginScrollView(bounds, installStatusScroll, new Rect(0f, 0f, width, height));
        try
        {
            // Keep the complete text selectable/copyable, including long pinned source URLs.
            // Discard edits: this field displays installer output, never user commands.
            GUI.TextArea(new Rect(0f, 0f, width, height), status, installStatusStyle);
        }
        finally { GUI.EndScrollView(); }
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
