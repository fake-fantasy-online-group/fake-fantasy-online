using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class PlayerEmote : MonoBehaviour
{
    public static PlayerEmote Local;
    private int face;
    private float start;
    private float until;
    private string bubble;
    private float bubbleUntil;
    private string bubbleTheme = "huang";
    private PlayerNameTag nameTag;

    private void Awake() { Local = this; nameTag = GetComponent<PlayerNameTag>(); }
    private void OnDestroy() { if (Local == this) Local = null; }
    public void ShowEmote(int f)
    {
        if (f < 1) return;
        face = f;
        start = Time.time;
        until = Time.time + 5f;
    }
    public void ShowBubble(string text, string theme)
    {
        if (string.IsNullOrEmpty(text)) return;
        bubble = text;
        bubbleTheme = theme ?? "huang";
        bubbleUntil = Time.time + 6f;
    }
    private void OnGUI()
    {
        bool emoteLive = face > 0 && Time.time <= until;
        bool bubbleLive = !string.IsNullOrEmpty(bubble) && Time.time <= bubbleUntil;
        if (!emoteLive && !bubbleLive) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (nameTag == null) nameTag = GetComponent<PlayerNameTag>();
        float height = nameTag != null ? nameTag.nameHeight + 0.1f : 1.3f;
        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * height);
        if (screen.z <= 0f) return;
        GUI.depth = 100;
        float y = Screen.height - screen.y;
        if (emoteLive) EmoteRender.Draw(screen.x, y, face, start);
        if (bubbleLive) NpcChatter.DrawBubble(screen.x, y - 22f, bubble, bubbleTheme);
    }
}

}
