using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    // Same official cctor entry as the "historybar" row of Defs (Win05.NestedData.cs); the table
    // is now the single source so the lookup and this adapter cannot drift apart.
    private static Def HistoryDefinition => Defs["historybar"];
    private static Vector2 histScroll;
    private static Texture2D[] chatTabTex;

    private static bool MatchTab(string ch, string want)
    {
        if (string.IsNullOrEmpty(ch)) return want == "系统";
        if (want == "家族") return ch == "家族" || ch == "帮派" || ch == "联盟";
        return ch == want;
    }

    private void DrawHistory(Func<float, float, float, float, Rect> L, Def d)
    {
        if (!BitmapFont.Ready) return;
        Event ev = Event.current;
        if (chatTabTex == null)
        {
            chatTabTex = new Texture2D[ChatTabs.Length];
            for (int i = 0; i < ChatTabs.Length; i++)
                chatTabTex[i] = ResLoader.I != null
                    ? ResLoader.I.LoadGsa("res/uires/chatbar/" + ChatTabs[i].Item1 + ".gsa") : null;
        }
        for (int i = 0; i < ChatTabs.Length; i++)
        {
            Rect tab = L(17 + i * 50, 40f, 50f, 23f);
            bool selected = i == chatTab;
            if (chatTabTex[i] != null)
            {
                float shade = selected ? 1f : .62f;
                GUI.color = new Color(shade, shade, shade, 1f);
                GUI.DrawTexture(tab, chatTabTex[i]);
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = selected ? new Color(.5f, .4f, .18f, .95f)
                    : new Color(.25f, .22f, .14f, .85f);
                GUI.DrawTexture(tab, Texture2D.whiteTexture);
                GUI.color = Color.white;
                BitmapFont.CenterOutline(tab, ChatTabs[i].Item2,
                    selected ? new Color(1f, .9f, .5f, 1f) : new Color(.7f, .68f, .6f, 1f), s);
            }
            if (ev.type == EventType.MouseDown && ev.button == 0 && tab.Contains(ev.mousePosition))
            {
                chatTab = i;
                ev.Use();
            }
        }
        GUI.color = new Color(1f, .85f, .3f, 1f);
        GUI.DrawTexture(L(chatTab * 50 + 17, 62f, 50f, 2f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (chatTab == 1) { DrawGmFeedback(L, d, ev); return; }
        if (chatTab == 2) { DrawVote(L, d, ev); return; }
        if (chatTab == 9) { DrawNoticeTab(L, d, ev); return; }

        string want = ChatTabs[chatTab].Item2;
        Rect view = L(22f, 70f, 490f, 418f);
        float prefixWidth = 48f * s;
        float rowH = 16f * s;
        if (_histRowsVer != ChatUI.Ver || _histRowsTab != chatTab ||
            !Mathf.Approximately(_histRowsS, s))
        {
            _histRowsVer = ChatUI.Ver;
            _histRowsTab = chatTab;
            _histRowsS = s;
            _histRows.Clear();
            foreach (var line in ChatUI.Snapshot())
            {
                if (!MatchTab(line.Item1, want)) continue;
                List<string> wraps = EmoteText.Wrap(line.Item2, view.width - prefixWidth - 4f * s, s);
                for (int i = 0; i < wraps.Count; i++)
                    _histRows.Add((wraps[i], line.Item3, line.Item4, line.Item1, i == 0, line.Item2, line.Item5));
            }
        }
        if (_histRows.Count == 0)
        {
            BitmapFont.CenterOutline(L(0f, 250f, d.w, 12f), "【" + want + "】暂无聊天记录",
                new Color32(150, 150, 150, 255), s);
            return;
        }
        float totalH = _histRows.Count * rowH;
        int sig = unchecked(ChatUI.Ver * 31 + chatTab);
        if (sig != _histSig)
        {
            bool stick = _histMaxY < 0f || histScroll.y >= _histMaxY - rowH;
            int oldTab = _histSigTab;
            _histSig = sig;
            _histSigTab = chatTab;
            if (stick || chatTab != oldTab) histScroll.y = Mathf.Max(0f, totalH - view.height);
        }
        _histMaxY = Mathf.Max(0f, totalH - view.height);
        float contentH = Mathf.Max(totalH, view.height);
        histScroll = GUI.BeginScrollView(view, histScroll,
            new Rect(0f, 0f, view.width, contentH), GUIStyle.none, GUIStyle.none);
        int start = Mathf.Max(0, Mathf.FloorToInt(histScroll.y / rowH) - 1);
        int end = Mathf.Min(_histRows.Count, start + Mathf.FloorToInt(view.height / rowH) + 2);
        for (int i = start; i < end; i++)
        {
            var row = _histRows[i];
            float y = i * rowH;
            if (row.Item5 && !string.IsNullOrEmpty(row.Item4))
                BitmapFont.Draw(0f, y, "[" + row.Item4 + "]", new Color32(255, 220, 120, 255), s);
            ChatUI.DrawShareRow(prefixWidth, y, row.Item1, row.Item2, row.Item7, s, false);
            Rect hit = new Rect(0f, y, view.width, rowH);
            if (ev.type == EventType.MouseDown && ev.button == 1 && hit.Contains(ev.mousePosition))
            {
                string who = row.Item3;
                int id;
                if (string.IsNullOrEmpty(who) || who == CharData.name) { id = 0; who = ""; }
                else id = CombatWorld.FindPlayerIdByName(who);
                PlayerActionMenu.Open(id, who, row.Item6, ChatUI.RecentText(30));
                ev.Use();
            }
        }
        GUI.EndScrollView();
        if (view.Contains(ev.mousePosition))
        {
            for (int i = start; i < end; i++)
            {
                var row = _histRows[i];
                ChatUI.ShareHover(view.x + prefixWidth - histScroll.x,
                    view.y + i * rowH - histScroll.y, row.Item1, row.Item7, s);
            }
        }
        Texture2D background = Bk(d);
        float barX = 527.5f;
        if (background != null)
            barX = (GsaDecoder.W(background) > 520f ? 538f : 487f) * d.w / GsaDecoder.W(background) - 7.5f;
        NativeScroll.BarAt(this, new Rect(L(barX, 0f, 15f, 0f).x, view.y, 15f * s, view.height),
            view.height, contentH, ref histScroll);
    }
}
}
