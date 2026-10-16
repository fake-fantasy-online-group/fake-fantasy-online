using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // The only device sampling boundary used by world movement and skill targeting.
    public static class ClientInput
    {
        private static readonly TouchPointerRouter router = new TouchPointerRouter();
        private static int frame = -1;
        private static bool mobileLastFrame, skillAim;
        private static Rect viewport;
        private static Vector2 aimPosition;
        private static int confirmFrame = -1, cancelFrame = -1;
        public static bool MobileSkillGestureActive => skillAim;
        public static bool SecondaryDown => !MobileClientPresentation.Enabled && Input.GetMouseButtonDown(1);
        public static bool SecondaryUp => !MobileClientPresentation.Enabled && Input.GetMouseButtonUp(1);
        public static ClientPointerFrame Pointer { get { Sample(); return pointer; } }
        private static ClientPointerFrame pointer;
        public static Vector2 MovementAxis { get { Sample(); return router.MovementAxis; } }
        public static bool JoystickHeld { get { Sample(); return router.JoystickHeld; } }
        public static Vector2 AimPosition => skillAim ? aimPosition : Pointer.Position;
        public static bool AimConfirm => skillAim ? confirmFrame >= 0 : Pointer.Down;
        public static bool AimCancel => cancelFrame >= 0 ||
            (!MobileClientPresentation.Enabled && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)));

        public static void Refresh() => Sample();

        private static void Sample()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            bool mobile = MobileClientPresentation.Enabled;
            if (mobile != mobileLastFrame) { router.CancelAll(); mobileLastFrame = mobile; }
            if (!mobile)
            {
                pointer = new ClientPointerFrame { Position = Input.mousePosition, Available = true,
                    Down = Input.GetMouseButtonDown(0), Held = Input.GetMouseButton(0), Up = Input.GetMouseButtonUp(0) };
                return;
            }
            if (viewport != MobileClientPresentation.SafeAreaGui)
            {
                viewport = MobileClientPresentation.SafeAreaGui;
                router.CancelAll(); SkillTargeting.CancelMobileHotSlot();
            }
            if (MobileClientPresentation.ModalOpen)
            {
                router.CancelAll(); pointer = router.Pointer;
                SkillTargeting.CancelMobileHotSlot();
                return;
            }
            router.BeginFrame();
            Rect stick = MobileClientPresentation.JoystickRect;
            Vector2 center = new Vector2(stick.center.x, Screen.height - stick.center.y);
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                ClientTouchPhase phase = touch.phase == TouchPhase.Began ? ClientTouchPhase.Began :
                    touch.phase == TouchPhase.Ended ? ClientTouchPhase.Ended :
                    touch.phase == TouchPhase.Canceled ? ClientTouchPhase.Cancelled :
                    touch.phase == TouchPhase.Stationary ? ClientTouchPhase.Stationary : ClientTouchPhase.Moved;
                router.CancelIfCovered(touch.fingerId, MobileClientPresentation.IsPanelPointer(touch.position));
                router.Feed(touch.fingerId, touch.position, phase,
                    MobileClientPresentation.IsPointerOverUi(touch.position),
                    MobileClientPresentation.IsJoystickRegion(touch.position), center, stick.width * .5f);
            }
            // Desktop mobile-preview uses the same ownership state machine, not a second control path.
            if (!UnityEngine.Application.isMobilePlatform && Input.touchCount == 0 &&
                (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)))
            {
                Vector2 position = Input.mousePosition;
                ClientTouchPhase phase = Input.GetMouseButtonDown(0) ? ClientTouchPhase.Began :
                    Input.GetMouseButtonUp(0) ? ClientTouchPhase.Ended : ClientTouchPhase.Moved;
                router.CancelIfCovered(-1, MobileClientPresentation.IsPanelPointer(position));
                router.Feed(-1, position, phase, MobileClientPresentation.IsPointerOverUi(position),
                    MobileClientPresentation.IsJoystickRegion(position), center, stick.width * .5f);
            }
            router.EndFrame();
            pointer = router.Pointer;
        }

        public static void SetSkillAim(Vector2 position, bool confirm, bool cancel)
        {
            skillAim = true; aimPosition = position;
            if (confirm) confirmFrame = Time.frameCount;
            if (cancel) cancelFrame = Time.frameCount;
        }
        public static void ConsumeAimConfirm() { confirmFrame = -1; }
        public static void ClearSkillAim() { skillAim = false; confirmFrame = cancelFrame = -1; }
        public static void CancelAll()
        {
            router.CancelAll(); pointer = router.Pointer; frame = Time.frameCount;
            ClearSkillAim(); SkillTargeting.CancelMobileHotSlot();
        }
    }
}
