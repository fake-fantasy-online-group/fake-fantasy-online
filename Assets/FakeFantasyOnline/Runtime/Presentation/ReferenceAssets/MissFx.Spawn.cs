using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MissFx
{
    public static void Spawn(Vector3 worldPos, float ppu)
    {
        if (!EnsureLoaded()) return;
        MissFx instance = Rent();
        instance.transform.position = worldPos;
        instance.ppu = ppu;
        instance.basePos = worldPos;
        if (!overlayMat)
        {
            Shader shader = Shader.Find("Overlay/NameTag");
            if (shader) overlayMat = new Material(shader);
        }
        float effectivePpu = ppu * glyphScale;
        int count = glyphs.Length;
        float maxHeight = 0f;
        for (int i = 0; i < count; i++)
            if (glyphs[i]) maxHeight = Mathf.Max(maxHeight, glyphs[i].rect.height / effectivePpu);
        if (maxHeight <= 0.01f) maxHeight = 0.46f;
        float[] scales = new float[count];
        float[] widths = new float[count];
        float total = 0f;
        for (int i = 0; i < count; i++)
        {
            float height = glyphs[i] ? glyphs[i].rect.height / effectivePpu : maxHeight;
            scales[i] = height > 0.001f ? maxHeight / height * 0.9f : 0.9f;
            float width = glyphs[i] ? glyphs[i].rect.width / effectivePpu : 0.2f;
            widths[i] = width * scales[i];
            total += widths[i];
        }
        float overlap = maxHeight * 0.06f * 0.9f;
        float x = (total - overlap * Mathf.Max(0, count - 1)) * -0.5f;
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer child = instance.NextChild();
            child.transform.localPosition = new Vector3(x + widths[i] * 0.5f, 0f, 0f);
            child.transform.localScale = new Vector3(scales[i], scales[i], 1f);
            child.sprite = glyphs[i];
            x += widths[i] - overlap;
        }
    }
}

}
