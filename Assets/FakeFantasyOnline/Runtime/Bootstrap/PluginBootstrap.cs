using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FakeFantasyOnline.PluginHost;
using FakeFantasyOnline.Presentation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Unity composition/lifetime owner. All game and script calls stay on Update's thread.</summary>
public sealed class PluginBootstrap : MonoBehaviour
{
    public const int DefaultAgentPort = 17845;
    private const int MaxPendingCommands = 64;
    private readonly Queue<Action> commands = new Queue<Action>();
    private ClientRootView view;
    private PluginGameApi game;
    private AddonUiHost ui;
    private AddonHost addons;
    private MainThreadGameDispatcher dispatcher;
    private GameAgentServer server;
    private string addonRoot;
    private string configRoot;
    private string addonError = string.Empty;
    private string bridgeError = string.Empty;
    private int agentPort = DefaultAgentPort;
    private bool initialized;
    private bool stopped;
    private float nextErrorLog;

    public void Initialize(ClientRootView currentView)
    {
        if (initialized) throw new InvalidOperationException("Plugin runtime is already initialized.");
        if (stopped) throw new ObjectDisposedException(nameof(PluginBootstrap));
        view = currentView != null ? currentView : throw new ArgumentNullException(nameof(currentView));
        initialized = true;
        // The delegate follows the current native owner instead of retaining a title-era session.
        game = new PluginGameApi(() => view != null ? view.CurrentSessionForPlugins : null);
        dispatcher = new MainThreadGameDispatcher();
        addonRoot = Path.Combine(UnityEngine.Application.streamingAssetsPath, "AddOns");
        configRoot = Path.Combine(UnityEngine.Application.persistentDataPath, "AddOns");
        ui = gameObject.AddComponent<AddonUiHost>();
        ParseArguments(Environment.GetCommandLineArgs());
        ui.ConfigureManager(ReadAddons, (id, operation) => Enqueue(() => RunAddon(id, operation)),
            () => Enqueue(DiscoverAddons), ReadBridge, (enabled, port) => Enqueue(() => SetBridge(enabled, port)));
        DiscoverAddons();
    }

    private void Enqueue(Action command)
    {
        if (stopped) throw new ObjectDisposedException(nameof(PluginBootstrap));
        if (commands.Count >= MaxPendingCommands) throw new InvalidOperationException("插件管理正忙，请稍后重试。");
        commands.Enqueue(command);
    }

    private void DiscoverAddons()
    {
        try
        {
            if (addons == null) addons = new AddonHost(addonRoot, configRoot, game, ui);
            addons.Discover();
            addonError = string.Empty;
        }
        catch (Exception exception) { addonError = exception.Message; LogFailure("Addon discovery", exception); }
    }

    private IReadOnlyList<AddonManagerEntry> ReadAddons()
    {
        List<AddonManagerEntry> result = new List<AddonManagerEntry>();
        if (addons != null)
        {
            foreach (AddonStatus status in addons.Statuses)
                result.Add(new AddonManagerEntry(status.Id, status.Name, status.Version, status.State.ToString(),
                    status.Error, status.State == AddonState.Enabled || status.State == AddonState.Loading));
        }
        if (!string.IsNullOrEmpty(addonError)) result.Add(new AddonManagerEntry(string.Empty,
            "插件运行环境", string.Empty, "Error", addonError, false));
        return result;
    }

    private void RunAddon(string id, string operation)
    {
        if (addons == null) { addonError = "插件运行环境不可用，请重新扫描后重试。"; return; }
        if (string.IsNullOrEmpty(id)) return;
        bool accepted;
        switch (operation)
        {
            case "enable": accepted = addons.Enable(id); break;
            case "disable": accepted = addons.Disable(id); break;
            case "reload": accepted = addons.Reload(id); break;
            default: throw new ArgumentException("Unknown addon operation.", nameof(operation));
        }
        addonError = accepted ? string.Empty : "插件操作未受理：" + id + " / " + operation + "。请查看插件状态。";
    }

