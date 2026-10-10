using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private void PickerRects(out float bx, out float by, out float trackX, out float trackY, out float travel)
    {
        bx = winX + (s * NW - s * 147f) * 0.5f;
        by = winY + (s * NH - s * 41f) * 0.5f;
        trackX = bx + 21f * s;
        trackY = by + 27f * s;
        travel = 80f * s;
    }

    private void PickerInput(Vector2 mp)
    {
        void CommitCart()
        {
            int quantity = int.TryParse(pickText, out int parsed) ? parsed : pickQty;
            AddToCart(pickTab, pickId, pickBagIdx, pickName, pickDesc, pickIcon, pickUnit,
                Mathf.Clamp(quantity, 1, pickMax), pickQuality, pickStallIdx);
            picking = false;
        }
        Event current = Event.current;
        if (current.type == EventType.KeyDown)
        {
            if (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
            {
                CommitCart();
                current.Use();
            }
            else if (current.keyCode == KeyCode.Escape)
            {
                picking = false;
                current.Use();
            }
            return;
        }
        if (current.type != EventType.MouseDown && current.type != EventType.MouseDrag &&
            current.type != EventType.MouseUp && current.type != EventType.ScrollWheel) return;
        PickerRects(out float bx, out float by, out float trackX, out float trackY, out float travel);
        Rect P(float x, float y, float w, float h) => new Rect(bx + x * s, by + y * s, w * s, h * s);
        void SetFromX(float mx)
        {
            float fraction = travel > 0f ? Mathf.Clamp01((mx - 12f * s - trackX) / travel) : 0f;
            SetPickVal(unchecked(1 + Mathf.RoundToInt(fraction * unchecked(pickMax - 1))));
        }
        if (current.type == EventType.MouseDown && current.button == 0)
        {
            if (P(24, 7, 54, 12).Contains(mp)) return;
            if (P(2, 3, 15, 20).Contains(mp)) SetPickVal(unchecked(pickQty - 1));
            else if (P(83, 3, 15, 20).Contains(mp)) SetPickVal(unchecked(pickQty + 1));
            else if (P(2, 26, 18, 10).Contains(mp)) SetPickVal(1);
            else if (P(124, 28, 18, 10).Contains(mp)) SetPickVal(pickMax);
            else if (P(100, 2, 44, 22).Contains(mp)) CommitCart();
            else if (new Rect(trackX - 12f * s, trackY - 4f * s, travel + 24f * s, 22f * s).Contains(mp))
            {
                pickDrag = true;
                SetFromX(mp.x);
            }
            else if (!P(0, 0, 147, 41).Contains(mp)) picking = false;
        }
        else if (current.type == EventType.MouseDrag && pickDrag) SetFromX(mp.x);
        else if (current.type == EventType.MouseUp) pickDrag = false;
        current.Use();
    }

    private void PickerRender(Vector2 mp)
    {
        GUI.color = new Color(0, 0, 0, 0.35f);
        GUI.DrawTexture(new Rect(winX, winY, NW * s, NH * s), Texture2D.whiteTexture);
        GUI.color = Color.white;
        PickerRects(out float bx, out float by, out float trackX, out float trackY, out float travel);
        Rect P(float x, float y, float w, float h) => new Rect(bx + x * s, by + y * s, w * s, h * s);
        Rect box = P(0, 0, 147, 41);
        if (numBg != null) GUI.DrawTexture(box, numBg);
        else
        {
            GUI.color = new Color(0.12f, 0.1f, 0.14f, 0.98f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        // UiInput.Draw (0x06000fd1) already drew the menu from the control that opened it.
        string text = UiInput.Field(P(24, 7, 54, 12), "pickNum", pickText ?? "", 3, PickNumStyle(), s);
        if (text != pickText)
        {
            pickText = new string(Array.FindAll((text ?? "").ToCharArray(), char.IsDigit));
            if (!int.TryParse(pickText, out int parsed)) pickQty = 0;
            else if (parsed > pickMax)
            {
                pickQty = pickMax;
                pickText = pickQty.ToString();
            }
            else pickQty = Mathf.Max(0, parsed);
        }
        void Btn(Rect rect, Texture2D texture)
        {
            if (texture == null) return;
            float tint = rect.Contains(mp) ? 1.25f : 1f;
            GUI.color = new Color(tint, tint, tint, 1f);
            GUI.DrawTexture(rect, texture);
            GUI.color = Color.white;
        }
        Btn(P(2, 3, 15, 20), spinL);
        Btn(P(83, 3, 15, 20), spinR);
        Btn(P(2, 26, 18, 10), minBtn);
        Btn(P(124, 28, 18, 10), maxBtn);
        Btn(P(100, 2, 44, 22), inputBtn);
        float fraction = pickMax > 1 ? (float)unchecked(pickQty - 1) / unchecked(pickMax - 1) : 0f;
        Rect thumb = new Rect(trackX + travel * fraction, trackY, 24f * s, 14f * s);
        if (slide != null) GUI.DrawTexture(thumb, slide);
        else
        {
            GUI.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            GUI.DrawTexture(thumb, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
}
