using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class ChatUI
{
    // Official static slot 0: the atlas fields table lists ChatUI.I (ordinal 3) as the first
    // static that owns storage - the three float slots ahead of it are literals - so the store
    // Awake performs into static_fields[0] is exactly this field. ChatUI.Awake (0x060000e8) is
    // its only official writer; UIManager.Update (0x06000fa8) reads it for both panel toggles
    // (arm64 0x3a7938 before the Alt+B slice and again at 0x3a7990 for the V slice).
    public static ChatUI I;

    // Official instance slots from the same fields table: faceOpen at offset 107 (0x6b) and
    // chanOpen at offset 108 (0x6c). ChatUI..ctor (0x060000fb) writes 0x69/0x70/0x78/0x80/0x88
    // only, so neither slot is touched at construction and both start false on both slices.
    private bool faceOpen;
    private bool chanOpen;

    // Official 1.6.0 instance +0x90, read and cleared by ToggleChan.
    private bool filterOpen;

    // Official static slot 88 (0x58): _pendingFocus. ChatUI.StartWhisper (0x060000e5) sets it
    // and ChatUI.OnGUI (0x060000f6, arm64 0x236258 / x86_64 0x243fb3) reads it, calls
    // GUI.FocusControl("chatinput") and clears it in the same frame, so it is one flag per
    // mounted owner - the same lifetime as ChatUI.I, not one flag per drawn chat bar.
    private static bool _pendingFocus;

    // 0x060000e8 ChatUI.Awake (arm64 rva=0x23204c / x86_64 rva=0x23fbd0, emittedExtent 108/96,
    // queries/WBD-037/{arm64,x86_64}-060000e8). Both slices are one guarded store:
    //   puVar1 = *(undefined8 **)(metadata_usage_01446_kind1 + 0xb8);  // ChatUI static_fields
    //   *puVar1 = param_1;                                             // static_fields[0] = this
    //   write_barrier(puVar1, param_1);
    // The only branches are the cctor latch (DAT_0151036a / DAT_0150488c) and the class-init
    // check at metadata +0xe4, both emitted by IL2CPP rather than by the method body. No
    // resource load, no second assignment, no return value.
    private void Awake()
    {
        I = this;
    }

    // Thin accessors onto the two official slots above. They hold no state of their own: the
    // drawn chat bar reads and writes the same faceOpen / chanOpen bytes the two official
    // toggles below flip, so there is only ever one face-panel flag and one channel-list flag.
    internal bool FaceOpenForIndependentRuntime { get => faceOpen; set => faceOpen = value; }
    internal bool ChanOpenForIndependentRuntime { get => chanOpen; set => chanOpen = value; }

    // Thin accessors onto the three slots above, in the same shape as the two flags: they hold
    // no state of their own, so the whisper target the drawn bar shows and the one StartWhisper
    // writes are one and the same slot.
    internal bool PanelsVisibleForIndependentRuntime
    {
        get => panelsVisible;
        set => panelsVisible = value;
    }

    internal string WhisperTargetForIndependentRuntime
    {
        get => whisperTarget;
        set => whisperTarget = value;
    }

    internal static bool PendingFocusForIndependentRuntime
    {
        get => _pendingFocus;
        set => _pendingFocus = value;
    }

    // Resource borrowing for the single manual draw driver. These expose the original field
    // and array identities without a second loader, cache, success flag or readiness policy.
    internal Texture2D InputBarForIndependentRuntime => inputBar;
    internal Texture2D WindowUpForIndependentRuntime => winUp;
    internal Texture2D WindowMiddleForIndependentRuntime => winMid;
    internal Texture2D WindowDownForIndependentRuntime => winDown;
    internal Texture2D[] DropButtonForIndependentRuntime => dropBtn;
    internal Texture2D[] SendButtonForIndependentRuntime => sendBtn;
    internal Texture2D[] FaceButtonForIndependentRuntime => faceBtn;
    internal Texture2D[] SizeButtonForIndependentRuntime => sizeBtn;
    internal Texture2D[][] SideButtonFramesForIndependentRuntime => sideBtn;
    internal static (string Name, float Offset, string Tooltip)[] SideButtonsForIndependentRuntime => SideBtns;

    private float s;
    internal float DrawScaleForIndependentRuntime { get => s; set => s = value; }

    internal void LoadResourcesForIndependentRuntime()
    {
        if (!loaded) Load();
    }

    // 0x060000e0 ChatUI.ToggleFacePanel (arm64 0x231b68 / x86_64 0x23f780, 16 bytes on both
    // slices, queries/WBD-034/{arm64,x86_64}-060000e0). The whole body is one byte flip:
    //   *(byte *)(param_1 + 0x6b) = *(byte *)(param_1 + 0x6b) ^ 1;
    // On a 0/1 boolean slot that is the same transition as faceOpen = !faceOpen. No guard, no
    // resource read, no return value, and no second face-panel state.
    public void ToggleFacePanel()
    {
        faceOpen = !faceOpen;
    }

    // Official 1.6.0 060000e3, ARM64 0x231f84..0x231f9c: the public V-key
    // entry retains the same transition as the separate private helper below.
    public void ToggleChannelList()
    {
        ToggleChan();
    }

    // Official 1.6.0 060000e2, ARM64 0x231f68..0x231f80. Opening the channel
    // list also closes the filter panel; closing the channel list leaves it alone.
    private void ToggleChan()
    {
        chanOpen = !chanOpen;
        if (chanOpen) filterOpen = false;
    }
}

}
