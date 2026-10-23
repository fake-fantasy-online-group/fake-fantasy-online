using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official EffectPlayer template, keyframe parser and natural-cubic spline owner.
/// </summary>
public partial class EffectPlayer : MonoBehaviour
{
    private struct KF
    {
        public float t;
        public Vector3 pos;
        public Vector2 scale;
        public float rot;
        public Color rgb;
    }

    private class Layer
    {
        public string sprite;
        public int blend;
        public float speed;
        public float offX;
        public float offY;
        public List<KF> kfs;
        public float cField;
        public int blendMode;
        public Spline sx;
        public Spline sy;
        public Spline sz;

        public Layer()
        {
            speed = 100f;
            kfs = new List<KF>();
        }
    }

    private class Spline
    {
        private readonly float[] t;
        private readonly float[] y;
        private readonly float[] m;

        private Spline(float[] t, float[] y, float[] m)
        {
            this.t = t;
            this.y = y;
            this.m = m;
        }

        public static Spline Build(float[] t, float[] y)
        {
            int count = t.Length;
            float[] moments = new float[count];
            if (count >= 3)
            {
                float[] diagonal = new float[count];
                float[] upper = new float[count];
                float[] lower = new float[count];
                float[] right = new float[count];
                diagonal[0] = 1f;
                diagonal[count - 1] = 1f;

                for (int index = 1; index < count - 1; index++)
                {
                    float before = t[index] - t[index - 1];
                    float after = t[index + 1] - t[index];
                    lower[index] = before;
                    diagonal[index] = 2f * (before + after);
                    upper[index] = after;
                    right[index] = 6f *
                        ((y[index + 1] - y[index]) / Mathf.Max(after, 0.0001f) -
                         (y[index] - y[index - 1]) / Mathf.Max(before, 0.0001f));
                }

                for (int index = 1; index < count; index++)
                {
                    float factor = diagonal[index - 1] == 0f
                        ? 0f
                        : lower[index] / diagonal[index - 1];
                    diagonal[index] -= factor * upper[index - 1];
                    right[index] -= factor * right[index - 1];
                }

                moments[count - 1] = diagonal[count - 1] == 0f
                    ? 0f
                    : right[count - 1] / diagonal[count - 1];
                for (int index = count - 2; index >= 0; index--)
                {
                    moments[index] = diagonal[index] == 0f
                        ? 0f
                        : (right[index] - upper[index] * moments[index + 1]) /
                          diagonal[index];
                }
            }

            return new Spline(t, y, moments);
        }

        public float Eval(float tt)
        {
            int count = t.Length;
            if (count == 0)
            {
                return 0f;
            }

            if (tt <= t[0])
            {
                return y[0];
            }

            if (tt >= t[count - 1])
            {
                return y[count - 1];
            }

            int index = 0;
            while (index < count - 2 && t[index + 1] < tt)
            {
                index++;
            }

            float width = t[index + 1] - t[index];
            if (width <= 0f)
            {
                return y[index];
            }

            float left = (t[index + 1] - tt) / width;
            float right = (tt - t[index]) / width;
            return left * y[index] + right * y[index + 1] +
                ((left * left * left - left) * m[index] +
                 (right * right * right - right) * m[index + 1]) * width * width / 6f;
        }
    }

    private class Template
    {
        public List<Layer> layers;
        public float lifeMs;
        public int loopFromTail;

        public Template()
        {
            layers = new List<Layer>();
        }
    }

