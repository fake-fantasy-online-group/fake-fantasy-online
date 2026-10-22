using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    private string lastNameTag;
    private Color lastNameCol;
    private object lastNameFont;
    private bool lastNameTintable;

    private bool HiddenByF11
    {
        get
        {
            if (!friendly || petOwnerId == 0 || petOwnerId == NetClient.SelfEntityId) return false;
            if (CharData.nameMode < 3) return false;
            if (CharData.nameMode == 4) return !SocialClient.InParty(petOwnerId);
            if (CharData.nameMode == 5) return !StallDlg.IsStallOwner(petOwnerId);
            return true;
        }
    }

    private void UpdateNameTag()
    {
        bool visible = !string.IsNullOrEmpty(mname) && !dying && BitmapFont.Ready;
        if (visible && friendly && !CharData.petShow) visible = false;
        if (visible && HiddenByF11) visible = false;
        if (visible && !friendly && !ally)
        {
            GameSettings.EnsureLoaded();
            visible = GameSettings.ShowMonsterNames;
        }
        if (!visible)
        {
            if (nameTagGo && nameTagGo.activeSelf) nameTagGo.SetActive(false);
            return;
        }
        if (nameTagGo && !nameTagGo.activeSelf) nameTagGo.SetActive(true);
        Color color = GameHud.TargetNameColor(this);
        // Tinting preserves the black shadow only for an opaque fill. Keep the
        // original colored bake for fractional alpha so its shadow stays opaque.
        bool tintable = color.a == 1f;
        object font = BitmapFont.NameTagIdentity;
        bool rebuilt = false;
        if (!nameTagSr || !nameTagSr.sprite || mname != lastNameTag ||
            lastNameTintable != tintable || (!tintable && color != lastNameCol) ||
            !ReferenceEquals(lastNameFont, font))
        {
            Sprite old = nameTagSr ? nameTagSr.sprite : null;
            Color fill = tintable ? Color.white : color;
            Texture2D texture = BitmapFont.BakeNameTag(mname, "", fill, fill, 1);
            if (!texture) return;
            if (!nameTagGo)
            {
                nameTagGo = new GameObject("MonName");
                nameTagGo.transform.SetParent(transform, false);
                nameTagSr = nameTagGo.AddComponent<SpriteRenderer>();
                nameTagSr.sortingOrder = 32000;
                if (!nameTagMat)
                {
                    Shader shader = Shader.Find("Overlay/NameTag");
                    if (shader) nameTagMat = new Material(shader);
                }
                if (nameTagMat) nameTagSr.sharedMaterial = nameTagMat;
            }
            SpriteRenderer renderer = nameTagSr;
            renderer.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0f), ppu);
            lastNameTag = mname;
            lastNameFont = font;
            lastNameTintable = tintable;
            rebuilt = true;
            if (old)
            {
                Texture2D oldTexture = old.texture;
                Destroy(old);
                if (oldTexture) Destroy(oldTexture);
            }
        }
        // Match the original bake's Color32 quantization, without another texture
        // allocation/upload when a player level change crosses a difficulty band.
        if (rebuilt || color != lastNameCol)
        {
            nameTagSr.color = tintable ? (Color)(Color32)color : Color.white;
            lastNameCol = color;
        }
        PlayerNameTag.PlaceSharp(nameTagGo.transform, headTopU + 0.15f, ppu, 0f);
    }

    internal void UpdateNameTagForIndependentRuntime() => UpdateNameTag();
    internal GameObject NameTagRootForIndependentRuntime => nameTagGo;
    internal SpriteRenderer NameTagRendererForIndependentRuntime => nameTagSr;
}

}
