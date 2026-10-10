using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 LevelUpFX owner (tokens 06000588..0600058B).
/// </summary>
/// <remarks>
/// Recovered from the frozen arm64 + x86_64 decompilations of the official 1.5.8 client
/// (<c>Build/ReferenceReverseGhidra-1.5.8/queries/&lt;arch&gt;/&lt;probe&gt;/packet-LevelUpFX/</c>).
/// Every constant below was read out of the native instruction stream, not from the Cpp2IL
/// managed draft: DUR 0.49 is <c>mov w8,#0xe148 / movk w8,#0x3efa,lsl #16</c> at 0x46e610,
/// and the Cpp2IL draft mis-renders the same constant as 1056629064.0.
/// </remarks>
public class LevelUpFX : MonoBehaviour
{
    private const float DUR = 0.49f;
    private const float Z0 = 100f;
    private const float Z1 = 120f;
    private const float S0 = 1f;
    private const float S1 = 1.5f;

    private SpriteRenderer sr;
    private Transform follow;
    private float t;

    /// <summary>
    /// Token 06000588. Official has no in-assembly caller; this is the public entry point.
    /// </summary>
    public static void Play()
    {
        Transform player = GameBootstrap.PlayerT;
        if (player == null) return;
        if (ResLoader.I == null) return;
        Texture2D tex = ResLoader.I.LoadTex("magic/magic/magic0139.gso", 0);
        if (tex == null) return;

        GameObject go = new GameObject("LevelUpFX");
        LevelUpFX fx = go.AddComponent<LevelUpFX>();
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();

        fx.sr = renderer;
        Sprite sprite = TexScale.MakeSprite(tex, 100f, 0.5f, 0f);
        renderer.sprite = sprite;
        // Official re-reads the field here rather than reusing the local.
        fx.sr.sortingOrder = 20000;
        fx.follow = player;
        fx.UpdatePos();
    }

    /// <summary>
    /// Token 06000589. Z and scale ramps are clamped independently: position uses
    /// <c>Clamp01</c> while scale uses <c>Min(p, 1)</c>, which is what arm64 0x46e6a4..0x46e6ac
    /// and x86_64 <c>minss</c> both emit.
    /// </summary>
    private void UpdatePos()
    {
        if (follow == null) return;

        float p = Mathf.Clamp01(t / DUR);
        float rise = (p * (Z1 - Z0) + Z0) / Z0;
        transform.position = follow.position + new Vector3(0f, rise, 0f);

        float scale = Mathf.Min(p, 1f) * (S1 - S0) + S0;
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    /// <summary>Token 0600058A.</summary>
    private void Update()
    {
        t += Time.deltaTime;
        if (t >= DUR)
        {
            Object.Destroy(gameObject);
            return;
        }

        UpdatePos();
    }
}

}
