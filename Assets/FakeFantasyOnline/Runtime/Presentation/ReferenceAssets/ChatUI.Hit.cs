using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // 060000f5: the original current ChatUI owns this hit query, independently of
    // any map view. Reuse the closed CHAT_HIT_BOTTOM_LAYOUT geometry/read order.
    public bool MouseOver()
    {
        if (MobileClientPresentation.Enabled)
        {
            Vector2 mobilePoint = MobileClientPresentation.GuiPoint(Input.mousePosition);
            return MobileClientPresentation.ChatRect.Contains(mobilePoint) ||
                (chanOpen && MobileChannelPanelRect().Contains(mobilePoint));
        }
        float scale = s > 0f ? s : UIScale.Factor;
        Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        int viewportHeight = Screen.height;
        Rect bar = new Rect(0f,
            (viewportHeight - 36f * scale) - GameHud.BottomHotbarHeight(scale, Screen.width),
            418f * scale, 30f * scale);
        if (bar.Contains(point)) return true;
        if (panelsVisible &&
            new Rect(0f, bar.y - 120f * scale, 24f * scale, 120f * scale).Contains(point))
            return true;
        if (faceOpen)
        {
            float width = EmotePicker.PanelW(scale);
            float height = EmotePicker.PanelH(scale);
            if (new Rect(412f * scale - width, bar.y - height - 3f * scale, width, height).Contains(point))
                return true;
        }
        if (chanOpen && ChannelPanelRect(bar.y, scale).Contains(point)) return true;
        return FilterMouseOverForIndependentRuntime(point, bar.y, scale);
    }
}
}
