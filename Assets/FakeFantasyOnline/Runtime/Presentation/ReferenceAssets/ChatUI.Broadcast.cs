using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    private class Banner
    {
        public string name, text;
        public int tier, skin;
        public List<Share> shares;
        public float shownAt;
        public List<string> rows;
    }
    private const float BC_EXPAND_SEC = .15f;
    private const float BC_HOLD_T1 = 4f;
    private const float BC_HOLD_T2 = 6f;
    private static Banner bcCur;

    public static void AddBroadcast(string name, string text, int tier, int skin, List<Share> shares = null)
    {
        var banner = new Banner
        {
            name = name ?? "", text = text ?? "", tier = tier >= 2 ? 2 : 1,
            skin = tier >= 2 ? Mathf.Clamp(skin, 0, 11) : 0, shares = shares
        };
        if (bcCur == null || bcCur.tier == 1)
        {
            bcCur = banner;
            banner.shownAt = Time.unscaledTime;
            return;
        }
        bcQueue.Add(banner);
        if (bcQueue.Count > 12)
            bcQueue.RemoveAt(Math.Max(0, bcQueue.FindIndex(b => b.tier == 1)));
    }

    private static float BcHold(Banner banner) => banner.tier >= 2 ? BC_HOLD_T2 : BC_HOLD_T1;

    private static void TickBanner()
    {
        if (bcCur == null) return;
        if (Time.unscaledTime - bcCur.shownAt < BC_EXPAND_SEC + BcHold(bcCur)) return;
        bcCur = null;
        if (bcQueue.Count > 0)
        {
            bcCur = bcQueue[0];
            bcQueue.RemoveAt(0);
            bcCur.shownAt = Time.unscaledTime;
        }
    }
    // Official 1.6.0 060000f7. Skin inner bounds move/resize the backing panel and text;
    // animation remains anchored to the original banner rectangle. ty/th are intentionally unused.
    private void DrawBanner(Rect rChat)
    {
        TickBanner();
        Banner banner = bcCur;
        if (banner == null || !BitmapFont.Ready) return;
        float elapsed = Time.unscaledTime - banner.shownAt;
        float expand = Mathf.Clamp01(elapsed / BC_EXPAND_SEC);
        // Only the banner rectangle keeps this observation; later consumers reread s.
        float scale = s;
        float artWidth = scale * 384f;
        float height = scale * 75f;
        float y0 = rChat.y + scale * -76f;
        float x0 = rChat.x;
        float width = artWidth;
        if (banner.tier >= 2 && banner.skin > 0 &&
            HornArt.Inner(banner.skin, out int tx, out int ty, out int tw, out int th))
        {
            scale = s;
            x0 += tx * scale;
            width = tw * scale;
        }
        Rect rect = new Rect(x0, y0, expand * width, height);
        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 33f / 255f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
        DrawPanel(rect);
        if (expand < 1f) return;
        if (banner.tier >= 2 && banner.skin > 0)
            HornArt.Draw(banner.skin, new Rect(rChat.x, rect.y, artWidth, height), s, elapsed * 1000f);
        if (banner.rows == null)
        {
            string text = (string.IsNullOrEmpty(banner.name) ? "" : banner.name + ": ") + banner.text;
            scale = s;
            banner.rows = EmoteText.Wrap(text, width + scale * -16f, scale);
        }
        int count = Math.Min(3, banner.rows.Count);
        Color color = banner.tier >= 2 ? Color.white : new Color32(255, 215, 60, 255);
        scale = s;
        float y = (rect.y + scale * 6f) + scale * ((3 - count) * 9);
        for (int i = 0; i < count; i++)
        {
            scale = s;
            float rowY = y + scale * (i * 18);
            DrawShareRow(rect.x + scale * 8f, rowY, banner.rows[i], color, banner.shares, s, true);
            scale = s;
            ShareHover(rect.x + scale * 8f, rowY, banner.rows[i], banner.shares, s);
        }
    }

}
}
