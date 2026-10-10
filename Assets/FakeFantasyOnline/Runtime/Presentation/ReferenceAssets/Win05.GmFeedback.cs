using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private static Vector2 fbScroll;
    private static float fbTotalH;
    private static int fbSub;

    // Official 1.5.8 token 0x06001059.
    private void DrawGmFeedback(Func<float, float, float, float, Rect> L, Def d, Event ev)
    {
        var feedbacks = NetClient.GmFeedbacks;
        int pending = 0, accepted = 0, rejected = 0;
        for (int i = 0; i < feedbacks.Count; i++)
        {
            if (feedbacks[i].Item4 == 0) pending++;
            else if (feedbacks[i].Item4 == 1) accepted++;
            else rejected++;
        }
        BitmapFont.DrawOutline(
            L(22f, 70f, 500f, 12f).x, L(22f, 70f, 500f, 12f).y,
            string.Format("玩家意见征集(共{0}条: 待审{1} · 已采纳{2}", feedbacks.Count, pending, accepted) +
                (rejected > 0 ? string.Format(" · 未采纳{0}", rejected) : "") + "; 实名展示)",
            new Color32(150, 200, 150, 255), s);
        Rect view = L(22f, 88f, 500f, 314f);
        float lineH = s * 14f;
        if (fbCacheVer != NetClient.GmFeedbackVer || !Mathf.Approximately(fbCacheS, s) || fbCacheSub != fbSub)
        {
            fbCacheVer = NetClient.GmFeedbackVer;
            fbCacheS = s;
            fbCacheSub = fbSub;
            fbRows.Clear();
            fbTotalH = 0f;
            foreach (var item in feedbacks)
            {
                int sub = fbSub;
                if (item.Item4 != sub) continue;
                string time = DateTimeOffset.FromUnixTimeSeconds(item.Item2).ToLocalTime().ToString("MM-dd HH:mm");
                List<string> body = WrapText(item.Item3, 37);
                List<string> reply = !string.IsNullOrEmpty(item.Item5)
                    ? WrapText("官方回复: " + item.Item5, 36)
                    : new List<string>();
                string suffix = item.Item4 == 2 ? "  【未采纳】" : item.Item4 == 0 ? "  【待审】" : "";
                string title = item.Item6 > 0 ? string.Format("意见 #{0}", item.Item6) : "玩家意见";
                string who = !string.IsNullOrEmpty(item.Item1) ? "  " + item.Item1 : "";
                float height = s * 8f + lineH + lineH * (reply.Count + body.Count);
                fbRows.Add((string.Concat(title, who, "  (", time, ")", suffix), sub, body, reply, height));
                fbTotalH = height + fbTotalH;
            }
        }
        int rowCount = fbRows.Count;
        if (rowCount == 0)
        {
            BitmapFont.CenterOutline(L(0f, 220f, d.w, 12f),
                fbSub == 0 ? "当前没有待审的意见" : fbSub == 1 ? "还没有已采纳的意见" : "没有未采纳的意见",
                new Color32(150, 150, 150, 255), s);
        }
        else
        {
            float contentH = Mathf.Max(fbTotalH, view.height);
            fbScroll = GUI.BeginScrollView(view, fbScroll,
                new Rect(0f, 0f, view.width, contentH), GUIStyle.none, GUIStyle.none);
            float lower = fbScroll.y - lineH;
            float upper = lineH + (view.height + fbScroll.y);
            float y = 0f;
            foreach (var row in fbRows)
            {
                float top = y;
                if (upper < top) break;
                y = top + row.Item5;
                if (y < lower) continue;
                Color head = row.Item2 == 0 ? new Color32(160, 190, 205, 255)
                    : row.Item2 == 2 ? new Color32(170, 160, 150, 255)
                    : new Color(1f, .85f, .3f, 1f);
                Color bodyColor = row.Item2 == 0 ? new Color32(200, 205, 205, 255)
                    : row.Item2 == 2 ? new Color32(160, 155, 140, 255)
                    : new Color32(225, 220, 195, 255);
                BitmapFont.DrawOutline(0f, top, row.Item1, head, s);
                float textY = top;
                foreach (string text in row.Item3)
                {
                    textY = lineH + textY;
                    BitmapFont.DrawOutline(s * 8f, textY, text, bodyColor, s);
                }
                textY = lineH + textY;
                foreach (string text in row.Item4)
                {
                    BitmapFont.DrawOutline(s * 8f, textY, text, new Color32(140, 210, 255, 255), s);
                    textY = lineH + textY;
                }
            }
            GUI.EndScrollView();
            Texture2D background = Bk(d);
            float barX = 527.5f;
            if (background != null)
            {
                float sourceX = GsaDecoder.W(background) > 520f ? 538f : 487f;
                barX = sourceX * d.w / GsaDecoder.W(background) - 7.5f;
            }
            NativeScroll.BarAt(this, new Rect(L(barX, 0f, 15f, 0f).x, view.y, s * 15f, view.height),
                view.height, contentH, ref fbScroll);
        }

        string[] labels = { string.Format("待审 {0}", pending), string.Format("已通过 {0}", accepted),
            string.Format("未采纳 {0}", rejected) };
        int subCount = rejected > 0 ? 3 : 2;
        if (fbSub >= subCount)
        {
            fbSub = 0;
            fbScroll = Vector2.zero;
        }
        for (int i = 0; i < subCount; i++)
        {
            Rect button = L(22 + i * 92, 406f, 88f, 20f);
            bool selected = i == fbSub;
            ArtworkSkin.Panel(button, s, selected || button.Contains(ev.mousePosition));
            float labelScale = Mathf.Min(s, (button.width - 10f * s) /
                Mathf.Max(1f, BitmapFont.Measure(labels[i], 1f)));
            BitmapFont.CenterOutline(button, labels[i], selected ? new Color(1f, .85f, .3f, 1f)
                : new Color32(170, 165, 150, 255), labelScale);
            if (ev.type == EventType.MouseDown && ev.button == 0 && button.Contains(ev.mousePosition))
            {
                if (fbSub != i)
                {
                    fbSub = i;
                    fbScroll = Vector2.zero;
                }
                ev.Use();
            }
        }
        BitmapFont.DrawOutline(L(subCount * 92 + 30, 411f, 120f, 12f).x,
            L(subCount * 92 + 30, 411f, 120f, 12f).y, string.Format("本页 {0} 条", rowCount),
            new Color32(150, 150, 140, 255), s);
        BitmapFont.DrawOutline(L(22f, 430f, 500f, 12f).x, L(22f, 430f, 500f, 12f).y,
            string.Format("你的意见(2~200字, 当前{0}字):", (fbInput ?? "").Length),
            new Color32(190, 185, 165, 255), s);
        Rect input = L(22f, 442f, 372f, 44f);
        ArtworkSkin.Input(input, s, GUI.GetNameOfFocusedControl() == "gmfbinput");
        GUIStyle style = HistoryInputStyle(true);
        bool enter = GUI.GetNameOfFocusedControl() == "gmfbinput" && ev.type == EventType.KeyDown &&
            (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter);
        if (enter) ev.Use();
        // UiInput.Draw (0x06000fd1) already drew the menu from the control that opened it.
        fbInput = UiInput.Area(new Rect(input.x + 4f * s, input.y + 3f * s, input.width - 8f * s, input.height - 6f * s),
            "gmfbinput", fbInput ?? "", 200, style, s);
        if (GUI.GetNameOfFocusedControl() == "gmfbinput") gmTypeFrame = Time.frameCount;
        Rect submit = L(402f, 442f, 68f, 20f);
        ArtworkSkin.ButtonFace(submit, "提 交", s, selected: submit.Contains(ev.mousePosition),
            pressed: submit.Contains(ev.mousePosition) && Input.GetMouseButton(0));
        bool clicked = ev.type == EventType.MouseDown && ev.button == 0 && submit.Contains(ev.mousePosition);
        if (enter || clicked)
        {
            string text = (fbInput ?? "").Trim();
            if (text.Length > 1)
            {
                NetClient.I?.SendFeedbackForIndependentRuntime(text);
                fbInput = "";
                GUI.FocusControl(null);
            }
            if (clicked) ev.Use();
        }
    }
}
}
