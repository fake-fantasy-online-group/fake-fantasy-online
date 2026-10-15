using System;
using FakeFantasyOnline.Presentation;
using UnityEngine;

namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Optional composition hook. With no installed framework this is a no-op.</summary>
public static class OptionalPluginRuntime
{
    private static Func<GameObject, ClientRootView, IDisposable> factory;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { factory = null; }

    /// <summary>Register a factory before scene loading; registration must not start a runtime.</summary>
    public static void Register(Func<GameObject, ClientRootView, IDisposable> create)
    {
        if (create == null) throw new ArgumentNullException(nameof(create));
        if (factory != null) throw new InvalidOperationException("A plugin framework is already registered.");
        factory = create;
    }

    public static IDisposable Create(GameObject owner, ClientRootView view)
    {
        if (owner == null) throw new ArgumentNullException(nameof(owner));
        if (view == null) throw new ArgumentNullException(nameof(view));
        return factory?.Invoke(owner, view);
    }
}
}
