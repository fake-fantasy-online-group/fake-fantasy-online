using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 macOS window owner. The Windows build centres the client
// area, but on macOS those paths are compiled out, so Center and EarlyCenter
// are the original empty bodies and only the install path creates the hidden
// persistent driver object.
public static partial class WindowCenter
{
    // 06001086: original macOS body is empty (ARM 0x3d41f0 4 bytes; x86_64 0x3e8480 16 bytes).
    public static void Center()
    {
        // No client-area centering on this platform.
    }

    // 06001087: original macOS body is empty (ARM 0x3d41f4 4 bytes; x86_64 0x3e8490 16 bytes).
    public static void EarlyCenter()
    {
        // No early client-area centering on this platform.
    }

    // 06001088: create the hidden, persistent driver object and attach the driver.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Install()
    {
        var go = new GameObject("~WindowCenter");
        Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        go.AddComponent<WindowCenterDriver>();
    }
}

}
