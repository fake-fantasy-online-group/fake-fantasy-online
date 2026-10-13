using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // The original monster-birth rune ring (magic0341) supplies every visible
    // motif. Geometry is a mask, not a substitute white-texture outline. The
    // static authored frame is deliberately not advertised as an animation.
    private readonly Dictionary<string, Texture2D> riftTellArtwork = new Dictionary<string, Texture2D>();
    private static Vector3 RiftScreenPoint(Camera camera, float x, float y)
    {
        Vector3 point = camera.WorldToScreenPoint(RiftWorldPoint(x, y));
        return new Vector3(point.x, Screen.height - point.y, point.z);
    }
    private static double TelegraphRemainingMS(EndgameRiftTelegraphView tell, DateTime receivedUtc)
        => Math.Max(0, tell.RemainingMS - Math.Max(0, (DateTime.UtcNow - receivedUtc).TotalMilliseconds));

    private void DrawRiftTelegraphs(EndgameRiftView rift, Camera camera, float scale)
    {
        if (Event.current.type != EventType.Repaint) return;
        foreach (var tell in rift.Telegraphs)
        {
            double remaining = TelegraphRemainingMS(tell, State.RiftReceivedUtc);
            if (remaining <= 0) continue;
            Vector3 center = RiftScreenPoint(camera, tell.X, tell.Y);
            if (center.z <= 0) continue;
            Texture2D artwork = RiftTelegraphArtwork(tell);
            if (artwork == null) continue;
            Color previous = GUI.color; Matrix4x4 matrix = GUI.matrix;
            try
            {
                // A quiet amber seal warms to red during the final 650ms. Its
                // footprint never grows/shrinks or implies a different damage area.
                GUI.color = remaining < 650 ? new Color(1f, .34f, .19f, 1f) : new Color(1f, .71f, .32f, .87f);
                if (tell.Shape == "line")
                {
                    Vector3 end = RiftScreenPoint(camera, tell.EndX, tell.EndY);
                    if (end.z <= 0) continue;
                    double dx = (double)tell.EndX - tell.X, dy = (double)tell.EndY - tell.Y;
                    double worldLength = Math.Sqrt(dx * dx + dy * dy);
                    if (worldLength <= 0) continue; // The server's zero-length rectangle cannot hit.
                    float ox = (float)(-dy / worldLength * tell.Radius), oy = (float)(dx / worldLength * tell.Radius);
                    Vector3 edgeA = RiftScreenPoint(camera, tell.X + ox, tell.Y + oy);
                    Vector3 edgeB = RiftScreenPoint(camera, tell.X - ox, tell.Y - oy);
                    float width = Distance2D(edgeA, edgeB), length = Distance2D(center, end);
                    GUIUtility.RotateAroundPivot((float)(Math.Atan2(end.y - center.y, end.x - center.x) * 180 / Math.PI), new Vector2(center.x, center.y));
                    GUI.DrawTexture(new Rect(center.x, center.y - width * .5f, length, width), artwork);
                }
                else
                {
                    Vector3 left = RiftScreenPoint(camera, tell.X - tell.Radius, tell.Y);
                    Vector3 right = RiftScreenPoint(camera, tell.X + tell.Radius, tell.Y);
                    Vector3 top = RiftScreenPoint(camera, tell.X, tell.Y - tell.Radius);
                    Vector3 bottom = RiftScreenPoint(camera, tell.X, tell.Y + tell.Radius);
                    GUI.DrawTexture(new Rect(left.x, top.y, right.x - left.x, bottom.y - top.y), artwork);
                }
            }
            finally { GUI.color = previous; GUI.matrix = matrix; }
        }
    }
    private static float Distance2D(Vector3 a, Vector3 b)
        => (float)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));

    private Texture2D RiftTelegraphArtwork(EndgameRiftTelegraphView tell)
    {
        string key = RiftTellArtworkKey(tell);
        if (riftTellArtwork.TryGetValue(key, out Texture2D result)) return result;
        Sprite[] native = EffectPlayer.GetMagicFrames(341);
        if (native == null || native.Length == 0 || native[0] == null || native[0].texture == null) return null;
        Texture2D source = native[0].texture;
        const int size = 256;
        Color32[] pixels = BuildRiftTelegraphPixels(source.GetPixels32(), source.width, source.height,
            size, tell.Shape, tell.Radius, tell.InnerRadius, Distance2D(new Vector3(tell.X, tell.Y, 0), new Vector3(tell.EndX, tell.EndY, 0)));
        result = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        // Point filtering keeps every transparent safe-center texel transparent.
        result.filterMode = FilterMode.Point; result.wrapMode = TextureWrapMode.Clamp;
        result.SetPixels32(pixels); result.Apply(false, false);
        riftTellArtwork[key] = result; return result;
    }
    private static string RiftTellArtworkKey(EndgameRiftTelegraphView tell)
        => tell.Shape + ":" + tell.Radius + ":" + tell.InnerRadius + (tell.Shape == "line"
            ? ":" + (tell.EndX - (long)tell.X) + ":" + (tell.EndY - (long)tell.Y) : "");

    internal static Color32[] BuildRiftTelegraphPixels(Color32[] source, int sourceWidth, int sourceHeight,
        int size, string shape, float radius, float innerRadius, float length)
    {
        var result = new Color32[size * size];
        if (source == null || source.Length != sourceWidth * sourceHeight || radius <= 0) return result;
        float inner = shape == "ring" ? innerRadius / radius : 0;
        long intensity = 0, weight = 0;
        foreach (Color32 pixel in source)
        { intensity += Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) * pixel.a; weight += pixel.a; }
        byte wash = (byte)(weight > 0 ? Math.Max(94, intensity / weight) : 94);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            double nx = (x + .5) * 2 / size - 1, ny = (y + .5) * 2 / size - 1;
            double distance = Math.Sqrt(nx * nx + ny * ny), angle = Math.Atan2(ny, nx), edge;
            if (shape == "line")
            {
                if (length <= 0) continue;
                // The native server uses a flat-ended rectangle, not a capsule.
                double fromEnd = (1 - Math.Abs(nx)) * length * .5;
                double fromSide = (1 - Math.Abs(ny)) * radius;
                edge = Math.Min(fromEnd, fromSide) / Math.Max(1, Math.Min(radius * .2, 20));
                // Wrap the authored runes around the rectangle instead of stretching
                // a circular sigil into an ambiguous oval danger zone.
                double perimeter;
                if (fromSide <= fromEnd)
                    perimeter = ny > 0 ? (nx + 1) * length * .5 : length + radius * 2 + (1 - nx) * length * .5;
                else
                    perimeter = nx > 0 ? length + (1 - ny) * radius : 2 * length + 2 * radius + (ny + 1) * radius;
                angle = perimeter / (2 * length + 4 * radius) * Math.PI * 2;
            }
            else
            {
                if (distance > 1 || distance < inner) continue;
                // Conservative mask: no texel whose footprint touches the safe hole
                // gets painted. The safe center remains fully clear at every scale.
                if (inner > 0 && distance - Math.Sqrt(2) / size < inner) continue;
                edge = Math.Min(1 - distance, inner > 0 ? distance - inner : 1) / .18;
            }
            double band = Math.Max(0, Math.Min(1, edge));
            double authoredRadius = .98 - .36 * band;
            int sx = Math.Max(0, Math.Min(sourceWidth - 1, (int)((.5 + Math.Cos(angle) * authoredRadius * .5) * sourceWidth)));
            int sy = Math.Max(0, Math.Min(sourceHeight - 1, (int)((.5 + Math.Sin(angle) * authoredRadius * .5) * sourceHeight)));
            Color32 sample = source[sy * sourceWidth + sx];
            byte luminance = Math.Max(sample.r, Math.Max(sample.g, sample.b));
            // Preserve authored rune highlights. A subdued wash connects the rune
            // strokes so their gaps cannot be mistaken for safe gaps in the area.
            byte value = band < 1 ? (byte)Math.Max(94, (int)luminance) : wash;
            byte alpha = (byte)(band < 1 ? 82 + sample.a * .58 : 35);
            result[y * size + x] = new Color32(value, value, value, alpha);
        }
        return result;
    }
    private void PruneRiftTelegraphArtwork(EndgameRiftView rift)
    {
        var keys = new HashSet<string>();
        foreach (var tell in rift.Telegraphs)
            if (TelegraphRemainingMS(tell, State.RiftReceivedUtc) > 0) keys.Add(RiftTellArtworkKey(tell));
        var removed = new List<string>();
        foreach (var pair in riftTellArtwork)
            if (!keys.Contains(pair.Key)) { if (pair.Value != null) Destroy(pair.Value); removed.Add(pair.Key); }
        foreach (string key in removed) riftTellArtwork.Remove(key);
    }
    private void ClearRiftTelegraphArtwork()
    {
        foreach (var pair in riftTellArtwork) if (pair.Value != null) Destroy(pair.Value);
        riftTellArtwork.Clear();
    }
}
}
