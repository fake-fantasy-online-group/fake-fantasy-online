using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 RollMarquee owner (tokens 06000C8B..06000C97): the whole owner, thirteen members.
/// </summary>
/// <remarks>
/// Recovered from the frozen arm64 + x86_64 decompilations of the official 1.5.8 client
/// (<c>Build/ReferenceReverseGhidra-1.5.8/queries/&lt;arch&gt;/&lt;probe&gt;/packet-RollMarquee/</c>
/// plus <c>packet-RollMarquee+Msg</c> and <c>packet-RollMarquee+&lt;&gt;c</c>).
///
/// Member tokens 06000C94..06000C97 belong to the two compiler-materialised nested types
/// (<c>RollMarquee+Msg</c> and <c>RollMarquee+&lt;&gt;c</c>) and carry no source-expressible
/// declaration; they are registered as evidenced exclusions. 06000C97 in particular is the body of
/// the cached <c>Comparison&lt;Msg&gt;</c> delegate that <see cref="Rebuild"/> builds, and its
/// behaviour is reproduced here as the inline sort lambda.
/// </remarks>
public class RollMarquee : MonoBehaviour
{
    public class Msg
    {
        public int bracket;   // +0x10
        public int idx;       // +0x14
        public int type;      // +0x18
        public string text;   // +0x20
    }

    public static RollMarquee I;
    private static List<Msg> all;
    private static readonly List<string> announceQ;
    private static float serverSpeed;
    private static float serverIntervalSec;
    private static bool serverEnabled;
    private static float serverPeriodMin;

    private readonly List<Msg> queue;
    private int qi;
    private float headX;
    private int curBracket;
    private bool showingAnnounce;
    private float pauseUntil;

    // Official 06000C92 (arm64 0x311c4c / 148 bytes, x86_64 0x324540 / 128 bytes):
    // queue = new List<Msg>() (allocated through the shared List`1 type metadata_usage_00932_kind1)
    // followed by the single int store 0xffffffff into curBracket (+0x30).
    public RollMarquee()
    {
        queue = new List<Msg>();
        curBracket = -1;
    }

    // Official 06000C93 (arm64 0x311ce0 / 184 bytes, x86_64 0x3245c0 / 176 bytes).
    // announceQ = new List<string>() (metadata_usage_00865_kind1), then one 8-byte store covering
    // serverSpeed (+0x18) and serverIntervalSec (+0x1c) from the literal at arm64 0x1127690
    // = 75f, 6f (x86_64 DAT_0111a900), then serverEnabled (+0x20) = 1 and serverPeriodMin (+0x24) = 0.
    static RollMarquee()
    {
        announceQ = new List<string>();
        serverSpeed = 75f;
        serverIntervalSec = 6f;
        serverEnabled = true;
        serverPeriodMin = 0f;
    }

    // Official 06000C8B (arm64 0x31095c / 344 bytes, x86_64 0x323330 / 304 bytes).
    // Parameter names come from the official managed-isil display:
    // SetSettings(System.Single speed, System.Single intervalSec, System.Int32 enabled,
    // System.Int32 periodMin). Ensure() runs unconditionally first; each field is only written
    // when its argument is a "supplied" value (speed > 0, everything else >= 0).
    public static void SetSettings(float speed, float intervalSec, int enabled, int periodMin)
    {
        Ensure();
        if (speed > 0f) serverSpeed = speed;
        if (intervalSec >= 0f) serverIntervalSec = intervalSec;
        if (enabled >= 0) serverEnabled = enabled != 0;
        if (periodMin >= 0) serverPeriodMin = periodMin;
    }

    // Official 06000C8C (arm64 0x310c5c / 996 bytes, x86_64 0x3235b0 / 976 bytes).
    // Official signature: SetServerPool(List<ValueTuple<int,string>> lines, float speed = 0,
    // float intervalSec = -1, int enabled = -1, int periodMin = -1).
    // Each tuple becomes a Msg with bracket = Item1, idx = 0, type = 1, text = Item2; the
    // idx/type pair is one 8-byte store from the literal at arm64 0x1127688 = 0, 1.
    public static void SetServerPool(List<ValueTuple<int, string>> lines, float speed = 0f,
                                     float intervalSec = -1f, int enabled = -1, int periodMin = -1)
    {
        Ensure();
        List<Msg> pool = new List<Msg>();
        if (lines != null)
        {
            foreach (ValueTuple<int, string> line in lines)
            {
                Msg m = new Msg();
                m.bracket = line.Item1;
                m.idx = 0;
                m.type = 1;
                m.text = line.Item2;
                pool.Add(m);
            }
        }
        all = pool;
        SetSettings(speed, intervalSec, enabled, periodMin);
        if (I != null)
        {
            I.curBracket = -1;
            I.pauseUntil = 0f;
        }
    }