    private static readonly Dictionary<string, Template> tplCache;
    private static readonly Dictionary<string, Sprite[]> spriteCache;
    private static readonly Dictionary<string, int> attribCache;
    private static readonly Dictionary<string, int> dirsCache;
    private static readonly Dictionary<string, bool> darkMapCache;
    private static Material alphaMat;
    private static Material addMat;
    private static Material maxMat;
    private Template tpl;
    private float ppu;
    private float t;
    private SpriteRenderer[] rends;
    private bool _dbgOnce;
    private Sprite[][] frames;
    private float[] frameDur;
    private bool[] layerMultiDir;
    public bool followFacing;
    public int facingDir;
    public bool facingFlip;
    private Sprite[][][] dirFrames;
    private MaterialPropertyBlock mpb;
    private Vector3 spawnPos;
    private Transform moveTarget;
    private float moveDur;
    private float moveT;
    private Vector3? moveTargetPos;
    public Transform followTarget;
    public SpriteRenderer attachSort;
    public bool attachUnder;
    private static readonly Dictionary<string, EffectPlayer> livePulse;
    private static readonly int[] tfxSuffix;
    private static readonly Stack<EffectPlayer> _pool;
    private readonly List<SpriteRenderer> _layerObjs;
    private bool _live;
    public static float NextScale;
    private const int PlaneOver = 20;
    private const int PlaneUnder = -10;
    private static readonly HashSet<string> PlaneOverAllow;
    public int loopMode;
    public float loopDelay;
    public bool selfDestroy;
    public int facingSign;
    public static string LastGlowDiag;
    public static readonly string[] GlowModeName;
    private static readonly Color[] GlowTint;
    private static readonly Material[] glowMats;
    private static readonly Material[] glowMatsPre;
    private static Material npcGlowMat;
    private static Material layerAddMat;
    private static Material zeroMat;
    private static readonly Dictionary<string, string> EffFileSwap;

    public EffectPlayer()
    {
        facingDir = -1;
        _layerObjs = new List<SpriteRenderer>();
        selfDestroy = true;
        facingSign = 1;
    }

    static EffectPlayer()
    {
        tplCache = new Dictionary<string, Template>();
        spriteCache = new Dictionary<string, Sprite[]>();
        _fxBudget = new ArtBudget<string>("\u7279\u6548\u5e27", FxBudgetBase, 1000000000f);
        _fxPinned = new HashSet<string>();
        attribCache = new Dictionary<string, int>();
        dirsCache = new Dictionary<string, int>();
        darkMapCache = new Dictionary<string, bool>();
        livePulse = new Dictionary<string, EffectPlayer>();
        tfxSuffix = new[] { 30, 40, 20, 10, 50, 60 };
        _pool = new Stack<EffectPlayer>();
        NextScale = 1f;
        PlaneOverAllow = new HashSet<string>
        {
            "magic8085",
            "magic8087"
        };
        PrewarmList = new[] { "升级特效" };
        LastGlowDiag = string.Empty;
        GlowModeName = new[]
        {
            "普通",
            "加色",
            "深色紫",
            "红焰",
            "橙焰",
            "金焰",
            "绿焰",
            "青焰",
            "蓝焰",
            "白焰",
            "红焰·炽",
            "橙焰·炽",
            "金焰·炽",
            "绿焰·炽",
            "青焰·炽",
            "蓝焰·炽",
            "白焰·炽"
        };
        GlowTint = new[]
        {
            new Color(1f, 0.22f, 0.26f, 1f),
            new Color(1f, 0.52f, 0.14f, 1f),
            new Color(1f, 0.84f, 0.22f, 1f),
            new Color(0.32f, 1f, 0.38f, 1f),
            new Color(0.26f, 0.94f, 1f, 1f),
            new Color(0.36f, 0.52f, 1f, 1f),
            Color.white
        };
        glowMats = new Material[17];
        glowMatsPre = new Material[17];
        EffFileSwap = new Dictionary<string, string>
        {
            { "补血", "补魔法" },
            { "补魔法", "补血" }
        };
        ArtDetail.OnBudgetScaleChanged += ApplyFxBudgetScale;
        ApplyFxBudgetScale();
    }

    private static Template LoadTemplate(string effName, bool quiet = false)
    {
        Template template;
        if (tplCache.TryGetValue(effName, out template))
        {
            return template;
        }

        LegendaryEffectAlias alias;
        if (LegendaryEffectAliases.TryGetValue(effName, out alias))
        {
            // Resolve through the same original EFF loader and package precedence.
            // Do not read a stale LP overlay or mutate its cached source template.
            template = SelectLegendaryLayers(LoadTemplate(alias.OriginalEffect, quiet),
                alias.SelectedMagicId);
            tplCache[effName] = template;
            return template;
        }

        string swapped;
        string resolvedName = EffFileSwap.TryGetValue(effName, out swapped)
            ? swapped
            : effName;
        byte[] bytes = ReadRes("effect/" + resolvedName + ".eff");
        template = bytes == null
            ? null
            : Parse(Encoding.GetEncoding("ISO-8859-1").GetString(bytes));
        tplCache[effName] = template;
        return template;
    }

