using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI : MonoBehaviour
{


    // Official 1.5.8 token 0x060000ea.
    public static void DrawShareRow(float x, float y, string rowTxt, Color col,
        List<Share> shares, float s, bool outline)
    {
        void DrawSeg(string text, Color color)
        {
            if (outline) EmoteText.DrawOutline(x, y, text, color, s);
            else EmoteText.DrawColored(x, y, text, color, s, false);
        }

        if (shares == null || shares.Count == 0 || string.IsNullOrEmpty(rowTxt))
        {
            DrawSeg(rowTxt, col);
            return;
        }
        var spans = new List<(int start, int end, Color color)>();
        int searchStart = 0;
        foreach (Share share in shares)
        {
            string token = "【" + share.name + "】";
            int start = rowTxt.IndexOf(token, searchStart, StringComparison.Ordinal);
            if (start < 0) continue;
            searchStart = start + token.Length;
            Color color = share.kind == 1 ? new Color(.55f, .92f, .85f, 1f)
                : (Color)UITip.QualityColor(share.quality);
            spans.Add((start, start + token.Length, color));
        }
        if (spans.Count == 0)
        {
            DrawSeg(rowTxt, col);
            return;
        }
        spans.Sort((a, b) => a.start.CompareTo(b.start));
        int offset = 0;
        foreach (var span in spans)
        {
            if (span.start > offset)
            {
                string plain = rowTxt.Substring(offset, span.start - offset);
                DrawSeg(plain, col);
                x = x + EmoteText.Measure(plain, s);
            }
            string token = rowTxt.Substring(span.start, span.end - span.start);
            DrawSeg(token, span.color);
            x = x + EmoteText.Measure(token, s);
            offset = span.end;
        }
        if (offset < rowTxt.Length) DrawSeg(rowTxt.Substring(offset), col);
    }

    // Official 1.5.8 token 0x060000eb. The bounds are inclusive and are not Rect.Contains.
    public static void ShareHover(float x, float y, string rowTxt, List<Share> shares, float s)
    {
        if (shares == null || shares.Count == 0 || string.IsNullOrEmpty(rowTxt)) return;
        Vector2 mouse = Event.current.mousePosition;
        if (mouse.y < y - (s + s) || mouse.y > y + s * 13f) return;
        int searchStart = 0;
        foreach (Share share in shares)
        {
            string token = "【" + share.name + "】";
            int start = rowTxt.IndexOf(token, searchStart, StringComparison.Ordinal);
            if (start < 0) continue;
            int length = token.Length;
            float before = EmoteText.Measure(rowTxt.Substring(0, start), s);
            float tokenWidth = EmoteText.Measure(token, s);
            searchStart = length + start;
            float left = x + before;
            float right = left + tokenWidth;
            if (mouse.x < left || mouse.x > right) continue;
            if (share.kind == 1 && share.pet.HasValue)
                PetInfoTooltip.Show(share.pet.Value, mouse, s, "(分享的宠物)");
            else
                UITip.Show(TooltipContent.Fmt(TooltipContent.QualityName(share.name, share.quality), share.desc), s);
            return;
        }
    }

    // Production ReadShares supplies this exact List<Share>; custom read-only collections
    // keep an explicit copy at their adapter edge. The PetBrief skill list is never projected.
    internal static List<Share> MapIncomingShares(IReadOnlyList<WorldChatSharedEntryState> incoming)
    {
        if (incoming == null) return null;
        return incoming as List<Share> ?? new List<Share>(incoming);
    }
}
}
