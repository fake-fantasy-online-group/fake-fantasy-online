using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// CloudLayer owner initially restored from 1.5.8 (tokens 0600010E..06000111). Called by
/// <c>GameBootstrap.BuildWorld</c> (token 060003D4, call site arm64 0x41f2dc).
/// </summary>
/// <remarks>
/// Recovered from the frozen arm64 + x86_64 decompilations of the official 1.5.8 client
/// (<c>Build/ReferenceReverseGhidra-1.5.8/queries/&lt;arch&gt;/&lt;probe&gt;/packet-CloudLayer/</c>).
/// Constants below were decoded straight out of the arm64 literal pool and
/// <c>mov/movk</c> pairs, and cross-checked against the x86_64 <c>roundss ...,10</c> (ceil)
/// and <c>DAT_</c> float slots.
/// The 1.3.4 reference <see cref="ReferenceWorldCloudMath"/> produces exactly the same
/// density count, velocity, wrap and alpha values and was used as an independent check;
/// The inline form is retained. Bounded 1.6.0 direct-owner corrections use tokens
/// 06000113..06000116; historical citations above retain their original version.
/// </remarks>
public class CloudLayer : MonoBehaviour
{
    private struct Cloud
    {
        public Transform t;
        public SpriteRenderer sr;
        public Vector2 off;
        public Vector2 vel;
        public float baseA;
        public float phase;
        public float pulse;
    }

    /// <summary>
    /// Declared by the official type; no 1.5.8 body reads it directly. The LateUpdate
    /// parallax term is <c>1f - Parallax</c> = 0.9f, which is what arm64 0x23a1cc materialises
    /// (0x3f666666) — see the contract for this inference.
    /// </summary>
    private const float Parallax = 0.1f;

    private readonly List<Cloud> clouds = new List<Cloud>();
    private readonly List<Texture2D> ownedTex = new List<Texture2D>();
    private Camera cam;
    private float winW;
    private float winH;
    private Vector3 lastCam;
    private bool camInit;

    // Decision-free observations for the existing composition diagnostics. These
    // expose native state; they do not add a second initialized flag or asset cache.
    public Camera Camera => cam;
    public int CloudCount => clouds.Count;
    public int OwnedTextureCount => ownedTex.Count;
    public float WindowWidth => winW;
    public float WindowHeight => winH;

    /// <summary>Token 0600010E.</summary>
    public void Init(
        int cloudType,
        int density,
        int dirDeg,
        int fdirDeg,
        float ppu,
        Camera camera,
        Transform parent,
        int sortingOrder)
    {
        cam = camera;
        if (ResLoader.I == null) return;

        List<ResLoader.ObjSprite> list = ResLoader.I.LoadClouds(cloudType);
        if (list == null || list.Count == 0) return;

        foreach (ResLoader.ObjSprite source in list)
        {
            if (source.tex != null) ownedTex.Add(source.tex);
        }

        // DAT_01126f68 holds the packed pair (1600.0, 1200.0); one fdiv writes both fields.
        winW = 1600f / ppu;
        winH = 1200f / ppu;

        if (density < 65) density = 64;
        int columns = Mathf.CeilToInt(1600f / density);
        int rows = Mathf.CeilToInt(1200f / density);
        int count = Mathf.Clamp(columns * rows, 6, 60);

        for (int index = 0; index < count; index++)
        {
            ResLoader.ObjSprite sprite = list[Random.Range(0, list.Count)];
            GameObject go = new GameObject("cloud" + index.ToString());
            go.transform.parent = parent;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();

            int hd = sprite.hdScale < 2 ? 1 : sprite.hdScale;
            Sprite cloudSprite = Sprite.Create(
                sprite.tex,
                new Rect(0f, 0f, sprite.tex.width, sprite.tex.height),
                new Vector2(0.5f, 0.5f),
                ppu * hd);

            renderer.sprite = cloudSprite;
            renderer.sortingOrder = sortingOrder;

            float scale = Random.Range(0.7f, 1.4f);
            go.transform.localScale = new Vector3(scale, scale, 1f);

            float direction = Random.Range(-dirDeg / 2f, dirDeg / 2f) + fdirDeg;
            float radians = direction * Mathf.Deg2Rad;
            float speed = Random.Range(0.15f, 0.4f);

            Cloud cloud = new Cloud
            {
                t = go.transform,
                sr = renderer,
                // Official 1.6.0 samples both axes across the full centered window.
                off = new Vector2(
                    Random.Range(-winW * 0.5f, winW * 0.5f),
                    Random.Range(-winH * 0.5f, winH * 0.5f)),
                vel = new Vector2(Mathf.Cos(radians) * speed, -Mathf.Sin(radians) * speed),
                baseA = Random.Range(0.35f, 0.6f),
                phase = Random.Range(0f, 6.28f),
                pulse = Random.Range(0.08f, 0.18f)
            };

            clouds.Add(cloud);
        }
    }

    /// <summary>Token 0600010F.</summary>
    private void OnDestroy()
    {
        foreach (Cloud cloud in clouds)
        {
            if (cloud.sr == null) continue;
            if (cloud.sr.sprite != null) Object.Destroy(cloud.sr.sprite);
        }

        foreach (Texture2D tex in ownedTex)
        {
            if (tex != null) Object.Destroy(tex);
        }

        ownedTex.Clear();
    }

    /// <summary>Token 06000110.</summary>
    private void LateUpdate()
    {
        if (cam == null) return;
        if (clouds.Count == 0) return;

        Vector3 camPos = cam.transform.position;
        float prevX;
        float prevY;
        if (camInit)
        {
            prevX = lastCam.x;
            prevY = lastCam.y;
        }
        else
        {
            camInit = true;
            prevX = camPos.x;
            prevY = camPos.y;
        }

        lastCam = camPos;

        float deltaTime = Time.deltaTime;
        float time = Time.time;
        float halfW = winW * 0.5f;
        float halfH = winH * 0.5f;

        for (int index = 0; index < clouds.Count; index++)
        {
            Cloud cloud = clouds[index];

            float x = cloud.off.x + (deltaTime * cloud.vel.x - (camPos.x - prevX) * (1f - Parallax));
            if (x > halfW) x -= winW;
            else if (x < -halfW) x += winW;

            float y = cloud.off.y + (deltaTime * cloud.vel.y - (camPos.y - prevY) * (1f - Parallax));
            if (y > halfH) y -= winH;
            else if (y < -halfH) y += winH;

            cloud.t.position = new Vector3(camPos.x + x, camPos.y + y, 0f);

            float alpha = Mathf.Clamp01(
                cloud.baseA + Mathf.Sin(time * 0.4f + cloud.phase) * cloud.pulse);
            Color color = cloud.sr.color;
            cloud.sr.color = new Color(color.r, color.g, color.b, alpha);

            cloud.off = new Vector2(x, y);
            clouds[index] = cloud;
        }
    }
}

}
