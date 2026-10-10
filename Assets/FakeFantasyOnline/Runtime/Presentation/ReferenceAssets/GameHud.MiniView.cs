using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official instance slots owned by the three members below: +0x04c s, +0x0178 miniView,
    // +0x01d0 lastSx, +0x01d4 lastSy, +0x0200 gateMarks, +0x0208 gateMarksSrc, +0x0210 miMarkOffX,
    // +0x0214 miMarkOffY, +0x0218 miCcx, +0x021c miCcy, +0x0220 miEz. +0x01c8 miniZoom and
    // +0x01cc miniDirty are declared in GameHud.Initialization.cs; the zoom below is the one
    // MinimapZoom (06000450) steps, and the raster/origin/canvas slots stay in
    // GameHud.InputState.cs, where LoadSketch (06000425) also resets +0x01d0 to -9999 per load.
    public struct GateMark
    {
        public float wx;
        public float wy;
        public int mapTo;
        public string tip;
    }

    // StringLiteral[17031] and StringLiteral[18175] of the frozen metadata usage table.
    private const string GateTipFallback = "传送点";
    private const string GateTipPrefix = "通往 ";
    // Official duplicate distance of BuildGateMarks: 300f squared.
    private const float DuplicateGateRadiusSquared = 90000f;

    internal float s;
    internal Texture2D miniView;
    internal int lastSy;
    internal object gateMarksSrc;
    internal float miMarkOffX;
    internal float miMarkOffY;
    internal float miCcx;
    internal float miCcy;
    internal float miEz;

    // 06000427 UpdateMiniView (arm64 0x437134 / x86_64 0x44dd30). Frozen anchors for the steps
    // below, arm64 unless marked; both architectures agree step for step:
    //   guard     :64-66 sketchPx (+0x188) null -> return; no second test.
    //   centre    :70-137 CurWorldW/H lifted to 1f, GameBootstrap.PlayerT (+0xb8) live gives
    //              worldW/2 + pos.x * CurPpu (+0xb0) and worldH/2 - pos.y * CurPpu, else the
    //              two halves alone. The body takes no centre argument.
    //   projection:128-135 canvasW* > 0 ? (world - smpOrg*)/canvasW* * sketch* : world *
    //              sketch* / world(H), i.e. the canvas<=0 case still projects over max(1, world).
    //   size      :138-224 zoom clamped to [0.5f, 4f] and written back to +0x1c8, two independent
    //              RoundToInt(138f * s) halves each lifted to 8 and Min'd, radius = size/2 - 0.5f.
    //   clamp     :229-250 sketch <= 2 * sourceRadius ? sketch * 0.5f : Clamp(source, r, sketch-r).
    //   store     :315-319 the five out-slots are written before the exit test, which needs
    //              !miniDirty (+0x1cc), both quarters equal and, if miniView exists, only
    //              width == size (:330); :336-338 then store the quarters and clear miniDirty.
    //   raster    :346-366 new Texture2D(w, h, TextureFormat.RGBA32, false mipChain) with
    //              filterMode/wrapMode both set to 1, rebuilt when null or width/height differ,
    //              then SetPixels32 (:410, (Color32[])) and Apply (:413, the zero-argument
    //              overload 0x102d944, not Apply(bool) 0x102d93c nor Apply(bool,bool) 0x102d864).
    // The one boundary worth keeping: nothing here tests +0x04c for finiteness, because the only
    // official writer is GameHud.OnGUI itself (06000414 arm64 :279-293, s = uiScale (+0x40) > 0 ?
    // uiScale : UIScale.Factor), and the independent runtime supplies that same value.
    private void UpdateMiniView()
    {
        if (sketchPx == null)
        {
            return;
        }

        float worldWidth = GameBootstrap.CurWorldW;
        if (!(worldWidth >= 1f))
        {
            worldWidth = 1f;
        }

        float worldHeight = GameBootstrap.CurWorldH;
        if (!(worldHeight >= 1f))
        {
            worldHeight = 1f;
        }

        Transform player = GameBootstrap.PlayerT;
        float worldX;
        float worldY;
        if (player != null)
        {
            Vector3 position = player.position;
            worldX = worldWidth * 0.5f + position.x * GameBootstrap.CurPpu;
            worldY = worldHeight * 0.5f - position.y * GameBootstrap.CurPpu;
        }
        else
        {
            worldX = worldWidth * 0.5f;
            worldY = worldHeight * 0.5f;
        }

        float sourceX = canvasWX > 0f
            ? (worldX - smpOrgX) / canvasWX * sketchW
            : worldX * sketchW / worldWidth;
        float sourceY = canvasWY > 0f
            ? (worldY - smpOrgY) / canvasWY * sketchH
            : worldY * sketchH / worldHeight;

        float zoom = miniZoom;
        float clamped = 0.5f;
        if (0.5f <= zoom)
        {
            clamped = zoom;
            if (4f < zoom)
            {
                clamped = 4f;
            }
        }

        miniZoom = clamped;
        int sizeWidth = Mathf.Max(
            8,
            Mathf.RoundToInt(ReferenceFirstWorldMinimapHud.LogicalMapPixels * s));
        int sizeHeight = Mathf.Max(
            8,
            Mathf.RoundToInt(ReferenceFirstWorldMinimapHud.LogicalMapPixels * s));
        int size = Mathf.Min(sizeWidth, sizeHeight);
        float radius = size * 0.5f - 0.5f;
        float effectiveZoom = clamped * s;
        float sourceRadius = radius / effectiveZoom;
        float centerX = sketchW <= sourceRadius * 2f
            ? sketchW * 0.5f
            : Mathf.Clamp(sourceX, sourceRadius, sketchW - sourceRadius);
        float centerY = sketchH <= sourceRadius * 2f
            ? sketchH * 0.5f
            : Mathf.Clamp(sourceY, sourceRadius, sketchH - sourceRadius);

        miMarkOffX = effectiveZoom * (sourceX - centerX);
        miMarkOffY = effectiveZoom * (sourceY - centerY);
        miCcx = centerX;
        miCcy = centerY;
        miEz = effectiveZoom;

        int quarterX = Mathf.RoundToInt(centerX * 4f);
        int quarterY = Mathf.RoundToInt(centerY * 4f);
        if (!miniDirty && quarterX == lastSx && quarterY == lastSy && miniView != null &&
            miniView.width == sizeWidth)
        {
            return;
        }

        lastSx = quarterX;
        lastSy = quarterY;
        miniDirty = false;
        // No Destroy precedes this assignment: the official body replaces +0x178 in place and
        // issues no release call, so the native behaviour (a resize leaving the previous GPU
        // texture behind) is kept rather than "corrected". The single release point for the
        // current instance is the minimap carrier's Dispose.
        if (miniView == null || miniView.width != sizeWidth || miniView.height != sizeHeight)
        {
            miniView = new Texture2D(sizeWidth, sizeHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        Color32[] pixels = new Color32[sizeHeight * sizeWidth];
        float radiusSquared = radius * radius;
        int rowBase = sizeWidth * (sizeHeight - 1);
        for (int row = 0; row != sizeHeight; row++)
        {
            float deltaY = row + 0.5f - sizeHeight * 0.5f;
            float sampleY = centerY + deltaY / effectiveZoom;
            int target = rowBase;
            for (int column = 0; column != sizeWidth; column++)
            {
                // SampleBilin (0600042a) is the per-pixel dependency; it reads sketchPx/sketchW/
                // sketchH itself, so only the source point and the packed fall-back 0xff2f6360 =
                // Color32(96, 99, 47, 255) are passed. The shared reader carries that body, and
                // CompositeFallback is the integer lane alpha-over composite 06000427 inlines
                // after it (arm64 :382-401: 'a' from bits 24-31, composited only when a < 250,
                // then (a * channel + (255 - a) * fallback_channel) / 255 with alpha forced to 255).
                float deltaX = column + 0.5f - sizeWidth * 0.5f;
                if (deltaX * deltaX + deltaY * deltaY <= radiusSquared)
                {
                    pixels[target] = ReferenceFirstWorldMinimapHud.CompositeFallback(
                        ReferenceSketchMapSample.SampleBilinear(
                            sketchPx,
                            sketchW,
                            sketchH,
                            centerX + deltaX / effectiveZoom,
                            sampleY,
                            ReferenceFirstWorldMinimapHud.FallbackColor));
                }

                target++;
            }

            rowBase -= sizeWidth;
        }

        miniView.SetPixels32(pixels);
        miniView.Apply();
    }

    // 06000428 MiniOffsetOf(Single wx, Single wy, Single rlim, Single& ox, Single& oy) (arm64
    // 0x4380f8 / x86_64 0x44f020). Both out halves are zeroed before the four-test guard (arm64
    // :31-32): sketchPx != null, sketchW > 0, sketchH > 0, miEz (+0x220) > 0. The canvas halves
    // keep the same positive test and max(1, CurWorld*) fall-back as UpdateMiniView, and the
    // result is only the radius test against rlim, never a visibility decision of its own.
    internal bool MiniOffsetOf(float wx, float wy, float rlim, out float ox, out float oy)
    {
        oy = 0f;
        ox = 0f;
        if (sketchPx == null || sketchW <= 0 || sketchH <= 0 || miEz <= 0f)
        {
            return false;
        }

        float sourceX;
        if (canvasWX > 0f)
        {
            sourceX = (wx - smpOrgX) / canvasWX * sketchW;
        }
        else
        {
            float worldWidth = GameBootstrap.CurWorldW;
            if (!(worldWidth >= 1f))
            {
                worldWidth = 1f;
            }

            sourceX = wx * sketchW / worldWidth;
        }

        float sourceY;
        if (canvasWY > 0f)
        {
            sourceY = (wy - smpOrgY) / canvasWY * sketchH;
        }
        else
        {
            float worldHeight = GameBootstrap.CurWorldH;
            if (!(worldHeight >= 1f))
            {
                worldHeight = 1f;
            }

            sourceY = wy * sketchH / worldHeight;
        }

        ox = (sourceX - miCcx) * miEz;
        oy = (sourceY - miCcy) * miEz;
        return ox * ox + oy * oy <= rlim * rlim;
    }

    // 06000429 BuildGateMarks (arm64 0x437b00 / x86_64 0x44e920). Frozen anchors, arm64 unless
    // marked: guards :103-128 PlayerController.I (static +0x78) null, transferProcs (+0x1f0) null
    // and the same list reference already in gateMarksSrc (+0x208); clear :130-137 in place with a
    // version bump and Array.Clear; dependencies :138-159 ResLoader.I is read for the Unity-null
    // test and read again for the MapDef.EnsureLoaded (060005b1) operand, never hoisted into a
    // local, and :160 re-reads transferProcs once more before enumerating; per proc :169-213
    // centroid = plain mean of every point, low half x and high half y; duplicate :205-213 walks
    // the fresh list and drops a candidate within 300f of an existing mark of the same mapTo;
    // mark :214-257 tip is "传送点" unless MapDef.TryGet (060005b3) resolves a non-empty name, in
    // which case "通往 " + name, and mapTo is the procId the lookups already used.
    private void BuildGateMarks()
    {
        PlayerController player = PlayerController.I;
        if (player == null)
        {
            return;
        }

        List<MapLink.MapProc> source = player.transferProcs;
        if (source == null)
        {
            return;
        }

        if (gateMarksSrc == source)
        {
            return;
        }

        gateMarksSrc = source;
        gateMarks.Clear();
        if (ResLoader.I != null)
        {
            MapDef.EnsureLoaded(ResLoader.I.Pkg);
        }

        source = player.transferProcs;
        if (source == null)
        {
            return;
        }

        for (int index = 0; index < source.Count; index++)
        {
            MapLink.MapProc proc = source[index];
            Vector2[] poly = proc.poly;
            if (poly == null || poly.Length < 3)
            {
                continue;
            }

            float sumX = 0f;
            float sumY = 0f;
            for (int point = 0; point < poly.Length; point++)
            {
                sumX += poly[point].x;
                sumY += poly[point].y;
            }

            float centroidX = sumX / poly.Length;
            float centroidY = sumY / poly.Length;
            bool duplicate = false;
            for (int existing = 0; existing < gateMarks.Count; existing++)
            {
                GateMark mark = gateMarks[existing];
                if (mark.mapTo != proc.procId)
                {
                    continue;
                }

                float deltaX = mark.wx - centroidX;
                float deltaY = mark.wy - centroidY;
                if (deltaX * deltaX + deltaY * deltaY < DuplicateGateRadiusSquared)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
            {
                continue;
            }

            MapDef.Entry entry;
            string name = MapDef.TryGet(proc.procId, out entry) ? entry.name : null;
            gateMarks.Add(new GateMark
            {
                wx = centroidX,
                wy = centroidY,
                mapTo = proc.procId,
                tip = string.IsNullOrEmpty(name) ? GateTipFallback : GateTipPrefix + name
            });
        }
    }

    // Independent-runtime bindings, in the shape GameHud.InputState.cs already uses for
    // LoadSketch. PublishUiScale restates the single official write of +0x04c (06000414 arm64
    // :279-293) because that slice of GameHud.OnGUI is not recovered; the caller supplies the
    // same value it already passes into the minimap draw. Nothing else here opens a gate the
    // three bodies above do not open themselves.
    internal bool SketchRasterLoadedForIndependentRuntime => sketchPx != null;
    internal IReadOnlyList<GateMark> GateMarksForIndependentRuntime => gateMarks;
    internal void BuildGateMarksForIndependentRuntime()
    {
        BuildGateMarks();
    }

    internal void PublishUiScaleForIndependentRuntime(float value)
    {
        s = value > 0f ? value : UIScale.Factor;
    }

    internal void UpdateMiniViewForIndependentRuntime()
    {
        UpdateMiniView();
    }

    internal bool MiniOffsetOfForIndependentRuntime(
        float wx,
        float wy,
        float rlim,
        out float ox,
        out float oy)
    {
        return MiniOffsetOf(wx, wy, rlim, out ox, out oy);
    }

    internal Texture2D MiniViewForIndependentRuntime => miniView;
    internal void ClearMiniViewForIndependentRuntime()
    {
        miniView = null;
    }

    // The three out-slots the mark layer consumes, read back from UpdateMiniView instead of the
    // caller keeping a second copy of the same decision.
    internal float MiniMarkOffsetXForIndependentRuntime => miMarkOffX;
    internal float MiniMarkOffsetYForIndependentRuntime => miMarkOffY;
    internal float MiniSampleCenterXForIndependentRuntime => miCcx;
    internal float MiniSampleCenterYForIndependentRuntime => miCcy;
    internal float MiniEffectiveZoomForIndependentRuntime => miEz;
}

}
