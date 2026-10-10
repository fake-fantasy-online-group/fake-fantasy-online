using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ChatUI
{
    // 060000fb: these five writes precede MonoBehaviour's constructor, in this order.
    // Keep them in one partial declaration so their order does not depend on file order.
    // The constructor allocates no view, style, list or texture and never publishes I.
    private bool panelsVisible = true;
    private float sysShownTime = -999f;
    private string inputText = string.Empty;
    private string channel = "附近";
    private string whisperTarget = string.Empty;

    // Native zero-initialized instance slots. A replacement manual view borrows the
    // existing component's state; creating that view is not another ChatUI constructor.
    private bool chatFade;
    private int sysSeqSeen;
    private int chIdx;
    private GUIStyle editSt;

    internal bool ChatFadeForIndependentRuntime { get => chatFade; set => chatFade = value; }
    internal float SystemShownTimeForIndependentRuntime { get => sysShownTime; set => sysShownTime = value; }
    internal int SystemSequenceSeenForIndependentRuntime { get => sysSeqSeen; set => sysSeqSeen = value; }
    internal int ChannelIndexForIndependentRuntime { get => chIdx; set => chIdx = value; }
    internal GUIStyle EditStyleForIndependentRuntime { get => editSt; set => editSt = value; }
}
}
