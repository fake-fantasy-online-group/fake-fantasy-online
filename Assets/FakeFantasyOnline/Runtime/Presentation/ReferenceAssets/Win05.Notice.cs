using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private static Vector2 ntScroll;

    // Official 1.5.8 0x06001055, with callback 0x06001073.
    private void DrawNoticeTab(Func<float, float, float, float, Rect> L, Def d, Event ev)
    {
        NoticeBoard.Ensure(this);
        Rect view = L(22f, 70f, 500f, 418f);
        float wrapWidth = view.width + s * -8f;
        int stamp = NoticeBoard.Loaded ? NoticeBoard.Stamp : 0;
        if (_ntStamp != stamp || !Mathf.Approximately(_ntW, wrapWidth))
        {
            _ntStamp = stamp;
            _ntW = wrapWidth;
            _ntBlocks.Clear();
            ntScroll = Vector2.zero;
            NoticeBoard.ForEachLine((text, isTitle) =>
            {
                foreach (string paragraph in NoticeBoard.Strip(text).Split('\n'))
                {
                    foreach (string line in EmoteText.Wrap(paragraph, wrapWidth, s))
                    {
                        _ntBlocks.Add((line, isTitle));
                    }
                }
                if (isTitle)
                {
                    _ntBlocks.Add(("", false));
                }
            });
        }

        if (_ntBlocks.Count == 0)
        {
            BitmapFont.CenterOutline(L(0f, 220f, d.w, 12f),
                "公告还没加载出来，稍等一下", new Color32(150, 150, 150, 255), s);
            return;
        }

        float lineH = s * 24f;
        float contentH = Mathf.Max(lineH * _ntBlocks.Count, view.height);
        ntScroll = GUI.BeginScrollView(view, ntScroll,
            new Rect(0f, 0f, wrapWidth, contentH), GUIStyle.none, GUIStyle.none);
        float lower = ntScroll.y - lineH;
        float upper = lineH + (view.height + ntScroll.y);
        for (int i = 0; i < _ntBlocks.Count; i++)
        {
            float y = lineH * i;
            if (y > upper)
            {
                break;
            }
            if (lineH + y < lower)
            {
                continue;
            }
            var block = _ntBlocks[i];
            if (block.Item1.Length > 0)
            {
                Color color = block.Item2 ? new Color(.88f, .78f, .54f, 1f)
                    : new Color(.81f, .75f, .62f, 1f);
                EmoteText.DrawColored(0f, y, block.Item1, color, s, true);
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
        NativeScroll.BarAt(this, new Rect(L(barX, 0f, 15f, 0f).x,
            view.y, s * 15f, view.height), view.height, contentH, ref ntScroll);
    }
}
}
