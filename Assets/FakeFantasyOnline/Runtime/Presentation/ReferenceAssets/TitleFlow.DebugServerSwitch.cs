#if FFO_DEBUG_SERVER_SWITCH
using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Composition-root bindings for the debug-only server switch (FFO_DEBUG_SERVER_SWITCH).
/// Bootstrap owns the server.json store; Presentation only draws and forwards choices.
/// </summary>
public static class DebugServerSwitchBindings
{
    public static string Running;
    public static string DefaultLocalHost = "127.0.0.1";
    public static int DefaultLocalPort = 19000;
    public static Func<string> DescribeNext;
    public static Action<string, int> SelectLocal;
    public static Func<bool> SelectOnline;
}

internal sealed partial class TitleFlow
{
    private bool _debugServerSwitchOpen, _debugServerSwitchLogged;
    private string _debugServerHost, _debugServerPort, _debugServerMessage;

    // Login page only. A choice takes effect on the next start; this process keeps its connection.
    private void DrawDebugServerSwitch()
    {
        if (DebugServerSwitchBindings.DescribeNext == null) return;
        string running = DebugServerSwitchBindings.Running ?? "未知";
        if (!_debugServerSwitchLogged)
        {
            _debugServerSwitchLogged = true;
            _debugServerHost = DebugServerSwitchBindings.DefaultLocalHost;
            _debugServerPort = DebugServerSwitchBindings.DefaultLocalPort.ToString();
            Debug.Log("FFO_DEBUG_SERVER_SWITCH visible running=" + running + " next=" + DebugServerNext());
        }

        bool previousEnabled = GUI.enabled;
        GUI.enabled = !loggingIn;
        if (GUI.Button(new Rect(586f, 538f, 210f, 24f), "服务器: " + running, _regButtonStyle))
        {
            _debugServerSwitchOpen = !_debugServerSwitchOpen;
            _debugServerMessage = null;
        }

        if (_debugServerSwitchOpen)
        {
            GUI.Box(new Rect(536f, 150f, 260f, 236f), GUIContent.none, _panelStyle);
            GUI.Label(new Rect(548f, 158f, 236f, 20f), "调试服务器切换（重启生效）", _loginLabel);
            GUI.Label(new Rect(548f, 180f, 236f, 20f), "当前: " + running, _loginLabel);
            GUI.Label(new Rect(548f, 202f, 236f, 20f), "下次启动: " + DebugServerNext(), _loginLabel);
            GUI.Label(new Rect(548f, 228f, 40f, 22f), "本地", _loginLabel);
            _debugServerHost = GUI.TextField(new Rect(588f, 228f, 120f, 22f), _debugServerHost ?? string.Empty, 253, _fieldStyle);
            _debugServerPort = GUI.TextField(new Rect(712f, 228f, 72f, 22f), _debugServerPort ?? string.Empty, 5, _fieldStyle);
            if (GUI.Button(new Rect(548f, 258f, 112f, 26f), "切到本地", _regButtonStyle)) ChooseDebugLocal();
            if (GUI.Button(new Rect(672f, 258f, 112f, 26f), "切到线上", _regButtonStyle)) ChooseDebugOnline();
            if (!string.IsNullOrEmpty(_debugServerMessage))
                GUI.Label(new Rect(548f, 290f, 236f, 52f), _debugServerMessage, _loginLabel);
            if (GUI.Button(new Rect(548f, 348f, 236f, 28f), "立即退出", _regButtonStyle))
            {
                Debug.Log("FFO_DEBUG_SERVER_SWITCH quit next=" + DebugServerNext());
                GameBootstrap.QuitGame();
            }
        }
        GUI.enabled = previousEnabled;
    }

    private string DebugServerNext()
    {
        try { return DebugServerSwitchBindings.DescribeNext(); }
        catch (Exception exception) { return "server.json 无效: " + exception.Message; }
    }

    private void ChooseDebugLocal()
    {
        int port;
        string host = (_debugServerHost ?? string.Empty).Trim();
        if (!int.TryParse(_debugServerPort, out port) || port < 1 || port > 65535)
        {
            _debugServerMessage = "端口无效";
            return;
        }
        try
        {
            DebugServerSwitchBindings.SelectLocal(host, port);
            _debugServerMessage = "已保存本地 " + host + ":" + port + "，重启后生效";
            Debug.Log("FFO_DEBUG_SERVER_SWITCH selected local " + host + ":" + port);
        }
        catch (Exception exception) { _debugServerMessage = "保存失败: " + exception.Message; }
    }

    private void ChooseDebugOnline()
    {
        try
        {
            DebugServerSwitchBindings.SelectOnline();
            _debugServerMessage = "已切到线上，重启后生效";
            Debug.Log("FFO_DEBUG_SERVER_SWITCH selected online");
        }
        catch (Exception exception) { _debugServerMessage = "保存失败: " + exception.Message; }
    }
}
}
#endif
