using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>World-root-owned art only. Every update samples the current server clock; no effect can deal damage.</summary>
internal sealed class RiftEliteWorldEffect : IDisposable
{
    private sealed class Part
    {
        internal GameObject Root;
        internal SpriteRenderer Sprite;
        internal MeshRenderer Surface;
        internal Mesh Mesh;
        internal Material OwnedMaterial;
        internal Vector3[] Vertices;
        internal Vector2[] UV;
        internal Color[] Colors;
        internal int[] Triangles;
    }
    private readonly GameObject root;
    private bool disposed;
    private float groundRadius;
    private readonly RiftEliteVisualAssets assets;
    private readonly Dictionary<string, Part> parts = new Dictionary<string, Part>();
    internal RiftEliteWorldEffect(string id, Transform worldRoot, RiftEliteVisualAssets assets)
    {
        this.assets = assets; root = new GameObject("RiftElite:" + id);
        root.transform.SetParent(worldRoot, false);
    }
    private void Begin(int x, int y, int radius)
    {
        groundRadius = radius;
        root.transform.position = EndgamePanel.RiftWorldPoint(x, y);
        foreach (Part part in parts.Values) part.Root.SetActive(false);
    }
    internal void Hazard(EndgameRiftHazardView h, double now)
    {
        Begin(h.X, h.Y, h.Radius);
        if (now < h.BornMS || now >= h.ExpiresMS) return;
        double age = now - h.BornMS; int order = RiftEliteVisualPlan.MapOrder(h.Y);
        if (h.Mechanism == "death_lava")
        {
            var core = assets.Magic(4149);
            if (core != null) Sprite("core", core, RiftEliteVisualPlan.LoopFrame(age, 55, 38),
                RiftEliteVisualPlan.LavaCoreScale(h.Radius, core.Width), 1, order + 3, true);
        }
        else if (h.Mechanism == "ice_orb")
        {
            var ice = assets.Magic(147);
            if (ice != null) Sprite("ice", ice, 0, RiftEliteVisualPlan.IceChargeScale(age) *
                RiftEliteVisualPlan.DiameterScale(h.Radius, ice.Width) / 1.9f, 1, order + 3, true, true);
        }
        else if (h.Mechanism == "lightning_orb")
        {
            var orb = assets.Magic(216);
            if (orb != null) Sprite("orb", orb, RiftEliteVisualPlan.LoopFrame(age, 100, 6),
                Math.Min(1.9f, h.Radius * .56f / orb.Width), 1, order + 5, true, false, 0, 36);
            if (RiftEliteVisualPlan.Ring(age, h.FirstPulseMS, h.PulseMS, h.PropagationMS, h.Radius, h.Width,
                out float inner, out float outer, out double pulseAge))
            {
                int frame = Math.Min(20, (int)(pulseAge * 21 / h.PropagationMS));
                Circle("discharge", assets.Magic(4040), frame, inner, outer, new Color(1, 1, 1, 1), false);
            }
        }
        else if (h.Mechanism == "poison_pool")
        {
            Circle("poisonA", assets.Magic(4145), RiftEliteVisualPlan.LoopFrame(age, 100, 34), 0, h.Radius,
                new Color(.25f, 1, .45f, .80f), true);
            Circle("poisonB", assets.Magic(4147), RiftEliteVisualPlan.LoopFrame(age, 100, 41), 0, h.Radius,
                new Color(.28f, 1, .38f, .55f), true);
        }
        else if (h.Mechanism == "walls")
        {
            for (int i = 0; i < h.Walls.Count; i++) Wall(h.Walls[i], i, h.X, h.Y);
        }
    }
    internal void Impact(EndgameRiftImpactView hit, double now)
    {
        Begin(hit.X, hit.Y, hit.Radius);
        if (now < hit.AtMS || now >= hit.ExpiresMS) return;
        double age = now - hit.AtMS; int order = RiftEliteVisualPlan.MapOrder(hit.Y);
        if (hit.Mechanism == "death_lava")
        {
            var blast = assets.Magic(420);
            if (blast != null) Sprite("burst", blast, RiftEliteVisualPlan.OnceFrame(age, 70, 10),
                RiftEliteVisualPlan.DiameterScale(hit.Radius, blast.Width), 1, order + 5, true);
            var core = assets.Magic(4149);
            if (age < 210 && core != null) Sprite("core", core, RiftEliteVisualPlan.LoopFrame(3000 + age, 55, 38),
                RiftEliteVisualPlan.LavaCoreScale(hit.Radius, core.Width) * (float)(1 - .8 * age / 210),
                (float)(1 - age / 210), order + 6, true);
        }
        else if (hit.Mechanism == "ice_orb")
        {
            var ice = assets.Magic(147);
            float scale = ice == null ? 0 : RiftEliteVisualPlan.DiameterScale(hit.Radius, ice.Width);
            if (ice != null) Sprite("ice", ice, RiftEliteVisualPlan.OnceFrame(age, 60, 9), scale, 1, order + 3, true);
            Sprite("shards", assets.Magic(43), RiftEliteVisualPlan.OnceFrame(age - 180, 60, 8),
                scale * (2.35f / 1.9f), 1, order + 5, true);
            if (age < 480)
            {
                float outer = hit.Radius * (float)(.4 + .6 * age / 480);
                Circle("iceRing", assets.Magic(230), 0, outer * .72f, outer,
                    new Color(1, 1, 1, (float)(1 - age / 480)), false);
            }
        }
    }
    private Part Get(string key, bool surface, bool green)
    {
        if (parts.TryGetValue(key, out Part part)) return part;
        part = new Part { Root = new GameObject(key) }; part.Root.transform.SetParent(root.transform, false);
        if (surface)
        {
            part.Mesh = new Mesh(); part.Root.AddComponent<MeshFilter>().sharedMesh = part.Mesh;
            part.Surface = part.Root.AddComponent<MeshRenderer>();
            part.OwnedMaterial = green ? RiftEliteVisualAssets.TintMaterial(new Color(.25f, 1, .4f, 1), true)
                : new Material(EffectPlayer.BorrowAdditiveMaterial());
            part.Surface.sharedMaterial = part.OwnedMaterial; part.Surface.sortingOrder = -4390;
        }
        else
        {
            part.Sprite = part.Root.AddComponent<SpriteRenderer>();
            part.Sprite.sharedMaterial = EffectPlayer.BorrowAdditiveMaterial();
        }
        parts.Add(key, part); return part;
    }
    private void Sprite(string key, RiftEliteVisualAssets.Clip clip, int frame, float scale, float alpha,
        int order, bool center, bool firstFrameCenter = false, float offsetX = 0, float offsetY = 0)
    {
        if (clip == null || frame < 0 || frame >= clip.Frames.Length || scale <= 0 || alpha <= 0) return;
        Part part = Get(key, false, false); part.Root.SetActive(true);
        part.Sprite.sprite = clip.Frames[frame]; part.Sprite.color = new Color(1, 1, 1, alpha);
        part.Sprite.flipX = part.Sprite.flipY = false; part.Sprite.sortingOrder = order;
        float cx = center ? clip.CenterX : 0, cy = center ? clip.CenterY : 0;
        if (firstFrameCenter) { Vector2 point = clip.AlphaCenter(0); cx = point.x; cy = point.y; }
        Camera camera = center ? Camera.main : null;
        float correction = ProjectedDiameterCorrection(camera, root.transform.position, groundRadius);
        part.Root.transform.localScale = new Vector3(scale * correction * 100 / GameBootstrap.CurPpu, scale * correction * 100 / GameBootstrap.CurPpu, 1);
        if (camera != null)
        {
            // Camera-facing artwork preserves the authored ice orientation even with a rotated
            // camera. Only its ground footprint is projected onto the map plane.
            part.Root.transform.rotation = camera.transform.rotation;
            part.Root.transform.position = root.transform.position + camera.transform.right * ((offsetX - cx * scale) * correction / GameBootstrap.CurPpu)
                + camera.transform.up * ((offsetY - cy * scale) * correction / GameBootstrap.CurPpu);
        }
        else
        {
            part.Root.transform.localPosition = new Vector3((offsetX - cx * scale) / GameBootstrap.CurPpu,
                (offsetY - cy * scale) / GameBootstrap.CurPpu, 0);
            part.Root.transform.localRotation = Quaternion.identity;
        }
    }
    internal static float ProjectedDiameterCorrection(Camera camera, Vector3 center, float mapRadius)
    {
        if (camera == null || mapRadius <= 0) return 1;
        float radius = mapRadius / GameBootstrap.CurPpu;
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        // Sample the actual map-plane circle, rather than assuming its x-axis remains
        // the horizontal diameter after camera rotation/isometric projection.
        for (int i = 0; i < 128; i++)
        {
            double angle = i * Math.PI * 2 / 128;
            Vector3 screen = camera.WorldToScreenPoint(center + new Vector3((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, 0));
            if (screen.z <= 0) return 1;
            min = Math.Min(min, screen.x); max = Math.Max(max, screen.x);
        }
        Vector3 origin = camera.WorldToScreenPoint(center);
        Vector3 right = camera.WorldToScreenPoint(center + camera.transform.right);
        float pixelsPerWorldUnit = Math.Abs(right.x - origin.x);
        return pixelsPerWorldUnit > .00001f ? (max - min) / (2 * radius * pixelsPerWorldUnit) : 1;
    }
    private void Circle(string key, RiftEliteVisualAssets.Clip clip, int frame, float inner, float outer, Color color, bool green)
    {
        if (clip == null || frame < 0 || frame >= clip.Frames.Length || outer <= inner || outer <= 0) return;
        Part part = Get(key, true, green); part.Root.SetActive(true);
        // A world-space mesh follows the real camera. The source ring is mapped onto the exact
        // authoritative annulus; no GUI ellipse, screen-sized overlay, or independent damage radius.
        const int segments = 128;
        if (part.Vertices == null)
        {
            part.Vertices = new Vector3[(segments + 1) * 2]; part.UV = new Vector2[part.Vertices.Length];
            part.Colors = new Color[part.Vertices.Length]; part.Triangles = new int[segments * 6];
        }
        var vertices = part.Vertices; var uv = part.UV; var colors = part.Colors; var triangles = part.Triangles;
        for (int i = 0; i <= segments; i++)
        {
            double angle = i * Math.PI * 2 / segments; float x = (float)Math.Cos(angle), y = (float)Math.Sin(angle);
            int j = i * 2;
            vertices[j] = new Vector3(x * inner / GameBootstrap.CurPpu, y * inner / GameBootstrap.CurPpu, 0);
            vertices[j + 1] = new Vector3(x * outer / GameBootstrap.CurPpu, y * outer / GameBootstrap.CurPpu, 0);
            float sourceInner = inner > 0 ? .60f : 0;
            uv[j] = clip.UV(frame, x * sourceInner, y * sourceInner); uv[j + 1] = clip.UV(frame, x, y);
            // Green is applied once by the owned luminance-tint material.
            colors[j] = colors[j + 1] = green ? new Color(1, 1, 1, color.a) : color;
            if (i == segments) continue;
            int t = i * 6; triangles[t] = j; triangles[t + 1] = j + 1; triangles[t + 2] = j + 3;
            triangles[t + 3] = j; triangles[t + 4] = j + 3; triangles[t + 5] = j + 2;
        }
        part.Mesh.Clear(); part.Mesh.vertices = vertices; part.Mesh.uv = uv; part.Mesh.colors = colors;
        part.Mesh.triangles = triangles; part.Mesh.RecalculateBounds();
        part.OwnedMaterial.mainTexture = clip.Frames[frame].texture;
    }
    private void Wall(EndgameRiftWallView wall, int index, int x, int y)
    {
        RiftEliteVisualAssets.Clip clip = assets.Wall(index); if (clip == null) return;
        RiftEliteVisualPlan.Wall(wall.X, wall.Y, wall.X2, wall.Y2, wall.Width,
            out double cx, out double cy, out double length, out double angle);
        float scale = (float)length / clip.Width;
        string key = "wall" + index;
        Sprite(key, clip, 0, scale, 1, RiftEliteVisualPlan.MapOrder(cy), false);
        Part part = parts[key]; part.Sprite.sharedMaterial = EffectPlayer.BorrowAlphaMaterial();
        part.Root.transform.localPosition = new Vector3((float)(cx - x) / GameBootstrap.CurPpu,
            (float)(y - cy) / GameBootstrap.CurPpu, 0);
        part.Root.transform.localRotation = Quaternion.Euler(0, 0, (float)angle);
        // Authored wall feet are centered on the exact server segment. Its native height
        // is decoration; the separate source-textured ground base is the actual rectangle.
        Part foot = Get(key + "Foot", true, false); foot.Root.SetActive(true);
        foot.Surface.sortingOrder = -4389;
        foot.OwnedMaterial.SetFloat("_DstF", 10); // Native alpha blend for opaque ice-rock artwork.
        float dx = (float)((wall.X2 - wall.X) / length), dy = (float)((wall.Y2 - wall.Y) / length);
        float w = (float)wall.Width * .5f;
        var vertices = new Vector3[4]; var uv = new Vector2[4];
        double[] xs = { wall.X, wall.X2, wall.X2, wall.X }, ys = { wall.Y, wall.Y2, wall.Y2, wall.Y };
        for (int i = 0; i < 4; i++)
        {
            float sign = i < 2 ? 1 : -1;
            vertices[i] = new Vector3((float)(xs[i] - x - dy * w * sign) / GameBootstrap.CurPpu,
                (float)(y - ys[i] - dx * w * sign) / GameBootstrap.CurPpu, 0);
        }
        uv[0] = clip.UV(0, -1, -.75f); uv[1] = clip.UV(0, 1, -.75f);
        uv[2] = clip.UV(0, 1, -1); uv[3] = clip.UV(0, -1, -1);
        foot.Mesh.Clear(); foot.Mesh.vertices = vertices; foot.Mesh.uv = uv;
        foot.Mesh.colors = new[] { new Color(1, 1, 1, .65f), new Color(1, 1, 1, .65f), new Color(1, 1, 1, .65f), new Color(1, 1, 1, .65f) };
        foot.Mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 }; foot.Mesh.RecalculateBounds();
        foot.OwnedMaterial.mainTexture = clip.Frames[0].texture;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (root != null) root.SetActive(false);
        foreach (Part part in parts.Values)
        { if (part.Mesh != null) UnityEngine.Object.Destroy(part.Mesh); if (part.OwnedMaterial != null) UnityEngine.Object.Destroy(part.OwnedMaterial); }
        if (root != null) UnityEngine.Object.Destroy(root);
        parts.Clear();
    }
}
}
