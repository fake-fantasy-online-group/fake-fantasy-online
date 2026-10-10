using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 PetSummonFx, tokens 06000967-0600096b. It plays the short ring
// effect that appears where a pet is summoned or recalled: the animation frames
// come from res/effect/<name>.gsa, are cached per name and pixels-per-unit, and
// a self-destroying MonoBehaviour drives them once.
public class PetSummonFx : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite[]> cache =
        new Dictionary<string, Sprite[]>();

    private Sprite[] frames;

    private SpriteRenderer sr;

    private float t;

    private float frameDur = 0.08f;

    private float life;

    private int frame;

    private bool recall;

    // 06000967
    public static void Spawn(Vector3 worldPos, Material depthMat, float ppu, bool recall,
        string effect = "circle")
    {
        Sprite[] sprites = Load(effect, ppu);
        if (sprites == null || sprites.Length == 0) return;

        GameObject gameObject = new GameObject(recall ? "PetRecallFx" : "PetSummonFx");
        gameObject.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        PetSummonFx fx = gameObject.AddComponent<PetSummonFx>();
        fx.frames = sprites;
        fx.recall = recall;
        fx.life = fx.frameDur * sprites.Length;

        fx.sr = gameObject.AddComponent<SpriteRenderer>();
        if (depthMat != null) fx.sr.sharedMaterial = depthMat;
        fx.sr.sortingOrder = 1;
        fx.sr.sprite = sprites[0];
    }

    // 06000968
    private static Sprite[] Load(string effect, float ppu)
    {
        string key = effect + "@" + ppu;
        Sprite[] cached;
        if (cache.TryGetValue(key, out cached)) return cached;

        Sprite[] result = null;
        if (ResLoader.I != null && ResLoader.I.Pkg != null)
        {
            byte[] bytes = ResLoader.I.Pkg.Read("res/effect/" + effect + ".gsa");
            if (bytes != null)
            {
                DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(bytes);
                if (anim.ok && anim.f != null)
                {
                    int dirs = anim.dirs;
                    if (dirs < 2) dirs = 1;
                    int count = anim.frames / dirs;
                    if (count < 1) count = anim.f.Length;

                    List<Sprite> list = new List<Sprite>();
                    for (int i = 0; i < count; i++)
                    {
                        if (i >= anim.f.Length) break;
                        Texture2D tex = anim.f[i].tex;
                        if (tex == null) continue;
                        TexScale.Register(tex, anim.scale);
                        list.Add(TexScale.MakeSprite(tex, ppu, 0.5f, 0.5f));
                    }
                    if (list.Count > 0) result = list.ToArray();
                }
            }
        }

        cache[key] = result;
        return result;
    }

    // 06000969
    private void Update()
    {
        if (frames == null || sr == null)
        {
            Destroy(gameObject);
            return;
        }

        t += Time.deltaTime;
        int index = Mathf.Min((int)(t / frameDur), frames.Length - 1);
        if (frame != index)
        {
            frame = index;
            sr.sprite = frames[frame];
        }

        float k = Mathf.Clamp01(t / life);
        float scale;
        float alpha = 1f;
        if (recall)
        {
            scale = k * -0.7f + 1f;
            alpha = 1f - k;
        }
        else
        {
            scale = k * 0.6f + 0.4f;
            if (k < 0.3f) alpha = k / 0.3f;
        }

        transform.localScale = new Vector3(scale, scale, 1f);
        Color color = sr.color;
        color.a = alpha;
        sr.color = color;

        if (t >= life) Destroy(gameObject);
    }
}
}
