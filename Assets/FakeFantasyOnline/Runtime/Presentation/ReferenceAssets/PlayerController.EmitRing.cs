using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    internal const float RingEmitIntervalSeconds = 0.049f;
    internal const int RingSortingOrder = -5000;
    internal const string RingObjectName = "ClickRing";

    private float lastRingTime = -1f;

    /// <summary>
    /// Official 1.5.8 <c>PlayerController.EmitRing</c> (0x060009fc). The ring GameObject is a
    /// scene-root object: the native body writes <c>Transform.set_position</c> and never calls
    /// SetParent, so the world units are world-space, not parent-relative.
    /// </summary>
    public void EmitRing(float wx, float wy)
    {
        if (ringSprite != null && RingEmitIntervalSeconds <= Time.time - lastRingTime)
        {
            lastRingTime = Time.time;
            GameObject ring = new GameObject(RingObjectName);
            ring.transform.position = new Vector3(
                (wx - worldW * 0.5f) / ppu,
                (worldH * 0.5f - wy) / ppu,
                0f);
            SpriteRenderer renderer = ring.AddComponent<SpriteRenderer>();
            renderer.sprite = ringSprite;
            renderer.sortingOrder = RingSortingOrder;
            ring.AddComponent<ClickRingLife>().Init(renderer);
        }
    }

    /// <summary>
    /// Single binding of the official <c>Effect/circle</c> sprite into the official field the
    /// native body reads at +0xb8. A missing sprite stays null, which silences emission exactly
    /// as the official body does; it never becomes a caller failure.
    /// </summary>
    internal void SetRingSpriteForIndependentRuntime(Sprite value)
    {
        ringSprite = value;
    }
}

}