    private static byte[] ReadRes(string path)
    {
        if (ResLoader.I == null)
        {
            return null;
        }

        byte[] patch = ResLoader.I.LoadRespatch(path);
        if (patch != null)
        {
            return patch;
        }

        // Official 1.6.0 checks the overlay before the existing effect package order.
        // A null path still reaches the original Contains failure after an overlay miss.
        if (path.Contains("聚气") && ResLoader.I.Pkg != null)
        {
            byte[] focused = ResLoader.I.Pkg.Read(path);
            if (focused != null)
            {
                return focused;
            }
        }

        if (SkillEffectDB.Use05Fx)
        {
            if (ResLoader.I.Pkg != null)
            {
                byte[] current = ResLoader.I.Pkg.Read(path);
                if (current != null)
                {
                    return current;
                }
            }

            if (ResLoader.I.Pkg24 != null)
            {
                return ResLoader.I.Pkg24.Read(path);
            }
        }
        else
        {
            return ResLoader.I.ReadRes24First(path);
        }

        return null;
    }

    private static EffectPlayer Rent(string effName)
    {
        EffectPlayer player = null;
        while (_pool.Count > 0)
        {
            player = _pool.Pop();
            if (player)
            {
                break;
            }
        }

        if (player)
        {
            player.gameObject.SetActive(true);
        }
        else
        {
            GameObject owner = new GameObject("Eff");
            player = owner.AddComponent<EffectPlayer>();
        }

        player.gameObject.name = "Eff:" + effName;
        player.ResetState();
        player._live = true;
        return player;
    }

    private void ResetState()
    {
        t = 0f;
        tpl = null;
        frames = null;
        frameDur = null;
        layerMultiDir = null;
        dirFrames = null;
        followFacing = false;
        facingDir = -1;
        facingFlip = false;
        facingSign = 1;
        moveTarget = null;
        moveDur = 0f;
        moveT = 0f;
        moveTargetPos = null;
        followTarget = null;
        attachSort = null;
        attachUnder = false;
        loopMode = 0;
        loopDelay = 0f;
        selfDestroy = true;
        _dbgOnce = false;
        for (int index = 0; index < _layerObjs.Count; index++)
        {
            SpriteRenderer layer = _layerObjs[index];
            if (layer)
            {
                layer.gameObject.SetActive(false);
                layer.sprite = null;
            }
        }
    }

    private SpriteRenderer NextLayer(int i)
    {
        SpriteRenderer layer;
        if (i < _layerObjs.Count)
        {
            layer = _layerObjs[i];
            layer.gameObject.SetActive(true);
        }
        else
        {
            GameObject owner = new GameObject("L");
            owner.transform.SetParent(transform, false);
            layer = owner.AddComponent<SpriteRenderer>();
            _layerObjs.Add(layer);
        }

        layer.transform.localPosition = Vector3.zero;
        layer.transform.localScale = Vector3.one;
        layer.transform.localRotation = Quaternion.identity;
        layer.enabled = true;
        layer.flipX = false;
        layer.color = Color.white;
        return layer;
    }

    private void ReturnToPool()
    {
        if (!_live)
        {
            return;
        }

        _live = false;
        transform.localScale = Vector3.one;
        StopAllCoroutines();
        transform.SetParent(null, false);
        for (int index = 0; index < _layerObjs.Count; index++)
        {
            SpriteRenderer layer = _layerObjs[index];
            if (layer)
            {
                layer.gameObject.SetActive(false);
            }
        }

        gameObject.SetActive(false);
        _pool.Push(this);
    }

    public void MoveTo(Transform target, float durMs)
    {
        moveTarget = target;
        moveDur = durMs;
        moveT = 0f;
        spawnPos = transform.position;
    }

    public void MoveToPos(Vector3 pos, float durMs)
    {
        moveTargetPos = pos;
        moveDur = durMs;
        moveT = 0f;
        spawnPos = transform.position;
    }

    public void Stop()
    {
        Destroy(gameObject);
    }

    public void SettleToEnd()
    {
        if (tpl != null && tpl.lifeMs > 0f)
        {
            t = tpl.lifeMs;
        }
    }

