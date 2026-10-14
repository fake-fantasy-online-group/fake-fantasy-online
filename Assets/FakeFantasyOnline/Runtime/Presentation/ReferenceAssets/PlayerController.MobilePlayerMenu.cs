using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class PlayerController
    {
        private readonly MobileWorldPlayerGesture mobilePlayerGesture = new MobileWorldPlayerGesture();
        private RemotePlayer mobilePlayerTarget;
        private void ResetMobilePlayerGesture()
        {
            mobilePlayerGesture.Reset(); mobilePlayerTarget = null;
        }
        private bool HandleMobilePlayerPointer(ClientPointerFrame pointer, bool overUi, Vector2 worldPoint)
        {
            if (pointer.Down)
            {
                ResetMobilePlayerGesture();
                if (!overUi && Time.time > suppressWorldClickUntil)
                    mobilePlayerTarget = PickSelectable(worldPoint) as RemotePlayer;
                if (mobilePlayerTarget != null)
                    mobilePlayerGesture.Begin(pointer.Position.x, pointer.Position.y, Time.unscaledTime);
            }
            if (!mobilePlayerGesture.Active) return false;
            RemotePlayer target = mobilePlayerTarget;
            MobileWorldPlayerAction action = mobilePlayerGesture.Sample(pointer.Position.x, pointer.Position.y,
                Time.unscaledTime, 12f * MobileClientPresentation.Scale, pointer.Up,
                pointer.Cancelled || overUi || target == null);
            if (action == MobileWorldPlayerAction.LongPress)
            {
                // Same remote-player menu and permission builder as the desktop target HUD.
                CombatWorld.StopAttack();
                StopForNpcPointerForIndependentRuntime();
                PlayerActionMenu.OpenAtScreen(target.id, target.pname, pointer.Position);
                SuppressWorldClick();
            }
            else if (action == MobileWorldPlayerAction.Tap)
            {
                if (target.HostileToLocal) CombatWorld.SetPvpTarget(target);
                else TargetSelect.Select(target);
            }
            if (!mobilePlayerGesture.Active) mobilePlayerTarget = null;
            return true;
        }
    }
}
