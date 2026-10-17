using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private void Tex(Rect rect, Texture2D texture)
    {
        if (texture != null) GUI.DrawTexture(rect, texture);
        else
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    private bool Click(Rect rect, Vector2 mouse)
    {
        if (uiBlocked) return false;
        if (rect.Contains(mouse) && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Event.current.Use();
            return true;
        }
        return false;
    }

    private bool Btn(Rect rect, Texture2D texture, Vector2 mouse)
    {
        bool hover = !uiBlocked && rect.Contains(mouse);
        if (texture != null)
        {
            float brightness = hover ? 1.25f : 1f;
            GUI.color = new Color(brightness, brightness, brightness, 1f);
        }
        else
        {
            GUI.color = hover
                ? new Color(0.55f, 0.42f, 0.18f, 0.95f)
                : new Color(0.3f, 0.26f, 0.16f, 0.88f);
            texture = Texture2D.whiteTexture;
        }
        GUI.DrawTexture(rect, texture);
        GUI.color = Color.white;
        if (hover && !SysMsgBox.Visible && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Event.current.Use();
            return true;
        }
        return false;
    }

    private void Slider(int index, Rect rect, Vector2 mouse)
    {
        bool mobile = MobileClientPresentation.Enabled;
        float minimum = mobile && index == 1 ? 0f : 1f;
        float range = 100f - minimum;
        float t = Mathf.Clamp01((sndVal[index] - minimum) / range);
        float knobWidth = 15f * s;
        float knobHeight = 20f * s;
        float travel = rect.width - knobWidth;
        Rect knobRect = new Rect(rect.x + t * travel,
            rect.y + rect.height * 0.5f - knobHeight * 0.5f, knobWidth, knobHeight);
        if (knob != null) GUI.DrawTexture(knobRect, knob);
        else
        {
            GUI.color = new Color(0.7f, 0.6f, 0.3f, 1f);
            GUI.DrawTexture(knobRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        bool pressed = !uiBlocked && Event.current.type == EventType.MouseDown && rect.Contains(mouse) &&
            (!mobile || Event.current.button == 0);
        if (pressed)
        {
            dragSlider = index;
            Event.current.Use();
        }
        // Keep the recovered desktop behavior; retain mobile press intent after
        // MouseDown becomes Used, even when no MouseDrag ever arrives.
        if (dragSlider == index &&
            (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseDown || (mobile && pressed)))
        {
            sndVal[index] = minimum + Mathf.Clamp01((mouse.x - rect.x - knobWidth * 0.5f) / travel) * range;
            if (!sndOn[index]) sndOn[index] = true;
            Event.current.Use();
        }
    }
}

}
