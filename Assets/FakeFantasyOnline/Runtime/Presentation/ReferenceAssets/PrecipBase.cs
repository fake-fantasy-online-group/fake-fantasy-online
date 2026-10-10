using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public abstract class PrecipBase : MonoBehaviour
{
    protected struct P
    {
        public float wx;
        public float wy;
        public float factor;
        public int life;
        public int fallDur;
        public int age;
        public int sprite;
        public bool alive;
    }

    protected P[] ps;
    protected int gravity;
    protected int spawnRate;
    protected int fallBase;
    protected int restFade;
    protected int spriteCount = 1;
    private const float GS = .001f;
    private float spawnAccum;
    protected Camera cam;

    protected void Alloc(int capacity)
    {
        ps = new P[capacity];
    }

    private void EnsureCam()
    {
        if (cam == null)
            cam = Camera.main;
    }

    private void ViewRect(out float cx, out float cy, out float halfW, out float halfH)
    {
        cx = cam.transform.position.x;
        cy = cam.transform.position.y;
        halfH = cam.orthographicSize;
        halfW = halfH * cam.aspect;
    }

    private void SpawnSome(int n)
    {
        ViewRect(out float cx, out float cy, out float halfW, out float halfH);
        for (int i = 0; i < ps.Length && n > 0; i++)
        {
            if (ps[i].alive)
                continue;
            ps[i].alive = true;
            ps[i].wx = cx + Random.Range(-halfW, halfW);
            ps[i].wy = cy + halfH + Random.value * (halfH * .3f);
            ps[i].factor = Random.value * .3f + .7f;
            ps[i].fallDur = Random.Range(0, fallBase) + fallBase / 2;
            ps[i].sprite = spriteCount > 1 ? Random.Range(0, spriteCount) : 0;
            ps[i].life = 0;
            ps[i].age = 0;
            n--;
        }
    }

    private void Update()
    {
        if (ps == null)
            return;
        EnsureCam();
        if (cam == null)
            return;

        float dtMs = Time.deltaTime * 1000f;
        spawnAccum += dtMs;
        int count = (int)(spawnAccum * spawnRate / 1000f);
        if (count > 0)
        {
            spawnAccum = 0f;
            SpawnSome(Mathf.Min(count, 5));
        }
        int elapsed = (int)dtMs;
        ViewRect(out float cx, out float cy, out float halfW, out float halfH);
        float speed = gravity / WeatherFX.Ppu;
        float bottom = cy - halfH - 1f;
        for (int i = 0; i < ps.Length; i++)
        {
            if (!ps[i].alive)
                continue;
            ps[i].life += elapsed;
            if (ps[i].life < ps[i].fallDur)
            {
                ps[i].wy -= speed * ps[i].factor * Time.deltaTime;
                ps[i].age += elapsed;
            }
            else if (ps[i].life >= ps[i].fallDur + restFade)
            {
                ps[i].alive = false;
            }
            if (ps[i].wy < bottom)
                ps[i].alive = false;
        }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || ps == null || cam == null)
            return;
        for (int i = 0; i < ps.Length; i++)
        {
            if (!ps[i].alive)
                continue;
            Vector3 screen = cam.WorldToScreenPoint(new Vector3(ps[i].wx, ps[i].wy, 0f));
            if (screen.z <= 0f)
                continue;
            P p = ps[i];
            Draw(p, screen.x, Screen.height - screen.y);
        }
    }

    protected abstract void Draw(P p, float sx, float sy);

    protected float AlphaOf(P p)
    {
        int rest = p.life - p.fallDur;
        if (rest < 0 || restFade <= 0)
            return 1f;
        return Mathf.Clamp01(1f - (float)rest / restFade);
    }
}
}
