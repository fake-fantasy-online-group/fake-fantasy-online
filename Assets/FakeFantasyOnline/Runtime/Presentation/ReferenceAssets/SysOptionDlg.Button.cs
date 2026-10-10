using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysOptionDlg
{
    private static bool PlayerDead => PlayerController.I != null && PlayerController.I.IsDead;

    private bool BtnImg(Rect rect, Texture2D texture, string label, Vector2 mouse)
    {
        bool hover = rect.Contains(mouse);
        if (texture != null)
        {
            float brightness = hover ? 1.3f : 1f;
            GUI.color = new Color(brightness, brightness, brightness, 1f);
            GUI.DrawTexture(rect, texture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = hover
                ? new Color(0.55f, 0.42f, 0.18f, 0.95f)
                : new Color(0.3f, 0.26f, 0.16f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready)
                BitmapFont.CenterOutline(rect, label, new Color(1f, 0.92f, 0.66f, 1f), s);
        }
        if (hover && !SysMsgBox.Visible && Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            Event.current.Use();
            return true;
        }
        return false;
    }
}

}
