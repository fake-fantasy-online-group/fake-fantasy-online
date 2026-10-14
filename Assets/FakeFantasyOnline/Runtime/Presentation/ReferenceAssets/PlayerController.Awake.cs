using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private Sprite ringSprite;

    private void Awake()
    {
        I = this;
        CancelMobileInput();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.color = Color.white;
        Material material = GameBootstrap.IsoYCharMat();
        if (material != null)
        {
            sr.sharedMaterial = material;
            sr.sortingOrder = 0;
        }
        sr.enabled = false;
        EnsureDoll();
        if (GetComponent<PlayerNameTag>() == null) gameObject.AddComponent<PlayerNameTag>();
        if (GetComponent<PlayerTaskTrack>() == null) gameObject.AddComponent<PlayerTaskTrack>();
        if (GetComponent<PlayerEmote>() == null) gameObject.AddComponent<PlayerEmote>();
        LoadFrames();
        Texture2D texture = Resources.Load<Texture2D>("Effect/circle");
        if (texture != null)
            ringSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), ppu);
    }
}

}
