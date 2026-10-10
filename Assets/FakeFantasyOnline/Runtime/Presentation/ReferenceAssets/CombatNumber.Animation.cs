using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private Vector3 basePos;
    private int type;
    private float life;
    private float ppu;
    private float hSign = 1f;
    private readonly List<SpriteRenderer> _children = new List<SpriteRenderer>();
    private float startYWorld;

    private void Update()
    {
        t += Time.deltaTime;
        float u = Mathf.Clamp01(t / life);
        float units = ppu;
        float x = 0f;
        float scale = 0.8f;
        float alpha;
        if (type == 1 || type == 3)
        {
            scale = Mathf.Min(u * 4f, 1f) * 1.2f + 0.8f;
            alpha = u < 0.7f ? 1f : (u - 0.7f) / -0.3f + 1f;
        }
        else
        {
            if (type == 4) x = (10f / units) * Mathf.Sin((u + u) * Mathf.PI);
            alpha = 1f - u;
        }
        transform.position = new Vector3(
            Mathf.Round((basePos.x + x) * ppu) / ppu,
            u * (100f / units) + basePos.y,
            basePos.z + 0f);
        transform.localScale = new Vector3(scale, scale, 1f);
        for (int i = 0; i < _activeCount; i++)
        {
            SpriteRenderer child = _children[i];
            if (!child) continue;
            Color color = child.color;
            color.a = alpha;
            child.color = color;
        }
        if (t >= life) ReturnToPool();
    }
}

}
