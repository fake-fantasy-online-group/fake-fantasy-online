using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed class PluginNativeWriteObservation
{
    internal readonly long Sequence;
    internal readonly Task<Result> Pending;
    internal PluginNativeWriteObservation(long sequence, Task<Result> pending)
    { Sequence = sequence; Pending = pending; }
}

internal partial class NetClient
{
    private long pluginWriteSequence;
    private readonly Dictionary<string, PluginNativeWriteObservation> pluginWrites =
        new Dictionary<string, PluginNativeWriteObservation>(StringComparer.Ordinal);
    private readonly Queue<string> pluginWriteKeys = new Queue<string>();

    internal long PluginWriteSequence => pluginWriteSequence;

    // Records the real gateway task without changing or awaiting the native writer.
    // This is intentionally a bounded observation surface, not an alternative sender.
    internal Task<Result> ObserveWriteForPlugins(string kind, int id, Task<Result> pending)
    {
        string key = kind + ":" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!pluginWrites.ContainsKey(key)) pluginWriteKeys.Enqueue(key);
        pluginWrites[key] = new PluginNativeWriteObservation(++pluginWriteSequence, pending);
        while (pluginWriteKeys.Count > 128) pluginWrites.Remove(pluginWriteKeys.Dequeue());
        return pending;
    }

    internal PluginNativeWriteObservation ReadWriteForPlugins(string kind, int id)
    {
        PluginNativeWriteObservation value;
        pluginWrites.TryGetValue(kind + ":" + id.ToString(System.Globalization.CultureInfo.InvariantCulture), out value);
        return value;
    }
}
}
