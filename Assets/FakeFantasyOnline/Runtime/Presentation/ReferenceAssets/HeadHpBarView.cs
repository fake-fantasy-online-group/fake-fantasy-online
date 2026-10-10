using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed class HeadHpBarView : IDisposable
{
    private GameObject root;
    private SpriteRenderer bgSr;
    private SpriteRenderer fgSr;
    private int lastIdx = -1;
    private int lastW = -1;

    public void Tick(Transform owner, bool show, int hp, int maxHp, float localY, float offX = 0f)
    {
        if (!show || !HeadHpBar.Show || maxHp <= 0 || !HeadHpBar.Ready)
        {
            if (root && root.activeSelf) root.SetActive(false);
            return;
        }
        if (!root) Build(owner);
        if (!root) return;
        if (!root.activeSelf) root.SetActive(true);

        int pct = HeadHpBar.Pct(hp, maxHp);
        int idx = HeadHpBar.ColorIdx(pct);
        int width = HeadHpBar.FillW(pct);
        if (lastIdx != idx || lastW != width)
        {
            lastIdx = idx;
            lastW = width;
            Sprite fill = HeadHpBar.Fill(idx, width);
            fgSr.sprite = fill;
            fgSr.enabled = fill;
        }
        PlayerNameTag.PlaceSharp(root.transform, localY, HeadHpBar.SPPU, offX);
    }

    private void Build(Transform owner)
    {
        if (!HeadHpBar.Ready) return;
        root = new GameObject("HpBar");
        root.transform.SetParent(owner, false);
        float left = unchecked(-HeadHpBar.W) / 2f / HeadHpBar.SPPU;
        bgSr = MakeSr(left, 31990);
        bgSr.sprite = HeadHpBar.Groove();
        fgSr = MakeSr(left, 31991);
        fgSr.enabled = false;
    }

    private SpriteRenderer MakeSr(float left, int sortingOrder)
    {
        GameObject child = new GameObject("q");
        child.transform.SetParent(root.transform, false);
        child.transform.localPosition = new Vector3(left, 0f, 0f);
        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = sortingOrder;
        Material material = HeadHpBar.Mat();
        if (material) renderer.sharedMaterial = material;
        return renderer;
    }

    public void Dispose()
    {
        if (root) UnityEngine.Object.Destroy(root);
        root = null;
        bgSr = null;
        fgSr = null;
        lastIdx = -1;
        lastW = -1;
    }

    // Existing presentation and Editor observers inspect the actual owner state.
    public GameObject Root => root;
    public SpriteRenderer Background => bgSr;
    public SpriteRenderer Foreground => fgSr;
    public int LastColorIndex => lastIdx;
    public int LastWidth => lastW;
}

}