    private AddonBridgeStatus ReadBridge()
    {
        bool running = server != null && server.IsRunning;
        string status = running ? "正在监听 127.0.0.1:" + server.Port : "已停止";
        if (!running && server != null && !string.IsNullOrEmpty(server.LastError)) status = "接口已停止：" + server.LastError;
        if (!string.IsNullOrEmpty(bridgeError)) status = bridgeError;
        return new AddonBridgeStatus(running, running ? server.Port : agentPort,
            running ? server.ConnectedClients : 0, status);
    }

    private void SetBridge(bool enabled, int port)
    {
        bridgeError = string.Empty;
        if (!enabled)
        {
            server?.Dispose();
            server = null;
            return;
        }
        if (port < 1024 || port > 65535) { bridgeError = "端口号须介于 1024 和 65535 之间。"; return; }
        if (server != null && server.IsRunning && server.Port == port) return;
        server?.Dispose();
        server = null;
        agentPort = port;
        try
        {
            server = new GameAgentServer(game, dispatcher, agentPort);
            server.Start();
        }
        catch (Exception exception)
        {
            server?.Dispose();
            server = null;
            bridgeError = "接口启动失败：" + exception.Message;
            LogFailure("Agent bridge", exception);
        }
    }

    private void ParseArguments(string[] arguments)
    {
        bool optIn = false;
        bool validPort = true;
        foreach (string argument in arguments)
        {
            if (string.Equals(argument, "--agent-bridge", StringComparison.Ordinal)) optIn = true;
            if (argument.StartsWith("--agent-port=", StringComparison.Ordinal))
            {
                validPort = int.TryParse(argument.Substring("--agent-port=".Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int selectedPort) && selectedPort >= 1024 && selectedPort <= 65535;
                if (validPort) agentPort = selectedPort;
                else bridgeError = "--agent-port 无效；须介于 1024 和 65535 之间。";
            }
        }
        if (optIn && validPort) SetBridge(true, agentPort);
    }

    private void Update()
    {
        if (!initialized || stopped) return;
        if (Input.GetKeyDown(KeyCode.F10) && GUIUtility.hotControl == 0) ui.ToggleManager();
        for (int i = 0; i < 16 && commands.Count > 0; i++)
        {
            try { commands.Dequeue()(); }
            catch (Exception exception) { addonError = exception.Message; LogFailure("Addon command", exception); }
        }
        try { game.Tick(); }
        catch (Exception exception) { LogFailure("Game observation tick", exception); }
        try { ui.DrainEvents(); addons?.Tick(Time.unscaledDeltaTime); }
        catch (Exception exception) { addonError = exception.Message; LogFailure("Addon tick", exception); }
        // Network workers may queue only transport-neutral operations. They never call Unity.
        try { dispatcher.Pump(64); }
        catch (Exception exception) { LogFailure("Agent dispatcher", exception); }
    }

    private void LogFailure(string operation, Exception exception)
    {
        if (Time.unscaledTime < nextErrorLog) return;
        nextErrorLog = Time.unscaledTime + 5f;
        Debug.LogWarning(operation + ": " + exception.Message);
    }

    public void Shutdown()
    {
        if (stopped) return;
        stopped = true;
        commands.Clear();
        // Stop producers first; fail queued dispatch before releasing script callbacks/UI.
        try { server?.Dispose(); } catch (Exception exception) { Debug.LogWarning(exception.Message); }
        server = null;
        try { dispatcher?.Dispose(); } catch (Exception exception) { Debug.LogWarning(exception.Message); }
        dispatcher = null;
        try { addons?.Dispose(); } catch (Exception exception) { Debug.LogWarning(exception.Message); }
        addons = null;
        try { game?.Dispose(); } catch (Exception exception) { Debug.LogWarning(exception.Message); }
        game = null;
        try { ui?.Dispose(); } catch (Exception exception) { Debug.LogWarning(exception.Message); }
        ui = null;
        view = null;
    }
    private void OnApplicationQuit() { Shutdown(); }
    private void OnDestroy() { Shutdown(); }
}
}
