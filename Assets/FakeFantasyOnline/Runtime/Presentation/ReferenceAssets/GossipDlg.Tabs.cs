using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class GossipDlg
    {
        // Official 0x060005da.
        private void DrawSessionTabs(Vector2 mouse)
        {
            if (peerOrder.Count < 2) { stripRect = Rect.zero; return; }
            int maximum = Mathf.Max(0, peerOrder.Count - TabsPerPage);
            tabPage = Mathf.Clamp(tabPage, 0, maximum);
            stripRect = ComputeStripRect();
            Rect strip = stripRect;
            PopUI.Panel(strip);
            if (Event.current.type == EventType.ScrollWheel && strip.Contains(mouse) && maximum > 0)
            {
                tabPage = Mathf.Clamp(tabPage + (Event.current.delta.y < 0f ? -1 : 1), 0, maximum);
                Event.current.Use();
            }
            for (int i = 0; i < TabsPerPage; i++)
            {
                int index = tabPage + i;
                if (index >= peerOrder.Count) break;
                string name = peerOrder[index];
                bool selected = name == peer;
                Rect rect = new Rect(strip.x + 2f * s + i * 57f * s, strip.y + 2f * s, TabW * s, TabH * s);
                bool hover = rect.Contains(mouse);
                bool hasUnread = unreadPeers.Contains(name);
                GUI.color = selected ? new Color(0.55f, 0.45f, 0.18f, 0.95f)
                    : hover ? new Color(0.36f, 0.32f, 0.22f, 0.95f) : new Color(0.2f, 0.19f, 0.16f, 0.9f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                if (BitmapFont.Ready)
                {
                    string label = BitmapFont.Elide((hasUnread ? "*" : "") + name, rect.width - 4f * s, s);
                    Color color = hasUnread ? new Color(1f, 0.55f, 0.5f, 1f)
                        : selected ? new Color(1f, 0.95f, 0.7f, 1f) : new Color(0.82f, 0.8f, 0.74f, 1f);
                    BitmapFont.CenterOutline(rect, label, color, s);
                }
                if (PopUI.Click(rect, mouse) && !selected) { Open(name); return; }
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(mouse))
                {
                    PlayerActionMenu.Open(CombatWorld.FindPlayerIdByName(name), name);
                    Event.current.Use();
                }
            }
            if (maximum > 0 && BitmapFont.Ready)
                BitmapFont.CenterOutline(new Rect(strip.xMax - 22f * s, strip.y + 2f * s, 20f * s, 14f * s),
                    tabPage >= maximum ? "◀" : "▶", new Color(0.8f, 0.75f, 0.6f, 1f), s);
        }

        // Official 0x060005e2.
        private void DrawBar(Vector2 mouse)
        {
            Rect bar = L(-3f, -3f, BAR_W, BAR_H);
            Texture2D texture = unread && bar1 != null && FLASH[Mathf.FloorToInt(Time.unscaledTime * 8f) % FLASH.Length] == 1
                ? bar1 : bar0 ?? bar1;
            if (texture != null) GUI.DrawTexture(bar, texture);
            else PopUI.Panel(bar);
            if (BitmapFont.Ready)
                BitmapFont.DrawOutline(winX + 50f * s, winY + 21f * s, peer, new Color(1f, 0.92f, 0.66f, 1f), s);
            Rect restore = L(193f, 18f, 20f, 19f);
            if (icoMin != null) GUI.DrawTexture(restore, icoMin);
            if (PopUI.Click(restore, mouse)) { minimized = false; unread = false; return; }
            Rect close = L(213f, 18f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (PopUI.Click(close, mouse)) { visible = false; minimized = false; return; }
            WinDrag.Handle(this, L(-3f, -3f, 188f, BAR_H), ref winX, ref winY);
        }
    }
}
