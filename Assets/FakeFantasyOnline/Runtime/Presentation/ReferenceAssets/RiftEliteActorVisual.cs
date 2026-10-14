using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Owns only this native actor's reversible tint, authored foot effects and short movement trail.</summary>
internal sealed class RiftEliteActorVisual : MonoBehaviour
{
    private SpriteRenderer body, goldenRing, shield, hasteRing;
    private readonly SpriteRenderer[] trail = new SpriteRenderer[2];
    private readonly double[] trailAt = new double[2];
    private readonly Vector3[] trailPosition = new Vector3[2];
    private Material originalMaterial, tintMaterial;
    private Color originalColor;
    private RiftEliteVisualAssets assets;
    private bool golden, agile, clone, released, tinted;
    private double serverTime, previousTrailTime;
    private Vector3 previousPosition;
    internal void Bind(SpriteRenderer nativeBody, RiftEliteVisualAssets sharedAssets)
    {
        body = nativeBody; assets = sharedAssets; previousPosition = body.transform.position;
    }
    internal void SetState(bool gold, bool agility, bool isClone, double now)
    {
        golden = gold; agile = agility; clone = isClone; serverTime = now;
        Refresh();
    }
    private void LateUpdate() { Refresh(); }
    private SpriteRenderer NewSprite(string name)
    {
        var root = new GameObject(name); root.transform.SetParent(transform, false);
        var renderer = root.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = EffectPlayer.BorrowAdditiveMaterial();
        return renderer;
    }
    private void Foot(ref SpriteRenderer renderer, string name, int magic, int count, float scale, bool wanted, float lift)
    {
        if (!wanted) { if (renderer != null) renderer.enabled = false; return; }
        var clip = assets.Magic(magic); if (clip == null) return;
        if (renderer == null) renderer = NewSprite(name);
        int frame = RiftEliteVisualPlan.LoopFrame(serverTime, 100, count);
        if (frame < 0 || frame >= clip.Frames.Length) { renderer.enabled = false; return; }
        renderer.sprite = clip.Frames[frame]; renderer.enabled = body.enabled;
        renderer.sortingLayerID = body.sortingLayerID;
        renderer.sortingOrder = body.sortingOrder + (lift > 0 ? 6 : -3);
        renderer.transform.localPosition = new Vector3(-clip.CenterX * scale / GameBootstrap.CurPpu, lift - clip.CenterY * scale / GameBootstrap.CurPpu, 0);
        renderer.transform.localScale = new Vector3(scale * 100 / GameBootstrap.CurPpu, scale * 100 / GameBootstrap.CurPpu, 1);
        renderer.flipX = renderer.flipY = false;
    }
    private void Refresh()
    {
        if (released || body == null || assets == null) return;
        bool needsTint = golden || clone;
        if (needsTint)
        {
            if (!tinted)
            {
                originalMaterial = body.sharedMaterial; originalColor = body.color;
                tintMaterial = RiftEliteVisualAssets.TintMaterial(new Color(1, .80f, .22f, 1), false); tinted = true;
            }
            tintMaterial.SetColor("_Tint", golden ? new Color(1, .80f, .22f, 1) : new Color(.43f, .80f, 1, 1));
            body.sharedMaterial = tintMaterial;
            body.color = new Color(1, 1, 1, clone ? .62f : 1);
        }
        else RestoreBody();
        Foot(ref goldenRing, "GoldenRune0201", 201, 14, 1.35f, golden, 0);
        // Native sprite bounds and pivot supply the real model's current head, regardless of model size.
        float lift = body.sprite == null ? .8f : Math.Max(.3f, body.sprite.bounds.max.y + .18f);
        Foot(ref shield, "GoldenShield0080", 80, 6, 1.10f, golden, lift);
        Foot(ref hasteRing, "Agility0085", 85, 5, 1.25f, agile, 0);
        UpdateTrail();
    }
    private void UpdateTrail()
    {
        Vector3 position = body.transform.position;
        float dx = position.x - previousPosition.x, dy = position.y - previousPosition.y;
        bool moved = dx * dx + dy * dy > .0001f;
        if (agile && body.enabled && moved && serverTime - previousTrailTime >= 85)
        {
            for (int i = 1; i >= 0; i--)
            {
                if (trail[i] == null) { trail[i] = NewSprite("AgilityAfterimage" + i); trail[i].sharedMaterial = EffectPlayer.BorrowAlphaMaterial(); }
                if (i == 1 && trail[0] != null)
                {
                    trail[i].sprite = trail[0].sprite; trailPosition[i] = trailPosition[0];
                    trail[i].flipX = trail[0].flipX; trailAt[i] = trailAt[0];
                }
                else
                {
                    trail[i].sprite = body.sprite; trailPosition[i] = previousPosition;
                    trail[i].flipX = body.flipX; trailAt[i] = serverTime;
                }
                trail[i].sortingLayerID = body.sortingLayerID; trail[i].sortingOrder = RiftEliteVisualPlan.MapOrder(GameBootstrap.CurWorldH * .5f - trailPosition[i].y * GameBootstrap.CurPpu) - 1;
                trail[i].transform.localScale = new Vector3(1, 1, 1); // Body is the actor root; descendants already inherit its scale.
            }
            previousTrailTime = serverTime;
        }
        for (int i = 0; i < trail.Length; i++) if (trail[i] != null)
        {
            trail[i].transform.position = trailPosition[i]; // Keep a captured world position while the actor moves.
            double age = serverTime - trailAt[i];
            trail[i].enabled = agile && body.enabled && age >= 0 && age < 180 && trail[i].sprite != null;
            trail[i].color = new Color(.60f, .80f, 1, (float)(.25 * Math.Max(0, 1 - age / 180)));
        }
        previousPosition = position;
    }
    private void RestoreBody()
    {
        if (!tinted) return;
        if (body != null && ReferenceEquals(body.sharedMaterial, tintMaterial))
        { body.sharedMaterial = originalMaterial; body.color = originalColor; }
        if (tintMaterial != null) Destroy(tintMaterial);
        tintMaterial = null; tinted = false;
    }
    internal void Release()
    {
        if (released) return;
        released = true; RestoreBody(); if (gameObject != null) gameObject.SetActive(false); assets = null;
    }
    private void OnDestroy() { Release(); }
}
}
