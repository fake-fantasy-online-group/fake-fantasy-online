using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public class StallSign : MonoBehaviour
{
    private const float PlateW = 110f;
    private const float PlateCxOff = -2f;
    public int browseOwnerId;
    public float ppu = 100f;
    public Material mat;
    private static Texture2D bubMaichu;
    private static Texture2D bubShougou;
    private static bool bubTried;
    private static Material ownMat;
    private static bool matTried;
    private GameObject bubGo;
    private SpriteRenderer bubSr;
    private GameObject txtGo;
    private SpriteRenderer txtSr;
    private int lastType = -2;
    private string lastName = "\0";

    private Material Mat()
    {
        if (mat) return mat;
        if (!matTried)
        {
            matTried = true;
            Shader shader = Shader.Find("Overlay/NameTag");
            if (shader) ownMat = new Material(shader);
        }
        return ownMat;
    }

    public void Tick(bool active, byte type, string name, float headTopY)
    {
        if (!active)
        {
            if (bubGo && bubGo.activeSelf) bubGo.SetActive(false);
            return;
        }
        if (!bubTried)
        {
            ResLoader loader = ResLoader.I;
            if (!loader) return;
            bubTried = true;
            bubMaichu = loader.LoadGsa2021First("res/effect/paopao_maichu.gsa");
            bubShougou = loader.LoadGsa2021First("res/effect/paopao_shougou.gsa");
            if (!bubMaichu || !bubShougou)
                Debug.LogWarning(string.Format("[StallSign] 摊位招牌气泡缺失(maichu={0} shougou={1})",
                    (bool)bubMaichu, (bool)bubShougou) +
                    " → 别人屏幕上看不到你的摊位。检查 res/effect/paopao_maichu.gsa / paopao_shougou.gsa 有没有打进资源包");
        }
        Texture2D bubble = type == 1 ? bubShougou : bubMaichu;
        if (!bubble) return;
        Material material = Mat();
        if (!bubGo)
        {
            bubGo = new GameObject("StallSignBubble");
            bubGo.transform.SetParent(transform, false);
            bubSr = bubGo.AddComponent<SpriteRenderer>();
            bubSr.sortingOrder = 32001;
            if (material) bubSr.sharedMaterial = material;
            if (browseOwnerId > 0)
            {
                BoxCollider2D collider = bubGo.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(118f / ppu, 69f / ppu);
                collider.offset = new Vector2(PlateCxOff / ppu, 43.5f / ppu);
                bubGo.AddComponent<StallSignClick>().ownerId = browseOwnerId;
            }
        }
        if (!bubGo.activeSelf) bubGo.SetActive(true);
        if (lastType != type)
        {
            lastType = type;
            bubSr.sprite = TexScale.MakeSprite(bubble, ppu, 0.5f, 0f);
        }
        bubGo.transform.localPosition = new Vector3(0f, headTopY, 0f);
        if (!BitmapFont.Ready) return;
        if (txtSr && txtSr.sprite && name == lastName) return;
        lastName = name;
        Sprite old = txtSr ? txtSr.sprite : null;
        string text = name ?? string.Empty;
        var lines = BitmapFont.Wrap(text, PlateW, 1f);
        string first = null;
        if (lines.Count > 1)
        {
            first = lines[0];
            if (lines.Count >= 2)
                text = BitmapFont.Elide(string.Concat(lines.GetRange(1, lines.Count - 1)), PlateW, 1f);
        }
        Texture2D texture = BitmapFont.BakeNameTag(text, string.Empty, Color.white,
            Color.white, 1, first != null ? new[] { first } : null, Color.white, null, null, null);
        if (!texture) return;
        if (!txtGo)
        {
            txtGo = new GameObject("StallSignText");
            txtGo.transform.SetParent(bubGo.transform, false);
            txtSr = txtGo.AddComponent<SpriteRenderer>();
            txtSr.sortingOrder = 32002;
            if (material) txtSr.sharedMaterial = material;
            txtGo.transform.localPosition = new Vector3(PlateCxOff / ppu, 40f / ppu, 0f);
        }
        txtSr.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), ppu);
        if (old)
        {
            Texture2D previousTexture = old.texture;
            Object.Destroy(old);
            if (previousTexture) Object.Destroy(previousTexture);
        }
    }

    private void OnDestroy()
    {
        if (bubSr && bubSr.sprite) Object.Destroy(bubSr.sprite);
        if (txtSr && txtSr.sprite)
        {
            Texture2D texture = txtSr.sprite.texture;
            Object.Destroy(txtSr.sprite);
            if (texture) Object.Destroy(texture);
        }
    }
}

}