    // Official 06000C8D (arm64 0x311048 / 244 bytes, x86_64 0x323990 / 224 bytes):
    // string.IsNullOrEmpty guard, Ensure(), then the inlined List<string>.Add fast path
    // (_size++ / _version++ / direct element store, falling back to the Add slow path on growth).
    public static void Announce(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Ensure();
        announceQ.Add(text);
    }

    // Official 06000C8E (arm64 0x310ab4 / 424 bytes, x86_64 0x323460 / 336 bytes).
    // The GameObject name is the string literal metadata_usage_13301_kind5 = "RollMarquee"; the
    // component is added through metadata_usage_07664_kind6 and the GameObject (not the component)
    // is passed to Object.DontDestroyOnLoad.
    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("RollMarquee");
        I = go.AddComponent<RollMarquee>();
        UnityEngine.Object.DontDestroyOnLoad(go);
    }

    // Official 06000C8F (arm64 0x31113c / 1024 bytes, x86_64 0x323a70 / 1040 bytes).
    public void Rebuild()
    {
        // The bracket index is CharData.level / 10 (metadata_usage_01441_kind1 + 0x20 is the public
        // static System.Int32 CharData.level), clamped at zero, capped at 9 and then
        // biased by one so that bracket 1 is the first bucket.
        int b = Mathf.Max(0, CharData.level / 10);
        if (b > 8) b = 9;
        b++;
        if (b == curBracket && queue.Count > 0) return;
        curBracket = b;
        queue.Clear();
        if (all != null)
        {
            foreach (Msg m in all)
            {
                if (m.bracket == b) queue.Add(m);
            }
        }
        // The Comparison<Msg> target is the cached <Rebuild>b__20_0 (token 06000C97):
        // a.idx.CompareTo(b.idx).
        queue.Sort((a, b2) => a.idx.CompareTo(b2.idx));
        qi = 0;
        headX = float.NaN;
    }

    // Official 06000C90 (arm64 0x31153c / 212 bytes, x86_64 0x323e80 / 192 bytes).
    // The "all != null" test is a real source-level guard: a null "all" skips Rebuild instead of
    // raising, which is why the emitted body branches around the call.
    public void Update()
    {
        if (all != null && all.Count > 0) Rebuild();
    }

    // Official 06000C91 (arm64 0x311610 / 1596 bytes, x86_64 0x323f40 / 1536 bytes).
    public void OnGUI()
    {
        if (!BitmapFont.Ready) return;
        if (announceQ.Count < 1)
        {
            if (!serverEnabled) return;
            if (queue.Count == 0) return;
            if (serverPeriodMin > 0f) return;
            if (Time.time < pauseUntil) return;
        }

        float k = UIScale.Factor;
        Rect band;
        if (GameHud.I != null)
        {
            band = GameHud.I.MarqueeBand();
        }
        else
        {
            float w = Mathf.Min((float)Screen.width * 0.42f, k * 420f);
            band = new Rect(((float)Screen.width - w) * 0.5f, k * 6f, w, k * 16f);
        }

        int n = announceQ.Count;
        if (showingAnnounce != (n > 0))
        {
            showingAnnounce = n > 0;
            headX = float.NaN;
        }

        string text;
        if (n > 0)
        {
            text = announceQ[0];
        }
        else
        {
            int c = queue.Count;
            if (c < 1) return;
            text = queue[qi % c].text;
        }
        if (text == null) return;

        float w2 = BitmapFont.Measure(text, k);
        // arm64 0x311978-0x311988: "and w8, w8, #0x7fffffff / cmp w8, #0x7f800001 / b.hs" is the
        // float.IsNaN test; a NaN head position restarts the scroll at the right edge of the band.
        if (float.IsNaN(headX)) headX = band.width;

        if (Event.current.type == EventType.Repaint)
        {
            headX -= k * serverSpeed * Time.deltaTime;
        }

        // Actual 1.6.0: arm64 0x321254/0x321258 and x86_64 0x3346ea/0x3346ed
        // complete only for an ordered <= comparison; unordered values keep drawing.
        if (!(headX <= -(k * 60f + w2)))
        {
            GUI.depth = 5;
            GUI.BeginGroup(band);
            BitmapFont.DrawOutline(headX, 0f, text, Color.white, k);
            GUI.EndGroup();
            return;
        }

        headX = float.NaN;
        if (n < 1)
        {
            int c = queue.Count;
            if (c > 0)
            {
                qi = (qi + 1) % c;
                if (serverIntervalSec > 0f) pauseUntil = Time.time + serverIntervalSec;
            }
            return;
        }
        announceQ.RemoveAt(0);
    }
}

}
