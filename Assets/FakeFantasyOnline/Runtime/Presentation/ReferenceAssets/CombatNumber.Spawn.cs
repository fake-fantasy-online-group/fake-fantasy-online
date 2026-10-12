using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    public static void Spawn(Vector3 headWorldPos, int value, int type, float ppu, Transform anchor = null)
    {
        TrySpawnForIndependentRuntime(headWorldPos, value, type, ppu, anchor);
    }

    internal static bool TrySpawnForIndependentRuntime(
        Vector3 headWorldPos, int value, int type, float ppu, Transform anchor = null)
    {
        // Non-positive inputs are intentional native no-ops, rather than retryable failure.
        if (value <= 0) return true;
        if (!overlayMat)
        {
            Shader shader = Shader.Find("Overlay/NameTag");
            if (shader) overlayMat = new Material(shader);
        }
        var glyphs = BuildGlyphs(value, type);
        CombatNumber number = Rent();
        number.type = type;
        number.ppu = ppu;
        number.anchor = anchor;
        number.life = type == 4 ? 1.1f : 1.2f;
        number.hSign = Random.value < 0.5f ? -1f : 1f;
        number.basePos = headWorldPos;
        number.transform.position = headWorldPos;
        number.startYWorld = headWorldPos.y;
        if (glyphs != null && glyphs.Count > 0)
        {
            float effectivePpu = ppu * Mathf.Max(1, glyphScale);
            float maxHeight = 0f;
            foreach (Sprite sprite in glyphs)
                if (sprite) maxHeight = Mathf.Max(maxHeight, sprite.rect.height / effectivePpu);
            if (maxHeight <= 0.01f) maxHeight = 0.46f;
            float[] scales = new float[glyphs.Count];
            float[] widths = new float[glyphs.Count];
            float totalWidth = 0f;
            for (int i = 0; i < glyphs.Count; i++)
            {
                Sprite sprite = glyphs[i];
                float height = sprite ? sprite.rect.height / effectivePpu : maxHeight;
                scales[i] = height > 0.001f ? maxHeight / height * 0.7f : 0.7f;
                float width = sprite ? sprite.rect.width / effectivePpu : 0.2f;
                widths[i] = width * scales[i];
                totalWidth += widths[i];
            }
            float overlap = maxHeight * 0.12f * 0.7f;
            float x = (totalWidth - overlap * Mathf.Max(0, glyphs.Count - 1)) * -0.5f;
            for (int i = 0; i < glyphs.Count; i++)
            {
                SpriteRenderer child = number.NextChild();
                child.transform.localPosition = new Vector3(x + widths[i] * 0.5f, 0f, 0f);
                child.transform.localScale = new Vector3(scales[i], scales[i], 1f);
                child.sprite = glyphs[i];
                Color color = child.color;
                color.a = 1f;
                child.color = color;
                x += widths[i] - overlap;
            }
            return true;
        }
        if (BitmapFont.Ready)
        {
            Color color = type == 4 ? new Color(0.62f, 1f, 0.42f, 1f) : Color.white;
            string text = (type == 4 ? "EXP+" : string.Empty) + value.ToString();
            Texture2D texture = BitmapFont.BakeNameTag(text, string.Empty, color, color, 3);
            if (texture)
            {
                SpriteRenderer child = number.NextChild();
                child.transform.localPosition = Vector3.zero;
                child.transform.localScale = Vector3.one;
                Sprite sprite = Sprite.Create(texture,
                    new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
                child.sprite = sprite;
                Color tint = child.color;
                tint.a = 1f;
                child.color = tint;
                number._ownedTex = texture;
                number._ownedSp = sprite;
                return true;
            }
        }
        number.ReturnToPool();
        return false;
    }
}

}
