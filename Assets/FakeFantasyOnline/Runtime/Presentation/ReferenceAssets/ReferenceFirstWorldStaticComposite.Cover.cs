using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceFirstWorldStaticComposite
{
    private Texture2D coverTexture;
    private Sprite coverSprite;
    private bool ownsCoverTexture;
    private bool hasPreparedCoverTexture;

    internal void AdoptCoverTextureForIndependentRuntime(Texture2D texture)
    {
        coverTexture = texture;
        ownsCoverTexture = true;
        hasPreparedCoverTexture = true;
    }

    private bool BuildCoverBackground()
    {
        if (!hasPreparedCoverTexture)
        {
            ownsCoverTexture = !map.HasTextureBackgroundForIndependentRuntime;
            coverTexture = ownsCoverTexture
                ? CoverFloor.Build(map.Native)
                : map.TextureBackgroundForIndependentRuntime;
        }
        if (coverTexture == null) return false;
        coverSprite = Sprite.Create(coverTexture,
            new Rect(0, 0, coverTexture.width, coverTexture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
        SpriteRenderer renderer = floorRootObject.AddComponent<SpriteRenderer>();
        renderer.sprite = coverSprite;
        renderer.sortingOrder = -10000;
        Bounds bounds = coverSprite.bounds;
        floorRootObject.transform.localScale = new Vector3(
            (map.MapWidth / PixelsPerUnit) / (bounds.extents.x + bounds.extents.x),
            (map.MapHeight / PixelsPerUnit) / (bounds.extents.y + bounds.extents.y), 1f);
        return true;
    }
}

}
