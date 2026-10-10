using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official GameHud instance slots owned by the two members below (atlas fields table):
    // +0x1c5 diagramOpen (System.Boolean, ordinal after miniOpen), +0x1c8 miniZoom (Single),
    // +0x1cc miniDirty (Boolean). ReferenceFirstWorldMinimapHud reads and writes them through
    // hudOwner exactly like it already does for miniOpen, so the zoom and the diagram flag have
    // one storage location, not two.
    internal bool diagramOpen;
    // 06000414 allocates this shared 96-point buffer lazily at either zone-map entry.
    internal Vector2[] _zonePts;

    // Compact-map zone slice of 06000414, ARM 42df34-42e304 / x86 443f78-4443a4.
    // The current circle is solid, the next one uses every other point. Unlike the diagram
    // helper below, these coordinates deliberately remain unrounded.
    internal void DrawCompactZoneRings(Rect mapRect, float radiusLimit)
    {
        if (!ZoneOverlay.Showing || !(canvasWX > 0f)) return;

        Texture2D dot = ZoneOverlay.Dot;
        Color previous = GUI.color;
        float dotSize = Mathf.Clamp(miEz * 40f, s * 2f, s * 8f);
        if (_zonePts == null) _zonePts = new Vector2[96];
        GUI.color = new Color(0.85f, 0.45f, 1f, 0.95f);
        ZoneOverlay.CirclePoints(ZoneOverlay.cx, ZoneOverlay.cy, ZoneOverlay.r,
            _zonePts.Length, _zonePts);
        for (int index = 0; index < _zonePts.Length; index++)
        {
            float ox;
            float oy;
            if (MiniOffsetOf(_zonePts[index].x, _zonePts[index].y, radiusLimit, out ox, out oy))
            {
                GUI.DrawTexture(new Rect(
                    mapRect.x + mapRect.width * 0.5f + ox - dotSize * 0.5f,
                    mapRect.y + mapRect.height * 0.5f + oy - dotSize * 0.5f,
                    dotSize, dotSize), dot);
            }
        }

        if (ZoneOverlay.nextR > 1f && Mathf.Abs(ZoneOverlay.nextR - ZoneOverlay.r) > 1f)
        {
            GUI.color = new Color(0.8f, 1f, 1f, 0.9f);
            ZoneOverlay.CirclePoints(ZoneOverlay.nextCx, ZoneOverlay.nextCy, ZoneOverlay.nextR,
                _zonePts.Length, _zonePts);
            for (int index = 0; index < _zonePts.Length; index += 2)
            {
                float ox;
                float oy;
                if (MiniOffsetOf(_zonePts[index].x, _zonePts[index].y, radiusLimit, out ox, out oy))
                {
                    GUI.DrawTexture(new Rect(
                        mapRect.x + mapRect.width * 0.5f + ox - dotSize * 0.5f,
                        mapRect.y + mapRect.height * 0.5f + oy - dotSize * 0.5f,
                        dotSize, dotSize), dot);
                }
            }
        }

        GUI.color = previous;
    }

    // Complete body of generated 06000462, ARM 438544/936 B, x86 44f780/1232 B.
    // diagram and dotSize/dot carry the two original local-function captures. The caller
    // owns allocation and color restoration; this body has neither a null guard nor cleanup.
    internal void DrawDiagramZoneRing(float worldX, float worldY, float radius, Color color,
        int step, Rect diagram, float dotSize, Texture2D dot)
    {
        GUI.color = color;
        ZoneOverlay.CirclePoints(worldX, worldY, radius, _zonePts.Length, _zonePts);
        for (int index = 0; index < _zonePts.Length; index += step)
        {
            float u = canvasWX > 0f
                ? (_zonePts[index].x - smpOrgX) / canvasWX
                : _zonePts[index].x / GameBootstrap.CurWorldW;
            float v = canvasWY > 0f
                ? (_zonePts[index].y - smpOrgY) / canvasWY
                : _zonePts[index].y / GameBootstrap.CurWorldH;
            if (u < -0.05f || u > 1.05f || v < -0.05f || v > 1.05f) continue;

            GUI.DrawTexture(new Rect(
                Mathf.Round(diagram.x + u * diagram.width - dotSize * 0.5f),
                Mathf.Round(diagram.y + v * diagram.height - dotSize * 0.5f),
                dotSize, dotSize), dot);
        }
    }

    // 0x06000451 GameHud.ToggleMapDiagram (arm64 0x442b40 / x86_64 0x45aad0, 16 bytes on both
    // slices, queries/WBD-034/{arm64,x86_64}-06000451). The whole body is one byte flip:
    //   *(byte *)(param_1 + 0x1c5) = *(byte *)(param_1 + 0x1c5) ^ 1;
    // i.e. the official field GameHud.diagramOpen (+0x1c5). No guard, no resource read, no
    // return value.
    public void ToggleMapDiagram()
    {
        diagramOpen = !diagramOpen;
    }

    // 0x06000450 GameHud.MinimapZoom(System.Boolean zoomIn) (arm64 0x442b00, 64 bytes;
    // x86_64 0x45aa70, 96 bytes). Both slices agree; arm64 computes both halves and selects,
    // x86_64 branches and still writes the same two slots:
    //   zoomIn  -> miniZoom = min(miniZoom * 1.25f, 3f)
    //   zoomOut -> miniZoom = max(miniZoom / 1.25f, 0.5f)
    //   then    -> miniDirty = true, unconditionally (not gated on the value changing).
    // The official step is 1.25 and the official ceiling is 3; the floor is 0.5 on both sides.
    // Those three constants come from the frozen bodies and they are the only step this slot has:
    // the carrier no longer keeps a second factor or clamp of its own.
    public void MinimapZoom(bool zoomIn)
    {
        float zoomedIn = miniZoom * 1.25f;
        if (zoomedIn > 3f)
        {
            zoomedIn = 3f;
        }
        float zoomedOut = miniZoom / 1.25f;
        if (zoomedOut < 0.5f)
        {
            zoomedOut = 0.5f;
        }
        miniZoom = zoomIn ? zoomedIn : zoomedOut;
        miniDirty = true;
    }
}

}
