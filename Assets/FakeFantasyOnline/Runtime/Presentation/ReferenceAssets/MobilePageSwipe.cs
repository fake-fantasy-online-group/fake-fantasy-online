namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // The page indicator is a swipe surface, never a tap-to-cycle button.
    // Resolve only on the owning pointer's release; a hold has no action.
    internal sealed class MobilePageSwipe
    {
        internal int Pointer { get; private set; } = -1;
        private float startX, startY;
        private bool cancelled;

        internal void Begin(int pointer, float x, float y)
        {
            if (Pointer != -1) return;
            Pointer = pointer; startX = x; startY = y; cancelled = false;
        }

        internal int Sample(int pointer, float x, float y, float threshold, bool end, bool cancel)
        {
            if (Pointer == -1 || pointer != Pointer) return 0;
            float dx = x - startX, dy = y - startY;
            float absX = System.Math.Abs(dx), absY = System.Math.Abs(dy);
            // Vertical scrolling or a cancellation permanently disqualifies this gesture.
            if (cancel || (absY >= threshold && absY > absX)) cancelled = true;
            if (!end && !cancel) return 0;
            int change = !cancelled && absX >= threshold && absX > absY * 1.25f
                ? (dx < 0f ? 1 : -1) : 0;
            Reset();
            return change;
        }

        internal static int PageAfter(int page, int change) => (page + change + 8) % 8;
        internal void Reset() { Pointer = -1; cancelled = false; }
    }
}