    public void SetBehind(bool behind, int baseOrder = int.MinValue)
    {
        if (rends == null)
        {
            return;
        }

        int order = behind ? -200 : 31000;
        if (baseOrder != int.MinValue)
        {
            order = behind ? baseOrder - 2 : baseOrder + 20;
        }

        for (int index = 0; index < rends.Length; index++)
        {
            if (rends[index])
            {
                rends[index].sortingOrder = order + index;
            }
        }
    }

    private int LayerPlaneOffset(Layer ly)
    {
        int plane = (int)Math.Round(ly.cField);
        if (plane < 0)
        {
            return PlaneUnder;
        }

        bool over = !attachUnder;
        if (plane != 0 && ly.sprite != null && PlaneOverAllow.Contains(ly.sprite))
        {
            over = true;
        }

        return over ? PlaneOver : PlaneUnder;
    }

    private KF Sample(Layer ly, float ms)
    {
        List<KF> keyframes = ly.kfs;
        if (ms <= keyframes[0].t)
        {
            return keyframes[0];
        }

        int last = keyframes.Count - 1;
        if (ms >= keyframes[last].t || last < 1)
        {
            return keyframes[last];
        }

        for (int index = 0; index < last; index++)
        {
            KF before = keyframes[index];
            KF after = keyframes[index + 1];
            if (before.t <= ms && ms <= after.t)
            {
                float width = after.t - before.t;
                float ratio = width > 0.01f
                    ? (ms - before.t) / width
                    : 0f;
                return new KF
                {
                    pos = Vector3.Lerp(before.pos, after.pos, ratio),
                    scale = Vector2.Lerp(before.scale, after.scale, ratio),
                    rot = Mathf.Lerp(before.rot, after.rot, ratio),
                    rgb = Color.Lerp(before.rgb, after.rgb, ratio)
                };
            }
        }

        return keyframes[last];
    }

