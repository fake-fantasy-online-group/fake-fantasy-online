// Frozen pre-magnification CameraFollow behavior for desktop parity regression.
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// CameraFollowBaseline owner: retained 1.5.8 base with the 1.6.0 spectator-camera lock in LateUpdate.
/// </summary>
/// <remarks>
/// Recovered from the frozen arm64 + x86_64 decompilations of the official 1.5.8 client
/// (<c>Build/ReferenceReverseGhidra-1.5.8/queries/&lt;arch&gt;/&lt;probe&gt;/packet-CameraFollowBaseline/</c>).
/// arm64 rva 0x22e2c8 / 0x22e32c / 0x22ea60, x86_64 rva 0x23be30 / 0x23be80 / 0x23c6e0.
///
/// Two decoding notes that the decompiler hides:
/// <list type="bullet">
/// <item>On arm64 an AAPCS64 HFA return places one member per SIMD register, so
/// <c>Input.get_mouseScrollDelta</c> comes back as <c>s0 = x</c> in v0 and <c>s1 = y</c> in v1;
/// the body reads <c>v1</c> (<c>mov v8.16b, v1.16b</c> at 0x22e408) and is therefore the
/// <b>y</b> component. x86_64 agrees independently: <c>movshdup xmm0, xmm0</c> at 0x23bf16
/// duplicates the high lane, i.e. also <b>y</b>.</item>
/// <item><c>Transform.get_position</c> returns its Vector3 the same way (v0 = x, v1 = y,
/// v2 = z), so this body reads <b>x</b> and <b>y</b>, not x and z.</item>
/// </list>
/// </remarks>
public class CameraFollowBaseline : MonoBehaviour
{
    public Transform target;
    public bool wheelZoom;
    public float zoomSpeed;
    public float minSize;
    public float maxSize;
    public float follow;
    public float ppu;
    public float worldHalfW;
    public float worldHalfH;
    public bool isoDiamond;
    private Camera cam;

    // Decision-free observations used by existing world adapters. Native methods below
    // remain the sole behavior owners; these aliases do not refresh or mutate anything.
    public float PixelsPerUnit => ppu;
    public float WorldHalfWidth => worldHalfW;
    public float WorldHalfHeight => worldHalfH;

    // Official 060000CC (arm64 0x22ea60 / 28 bytes, x86_64 0x23c6e0 / 32 bytes).
    //
    // zoomSpeed..follow are four consecutive System.Single fields, so both compilers fold the
    // stores: arm64 writes one 16-byte literal (0x1126f40 = 4f, 2f, 40f, 8f) with a single
    // ldp/stp pair, x86_64 keeps four separate movss slots (_DAT_01118ab0.._UNK_01118abc).
    // ppu is stored on its own as 0x42c80000 = 100f. Nothing else is initialized: target stays
    // null, wheelZoom and isoDiamond stay false, worldHalfW/worldHalfH stay 0.
    public CameraFollowBaseline()
    {
        zoomSpeed = 4f;
        minSize = 2f;
        maxSize = 40f;
        follow = 8f;
        ppu = 100f;
    }

    // Official 060000CA (arm64 0x22e2c8 / 100 bytes, x86_64 0x23be30 / 80 bytes).
    //
    // Component.GetComponent is entered through its non-generic (System.Type) overload with
    // metadata_usage_06965_kind6, and the result is stored into the private Camera field through
    // the incremental-GC write barrier (thunk_FUN_00178e5c / FUN_0016bdd0). There is no cast in
    // the body, so the source form is the generic GetComponent&lt;Camera&gt;().
    public void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // Official 1.6.0 060000cc (ARM64 0x22e6dc). Historical 1.5.8 base was
    // 060000cb (arm64 0x22e32c / 1844 bytes, x86_64 0x23be80 / 2144 bytes).
    public void LateUpdate()
    {
        if (cam == null) return;

        if (wheelZoom)
        {
            Vector2 delta = Input.mouseScrollDelta;
            if ((GameHud.I == null || !GameHud.I.MouseOverHud()) && Mathf.Abs(delta.y) > 0.001f)
            {
                // arm64 0x22e4dc: ldp s2, s0 -> zoomSpeed and minSize, fsub, then the two-compare
                // clamp against minSize (0x30) and maxSize (0x34) that Mathf.Clamp lowers to.
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - delta.y * zoomSpeed,
                                                   minSize, maxSize);
            }
        }

