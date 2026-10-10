using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeTipSeen;
    internal void BindTipSeenWriterForIndependentRuntime(Action<int> writer) => writeTipSeen = writer;
    public void TipSeenForIndependentRuntime(int tipId) => writeTipSeen(tipId);
}
}
