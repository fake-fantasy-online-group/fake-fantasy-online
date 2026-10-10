using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // The real original component message. It does not require an admitted window,
    // frame projection or avatar view, and no host manually invokes this frame.
    private void Update()
    {
        if (_apVersion != CharData.apVersion) LoadFrames();
        UpdateFrameTintForIndependentRuntime();

        if (Input.GetKeyDown(KeyCode.F11) && !ChatUI.TypingNow &&
            !Win05.GmTyping && GUIUtility.keyboardControl == 0)
        {
            int nextMode = unchecked(CharData.nameMode + 1) % CharData.NAMEMODE_COUNT;
            CharData.nameMode = nextMode;
            string feedback = "其他玩家显示: " + CharData.NAMEMODE_TIPS[nextMode];
            ChatUI.Add("系统", feedback,
                new Color(180f / 255f, 220f / 255f, 1f, 1f), "", null);
            CharData.SetPref("namemode_", CharData.nameMode);
        }

        if (dead)
        {
            UpdateDeadMovementForIndependentRuntime();
            UpdateAnim(0f, 0f, false);
            UpdateBuffAura();
            return;
        }
        if (CharData.rideAnchorId != 0)
        {
            FollowRideAnchor();
            path.Clear();
            moveToTarget = false;
            followMode = false;
            followId = 0;
            if (sr) sr.sortingOrder = GroupBase();
            IsMoving = false;
            UpdateAnim(0f, 0f, false);
            UpdateBuffAura();
            return;
        }
        if (workSkill != 0)
        {
            UpdateWork();
            if (Input.GetMouseButtonDown(1) && !SysMsgBox.Visible && !OverAnyUI())
                SysMsgBox.Show("是否要结束打工？", CancelWork, null);
            UpdateAnim(0f, 0f, false);
            return;
        }
        if (CharData.immobile != 0)
        {
            path.Clear();
            moveToTarget = false;
            pathIdx = 0;
            pendHops = 0;
            CombatWorld.StopAttack();
            sitting = false;
            UpdateAnim(0f, 0f, false);
            UpdateBuffAura();
            return;
        }

        HandleSitKey();
        UpdateLocalStallForIndependentRuntime();
        HandleGlowToggleKeyForIndependentRuntime();
        HandleTitleToggleKey();
        HandlePickupKey();
        if (CharData.riding) sitting = false;
        UpdateSittingForIndependentRuntime();

        UpdatePointerFrame();
        ApplyChantMovementForIndependentRuntime();
        ApplyApproachForIndependentRuntime();
        AdvanceFollowForIndependentRuntime();
        StopApproachForIndependentRuntime();
        DisplacementObservation step = AdvanceDisplacementForIndependentRuntime();
        AdvancePickupForIndependentRuntime();
        if (AdvanceTransferForIndependentRuntime()) return;
        ProjectMovementForIndependentRuntime();
        if (sr) sr.sortingOrder = GroupBase();
        PublishMovingForIndependentRuntime(step);
        UpdateAnim(step.X, step.Y, step.Moving);
        UpdateBuffAura();
    }
}

}