    private static Template Parse(string text)
    {
        int position = 0;
        List<object> root = ParseVal(text, ref position) as List<object>;
        if (root == null || root.Count < 2)
        {
            return null;
        }

        List<object> sourceLayers = root[1] as List<object>;
        if (sourceLayers == null)
        {
            return null;
        }

        Template template = new Template();
        if (root.Count > 2)
        {
            List<object> loop = root[2] as List<object>;
            if (loop != null && loop.Count > 0)
            {
                template.loopFromTail = (int)ToNum(loop[0]);
            }
        }

        foreach (object sourceLayerValue in sourceLayers)
        {
            List<object> sourceLayer = sourceLayerValue as List<object>;
            if (sourceLayer == null || sourceLayer.Count < 2)
            {
                continue;
            }

            List<object> header = sourceLayer[0] as List<object>;
            List<object> sourceKeyframes = sourceLayer[1] as List<object>;
            if (header == null || sourceKeyframes == null)
            {
                continue;
            }

            Layer layer = new Layer();
            if (header.Count > 1)
            {
                List<object> sprite = header[1] as List<object>;
                if (sprite != null && sprite.Count > 0)
                {
                    layer.sprite = sprite[0] as string;
                    if (sprite.Count > 1)
                    {
                        layer.blend = (int)ToNum(sprite[1]);
                    }

                    if (sprite.Count > 2)
                    {
                        layer.speed = (float)ToNum(sprite[2]);
                    }

                    if (sprite.Count > 3)
                    {
                        layer.cField = (float)ToNum(sprite[3]);
                    }
                }
            }

            if (header.Count > 2)
            {
                List<object> blend = header[2] as List<object>;
                if (blend != null && blend.Count > 0)
                {
                    layer.blendMode = (int)ToNum(blend[0]);
                }
            }

            if (header.Count > 5)
            {
                List<object> offset = header[header.Count - 1] as List<object>;
                if (offset != null && offset.Count > 1)
                {
                    layer.offX = (float)ToNum(offset[0]);
                    layer.offY = (float)ToNum(offset[1]);
                }
            }

            foreach (object sourceKeyframeValue in sourceKeyframes)
            {
                List<object> sourceKeyframe = sourceKeyframeValue as List<object>;
                if (sourceKeyframe == null || sourceKeyframe.Count < 2)
                {
                    continue;
                }

                float time = (float)ToNum(sourceKeyframe[0]);
                List<object> values = sourceKeyframe[1] as List<object>;
                if (values == null || values.Count < 5)
                {
                    continue;
                }

                List<object> positionValues = values[0] as List<object>;
                List<object> scaleValues = values[1] as List<object>;
                List<object> colorValues = values[3] as List<object>;

                Vector3 framePosition = Vector3.zero;
                if (positionValues != null && positionValues.Count > 2)
                {
                    framePosition = new Vector3(
                        (float)ToNum(positionValues[0]),
                        (float)ToNum(positionValues[1]),
                        (float)ToNum(positionValues[2]));
                }

                // Official 1.5.8 leaves the scale at zero when the keyframe carries no
                // scale pair: Parse (0x060002d0) writes KF.scale from values[1] only when
                // its count is greater than one and every other path keeps 0f
                // (arm64 fVar30/fVar22, x86_64 local_6c/local_80).
                Vector2 frameScale = Vector2.zero;
                if (scaleValues != null && scaleValues.Count > 1)
                {
                    frameScale = new Vector2(
                        (float)ToNum(scaleValues[0]),
                        (float)ToNum(scaleValues[1]));
                }

                Color frameColor = Color.white;
                if (colorValues != null && colorValues.Count > 2)
                {
                    frameColor = new Color(
                        (float)ToNum(colorValues[0]) / 255f,
                        (float)ToNum(colorValues[1]) / 255f,
                        (float)ToNum(colorValues[2]) / 255f,
                        1f);
                }

                frameColor.a = (float)ToNum(values[4]);
                layer.kfs.Add(new KF
                {
                    t = time,
                    pos = framePosition,
                    scale = frameScale,
                    rot = (float)ToNum(values[2]),
                    rgb = frameColor,
                });
                if (template.lifeMs < time)
                {
                    template.lifeMs = time;
                }
            }

            if (layer.kfs.Count > 2)
            {
                int count = layer.kfs.Count;
                float[] times = new float[count];
                float[] xs = new float[count];
                float[] ys = new float[count];
                float[] zs = new float[count];
                for (int index = 0; index < count; index++)
                {
                    KF keyframe = layer.kfs[index];
                    times[index] = keyframe.t;
                    xs[index] = keyframe.pos.x;
                    ys[index] = keyframe.pos.y;
                    zs[index] = keyframe.pos.z;
                }

                layer.sx = Spline.Build(times, xs);
                layer.sy = Spline.Build(times, ys);
                layer.sz = Spline.Build(times, zs);
            }

            if (layer.kfs.Count > 0 && !string.IsNullOrEmpty(layer.sprite))
            {
                template.layers.Add(layer);
            }
        }

        template.lifeMs += 100f;
        return template;
    }

    private static double ToNum(object value)
    {
        return value is double ? (double)value : 0d;
    }

    private static object ParseVal(string text, ref int position)
    {
        SkipWs(text, ref position);
        char current = text[position];
        if (current == '\'' || current == '"')
        {
            char quote = current;
            int start = ++position;
            while (position < text.Length && text[position] != quote)
            {
                position++;
            }

            string value = text.Substring(start, position - start);
            position++;
            return value;
        }

        if (current == '[' || current == '(')
        {
            char close = current == '(' ? ')' : ']';
            position++;
            List<object> values = new List<object>();
            while (true)
            {
                SkipWs(text, ref position);
                if (position >= text.Length)
                {
                    return values;
                }

                current = text[position];
                if (current == close)
                {
                    position++;
                    return values;
                }

                if (current == ',')
                {
                    position++;
                }
                else
                {
                    values.Add(ParseVal(text, ref position));
                }
            }
        }

        int numberStart = position;
        while (position < text.Length)
        {
            current = text[position];
            if (!char.IsDigit(current) && current != '-' && current != '+' &&
                current != '.' && current != 'e' && current != 'E')
            {
                break;
            }

            position++;
        }

        double number;
        double.TryParse(
            text.Substring(numberStart, position - numberStart),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out number);
        return number;
    }

    private static void SkipWs(string text, ref int position)
    {
        while (position < text.Length)
        {
            char current = text[position];
            if (current != ' ' && current != '\t' && current != '\r' && current != '\n')
            {
                return;
            }

            position++;
        }
    }
}

}
