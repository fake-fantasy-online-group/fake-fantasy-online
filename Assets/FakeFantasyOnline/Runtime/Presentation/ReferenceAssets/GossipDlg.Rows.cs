using Share = FakeFantasyOnline.Application.WorldChatSharedEntryState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class GossipDlg
    {
        // Official 0x060005e3.
        private void DrawMsgList(List<string> lines, Rect area, Rect scrollbar, Vector2 mouse)
        {
            if (!BitmapFont.Ready) return;
            float lineHeight = 13f * s;
            int visibleRows = Mathf.Max(1, Mathf.FloorToInt(area.height / lineHeight));
            float wrapWidth = area.width - 6f * s;
            var rows = new List<string>();
            var rowShares = new List<List<Share>>();
            List<List<Share>> shares = ShrOf(peer);
            var originalRows = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                List<Share> lineShares = i < shares.Count ? shares[i] : null;
                foreach (string row in EmoteText.Wrap(lines[i], wrapWidth, s))
                {
                    rows.Add(row);
                    rowShares.Add(lineShares);
                    originalRows.Add(lines[i]);
                }
            }
            int maximum = Math.Max(0, rows.Count - visibleRows);
            Event e = Event.current;
            if (area.Contains(mouse) && e.type == EventType.ScrollWheel)
            {
                followBottom = false;
                listScroll = Mathf.Clamp(listScroll + (e.delta.y < 0f ? -1f : 1f), 0f, maximum);
                e.Use();
            }
            if (followBottom) listScroll = maximum;
            listScroll = Mathf.Clamp(listScroll, 0f, maximum);
            int first = (int)Math.Round(listScroll);
            NativeScroll.BarRows(this, scrollbar, visibleRows, rows.Count, ref first);
            listScroll = first;
            followBottom = first >= maximum;
            float y = area.y + 2f * s;
            for (int i = first; i < Math.Min(first + visibleRows, rows.Count); i++)
            {
                ChatUI.DrawShareRow(area.x + 3f * s, y, rows[i], new Color(0.92f, 0.92f, 0.86f, 1f), rowShares[i], s, true);
                ChatUI.ShareHover(area.x + 3f * s, y, rows[i], rowShares[i], s);
                if (e.type == EventType.MouseDown && e.button == 1 && new Rect(area.x, y - 2f * s, area.width, lineHeight).Contains(mouse))
                {
                    int id = peer.Length > 0 ? CombatWorld.FindPlayerIdByName(peer) : 0;
                    PlayerActionMenu.Open(id, "", originalRows[i], string.Join("\n", lines));
                    e.Use();
                }
                y += lineHeight;
            }
        }

        // Official 0x060005e5: compact tail rendering has no scroll state and no peer action target.
        private void DrawLines(List<string> lines, Rect area)
        {
            if (!BitmapFont.Ready) return;
            float lineHeight = 13f * s;
            int visibleRows = Mathf.Max(1, Mathf.FloorToInt(area.height / lineHeight));
            float wrapWidth = area.width - 6f * s;
            var rows = new List<string>();
            var rowShares = new List<List<Share>>();
            List<List<Share>> shares = ShrOf(peer);
            for (int i = 0; i < lines.Count; i++)
            {
                List<Share> lineShares = i < shares.Count ? shares[i] : null;
                foreach (string row in EmoteText.Wrap(lines[i], wrapWidth, s))
                {
                    rows.Add(row);
                    rowShares.Add(lineShares);
                }
            }
            int first = Math.Max(0, rows.Count - visibleRows);
            float y = area.y + 2f * s;
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            for (int i = first; i < rows.Count; i++)
            {
                ChatUI.DrawShareRow(area.x + 3f * s, y, rows[i], new Color(0.92f, 0.92f, 0.86f, 1f), rowShares[i], s, true);
                ChatUI.ShareHover(area.x + 3f * s, y, rows[i], rowShares[i], s);
                if (e.type == EventType.MouseDown && e.button == 1 && new Rect(area.x, y - 2f * s, area.width, lineHeight).Contains(mouse))
                {
                    PlayerActionMenu.Open(0, "", rows[i], string.Join("\n", lines));
                    e.Use();
                }
                y += lineHeight;
            }
        }
    }
}
