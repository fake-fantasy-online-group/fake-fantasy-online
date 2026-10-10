using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MissFx : MonoBehaviour
{
    private static Sprite[] glyphs;
    private static int glyphScale = 1;
    private static Material overlayMat;
    private static readonly Stack<MissFx> _pool = new Stack<MissFx>();
    private float t;
    private float life = 0.7f;
    private float ppu;
    private Vector3 basePos;
    private readonly List<SpriteRenderer> _children = new List<SpriteRenderer>();
    private int _activeCount;

    private static MissFx Rent()
    {
        MissFx instance = null;
        while (_pool.Count > 0)
        {
            instance = _pool.Pop();
            if (instance) break;
        }
        if (instance) instance.gameObject.SetActive(true);
        else instance = new GameObject("MissFx").AddComponent<MissFx>();
        instance.t = 0f;
        instance._activeCount = 0;
        instance.transform.localScale = Vector3.one;
        return instance;
    }

    private SpriteRenderer NextChild()
    {
        SpriteRenderer renderer;
        if (_activeCount < _children.Count)
        {
            renderer = _children[_activeCount];
            renderer.gameObject.SetActive(true);
        }
        else
        {
            GameObject child = new GameObject("L");
            child.transform.SetParent(transform, false);
            renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 32100;
            if (overlayMat) renderer.sharedMaterial = overlayMat;
            _children.Add(renderer);
        }
        Color color = renderer.color;
        color.a = 1f;
        renderer.color = color;
        _activeCount++;
        return renderer;
    }

    private void ReturnToPool()
    {
        for (int i = 0; i < _children.Count; i++)
            if (_children[i]) _children[i].gameObject.SetActive(false);
        gameObject.SetActive(false);
        _pool.Push(this);
    }

    private void Update()
    {
        t += Time.deltaTime;
        float u = Mathf.Clamp01(t / life);
        float scale = Mathf.Min(u / 0.3f, 1f) * 0.6f + 0.4f;
        float alpha = u < 0.6f ? 1f : (u - 0.6f) / -0.4f + 1f;
        transform.position = basePos + new Vector3(0f, u * (60f / ppu), 0f);
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
