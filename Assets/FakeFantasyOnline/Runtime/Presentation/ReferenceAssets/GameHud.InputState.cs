using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    public static GameHud I;
    public float uiScale;
    private Font font;
    private Color32[] sketchPx;
    private int sketchW, sketchH;
    // Original 06000425 slots. The owner publishes each load result before the next
    // potentially throwing operation; the retained minimap reads these same slots.
    private Texture2D sketchTex;
    private bool hasSketch;
    private float smpOrgX, smpOrgY, canvasWX, canvasWY;
    // +0x180 generated diagram, +0x1ac/+0x1b0 sample cell dimensions.
    internal Texture2D diagramTex;
    internal float cellW, cellH;

    private void Awake()
    {
        I = this;
        font = NpcChatter.CnFont();
    }
    internal bool ShowHotbarForIndependentRuntime { get => showHotbar; set => showHotbar = value; }
    internal bool MiniOpenForIndependentRuntime { get => miniOpen; set => miniOpen = value; }
    // Read side of the chat master door, in the same shape as the two above: the drawn chat bar
    // reads the same byte ChatUI.OnGUI (0x060000f6) tests before it draws anything. It is not
    // ChatUI.panelsVisible (+0x69): that slot only collapses the message panels and leaves the
    // bar, the input box and the size button drawn. The only official writer of this byte is
    // GameHud..ctor (0x0600454), which stores 0x01010101 over +0x44 and +0x47 - showChat starts
    // true and no frozen body writes it again.
    internal bool ShowChatForIndependentRuntime => showChat;
    // Read side of the four official slots LoadSketch latches: the drawing path consumes these
    // latched values instead of re-deriving the canvas fall-back and the origin halves from
    // GameBootstrap.CurWorldW/H at draw time, so both readers see the same time point.
    internal float SketchCanvasWidthForIndependentRuntime => canvasWX;
    internal float SketchCanvasHeightForIndependentRuntime => canvasWY;
    internal float SketchOriginXForIndependentRuntime => smpOrgX;
    internal float SketchOriginYForIndependentRuntime => smpOrgY;

    // The official 06000414 GameHud.OnGUI runs LoadSketch once per frame before it draws; the
    // official cache guard makes every call after the first one a no-op.
    internal void LoadSketchForIndependentRuntime()
    {
        LoadSketch();
    }

    internal void BindSketchForIndependentRuntime(
        Color32[] pixels,
        int width,
        int height,
        Texture2D sketch,
        Texture2D diagram,
        float canvasWidth,
        float canvasHeight,
        float cellWidth,
        float cellHeight)
    {
        sketchPx = pixels;
        sketchW = width;
        sketchH = height;
        sketchTex = sketch;
        diagramTex = diagram;
        canvasWX = canvasWidth;
        canvasWY = canvasHeight;
        cellW = cellWidth;
        cellH = cellHeight;
        hasSketch = sketchTex != null;
        // sketchName (+0x198) is deliberately not published here. The official body writes it
        // only inside LoadSketch, and pre-assigning it makes the official cache guard
        // short-circuit before the load branch below ever runs.
    }

    internal Texture2D SketchTextureForIndependentRuntime => sketchTex;
    internal Texture2D DiagramTextureForIndependentRuntime => sketchTex != null ? sketchTex : diagramTex;
    internal Color32[] SketchPixelsForIndependentRuntime => sketchPx;
    internal int SketchWidthForIndependentRuntime => sketchW;
    internal int SketchHeightForIndependentRuntime => sketchH;

    internal void PublishSketchCanvasForIndependentRuntime(float width, float height)
    {
        canvasWX = width;
        canvasWY = height;
        smpOrgX = (GameBootstrap.CurWorldW - canvasWX) * 0.5f;
        smpOrgY = (GameBootstrap.CurWorldH - canvasWY) * 0.5f;
    }

    internal void PublishSketchRasterForIndependentRuntime(Color32[] pixels, int width, int height)
    {
        sketchW = width;
        sketchH = height;
        sketchPx = pixels;
    }

    // 0x06000425. Keep every native field publication on its original side of the
    // next resource/engine/array operation, including an exception that stops this pass.
    private void LoadSketch()
    {
        ResLoader loader = ResLoader.I;
        if (loader == null) return;
        string name = GameBootstrap.CurMapName;
        if (string.IsNullOrEmpty(name)) return;
        if (name == sketchName)
        {
            if (sketchTex != null) return;
            if (!hasSketch) return;
        }
        sketchName = name;
        sketchTex = loader.LoadGsa("res/sketchmap/" + name + ".gsa");
        hasSketch = sketchTex != null;
        ReferenceFirstWorldMinimapHud.NormalizeSketchAlpha(sketchTex);

        sketchPx = null;
        sketchW = 0;
        sketchH = 0;
        cellW = 0f;
        cellH = 0f;
        smpOrgX = 0f;
        smpOrgY = 0f;
        canvasWX = 0f;
        canvasWY = 0f;
        byte[] bytes = loader.ReadMap("map/" + name + ".smp");
        ReferenceSketchMapSample.TryParse(bytes, out _, out _, this);

        if (diagramTex != null)
        {
            ReferenceFirstWorldMinimapHud.DestroyOwned(diagramTex);
            diagramTex = null;
        }
        if (sketchTex == null && sketchPx != null && sketchW > 0 && sketchH > 0)
        {
            ReferenceFirstWorldMinimapHud.BuildDiagramTexture(
                sketchW, sketchH, sketchPx, this);
        }
        if (sketchPx == null && sketchTex != null)
        {
            Color32[] source = sketchTex.GetPixels32();
            sketchW = sketchTex.width;
            sketchH = sketchTex.height;
            sketchPx = new Color32[sketchW * sketchH];
            ReferenceFirstWorldMinimapHud.CopyRowsReversedInto(
                source, sketchPx, sketchW, sketchH);
        }

        // Official fall-back after the image stages (arm64 0x437024 / x86_64 0x44dc28): when no
        // cell size produced a canvas, both canvas halves take the whole world extent, which
        // leaves DiagramFrac dividing by CurWorldW/CurWorldH over a zero origin.
        if (canvasWX <= 0f)
        {
            canvasWX = GameBootstrap.CurWorldW;
            canvasWY = GameBootstrap.CurWorldH;
        }

        // Official tail (arm64 06000425.c:418-430 / x86_64 06000425.c:431-435): the map id is the
        // numeric tail of CurMapName, with the literal "0" when the name is too short to carry
        // one; MapDef is refreshed from the loader's primary package; the resolved display name
        // lands at +0x1a0, and then both halves of the UpdateMiniView cache key are written
        // unconditionally - +0x1cc miniDirty = 1 and +0x1d0 lastSx = 0xffffd8f1 (-9999). The
        // atlas names that slot run miniDirty, lastSx, lastSy, so those two stores are the ones
        // UpdateMiniView (06000427) tests at arm64 06000427.c:320-321 before it reuses a raster:
        // a map switch publishes them and the view body cannot early-return onto the old pixels.
        int mapId = 0;
        int.TryParse(name.Length > 2 ? name.Substring(2) : "0", out mapId);
        MapDef.EnsureLoaded(loader.Pkg);
        MapDef.Entry entry;
        string resolved = string.Empty;
        if (MapDef.TryGet(mapId, out entry) && !string.IsNullOrEmpty(entry.name))
        {
            resolved = entry.name;
        }
        mapName = resolved;
        miniDirty = true;
        lastSx = -9999;
    }

    // 0x06000426 DiagramFrac. fu is the X fraction and fv the Y fraction: the OnGUI call site
    // passes (&local_f8 + 4, &local_f8) and draws x from local_f8._4_4_ and y from local_f8.
    internal bool DiagramFrac(out float fu, out float fv)
    {
        fu = 0.5f;
        fv = 0.5f;
        Transform player = GameBootstrap.PlayerT;
        if (player == null) return false;
        float worldW = GameBootstrap.CurWorldW;
        // Official entry test, read off the frozen machine code of both slices:
        //   arm64  0x43e7e0 ldr s0,[x8,#0xa8] ; 0x43e7e4 fcmp s0,#0.0 ; 0x43e7e8 b.ls -> return 0
        //          (second copy 0x43e8b0/0x43e8b4/0x43e8b8 b.hi -> body, fallthrough -> return 0)
        //   x86_64 0x456527 xorps %xmm0,%xmm0 ; 0x45652a ucomiss 0xa8(%rax),%xmm0
        //          0x456531 jae -> return 0 ; (second copy 0x45660b/0x45660e/0x456615 jb -> body)
        // The two codegens genuinely disagree for a NaN CurWorldW: arm64's LS/HI pair treats an
        // unordered compare as "not > 0" and returns false, x86_64's CF-only jae/jb pair treats it
        // as "0.0 < worldW" and enters the body. The product keeps the x86_64 reading it already
        // had; the boundary is recorded, not unified, because nothing adjudicates either codegen.
        if (worldW <= 0f) return false;
        LoadSketch();
        float ppu = GameBootstrap.CurPpu;
        Vector3 position = player.position;
        float worldX = position.x * ppu + worldW * 0.5f;
        float worldY = GameBootstrap.CurWorldH * 0.5f - position.y * ppu;
        // Official X branch, read off the frozen machine code of both slices (they agree):
        //   arm64  0x43e858 ldr s0,[x20,#0x1bc] ; 0x43e85c fcmp s0,#0.0 ; 0x43e860 b.le 0x43e8c4
        //   x86_64 0x4565ca movss 0x1bc(%r14),%xmm0 ; 0x4565d6 ucomiss %xmm1,%xmm0 ; 0x4565d9 jbe
        // Both jump to the whole-world fall-back for canvasWX <= 0 and also for a NaN canvas
        // (b.le is true and jbe is taken when the compare is unordered), so the positive test is
        // the machine truth on both architectures. An earlier pass had copied the Ghidra C text
        // "+0x1bc <= 0.0" literally, which sends a NaN canvas into the division: that polarity is
        // a decompiler rendering, not the instruction.
        fu = canvasWX > 0f ? (worldX - smpOrgX) / canvasWX : worldX / worldW;
        // Official Y branch, same reading; arm64 has the decision twice and both copies agree:
        //   0x43e878/0x43e87c/0x43e880 fcmp + b.gt -> division at 0x43e8f4, fallthrough -> fall-back
        //   0x43e8e8/0x43e8ec/0x43e8f0 fcmp + b.le -> fall-back at 0x43e884, fallthrough -> division
        //   x86_64 0x456663/0x45666f/0x456672 ucomiss + jbe -> fall-back at 0x456683
        // Both rejoin at 0x43e900 (arm64) / 0x4566ae (x86_64) where fv is stored. A NaN canvas
        // height lands in the fall-back on both, so the earlier "arm64 >0 vs x86_64 <=0"
        // divergence was only the decompiler's rendering of the same predicate.
        fv = canvasWY > 0f ? (worldY - smpOrgY) / canvasWY : worldY / GameBootstrap.CurWorldH;
        return true;
    }
}
}
