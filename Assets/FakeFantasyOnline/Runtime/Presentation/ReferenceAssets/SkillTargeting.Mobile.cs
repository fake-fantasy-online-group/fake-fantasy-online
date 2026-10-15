using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class SkillTargeting
    {
        private static int mobileSlot = -1;
        private static int mobileSkill;
        private static int mobileKind;
        private static Aim mobileMode;
        internal static bool MobileHotSlotActive => mobileSlot >= 0;

        public static void BeginMobileHotSlot(int slot)
        {
            ClientInput.Refresh();
            CancelMobileHotSlot();
            if (!MobileClientPresentation.Enabled || !GameHud.MobileHotSlotUsable(slot)) return;
            mobileSlot = slot;
            mobileSkill = GameHud.hotSkill[slot];
            mobileKind = GameHud.hotKind[slot];
            if (GameHud.hotKind[slot] != 0) return;
            mobileMode = ResolveMobile(mobileSkill, out float range, out float radius, out float angle,
                out float length, out float width, out bool trap);
            if (mobileMode == Aim.Unavailable) { CancelMobileHotSlot(); return; }
            if (mobileMode == Aim.Instant) return;
            // Prepare a preview only. No target choice or cast occurs on pointer down.
            ClientInput.SetSkillAim(ClientInput.AimPosition, false, false);
            Begin(mobileSkill, mobileMode, range, mobileMode == Aim.Single || mobileMode == Aim.Trap ? 24f : radius, angle, length, width, trap);
        }

        internal static Vector2 MobileAimPosition(Vector2 delta)
        {
            PlayerController pc = PlayerController.I;
            if (pc == null || Camera.main == null) return new Vector2(Screen.width * .5f, Screen.height * .5f);
            Vector3 feet = new Vector3((pc.worldX - pc.worldW * .5f) / pc.ppu,
                (pc.worldH * .5f - pc.worldY) / pc.ppu, 0f);
            Vector2 origin = Camera.main.WorldToScreenPoint(feet);
            float range = I != null && Active ? I.rangePx : DefaultRange;
            if (float.IsInfinity(range) || range <= 0f) range = DefaultRange;
            Vector2 edge = Camera.main.WorldToScreenPoint(feet + new Vector3(range / pc.ppu, 0f, 0f));
            float screenRange = (edge - origin).magnitude;
            return origin + Vector2.ClampMagnitude(delta / (120f * MobileClientPresentation.Scale), 1f) * screenRange;
        }

        public static void ReleaseMobileHotSlot(Vector2 screenPosition, bool dragged)
        {
            int slot = mobileSlot;
            int skill = mobileSkill;
            if (slot < 0) return;
            if (!GameHud.MobileHotSlotUsable(slot) || GameHud.hotSkill[slot] != skill || GameHud.hotKind[slot] != mobileKind)
            { CancelMobileHotSlot(); return; }
            GameSettings.EnsureLoaded();
            Aim aim = ResolveMobile(skill, out float range, out _, out _, out _, out _, out _);
            if (GameHud.hotKind[slot] != 0)
            { CancelMobileHotSlot(); GameHud.UseHotSlot(slot); return; }
            if (aim == Aim.Instant)
            {
                CancelMobileHotSlot();
                // A genuine self/instant action never borrows a selected unit.
                if (!AuthoritativeProfessionSkill.Uses(skill) || AuthoritativeProfessionSkill.LearnedActive(skill))
                    NetClient.I.UseSkill(skill, 0);
                return;
            }
            if (aim != mobileMode || aim == Aim.Unavailable || (!dragged && !GameSettings.SmartCast))
            { CancelMobileHotSlot(); return; }
            float effectiveRange = AuthoritativeProfessionSkill.Uses(skill)
                ? (range > 0f ? range : float.PositiveInfinity) : range > 0f ? range : DefaultRange;
            PlayerController pc = PlayerController.I;
            if (pc == null || Camera.main == null) { CancelMobileHotSlot(); return; }
            Vector3 world = Camera.main.ScreenToWorldPoint(screenPosition);
            int gx = Mathf.RoundToInt(world.x * pc.ppu + pc.worldW * .5f);
            int gy = Mathf.RoundToInt(pc.worldH * .5f - world.y * pc.ppu);
            if (aim == Aim.Single)
            {
                if (dragged) CombatWorld.TryMobileUnitCastAt(skill, gx, gy, effectiveRange);
                else CombatWorld.TryMobileUnitCast(skill, effectiveRange, true);
            }
            else if (aim == Aim.Trap)
            {
                if (dragged) CombatWorld.TryMobileTrapCast(skill, gx, gy, effectiveRange);
                else CombatWorld.TryMobileNearestTrapCast(skill, effectiveRange);
            }
            else if (dragged)
                CombatWorld.TryMobileGroundCast(skill, gx, gy, effectiveRange, aim == Aim.Direction);
            else if (CombatWorld.TryMobileSkillTarget(skill, effectiveRange, true, out int target, out Vector2 position))
                CombatWorld.TryMobileGroundCast(skill, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y),
                    effectiveRange, aim == Aim.Direction, target);
            CancelMobileHotSlot();
        }

        private static Aim ResolveMobile(int skill, out float range, out float radius, out float angle,
            out float length, out float width, out bool trap)
        {
            Aim aim=Resolve(skill,out range,out radius,out angle,out length,out width,out trap);
            if (!AuthoritativeProfessionSkill.Uses(skill) && aim==Aim.Instant)
            {
                Win05.SkInfo info=Win05.SkillInfo(skill);
                if (info!=null && info.castkind!=0 && info.canFriendly==1 && info.canEnemy==0 && !SelfCenteredCircle(info))
                    return Aim.Single;
            }
            return aim;
        }

        public static void CancelMobileHotSlot()
        {
            mobileSlot = -1;
            mobileSkill = 0;
            if (I != null && Active) I.End();
            ClientInput.ClearSkillAim();
        }
    }
}
