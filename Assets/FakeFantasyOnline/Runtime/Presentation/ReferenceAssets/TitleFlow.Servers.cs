using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    private struct SrvLine
    {
        internal string name, host;
        internal int port;
        internal bool pvp;
        internal int busy;
        // Adapter identity only. Native decisions always use the copied endpoint, never this index.
        internal int directoryIndex;
    }

    private List<string> srvWorlds;
    private List<List<SrvLine>> srvLines;
    private int selWorld, selZone;
    private float nextStatAt;
    private GUIStyle srvItem, infoTxt;
    private Texture2D zonepanel, btnAffirm, btnAuto, btnQuit, infoBar;
    private Texture2D serverLoginFallback => btnLogin;
    private ClientSessionCoordinator serverSession;
    private Func<string> readServerMessage;
    private Action returnFromServerPage;
    private Action<Result> observeServerConfirmation;

    // This partial borrows the selection/creation TitleFlow instance and its ImgBtn, btnReturn,
    // and setMessage. The surface owns the resource lifetime, not a second page state machine.
    internal void BindServerPageForIndependentRuntime(
        ClientSessionCoordinator session, Func<string> message, Action returnToLogin,
        Action<Result> observeConfirmation)
    {
        serverSession = session;
        readServerMessage = message;
        returnFromServerPage = returnToLogin;
        observeServerConfirmation = observeConfirmation;

    }

    // Official 1.5.8 06000ef1. I is captured before Unity null and endpoint tests. An empty
    // host skips the endpoint comparison; Connected is deliberately not a load-level predicate.
    private int LevelOf(SrvLine it)
    {
        NetClient net = NetClient.I;
        if (net != null &&
            (string.IsNullOrEmpty(it.host) ||
             (it.host == net.TitleServerHostForIndependentRuntime &&
              it.port == net.TitleServerPortForIndependentRuntime)))
        {
            int onlineCount = NetClient.OnlineCount;
            if (onlineCount >= 0)
            {
                if (onlineCount < 20) return 0;
                return onlineCount < 80 ? 1 : 2;
            }
        }
        return Mathf.Clamp(unchecked(it.busy - 1), 0, 3);
    }

    // Official 1.5.8 06000ef2: normal List indexing/foreach exceptions and enumerator disposal.
    private int WorldLevel(int wi)
    {
        int result = 0;
        foreach (SrvLine line in srvLines[wi]) result = Mathf.Max(result, LevelOf(line));
        return result;
    }

    // Official 1.5.8 06000ef3. The tree owns the normalized row fields; the source DTO remains
    // lossless. Length, not source-reference identity, is the native invalidation key.
    private void BuildSrvTree()
    {
        IReadOnlyList<ServerSelectionItem> source =
            serverSession.ReadTitleServersForIndependentRuntime() ?? new ServerSelectionItem[0];
        srvBuiltFor = source.Count;
        srvWorlds = new List<string>();
        srvLines = new List<List<SrvLine>>();
        if (source.Count == 0)
        {
            NetClient net = NetClient.I;
            srvWorlds.Add("龙城");
            srvLines.Add(new List<SrvLine>
            {
                new SrvLine
                {
                    name = "联网",
                    host = net ? net.TitleServerHostForIndependentRuntime : string.Empty,
                    port = net ? net.TitleServerPortForIndependentRuntime : 0,
                    pvp = false, busy = 1, directoryIndex = -1
                }
            });
            return;
        }
        for (int i = 0; i < source.Count; i++)
        {
            ServerSelectionItem row = source[i];
            if (row == null) continue;
            string world = string.IsNullOrWhiteSpace(row.ZoneName) ? "龙城" : row.ZoneName.Trim();
            int wi = srvWorlds.IndexOf(world);
            if (wi < 0)
            {
                srvWorlds.Add(world);
                srvLines.Add(new List<SrvLine>());
                wi = srvWorlds.Count - 1;
            }
            srvLines[wi].Add(new SrvLine
            {
                name = string.IsNullOrWhiteSpace(row.DisplayName)
                    ? srvLines[wi].Count == 0 ? "联网" : "联网" + (srvLines[wi].Count + 1)
                    : row.DisplayName.Trim(),
                host = row.Host, port = row.Port, pvp = row.IsPlayerVersusPlayer,
                busy = Mathf.Clamp(row.BusyLevel, 1, 4), directoryIndex = row.DirectoryIndex
            });
        }
    }

    // Recovered 1.5.8 06000ef4 with approved native button states and a safe empty-directory
    // presentation. Actions still execute at their original controls; no request collector
    // or new status busy gate is introduced.
    internal void DrawServer()
    {
        EnsureSelectionPresentation();
        if (zonepanel) TexScale.Draw(0f, 18f, zonepanel);
        if (srvWorlds == null || srvBuiltFor != serverSession.TitleServerCountForIndependentRuntime) BuildSrvTree();
        if (srvItem == null)
        {
            srvItem = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontSize = 12,
                wordWrap = false, clipping = (TextClipping)0
            };
            infoTxt = new GUIStyle(srvItem);
            infoTxt.normal.textColor = new Color(232f / 255f, 224f / 255f, 198f / 255f, 1f);
        }
        Event current = Event.current;
        selWorld = Mathf.Clamp(selWorld, 0, Mathf.Max(0, srvWorlds.Count - 1));
        if (current.type == EventType.Repaint && Time.realtimeSinceStartup >= nextStatAt &&
            NetClient.I != null && NetClient.I.ConnectedForIndependentRuntime)
        {
            nextStatAt = Time.realtimeSinceStartup + 5f;
            NetClient.I.RequestTitleServerStatusForIndependentRuntime();
        }

        string hint = null;
        for (int i = 0; i < srvWorlds.Count; i++)
        {
            if (i == 40) break;
            float x = 67f + (i & 3) * 113f;
            float y = 72f + (i >> 2) * 30f;
            Texture2D texture = worldBtn[i == selWorld ? 1 : 0];
            Rect rect = new Rect(x, y, 113f, 30f);
            if (texture) GUI.DrawTexture(rect, texture);
            else RecoveredTitleCharacterSurface.DrawBorder(rect, 1f,
                new Color(0.72f, 0.58f, 0.3f, i == selWorld ? 1f : 0.5f));
            srvItem.normal.textColor = WORLD_TXT;
            DrawSelectionLabel(new Rect(x + 14f, y + 6f, 82f, 20f), srvWorlds[i], WORLD_TXT);
            int level = WorldLevel(i);
            if (level != 3 && busyWorld[WORLD_BAR_FRAME[level]])
                GUI.DrawTexture(new Rect(x + 14f, y + 24f, 82f, 2f), busyWorld[WORLD_BAR_FRAME[level]]);
            if (rect.Contains(current.mousePosition)) hint = "选择要登录的服务器组!";
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) && i != selWorld)
            {
                selWorld = i;
                selZone = 0;
            }
        }

        // A nonempty directory may contain only null rows. Keep its UI empty rather
        // than indexing an absent world; the protocol/source and auto-pick policy stay intact.
        IReadOnlyList<SrvLine> lines = selWorld < srvLines.Count
            ? (IReadOnlyList<SrvLine>)srvLines[selWorld] : Array.Empty<SrvLine>();
        selZone = Mathf.Clamp(selZone, 0, Mathf.Max(0, lines.Count - 1));
        for (int i = 0, x = 64; i < lines.Count; i++, x += 90)
        {
            SrvLine line = lines[i];
            if (x == 604) break;
            Texture2D texture = (line.pvp ? pvpBtn : pveBtn)[i == selZone ? 1 : 0];
            Rect rect = new Rect(x, 415f, 86f, 40f);
            if (texture) GUI.DrawTexture(new Rect(x, 415f, 87f, 40f), texture);
            else RecoveredTitleCharacterSurface.DrawBorder(rect, 1f,
                new Color(0.72f, 0.58f, 0.3f, i == selZone ? 1f : 0.5f));
            int level = LevelOf(line);
            srvItem.normal.textColor = BUSY_COL[level];
            DrawSelectionLabel(new Rect(x + 4f, 428f, 76f, 20f), line.name, BUSY_COL[level]);
            Texture2D bar = busyBar[level];
            if (bar) GUI.DrawTexture(new Rect(x + 7f, 449f, 74f, 2f), bar);
            if (rect.Contains(current.mousePosition)) hint = "选择要登录的服务器!";
            bool doubleClick = current.type == EventType.MouseDown && current.clickCount == 2 &&
                rect.Contains(current.mousePosition);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none) | doubleClick)
            {
                selZone = i;
                if (doubleClick) ConfirmServer();
            }
        }
        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && lines.Count != 0;
        if (new Rect(644f, 371f, 152f, 33f).Contains(current.mousePosition)) hint = "Enter登录游戏!";
        if (DrawSelectionButton(644f, 371f, SelectionButton.Confirm, "确认选择")) ConfirmServer();
        if (new Rect(644f, 413f, 152f, 33f).Contains(current.mousePosition)) hint = "自动选择服务器区!";
        if (DrawSelectionButton(644f, 413f, SelectionButton.Auto, "自动选择")) AutoPickServer();
        GUI.enabled = previousEnabled;
        if (new Rect(644f, 456f, 152f, 33f).Contains(current.mousePosition)) hint = "返回登录界面!";
        if (DrawSelectionButton(644f, 456f, SelectionButton.Return, "返回上层"))
        {
            _page = TitlePage.Login;
            returnFromServerPage();
            setMessage(string.Empty);
        }
        if (new Rect(644f, 498f, 152f, 33f).Contains(current.mousePosition)) hint = "退出游戏并关闭客户端!";
        if (DrawSelectionButton(644f, 498f, SelectionButton.Quit, "退出游戏")) GameBootstrap.QuitGame();
        Rect statusRect = new Rect(79f, 477f, 530f, 24f);
        if (statusRect.Contains(current.mousePosition)) hint = "当前选择服务器状态显示!";
        srvItem.normal.textColor = new Color(243f / 255f, 220f / 255f, 64f / 255f, 1f);
        int latencyMs = NetClient.RttMs;
        string latency = latencyMs >= 0 ? string.Format("延迟 {0} ms", latencyMs) :
            NetClient.I != null && NetClient.I.ConnectedForIndependentRuntime ? "延迟 测量中…" : "延迟 --";
        DrawSelectionLabel(statusRect, latency, srvItem.normal.textColor);
        if (infoBar) GUI.DrawTexture(new Rect(84f, 523f, 529f, 21f), infoBar);
        string info = hint ?? (string.IsNullOrEmpty(readServerMessage())
            ? (srvWorlds.Count == 0 ? "暂无可用服务器" : "请选择服务器区!") : readServerMessage());
        DrawSelectionLabel(new Rect(84f, 523f, 529f, 21f), info, infoTxt.normal.textColor);
    }

    // Official 1.5.8 06000ef5. Ordinary List bounds and original empty/null distinctions stay
    // in this body. The session bridge retains its disclosed stage/serialization policy.
    private void ConfirmServer()
    {
        if (srvLines == null || srvWorlds.Count < 1) return;
        List<SrvLine> lines = srvLines[selWorld];
        if (lines == null || lines.Count == 0) return;
        SrvLine line = lines[selZone];
        if (line.busy > 3)
        {
            setMessage("该线维护中, 请选其它线");
            return;
        }
        NetClient net = NetClient.I;
        bool switchEndpoint = net != null && !string.IsNullOrEmpty(line.host) &&
            (line.host != net.TitleServerHostForIndependentRuntime ||
             line.port != net.TitleServerPortForIndependentRuntime);
        Result result = serverSession.ApplyServerSelectionForIndependentRuntime(
            line.directoryIndex, line.host, line.port, switchEndpoint,
            serverSession.TitleServerCountForIndependentRuntime);
        if (result.IsSuccess)
            _page = TitlePage.CharacterSelection;
        else if (result.Error.Code == ClientSessionCoordinator.ServerSwitchReauthenticationCode)
        {
            _page = TitlePage.Login;
            setMessage("已切换到【" + srvWorlds[selWorld] + " " + line.name + "】, 请重新登录");
        }
        observeServerConfirmation(result);
    }

    // Official 1.5.8 06000ef6. Reused closed raw-busy algorithm over the complete native tree,
    // including offscreen rows. This is a move, not new semantic credit for automatic choice.
    private void AutoPickServer()
    {
        if (srvWorlds == null) return;
        int bestWorld = selWorld, bestLine = selZone, best = int.MaxValue;
        for (int wi = 0; wi < srvLines.Count; wi++)
        {
            for (int li = 0; li < srvLines[wi].Count; li++)
            {
                int level = srvLines[wi][li].busy;
                if (level < 4 && level < best)
                {
                    best = level;
                    bestWorld = wi;
                    bestLine = li;
                }
            }
        }
        selWorld = bestWorld;
        selZone = bestLine;
    }
}
}
