using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class SkillTargeting
    {
        private static int mobileSlot = -1;
        private static bool mobileTargeted;

        public static void BeginMobileHotSlot(int slot)
        {
            ClientInput.Refresh();
            CancelMobileHotSlot();
            if (!MobileClientPresentation.Enabled || (uint)slot > 29 || GameHud.hotSkill[slot] == 0) return;
            mobileSlot = slot;
            if (GameHud.hotKind[slot] != 0) return;
            Aim aim = Resolve(GameHud.hotSkill[slot], out _, out _, out _, out _, out _, out _);
            mobileTargeted = aim == Aim.Circle || aim == Aim.Direction || aim == Aim.Trap;
            if (!mobileTargeted) return;
            // The HUD supplies its drag position next; never use smart-cast on pointer down.
            ClientInput.SetSkillAim(ClientInput.AimPosition, false, false);
            GameHud.UseHotSlot(slot);
            if (!Active) { mobileSlot = -1; mobileTargeted = false; ClientInput.ClearSkillAim(); }
        }

        public static void ReleaseMobileHotSlot(Vector2 screenPosition)
        {
            int slot = mobileSlot;
            mobileSlot = -1;
            if (slot < 0) return;
            if (mobileTargeted)
            {
                if (Active) ClientInput.SetSkillAim(screenPosition, true, false);
                else ClientInput.ClearSkillAim();
            }
            else { ClientInput.ClearSkillAim(); GameHud.UseHotSlot(slot); }
            mobileTargeted = false;
        }

        public static void CancelMobileHotSlot()
        {
            mobileSlot = -1; mobileTargeted = false;
            if (I != null && Active) I.End();
            ClientInput.ClearSkillAim();
        }
    }
}
