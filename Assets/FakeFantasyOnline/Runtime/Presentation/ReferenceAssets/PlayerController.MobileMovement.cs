using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class PlayerController
    {
        private bool mobileSteering;
        private float mobileNextPathTime;
        private Vector2 mobileLastAxis;

        private void UpdateMobileMovement()
        {
            if (!MobileClientPresentation.Enabled) return;
            if (MobileClientPresentation.ModalOpen) { CancelMobileInput(); return; }
            Vector2 axis = ClientInput.MovementAxis;
            bool steering = ClientInput.JoystickHeld && axis.sqrMagnitude > .04f;
            if (!steering)
            {
                if (mobileSteering) StopMobileMovement();
                return;
            }
            if (!mobileSteering)
            {
                CombatWorld.StopAttack();
                followMode = false; followId = 0; sitting = false;
                mobileNextPathTime = 0f;
            }
            mobileSteering = true;
            // Reuse navigation and the existing collision, displacement and position sender.
            // No transform write or separate movement protocol exists in the touch adapter.
            if (Time.unscaledTime < mobileNextPathTime && (axis - mobileLastAxis).sqrMagnitude < .04f) return;
            mobileNextPathTime = Time.unscaledTime + .1f;
            mobileLastAxis = axis;
            float reach = Mathf.Max(30f, MoveStepPx() * .2f);
            SetPathTo(Mathf.Clamp(worldX + axis.x * reach, 0f, worldW),
                Mathf.Clamp(worldY - axis.y * reach, 0f, worldH));
        }

        private void StopMobileMovement()
        {
            mobileSteering = false; mobileNextPathTime = 0f;
            path.Clear(); pathIdx = pendHops = 0;
            moveToTarget = false; followMode = false;
        }

        private void CancelMobileInput()
        {
            if (!MobileClientPresentation.Enabled) return;
            ResetMobilePlayerGesture();
            ClientInput.CancelAll();
            if (mobileSteering) StopMobileMovement();
        }
        private void OnApplicationPause(bool paused) { if (paused) CancelMobileInput(); }
        private void OnApplicationFocus(bool focused) { if (!focused) CancelMobileInput(); }
        private void OnDisable() { CancelMobileInput(); }
    }
}