        if (ppu > 0f && Screen.height > 0)
        {
            int h = Screen.height;
            float size = cam.orthographicSize;
            float cur = (float)h / (size + size);
            if (!wheelZoom || ppu * 0.75f <= cur)
            {
                int k = GameSettings.DpiK();
                float f = wheelZoom
                    ? cur / (ppu * k)
                    : (float)Screen.height / (ppu * 10f * k);
                int px = Mathf.RoundToInt(f);
                if (px < 2) px = 1;
                // arm64 0x22e6c0-0x22e6cc: FCVTZS saturates, so the emitted body adds an explicit
                // "+infinity -> 1" select after "cmp w8, #1 / csinc gt". x86_64 needs no second
                // guard because CVTTSD2SI(+inf) is int.MinValue, which the guard above already
                // replaces with 1. Both architectures therefore end at 1.
                if (float.IsInfinity(f)) px = 1;
                cam.orthographicSize = (float)Screen.height / ((ppu + ppu) * (px * k));
                ArtDetail.SetPixelScale(px * k);
            }
        }

        if (target == null) return;
        // 1.6.0 0x22eb54/0x22eb7c: lock is sampled after zoom/art scale and
        // the target guard. Both branches still use the same clamp/snap suffix.
        if (!DuelHud.CameraLock(out Vector3 p))
        {
            // Native fallback reads the target separately for x and then y.
            p.x = target.position.x;
            p.y = target.position.y;
        }
        float x = p.x;
        float y = p.y;

        if (worldHalfW > 0f && cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            if (isoDiamond)
            {
                // arm64 0x22e7d0-0x22e828: both axes start at zero and stay zero unless the
                // diamond margin is not <= zero; inside that case the two axes are scaled by the
                // same factor m/d, so the point is pulled back onto the diamond edge.
                float m = (1f - halfW / worldHalfW) - halfH / worldHalfH;
                x = 0f;
                y = 0f;
                if (!(m <= 0f))
                {
                    float d = Mathf.Abs(p.x) / worldHalfW + Mathf.Abs(p.y) / worldHalfH;
                    x = p.x;
                    y = p.y;
                    if (m < d)
                    {
                        m /= d;
                        x = p.x * m;
                        y = p.y * m;
                    }
                }
            }
            else
            {
                // arm64 0x22ea04-0x22ea58: the zeroing "movi v9.2d"/"movi v8.2d" is the default
                // for the "view wider than the world" case; once halfW < worldHalfW holds the two
                // compares are exactly Mathf.Clamp; the negated >= guards retain unordered inputs.
                // The lo value is written into the destination register
                // by the fsub before the b.mi, so a value below lo keeps lo, not zero.
                x = 0f;
                y = 0f;
                if (!(halfW >= worldHalfW)) x = Mathf.Clamp(p.x, halfW - worldHalfW, worldHalfW - halfW);
                if (!(halfH >= worldHalfH)) y = Mathf.Clamp(p.y, halfH - worldHalfH, worldHalfH - halfH);
            }
        }

        if (ppu > 0f)
        {
            // Pixel-snap both axes onto the art grid. The frintp/frintm + fcsel pair (arm64
            // 0x22e898-0x22e958) is Mathf.Round, with midpoint-to-even selection.
            x = Mathf.Round(ppu * x) / ppu;
            y = Mathf.Round(ppu * y) / ppu;
        }

        int viewportHeight = Screen.height;
        float currentSize = cam.orthographicSize;
        float scale = (float)viewportHeight / (currentSize + currentSize);
        if (scale > 0f)
        {
            // Half-pixel nudge for odd viewport sizes: tbz w0, #0 tests bit 0 of the size.
            if ((Screen.width & 1) != 0) x -= 0.5f / scale;
            if ((Screen.height & 1) != 0) y -= 0.5f / scale;
        }
        transform.position = new Vector3(x, y, -10f);
    }
}

}
