using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    private string bubbleText;
    private float bubbleUntil;
    private int emoteFace;
    private float emoteStart;
    private float emoteUntil;
    private Camera nameCam;

    public void ShowBubble(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        bubbleText = text;
        bubbleUntil = Time.time + 5f;
    }

    public void ShowEmote(int face)
    {
        if (face <= 0) return;
        emoteFace = face;
        emoteStart = Time.time;
        emoteUntil = Time.time + 5f;
    }

    private void OnGUI()
    {
        bool bubbleLive = !string.IsNullOrEmpty(bubbleText) && Time.time < bubbleUntil;
        bool emoteLive = emoteFace > 0 && Time.time < emoteUntil;
        if (!bubbleLive && !emoteLive) return;
        if (dying) return;
        if (friendly && !CharData.petShow) return;
        if (nameCam == null) nameCam = Camera.main;
        if (nameCam == null) return;
        GUI.depth = 100;
        Vector3 screen = nameCam.WorldToScreenPoint(transform.position + new Vector3(0f, headTopU + 0.15f, 0f));
        if (screen.z <= 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height) return;
        float scale = UIScale.Factor;
        float topY = (Screen.height - screen.y) + (-12f * scale);
        if (bubbleLive) NpcChatter.DrawBubble(screen.x, topY, bubbleText, "guaiwu");
        if (emoteLive) EmoteRender.Draw(screen.x, topY - 4f, emoteFace, emoteStart);
    }
}

}
