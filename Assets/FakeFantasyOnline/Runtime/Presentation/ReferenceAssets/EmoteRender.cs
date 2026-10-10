using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class EmoteRender
{
    private const float FRAME_SEC = 0.3f;
    private const float SCALE = 1.6f;

    public static bool Draw(float cx, float topY, int face, float startTime)
    {
        if (face <= 0) return false;
        if (ResLoader.I == null) return false;
        string path = "res/npcicon/face" + face.ToString("D2") + ".gsa";
        int count = Mathf.Max(1, ResLoader.I.GsaFrameCount(path));
        int frame = unchecked((int)((Time.time - startTime) / FRAME_SEC)) % count;
        Texture2D texture = ResLoader.I.LoadGsaFrame(path, frame);
        if (texture == null) return false;
        float width = GsaDecoder.W(texture) * SCALE;
        float height = GsaDecoder.H(texture) * SCALE;
        UIScale.SnapDraw(new Rect(cx - width * 0.5f, topY - height, width, height), texture);
        return true;
    }
}

}
