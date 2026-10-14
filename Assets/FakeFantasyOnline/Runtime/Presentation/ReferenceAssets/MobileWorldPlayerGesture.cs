namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal enum MobileWorldPlayerAction { None, Tap, LongPress, Cancel }

    // A world player press is resolved once. Movement, cancellation and a long press
    // consume its eventual release rather than replaying a primary combat action.
    internal sealed class MobileWorldPlayerGesture
    {
        internal bool Active { get; private set; }
        private float x, y, started;
        private bool cancelled, fired;
        internal void Begin(float px, float py, float time)
        {
            Active = true; x = px; y = py; started = time; cancelled = fired = false;
        }
        internal MobileWorldPlayerAction Sample(float px, float py, float time, float threshold,
            bool end, bool cancel)
        {
            if (!Active) return MobileWorldPlayerAction.None;
            float dx = px - x, dy = py - y;
            if (cancel || dx * dx + dy * dy > threshold * threshold) cancelled = true;
            MobileWorldPlayerAction action = MobileWorldPlayerAction.None;
            if (cancelled) action = MobileWorldPlayerAction.Cancel;
            else if (!fired && time - started >= .5f) { fired = true; action = MobileWorldPlayerAction.LongPress; }
            else if (end && !fired) action = MobileWorldPlayerAction.Tap;
            // Invalid target/UI crossing keeps ownership until release. Lifecycle cancellation
            // resets explicitly, so returning outside UI cannot turn this into a world tap.
            if (end) Reset();
            return action;
        }
        internal void Reset() { Active = cancelled = fired = false; }
    }
}
