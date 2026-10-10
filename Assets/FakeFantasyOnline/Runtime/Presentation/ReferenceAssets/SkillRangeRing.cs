using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class SkillRangeRing : MonoBehaviour
{
    private const float FadeSec = 0.9f;
    private const float HoldFrac = 0.35f;
    private static readonly Color Tint = new Color(1f, 0.92f, 0.45f, 0.85f);
    private static Sprite _ring;
    private static int _texW;
    private static int _hdScale = 1;
    private static bool _tried;
    private static readonly List<SkillRangeRing> _pool = new List<SkillRangeRing>();
    private SpriteRenderer _sr;
    private float _t;
    private float _life;

    private static void EnsureSprite()
    {
        if (_ring != null && _ring.texture != null) return;
        if (_ring != null)
        {
            _ring = null;
            _tried = false;
        }
        if (_tried) return;
        _tried = true;
        if (ResLoader.I == null)
        {
            _tried = false;
            return;
        }
        Texture2D texture = ResLoader.I.LoadUI("effect/fanwei_0.png") ??
            ResLoader.I.LoadGsaFrame("res/effect/fanwei.gsa", 0);
        if (texture == null) return;
        _texW = texture.width;
        _hdScale = Mathf.Max(1, TexScale.Of(texture));
        _ring = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100f * _hdScale);
    }

    public static void Show(Vector3 worldPos, float radiusPx, float ppu)
    {
        if (radiusPx <= 0f || ppu <= 0f) return;
        EnsureSprite();
        if (_ring == null || _texW < 1) return;

        for (int i = _pool.Count - 1; i >= 0; i--)
        {
            if (_pool[i] == null) _pool.RemoveAt(i);
        }
        SkillRangeRing ring = null;
        foreach (SkillRangeRing candidate in _pool)
        {
            if (!candidate.gameObject.activeSelf)
            {
                ring = candidate;
                break;
            }
        }
        if (ring == null)
        {
            GameObject owner = new GameObject("SkillRangeRing");
            ring = owner.AddComponent<SkillRangeRing>();
            ring._sr = owner.AddComponent<SpriteRenderer>();
            ring._sr.sprite = _ring;
            ring._sr.sortingOrder = -4998;
            _pool.Add(ring);
        }
        ring.gameObject.SetActive(true);
        ring._sr.sprite = _ring;
        ring.transform.position = worldPos;
        float scale = ((radiusPx + radiusPx) / ppu) / ((float)_texW / (100f * _hdScale));
        ring.transform.localScale = new Vector3(scale, scale, 1f);
        ring._sr.color = Tint;
        ring._t = 0f;
        ring._life = FadeSec;
    }

    private void Update()
    {
        _t += Time.deltaTime;
        if (_t >= _life)
        {
            gameObject.SetActive(false);
            return;
        }
        float u = _t / _life;
        // Official 1.6.0 selects the fade for unordered u as well as u > HoldFrac
        // (arm64 0x32f8d0 b.hi; x86_64 0x343653 cmpnless). Preserve that direction.
        float factor = u <= HoldFrac ? 1f : 1f - (u - HoldFrac) / (1f - HoldFrac);
        _sr.color = new Color(Tint.r, Tint.g, Tint.b, Tint.a * factor);
    }
}

}
