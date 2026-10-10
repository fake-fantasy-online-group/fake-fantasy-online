using System.Collections.Generic;
using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered constructor and state owner for official 1.3.4 ArtBudget&lt;TKey&gt;.
/// </summary>
/// <remarks>
/// Complete official state/accounting owner used by ResLoader, including safe-point eviction and
/// diagnostics. ARM generic bodies and x86 concrete shared instantiations are compared separately.
/// </remarks>
public partial class ArtBudget<TKey>
{
    private struct Entry
    {
        public long bytes;
        public float lastUse;
    }

    private readonly Dictionary<TKey, Entry> _e = new Dictionary<TKey, Entry>();
    private readonly List<KeyValuePair<TKey, Entry>> _buf =
        new List<KeyValuePair<TKey, Entry>>();
    private readonly string _name;

    public long BudgetBytes;
    public float IdleEvictSec;

    public long Bytes { get; private set; }

    public long PeakBytes { get; private set; }

    public int EvictedCount { get; private set; }

    public long EvictedBytes { get; private set; }

    private static float Now => Time.realtimeSinceStartup;

    public bool OverBudget => Bytes > BudgetBytes;

    public int PinnedLastRun { get; private set; }

    public ArtBudget(string name, long budgetBytes, float idleEvictSec = 300f)
    {
        _name = name;
        BudgetBytes = budgetBytes;
        IdleEvictSec = idleEvictSec;
    }

    public void Note(TKey k, long bytes)
    {
        Entry value;
        if (_e.TryGetValue(k, out value))
        {
            Bytes -= value.bytes;
        }

        value.bytes = bytes;
        value.lastUse = Now;
        _e[k] = value;
        Bytes += bytes;
        if (Bytes > PeakBytes)
        {
            PeakBytes = Bytes;
        }
    }

    public void Touch(TKey k)
    {
        Entry value;
        if (_e.TryGetValue(k, out value))
        {
            value.lastUse = Now;
            _e[k] = value;
        }
    }

    public void UpdateBytes(TKey k, long bytes)
    {
        Entry value;
        if (!_e.TryGetValue(k, out value))
        {
            return;
        }

        Bytes += bytes - value.bytes;
        value.bytes = bytes;
        _e[k] = value;
        if (Bytes > PeakBytes)
        {
            PeakBytes = Bytes;
        }
    }

    public void Forget(TKey k)
    {
        Entry value;
        if (_e.TryGetValue(k, out value))
        {
            Bytes -= value.bytes;
            _e.Remove(k);
        }
    }

    public void Reset()
    {
        _e.Clear();
        Bytes = 0L;
    }

    public int EvictAtSafePoint(Action<TKey> drop, Func<TKey, bool> canEvict = null)
    {
        PinnedLastRun = 0;
        if (drop == null)
        {
            return 0;
        }

        float now = Now;
        _buf.Clear();
        foreach (KeyValuePair<TKey, Entry> pair in _e)
        {
            float idle = now - pair.Value.lastUse;
            if (OverBudget || idle > IdleEvictSec)
            {
                _buf.Add(pair);
            }
        }

        if (_buf.Count == 0)
        {
            return 0;
        }

        _buf.Sort((left, right) => left.Value.lastUse.CompareTo(right.Value.lastUse));
        int evicted = 0;
        foreach (KeyValuePair<TKey, Entry> pair in _buf)
        {
            float idle = now - pair.Value.lastUse;
            if (!OverBudget && !(idle > IdleEvictSec))
            {
                break;
            }

            if (canEvict != null && !canEvict(pair.Key))
            {
                PinnedLastRun++;
                continue;
            }

            drop(pair.Key);
            Entry current;
            if (_e.TryGetValue(pair.Key, out current))
            {
                Bytes -= current.bytes;
                _e.Remove(pair.Key);
            }

            EvictedCount++;
            evicted++;
            EvictedBytes += pair.Value.bytes;
        }

        return evicted;
    }

    public string Stat()
    {
        const float ToMegabytes = 1f / (1024f * 1024f);
        string text = string.Format(
            "{0} {1}条/{2:F1}MB(预算{3:F0} 峰值{4:F1}",
            _name,
            _e.Count,
            Bytes * ToMegabytes,
            BudgetBytes * ToMegabytes,
            PeakBytes * ToMegabytes);
        string evicted = EvictedCount > 0
            ? string.Format(" 已淘汰{0}条/{1:F1}MB", EvictedCount,
                EvictedBytes * ToMegabytes)
            : string.Empty;
        return text + evicted + ")" + (OverBudget ? " ⚠超预算" : string.Empty);
    }
}

}
