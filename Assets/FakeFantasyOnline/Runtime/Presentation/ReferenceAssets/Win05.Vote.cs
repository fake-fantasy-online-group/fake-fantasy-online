using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private static Vector2 voteScroll;
    private static float voteTotalH;

    // Official 1.5.8 0x06001058. Cache and input belong to Win05 across openings.
    private void DrawVote(Func<float, float, float, float, Rect> L, Def d, Event ev)
    {
        var options = NetClient.VoteOptions;
        BitmapFont.DrawOutline(L(22f, 70f, 500f, 12f).x,
            L(22f, 70f, 500f, 12f).y,
            "投票选出优先用 AI 重绘的【怪物 / 角色 / 变装】",
            new Color32(150, 200, 150, 255), s);
        BitmapFont.DrawOutline(L(22f, 84f, 500f, 12f).x,
            L(22f, 84f, 500f, 12f).y,
            string.Format("在下面输入名字提名，再给它投票；每项只能投一次，投完不能改。(共{0}项)", options.Count),
            new Color32(150, 150, 140, 255), s);
        Rect view = L(22f, 100f, 500f, 320f);
        float lineH = s * 14f;
        float minH = s * 22f;
        if (voteCacheVer != NetClient.VoteVer || !Mathf.Approximately(voteCacheS, s))
        {
            voteCacheVer = NetClient.VoteVer;
            voteCacheS = s;
            voteRows.Clear();
            voteTotalH = 0f;
            foreach (var option in options)
            {
                List<string> lines = WrapText(option.Item2, 15);
                if (lines.Count == 0)
                {
                    lines.Add("");
                }
                float height = Mathf.Max(minH, s * 8f + lineH * lines.Count);
                voteRows.Add((option.Item1, lines, option.Item3, option.Item4, option.Item5, height));
                voteTotalH = height + voteTotalH;
            }
        }

        if (voteRows.Count == 0)
        {
            BitmapFont.CenterOutline(L(0f, 230f, d.w, 12f),
                "还没有人提名，第一个来吧", new Color32(150, 150, 150, 255), s);
        }
        else
        {
            float contentH = Mathf.Max(voteTotalH, view.height);
            voteScroll = GUI.BeginScrollView(view, voteScroll,
                new Rect(0f, 0f, view.width, contentH), GUIStyle.none, GUIStyle.none);
            float lower = voteScroll.y - minH;
            float upper = minH + (view.height + voteScroll.y);
            float y = 0f;
            for (int i = 0; i < voteRows.Count; i++)
            {
                var row = voteRows[i];
                if (y > upper)
                {
                    break;
                }
                float nextY = y + row.Item6;
                if (!(nextY < lower))
                {
                    BitmapFont.DrawOutline(0f, y + s * 4f, string.Format("{0}.", i + 1),
                        new Color32(150, 150, 140, 255), s);
                    Color textColor = row.Item5 == 0
                        ? (Color)new Color32(225, 220, 195, 255)
                        : new Color(1f, .85f, .3f, 1f);
                    for (int line = 0; line < row.Item2.Count; line++)
                    {
                        BitmapFont.DrawOutline(s * 26f, lineH * line + (y + s * 4f),
                            row.Item2[line], textColor, s);
                    }
                    BitmapFont.DrawOutline(s * 230f, y + s * 4f,
                        string.Format("赞成 {0}", row.Item3), new Color32(140, 210, 140, 255), s);
                    BitmapFont.DrawOutline(s * 310f, y + s * 4f,
                        string.Format("反对 {0}", row.Item4), new Color32(215, 140, 130, 255), s);
                    Rect yes = new Rect(s * 384f, y + s, s * 52f, s * 18f);
                    Rect no = new Rect(s * 440f, y + s, s * 52f, s * 18f);
                    if (row.Item5 == 0)
                    {
                        ArtworkSkin.ButtonFace(yes, "赞成", s, selected: yes.Contains(ev.mousePosition), pressed: yes.Contains(ev.mousePosition) && Input.GetMouseButton(0));
                        ArtworkSkin.ButtonFace(no, "反对", s, selected: no.Contains(ev.mousePosition), pressed: no.Contains(ev.mousePosition) && Input.GetMouseButton(0));
                        if (ev.type == EventType.MouseDown && ev.button == 0)
                        {
                            if (yes.Contains(ev.mousePosition))
                            {
                                NetClient.I?.SendVoteCastForIndependentRuntime(row.Item1, 1);
                                ev.Use();
                            }
                            else if (no.Contains(ev.mousePosition))
                            {
                                NetClient.I?.SendVoteCastForIndependentRuntime(row.Item1, 2);
                                ev.Use();
                            }
                        }
                    }
                    else
                    {
                        BitmapFont.CenterOutline(new Rect(yes.x, yes.y, no.xMax - yes.x, yes.height),
                            row.Item5 == 1 ? "已赞成" : "已反对",
                            row.Item5 == 1 ? new Color32(140, 210, 140, 255)
                                          : new Color32(215, 140, 130, 255), s);
                    }
                }
                y = nextY;
            }
            GUI.EndScrollView();
            Texture2D background = Bk(d);
            float barX;
            if (background == null)
            {
                barX = 527.5f;
            }
            else
            {
                float x = GsaTextureScale.Width(background) > 520f ? 538f : 487f;
                barX = x * d.w / GsaTextureScale.Width(background) - 7.5f;
            }
            float trackX = L(barX, 0f, 15f, 0f).x;
            NativeScroll.BarAt(this, new Rect(trackX, view.y, s * 15f, view.height),
                view.height, contentH, ref voteScroll);
        }

        BitmapFont.DrawOutline(L(22f, 430f, 500f, 12f).x,
            L(22f, 430f, 500f, 12f).y,
            string.Format("提名(怪物/角色/变装的名字, 2~20字, 当前{0}字):", (voteInput ?? "").Length),
            new Color32(190, 185, 165, 255), s);
        Rect input = L(22f, 442f, 372f, 22f);
        ArtworkSkin.Input(input, s, GUI.GetNameOfFocusedControl() == "voteinput");
        GUIStyle style = HistoryInputStyle(false);
        bool enter = GUI.GetNameOfFocusedControl() == "voteinput" &&
            ev.type == EventType.KeyDown &&
            (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter);
        if (enter)
        {
            ev.Use();
        }
        // UiInput.Draw (0x06000fd1) already drew the menu from the control that opened it.
        voteInput = UiInput.Field(new Rect(input.x + 4f * s, input.y + 2f * s,
            input.width - 8f * s, input.height - 4f * s), "voteinput", voteInput ?? "", 20, style, s);
        if (GUI.GetNameOfFocusedControl() == "voteinput")
        {
            gmTypeFrame = Time.frameCount;
        }
        Rect nominate = L(402f, 442f, 68f, 20f);
        ArtworkSkin.ButtonFace(nominate, "提 名", s, selected: nominate.Contains(ev.mousePosition),
            pressed: nominate.Contains(ev.mousePosition) && Input.GetMouseButton(0));
        bool click = ev.type == EventType.MouseDown && ev.button == 0 &&
            nominate.Contains(ev.mousePosition);
        if (enter || click)
        {
            string text = (voteInput ?? "").Trim();
            if (text.Length > 1)
            {
                NetClient.I?.SendVoteAddForIndependentRuntime(text);
                voteInput = "";
                GUI.FocusControl(null);
            }
            if (click)
            {
                ev.Use();
            }
        }
    }
}
}
