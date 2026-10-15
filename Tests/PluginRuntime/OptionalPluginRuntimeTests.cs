using System;
using System.Reflection;
using FakeFantasyOnline.Bootstrap;
using FakeFantasyOnline.Presentation;
using UnityEngine;

namespace UnityEngine
{
    public sealed class GameObject { }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value) { } }
}
namespace FakeFantasyOnline.Presentation { public sealed class ClientRootView { } }
internal static class OptionalPluginRuntimeTests
{
    private sealed class Runtime : IDisposable { public int Disposals; public void Dispose() { Disposals++; } }
    private static int assertions;
    private static void Check(bool condition, string label) { assertions++; if (!condition) throw new Exception(label); }
    public static int Main()
    {
        var owner = new GameObject(); var view = new ClientRootView();
        Check(OptionalPluginRuntime.Create(owner, view) == null, "absent framework is no-op");
        int creates = 0; var runtime = new Runtime();
        OptionalPluginRuntime.Register((actualOwner, actualView) => {
            Check(ReferenceEquals(owner, actualOwner) && ReferenceEquals(view, actualView), "existing native owners passed unchanged");
            creates++; return runtime;
        });
        Check(creates == 0, "registration has no runtime side effects");
        Check(ReferenceEquals(OptionalPluginRuntime.Create(owner, view), runtime) && creates == 1, "creation returns framework lifetime to client");
        runtime.Dispose(); Check(runtime.Disposals == 1, "client can dispose returned framework lifetime");
        bool duplicate = false;
        try { OptionalPluginRuntime.Register((o, v) => null); } catch (InvalidOperationException) { duplicate = true; }
        Check(duplicate, "duplicate framework fails closed");
        // Simulate Unity's subsystem reset hook when domain reload is disabled.
        typeof(OptionalPluginRuntime).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Check(OptionalPluginRuntime.Create(owner, view) == null, "new play session clears stale factory");
        bool missingOwner = false; try { OptionalPluginRuntime.Create(null, view); } catch (ArgumentNullException) { missingOwner = true; }
        Check(missingOwner, "missing owner rejected");
        bool missingView = false; try { OptionalPluginRuntime.Create(owner, null); } catch (ArgumentNullException) { missingView = true; }
        Check(missingView, "missing view rejected");
        OptionalPluginRuntime.Register((o, v) => { throw new InvalidOperationException("startup failure"); });
        bool propagated = false; try { OptionalPluginRuntime.Create(owner, view); } catch (InvalidOperationException) { propagated = true; }
        Check(propagated, "framework initialization failure remains visible");
        Console.WriteLine("PASS " + assertions + " optional framework hook assertions (Unity stubs, package absent).");
        return 0;
    }
}
