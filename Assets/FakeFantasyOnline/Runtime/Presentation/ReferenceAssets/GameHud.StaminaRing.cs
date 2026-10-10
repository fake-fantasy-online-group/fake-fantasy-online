using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Official 04000604/04000605, instance +0x228/+0x230. The -1 initializer
    // is ordered with the other instance fields in GameHud.Initialization.cs.
    private Texture2D staminaRing;

    // Official 04000608, static +0xb0. The 06000455 initializer copies these
    // exact twelve floats from field 040017e3 (48-byte SHA256 51d8093a...b6138).
    // Its initializer is ordered in GameHud.Initialization.cs.

    // 06000422, original 1.5.8 GameHud.UpdateStaminaRing. Owner-only: no
    // recovered caller currently reaches this body. It is separate from both
    // BuildRingTex and the loaded naili/lianli texture-mask drawing carrier.
    private void UpdateStaminaRing()
    {
        float fraction = CharData.maxStamina < 1
            ? 1f
            : Mathf.Clamp01((float)CharData.stamina / CharData.maxStamina);
        int quantized = Mathf.RoundToInt(fraction * 50f);
        if (quantized == ringFracQ && staminaRing != null)
        {
            return;
        }

        ringFracQ = quantized;
        int profileLength = BarProf.Length;
        Color32[] pixels = new Color32[112 * 112];
        float limit = (quantized / 50f) * Mathf.PI;
        for (int y = 0; y < 112; y++)
        {
            for (int x = 0; x < 112; x++)
            {
                float alpha = 0f;
                float red = 0f;
                float green = 0f;
                float blue = 0f;
                for (int sampleY = 0; sampleY < 2; sampleY++)
                {
                    float py = y + (sampleY + 0.5f) * 0.5f;
                    for (int sampleX = 0; sampleX < 2; sampleX++)
                    {
                        float px = x + (sampleX + 0.5f) * 0.5f;
                        if (py <= px) continue;

                        float dx = px - 55f;
                        float dy = py - 55f;
                        float radius = Mathf.Sqrt(dx * dx + dy * dy);
                        if (radius < 44f || radius > 57f) continue;
                        float angle = Mathf.Repeat(
                            Mathf.Atan2(dy, dx) - Mathf.PI * 0.25f,
                            Mathf.PI * 2f);
                        if (angle > limit) continue;

                        float position = Mathf.Clamp01((radius - 44f) / 13f)
                            * (profileLength - 1);
                        int first = (int)position;
                        int second = Mathf.Min(first + 1, profileLength - 1);
                        float profile = Mathf.Lerp(BarProf[first], BarProf[second],
                            position - first);
                        float weight = Mathf.Clamp01(
                            Mathf.Min(radius - 44f, 57f - radius) / 2.6f);
                        alpha += weight;
                        red += (214f * profile) * weight;
                        green += (172f * profile) * weight;
                        blue += (56f * profile) * weight;
                    }
                }

                pixels[(111 - y) * 112 + x] = alpha > 0f
                    ? new Color32((byte)(red / alpha), (byte)(green / alpha),
                        (byte)(blue / alpha), (byte)((alpha * 0.25f) * 255f))
                    : new Color32(0, 0, 0, 0);
            }
        }

        if (staminaRing == null)
        {
            Texture2D texture = new Texture2D(112, 112, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            staminaRing = texture;
        }

        staminaRing.SetPixels32(pixels);
        staminaRing.Apply();
    }
}
}
