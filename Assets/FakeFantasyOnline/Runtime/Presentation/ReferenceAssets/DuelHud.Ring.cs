using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class DuelHud
{
    private const int SEG = 96;
    private LineRenderer ring;
    private static Vector3 W2U(float wx, float wy) => new Vector3(
        (wx - GameBootstrap.CurWorldW * .5f) / GameBootstrap.CurPpu,
        (GameBootstrap.CurWorldH * .5f - wy) / GameBootstrap.CurPpu, 0f);

    private void SetRing(bool on)
    {
        if (ring == null)
        {
            if (!on) return;
            var go = new GameObject("DuelRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = true;
            ring.positionCount = SEG;
            ring.material = new Material(Shader.Find("Sprites/Default"));
            ring.endColor = new Color(1f, .84f, .3f, .9f);
            ring.startColor = new Color(1f, .84f, .3f, .9f);
            ring.sortingOrder = 32000;
        }
        ring.enabled = on;
    }
    // Host-only resource lifetime. Native Clear keeps its original retained state.
    internal void ReleaseForIndependentRuntime()
    {
        if (ReferenceEquals(I, this)) { Clear(); I = null; }
        Material material = ring == null ? null : ring.sharedMaterial;
        if (UnityEngine.Application.isPlaying)
        {
            if (material != null) Object.Destroy(material);
            Object.Destroy(gameObject);
        }
        else
        {
            if (material != null) Object.DestroyImmediate(material);
            Object.DestroyImmediate(gameObject);
        }
    }

    private void Update()
    {
        if (!active || GameBootstrap.CurMapId != recvMapId || GameBootstrap.CurPpu <= 0f)
        { SetRing(false); return; }
        SetRing(true);
        float width = 5f / GameBootstrap.CurPpu;
        ring.endWidth = width;
        ring.startWidth = width;
        for (int i = 0; i < SEG; i++)
        {
            float angle = i * Mathf.PI * 2f / SEG;
            ring.SetPosition(i, W2U(cx + Mathf.Cos(angle) * r, cy + Mathf.Sin(angle) * r));
        }
    }
}
}
