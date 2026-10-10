using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    // Whole 06000b21 owner. SpawnPlayer installs this component on the actual active player
    // root. Unity alone drives the frame, including when no borrowed actor view was admitted.
    private void Update()
    {
        long frameStamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
        try
        {
            if (body == null) return;

            long headStamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
            TickStatus();
            UpdateAppearanceVisibilityForIndependentRuntime();
            UpdatePickBoxVisibilityForIndependentRuntime();
            if (statusFx) statusFx.SetHidden(_hideEntity);
            if (LoadTrace.On)
            {
                LoadTrace.AddSeg(LoadTrace.Seg.玩_头部, headStamp);
                LoadTrace.NoteSegCall(LoadTrace.Seg.玩_头部);
            }

            if (_hideEntity)
            {
                if (tagGo && tagGo.activeSelf) tagGo.SetActive(false);
                hpBarView.Tick(transform, false, hp, maxHp, 0f, 0f);
            }
            if (UpdateMissingAvatarForIndependentRuntime()) return;
            UpdateHeadFeedbackForIndependentRuntime();

            if (dead)
            {
                StepDie();
                return;
            }

            long ridingStamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
            if (rideAnchorId > 0) FollowRideAnchor();
            // Read after FollowRideAnchor. The received snapshot does not own this decision.
            if (riding)
            {
                StepRiding();
                if (LoadTrace.On)
                {
                    LoadTrace.AddSeg(LoadTrace.Seg.玩_骑乘, ridingStamp);
                    LoadTrace.NoteSegCall(LoadTrace.Seg.玩_骑乘);
                }
                return;
            }
            if (LoadTrace.On)
            {
                LoadTrace.AddSeg(LoadTrace.Seg.玩_骑乘, ridingStamp);
                LoadTrace.NoteSegCall(LoadTrace.Seg.玩_骑乘);
            }

            long ordinaryStamp = LoadTrace.On ? LoadTrace.SegStamp() : 0;
            try
            {
                if (acting)
                {
                    AdvancePath();
                    StepAct();
                }
                else if (moving)
                {
                    // Choose this branch before AdvancePath may clear moving on arrival.
                    AdvancePath();
                    StepWalk();
                }
                else if (_isStallOwner)
                {
                    ApplyPose(SitPose(curDir), flip, SortFor(worldY));
                }
                else UpdateIdle();
            }
            finally
            {
                if (LoadTrace.On)
                {
                    LoadTrace.AddSeg(LoadTrace.Seg.玩_走动待机, ordinaryStamp);
                    LoadTrace.NoteSegCall(LoadTrace.Seg.玩_走动待机);
                }
            }
            return;
        }
        finally
        {
            if (LoadTrace.On)
            {
                LoadTrace.AddSeg(LoadTrace.Seg.远端玩家, frameStamp);
                LoadTrace.NoteSegCall(LoadTrace.Seg.远端玩家);
            }
        }
    }
}

}
