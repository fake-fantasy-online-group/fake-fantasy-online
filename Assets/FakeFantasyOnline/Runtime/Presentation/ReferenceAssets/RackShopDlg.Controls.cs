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
                _qtySt.normal.textColor = _qtySt.hover.textColor = _qtySt.active.textColor = _qtySt.focused.textColor = Color.white;
                _qtySt.onNormal.textColor = _qtySt.onHover.textColor = _qtySt.onActive.textColor = _qtySt.onFocused.textColor = Color.white;
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

        // Shared patch artwork preserves its corners at every shop panel size.
        private void FrameBox(Rect rect)
            => ArtworkSkin.Panel(rect, s);

        // Draw only: shop controls retain their original MouseDown/Click contract.
        private void TextButtonFace(Rect rect, string text, Vector2 mouse, bool selected = false, bool enabled = true)
        {
            bool usable = enabled && GUI.enabled && !SysMsgBox.Visible;
            bool pressed = usable && rect.Contains(mouse) && Input.GetMouseButton(0);
            ArtworkSkin.ButtonFace(rect, text, s, usable, selected || usable && rect.Contains(mouse), 1f, pressed);
        }

        private void IconButtonFace(Rect rect, string path, string fallback, Vector2 mouse, bool enabled = true)
        {
            bool usable = enabled && GUI.enabled && !SysMsgBox.Visible;
            bool hover = usable && rect.Contains(mouse);
            Texture2D[] frames = ArtworkSkin.Frames(path);
            if (frames != null && frames.Length > 0 && frames[0] != null)
                ArtworkSkin.IconFace(rect, frames, hover, hover && Input.GetMouseButton(0), usable);
            else TextButtonFace(rect, fallback, mouse, false, enabled);
        }

        // Keep long dynamic labels inside their allocated region without hiding data.
        private void FitLabel(Rect rect, string text, Color color)
        {
            if (!BitmapFont.Ready || string.IsNullOrEmpty(text)) return;
            float font = s;
            float width = BitmapFont.Measure(text, font);
            if (width > rect.width && width > 0f) font *= rect.width / width;
            font = Mathf.Min(font, rect.height / 12f);
            BitmapFont.CenterOutline(rect, text, color, font);
        }

        // All text-field states stay transparent over the artwork, including focus.
        private GUIStyle InputStyle(ref GUIStyle style, float fontSize, TextAnchor alignment)
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.textField) { font = NpcChatter.CnFont(), alignment = alignment };
                style.normal.background = style.hover.background = style.active.background = style.focused.background = null;
                style.onNormal.background = style.onHover.background = style.onActive.background = style.onFocused.background = null;
                style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = Color.white;
                style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = Color.white;
            }
            style.fontSize = Mathf.RoundToInt(s * fontSize);
            style.padding.left = style.padding.right = Mathf.RoundToInt(4f * s);
            style.padding.top = style.padding.bottom = 0;
            return style;
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
            else TextButtonFace(rect, name, mouse, selected);
            return Click(rect, mouse);
        }

        // Official 0x06000ad2: this member Uses but does not suppress the world click.
        private bool StateBtn(Rect rect, Texture2D[] frames, string fallback, Vector2 mouse)
            => StateBtn(rect, frames, fallback, mouse, false);

        private bool StateBtn(Rect rect, Texture2D[] frames, string fallback, Vector2 mouse, bool selected)
        {
            bool hover = rect.Contains(mouse);
            bool enabled = GUI.enabled && !SysMsgBox.Visible;
            int state = !enabled ? 3 : hover && Input.GetMouseButton(0) ? 2 : hover || selected ? 1 : 0;
            Texture2D texture = frames != null && frames.Length > 0
                ? frames[Mathf.Min(state, frames.Length - 1)] ?? frames[0] : null;
            if (texture != null) GUI.DrawTexture(rect, texture);
            else TextButtonFace(rect, fallback, mouse, selected);
            if (!hover || !enabled) return false;
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
