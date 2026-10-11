using System;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Use the native map/camera and existing white texture. These are warnings only;
    // hit detection, damage and expiry remain server-owned.
    private static Vector3 RiftScreenPoint(Camera camera, float x, float y)
    {
        Vector3 point = camera.WorldToScreenPoint(new Vector3(x / GameBootstrap.CurPpu, (GameBootstrap.CurWorldH - y) / GameBootstrap.CurPpu, 0));
        return new Vector3(point.x, Screen.height - point.y, point.z);
    }
    private static double TelegraphRemainingMS(EndgameRiftTelegraphView tell, DateTime receivedUtc)
        => Math.Max(0, tell.RemainingMS - Math.Max(0, (DateTime.UtcNow - receivedUtc).TotalMilliseconds));
    private void DrawRiftTelegraphs(EndgameRiftView rift, Camera camera, float scale)
    {
        foreach (var tell in rift.Telegraphs)
        {
            double remaining = TelegraphRemainingMS(tell, State.RiftReceivedUtc);
            if (remaining <= 0) continue;
            Vector3 center = RiftScreenPoint(camera, tell.X, tell.Y);
            if (center.z <= 0) continue;
            Color edge = remaining < 650 ? new Color(1f, .27f, .2f, .98f) : new Color(1f, .67f, .22f, .95f);
            Color fill = new Color(edge.r, edge.g, edge.b, .17f);
            string instruction;
            if (tell.Shape == "line")
            {
                Vector3 end = RiftScreenPoint(camera, tell.EndX, tell.EndY);
                if (end.z <= 0) continue;
                double dx = (double)tell.EndX - tell.X, dy = (double)tell.EndY - tell.Y;
                double length = Math.Sqrt(dx * dx + dy * dy);
                float ox = length > 0 ? (float)(-dy / length * tell.Radius) : tell.Radius;
                float oy = length > 0 ? (float)(dx / length * tell.Radius) : 0;
                Vector3 a = RiftScreenPoint(camera, tell.X + ox, tell.Y + oy), b = RiftScreenPoint(camera, tell.EndX + ox, tell.EndY + oy);
                Vector3 c = RiftScreenPoint(camera, tell.X - ox, tell.Y - oy), d = RiftScreenPoint(camera, tell.EndX - ox, tell.EndY - oy);
                float width = (float)Math.Sqrt((a.x - c.x) * (a.x - c.x) + (a.y - c.y) * (a.y - c.y));
                DrawRiftLine(center, end, fill, width);
                DrawRiftLine(a, b, edge, 3 * scale); DrawRiftLine(c, d, edge, 3 * scale);
                DrawRiftLine(a, c, edge, 3 * scale); DrawRiftLine(b, d, edge, 3 * scale);
                instruction = "离开直线";
            }
            else
            {
                DrawRiftArea(camera, tell.X, tell.Y, tell.Radius, tell.Shape == "ring" ? tell.InnerRadius : 0, fill);
                DrawRiftRing(camera, tell.X, tell.Y, tell.Radius, edge, scale);
                if (tell.Shape == "ring" && tell.InnerRadius > 0) DrawRiftRing(camera, tell.X, tell.Y, tell.InnerRadius, edge, scale);
                instruction = tell.Shape == "ring" ? "避开圆环" : "离开圆圈";
            }
            string text = tell.Ability + " · " + (remaining / 1000d).ToString("0.0", CultureInfo.InvariantCulture) + "秒 · " + instruction;
            BitmapFont.CenterOutline(new Rect(center.x - 180 * scale, center.y - 29 * scale, 360 * scale, 24 * scale), text, edge, scale);
        }
    }
    private static void DrawRiftRing(Camera camera, float x, float y, float radius, Color color, float scale)
    {
        const int segments = 48;
        Vector3 previous = RiftScreenPoint(camera, x + radius, y);
        for (int i = 1; i <= segments; i++)
        {
            double angle = i * Math.PI * 2 / segments;
            Vector3 next = RiftScreenPoint(camera, x + (float)Math.Cos(angle) * radius, y + (float)Math.Sin(angle) * radius);
            if (previous.z > 0 && next.z > 0) DrawRiftLine(previous, next, color, 3 * scale);
            previous = next;
        }
    }
    private static void DrawRiftArea(Camera camera, float x, float y, float outer, float inner, Color color)
    {
        // Horizontal scan strips leave the safe center of a ring genuinely clear.
        const int strips = 24;
        float height = outer * 2 / strips;
        for (int i = 0; i < strips; i++)
        {
            float offset = -outer + (i + .5f) * height;
            float span = (float)Math.Sqrt(Math.Max(0, outer * outer - offset * offset));
            float nearestToCenter = Math.Max(0, Math.Abs(offset) - height / 2);
            float gap = nearestToCenter < inner ? (float)Math.Sqrt(inner * inner - nearestToCenter * nearestToCenter) : 0;
            Vector3 left = RiftScreenPoint(camera, x - span, y + offset), right = RiftScreenPoint(camera, x + span, y + offset);
            Vector3 top = RiftScreenPoint(camera, x, y + offset - height / 2), bottom = RiftScreenPoint(camera, x, y + offset + height / 2);
            float thickness = Math.Abs(bottom.y - top.y);
            if (gap == 0) DrawRiftLine(left, right, color, thickness);
            else
            {
                DrawRiftLine(left, RiftScreenPoint(camera, x - gap, y + offset), color, thickness);
                DrawRiftLine(RiftScreenPoint(camera, x + gap, y + offset), right, color, thickness);
            }
        }
    }
    private static void DrawRiftLine(Vector3 from, Vector3 to, Color color, float width)
    {
        if (from.z <= 0 || to.z <= 0 || width <= 0) return;
        float dx = to.x - from.x, dy = to.y - from.y;
        float length = (float)Math.Sqrt(dx * dx + dy * dy);
        if (length <= 0) return;
        Color previous = GUI.color; Matrix4x4 matrix = GUI.matrix;
        try
        {
            GUI.color = color;
            GUIUtility.RotateAroundPivot((float)(Math.Atan2(dy, dx) * 180 / Math.PI), new Vector2(from.x, from.y));
            GUI.DrawTexture(new Rect(from.x, from.y - width / 2, length, width), Texture2D.whiteTexture);
        }
        finally { GUI.matrix = matrix; GUI.color = previous; }
    }
}
}
