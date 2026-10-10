using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private string chatBubble;
    private float chatBubbleUntil;

    public void ShowChatBubble(string text, string theme)
    {
        if (string.IsNullOrEmpty(text)) return;
        chatBubble = text;
        chatBubbleTheme = theme ?? "huang";
        chatBubbleUntil = Time.time + 6f;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(chatBubble) || Time.time > chatBubbleUntil) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        float height = avatar != null && avatar.headPx > 0 ? (float)avatar.headPx / ppu + 0.2f : 1.6f;
        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * height);
        if (screen.z <= 0f) return;
        GUI.depth = 100;
        NpcChatter.DrawBubble(screen.x, Screen.height - screen.y, chatBubble, chatBubbleTheme);
    }
}

}
