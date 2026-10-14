using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // The existing live tracker state follows this native component's lifetime.
    // Preserve the established sentinel/threshold qualification; release does not reset it.
    private float rightDownTime = -1f;
    private Vector3 rightDownScreen;
    private float holdThreshold = 0.25f;

    private void UpdatePointerFrame()
    {
        if (Camera.main == null) return;
        ClientPointerFrame pointer = ClientInput.Pointer;
        bool mobile = MobileClientPresentation.Enabled;
        if (mobile && pointer.Cancelled) { followMode = false; ResetMobilePlayerGesture(); return; }
        if (mobile && !pointer.Available) return;
        bool overUi = mobile ? MobileClientPresentation.IsPointerOverUi(pointer.Position) || SkillTargeting.Active : OverAnyUI();
        Vector3 world = Camera.main.ScreenToWorldPoint(pointer.Position);
        Physics2D.SyncTransforms();
        float px = Mathf.Clamp(world.x * ppu + worldW * 0.5f, 0f, worldW);
        float py = Mathf.Clamp(worldH * 0.5f - world.y * ppu, 0f, worldH);
        Vector2 point = new Vector2(world.x, world.y);
        if (mobile && HandleMobilePlayerPointer(pointer, overUi, point)) return;
        if (pointer.Down) LatchPrimaryPressForIndependentRuntime(overUi);
        float elapsed = Time.time - mouseDownTime;

        if (overUi && CursorManager.I != null)
        {
            CursorManager.I.SetAttackHover(-2);
            CursorManager.I.SetGatherHover(0);
        }

        NpcInteract npc = null;
        if (!overUi && StallConfigDlg.LocalStalling)
        {
            if (pointer.Down) BrowseOwnStallAtForIndependentRuntime(point);
        }
        else if (!overUi)
        {
            ISelectable selected = PickSelectable(point);
            npc = selected as NpcInteract;
            MonsterEntity selectedMonster = selected as MonsterEntity;
            MonsterEntity attackableMonster = selectedMonster;
            if (attackableMonster != null && attackableMonster.NoAttack) attackableMonster = null;
            RemotePlayer remote = selected as RemotePlayer;
            bool hostileRemote = remote != null && remote.HostileToLocal;
            if (pointer.Down)
                mouseDownOverMob = attackableMonster != null || hostileRemote;
            StallSignClick sign = PickStallSignForIndependentRuntime(point, npc != null);
            if (pointer.Down && sign != null) mouseDownOverSign = true;

            if (CursorManager.I != null)
            {
                int attackKind = attackableMonster != null
                    ? Math.Sign(unchecked(attackableMonster.level - CharData.level))
                    : hostileRemote || CombatWorld.AwaitingTarget ? 0 : -2;
                CursorManager.I.SetAttackHover(attackKind);
            }
            bool gatherAllowed = selected == null || selected is PlayerController ||
                (selectedMonster != null && selectedMonster.friendly);
            int gatherKind = GatherKindForIndependentRuntime(px, py, gatherAllowed);
            if (CursorManager.I != null) CursorManager.I.SetGatherHover(gatherKind);

            bool pickedGround = mobile && pointer.Down && !mouseDownOverUI &&
                (selected == null || selected is PlayerController) && TryMobileGroundPickup(point);
            if (pickedGround) mouseDownOverSign = true; // consume release; retain pickup navigation
            bool openedWork = gatherKind != 0 && pointer.Down && !mouseDownOverUI;
            if (pickedGround) { }
            else if (openedWork)
            {
                OpenWorkTipForIndependentRuntime(gatherKind);
            }
            else if (pointer.Down && sign != null && !mouseDownOverUI)
            {
                BrowseStallForIndependentRuntime(sign);
            }
            else if (pointer.Down && npc != null && !mouseDownOverUI)
            {
                npc.Show();
                StopForNpcPointerForIndependentRuntime();
            }
            else if (pointer.Down && attackableMonster != null && !mouseDownOverUI)
            {
                ApproachMonsterPointerForIndependentRuntime(px, py);
            }
            else if (followMode && pointer.Up && elapsed < holdThreshold && !mouseDownOverUI)
            {
                followMode = false;
                if (!mouseDownOverMob && !mouseDownOverSign)
                {
                    CombatWorld.StopAttack();
                    SetPathTo(px, py);
                }
            }
            else if (!mobile && !followMode && pointer.Held && holdThreshold < elapsed &&
                npc == null && attackableMonster == null && !mouseDownOverUI)
            {
                followMode = true;
                sitting = false;
                CombatWorld.StopAttack();
            }
            else if (!followMode && pointer.Up && elapsed < holdThreshold &&
                npc == null && attackableMonster == null && !mouseDownOverUI &&
                !mouseDownOverMob && !mouseDownOverSign)
            {
                CombatWorld.StopAttack();
                SetPathTo(px, py);
                sitting = false;
            }

            // Mobile world ownership is explicit; Unity mouse callbacks remain desktop-only.
            if (mobile && pointer.Down && !mouseDownOverUI)
            {
                if (selectedMonster != null)
                {
                    if (!selectedMonster.friendly && !selectedMonster.ally) CombatWorld.SetTarget(selectedMonster, true);
                    else TargetSelect.Select(selectedMonster);
                }
                else if (remote != null)
                {
                    if (hostileRemote) CombatWorld.SetPvpTarget(remote);
                    else TargetSelect.Select(remote);
                }
            }

            if (ClientInput.SecondaryDown)
            {
                rightDownTime = Time.time;
                rightDownScreen = pointer.Position;
            }
            if (ClientInput.SecondaryUp && rightDownTime > 0f &&
                Time.time - rightDownTime < holdThreshold &&
                ((Vector3)pointer.Position - rightDownScreen).sqrMagnitude < 64f &&
                !PlayerActionMenu.Visible)
            {
                if (CombatWorld.AwaitingTarget)
                {
                    CombatWorld.CancelPending();
                    UITip.Show("取消释放", 0.8f);
                }
                else
                {
                    ISelectable rightSelected = PickSelectable(point);
                    if (rightSelected == null) CombatWorld.ClearTarget();
                    else if (rightSelected is MonsterEntity monster && !monster.friendly)
                        CombatWorld.SetTarget(monster, false);
                    else TargetSelect.Select(rightSelected);
                }
            }
        }

        bool emitRing = AdvancePointerFollowTailForIndependentRuntime(npc != null, px, py);
        if (CursorManager.I != null) CursorManager.I.SetNpcHover(npc != null && !overUi);
        if (emitRing) EmitRing(px, py);
    }
}

}
