using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    private bool ready;
    public float terrainFrameSec = 0.2f;
    private int curFrame;

    // Official 06000ba6: the official member takes the native GsmMap owner directly, the same
    // convention as 06000b82 CoverFloor.Build(GsmMap) and 06000be1 LegacyFloor.Build(PkgArchive,
    // GsmMap). The live path still calls the ReferenceGsmMap projection overload just below.
    public void Init(GsmMap g, PkgArchive p, Camera c, float pixelsPerUnit, float ww, float wh)
    {
        gm = g == null ? null : new ReferenceGsmMap(g);
        pkg = p;
        cam = c;
        ppu = pixelsPerUnit;
        worldW = ww;
        worldH = wh;
        mat = GameBootstrap.IsoYObjMat();
        if (mat == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            mat = shader != null ? new Material(shader) : null;
        }
        if (gm != null)
        {
            if (gm.IsLegacy)
            {
                TileImgLib.Load(pkg, gm.TileSet);
                TerraShp.Load(pkg, gm.TileSet);
                ready = TileImgLib.Ready && TerraShp.Ready;
            }
            else if (gm.Ok)
            {
                code2name = LoadTileIni(pkg);
                if (!code2name.ContainsKey(0)) code2name[0] = "新手浅草";
                ready = true;
            }
        }
        Refresh(true);
    }

    // Compatibility boundary: existing consumers hold the ReferenceGsmMap projection, so the
    // adapter unwraps to the native owner and runs the restored official member.
    public void Init(ReferenceGsmMap g, PkgArchive p, Camera c, float pixelsPerUnit, float ww, float wh)
        => Init(g == null ? null : g.Native, p, c, pixelsPerUnit, ww, wh);

    private void LateUpdate()
    {
        if (!ready) return;
        Refresh(false);
        TickAnim();
    }

    private void Refresh(bool force)
    {
        if (cam == null || gm == null) return;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        Vector3 position = cam.transform.position;
        float x0 = ppu * (position.x - halfWidth) + worldW * 0.5f;
        float x1 = ppu * (halfWidth + position.x) + worldW * 0.5f;
        float y0 = worldH * 0.5f - ppu * (halfHeight + position.y);
        float y1 = worldH * 0.5f - ppu * (position.y - halfHeight);
        if (gm.IsLegacy) RefreshLegacy(x0, x1, y0, y1, force);
        else RefreshV27(x0, x1, y0, y1, force);
    }

    private void TickAnim()
    {
        float now = Time.time;
        if (animCode.Count > 0)
        {
            int frame = (int)(now / Mathf.Max(0.02f, terrainFrameSec)) & 3;
            if (frame != curFrame)
            {
                curFrame = frame;
                foreach (var pair in animCode)
                {
                    int iid = TerraShp.GetTile(pair.Value, curFrame);
                    if (iid < 0) continue;
                    Sprite sprite = GetLegacySprite(iid);
                    if (sprite != null && active.TryGetValue(pair.Key, out SpriteRenderer renderer))
                        renderer.sprite = sprite;
                }
            }
        }
        TickModernAnim(now);
    }
}

}
