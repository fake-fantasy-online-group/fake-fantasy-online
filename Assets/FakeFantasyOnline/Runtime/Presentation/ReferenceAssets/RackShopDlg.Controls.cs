using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class RackShopDlg
    {
        private const float NW = 844f;
        private const float NH = 597f;
        private bool visible;
        private bool tried;
        private float s;
        private float winY;
        private int topTab;
        private Texture2D bg;
        private Texture2D shopBk;
        private Texture2D icoClose;
        private Texture2D itemBk;
        private Texture2D[] topShop;
        private Texture2D[] topCz;
        private Texture2D[] topCzr;
        private Texture2D[] buyBtn;
        private Texture2D[] clearBtn;
        private Texture2D[] carBtn;
        private Texture2D[] pageUp;
        private Texture2D[] pageDn;
        private GUIStyle _qtySt;

        // Official 0x06000aae: retry only when the resource owner is not ready.
        private void Load()
        {
            tried = true;
            ResLoader loader = ResLoader.I;
            if (loader == null) { tried = false; return; }
            bg = loader.LoadGsa2021First("res/uires/rackdlg/shopdlgbk.gsa");
            shopBk = loader.LoadGsa2021First("res/uires/rackdlg/shopbk.gsa");
            icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
            topShop = Frames(loader, "res/uires/rackdlg/shoptab.gsa", 2);
            topCz = Frames(loader, "res/uires/rackdlg/chongzhitab.gsa", 2);
            topCzr = Frames(loader, "res/uires/rackdlg/chongzhireturntab.gsa", 2);
            itemBk = loader.LoadGsa2021First("res/uires/rackdlg/exitembk.gsa");
            buyBtn = Frames(loader, "res/uires/rackdlg/shopbuybtn.gsa", 4);
            clearBtn = Frames(loader, "res/uires/rackdlg/clearbtn.gsa", 4);
            carBtn = Frames(loader, "res/uires/rackdlg/carbtn.gsa", 3);
            pageUp = Frames(loader, "res/uires/common/pageup.gsa", 4);
            pageDn = Frames(loader, "res/uires/common/pagedn.gsa", 4);
        }

        // Official 0x06000aaf. Preserve frame order and the 2021-first mount lookup.
        private static Texture2D[] Frames(ResLoader loader, string path, int n)
        {
            var frames = new Texture2D[n];
            for (int i = 0; i < n; i++) frames[i] = loader.LoadGsaFrameA2021First(path, i);
            return frames;
        }

        // Official 0x06000a9d.
        private GUIStyle QtyStyle()
        {
            if (_qtySt == null)
            {
                var style = new GUIStyle(GUIStyle.none);
                style.alignment = TextAnchor.MiddleCenter;
                style.font = NpcChatter.CnFont();
                _qtySt = style;
                _qtySt.normal.textColor = Color.white;
            }
            _qtySt.fontSize = (int)Math.Round(s * 11f);
            return _qtySt;
        }

        // Official 0x06000ab0..0x06000ab2.
        private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
        private void Lbl(float x, float y, string text, Color color)
        {
            if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s);
        }
        private void LblC(Rect rect, string text, Color color)
        {
            if (BitmapFont.Ready) BitmapFont.CenterOutline(rect, text, color, s);
        }

        // Official 0x06000ab3.
        private void FrameBox(Rect rect)
        {
            float thickness = Mathf.Max(1f, s);
            GUI.color = new Color(0f, 0f, 0f, 0.12f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(17f / 255f, 17f / 255f, 17f / 255f, 1f);
            EdgeRect(rect, thickness);
            GUI.color = new Color(110f / 255f, 110f / 255f, 110f / 255f, 1f);
            EdgeRect(new Rect(rect.x + thickness, rect.y + thickness,
                rect.width - 2f * thickness, rect.height - 2f * thickness), thickness);
            GUI.color = Color.white;
        }

        // Official 0x06000ab4; uses the caller's colour.
        private void EdgeRect(Rect rect, float thickness)
        {
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        }

        // Official 0x06000ad1. Selection remains the caller's decision.
        private bool TopTab(float x, string name, Texture2D[] frames, int index, Vector2 mouse)
        {
            bool selected = topTab == index;
            Texture2D texture = frames == null ? null : (frames[selected ? 1 : 0] ?? frames[0]);
            Rect rect = L(x, 47f, 74f, 20f);
            if (texture != null)
            {
                float brightness = rect.Contains(mouse) && !selected ? 1.15f : 1f;
                GUI.color = new Color(brightness, brightness, brightness, 1f);
                GUI.DrawTexture(rect, texture);
                GUI.color = Color.white;
            }
            else LblC(rect, name, selected ? GOLD : GRAY);
            return Click(rect, mouse);
        }

        // Official 0x06000ad2: this member Uses but does not suppress the world click.
        private bool StateBtn(Rect rect, Texture2D[] frames, string fallback, Vector2 mouse)
        {
            bool hover = rect.Contains(mouse);
            Texture2D texture = frames != null && frames.Length > 0
                ? frames[hover && frames.Length > 1 ? 1 : 0] : null;
            if (texture != null) GUI.DrawTexture(rect, texture);
            else
            {
                GUI.color = hover ? new Color(0.55f, 0.42f, 0.18f, 0.95f) : new Color(0.3f, 0.26f, 0.16f, 0.88f);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                LblC(rect, fallback, GOLD);
            }
            if (!hover || SysMsgBox.Visible) return false;
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return false;
            e.Use();
            return true;
        }

        // Official 0x06000ad3.
        private bool Click(Rect rect, Vector2 mouse)
        {
            if (!rect.Contains(mouse) || SysMsgBox.Visible) return false;
            Event e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0) return false;
            e.Use();
            PlayerController.SuppressWorldClick();
            return true;
        }
    }
}
