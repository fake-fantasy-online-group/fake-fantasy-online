using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal enum MobileLayoutUnlockResult { None, Tap, Unlock }

// Long-press admission is independent of inventory/network state. The same fixed
// key can unlock layout editing even when there is no potion or mount available.
internal sealed class MobileLayoutUnlockGesture
{
    internal const float HoldSeconds = .65f;
    private int owner = -1;
    private float startX, startY, started, slop;
    private bool cancelled, unlocked;
    internal int Owner => owner;
    internal bool Pressed => owner != -1 && !cancelled && !unlocked;
    internal float Progress(float now) => Pressed ? Math.Max(0f, Math.Min(1f, (now-started)/HoldSeconds)) : 0f;
    internal bool Begin(int pointer, float x, float y, float now, float scale)
    {
        if (owner != -1) return false;
        owner=pointer; startX=x; startY=y; started=now; slop=12f*Math.Max(.001f,scale);
        cancelled=unlocked=false;
        return true;
    }
    internal bool UnlockDue(int pointer,float x,float y,float now,bool inside)
    {
        float dx=x-startX,dy=y-startY;
        return pointer==owner && owner!=-1 && !cancelled && !unlocked && inside &&
            dx*dx+dy*dy<=slop*slop && now-started>=HoldSeconds;
    }
    internal MobileLayoutUnlockResult Sample(int pointer, float x, float y, float now,
        bool ended, bool cancel, bool inside)
    {
        if (pointer!=owner || owner==-1) return MobileLayoutUnlockResult.None;
        float dx=x-startX,dy=y-startY;
        cancelled |= cancel || !inside || dx*dx+dy*dy>slop*slop;
        MobileLayoutUnlockResult result=MobileLayoutUnlockResult.None;
        if (!cancelled && !unlocked && now-started>=HoldSeconds)
        { unlocked=true; result=MobileLayoutUnlockResult.Unlock; }
        else if (ended && !cancelled && !unlocked) result=MobileLayoutUnlockResult.Tap;
        if (ended || cancel) Reset();
        return result;
    }
    internal void Reset() { owner=-1; cancelled=unlocked=false; }
}
}
