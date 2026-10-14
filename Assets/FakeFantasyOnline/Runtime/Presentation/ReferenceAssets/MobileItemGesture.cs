namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Engine-independent, single-owner gesture. Long press never activates an item.
    internal sealed class MobileItemGesture
    {
        internal int Pointer { get; private set; } = -1;
        internal int Slot { get; private set; } = -1;
        private float x, y, started;
        private bool cancelled, fired;
        internal void Begin(int pointer, int slot, float px, float py, float time)
        {
            if (Pointer != -1) return;
            Pointer = pointer; Slot = slot; x = px; y = py; started = time;
            cancelled = fired = false;
        }
        internal bool Move(int pointer, float px, float py, float time, float threshold)
        {
            if (pointer != Pointer || Pointer == -1) return false;
            float dx = px - x, dy = py - y;
            if (dx * dx + dy * dy > threshold * threshold) cancelled = true;
            if (cancelled || fired || time - started < .5f) return false;
            fired = true;
            return true;
        }
        internal bool Sample(int pointer, float px, float py, float time, float threshold, bool end, bool cancel)
        {
            // Cancellation has priority over elapsed hold time, including the cancellation frame.
            if (cancel) { End(pointer, px, py, threshold, true); return false; }
            bool select = Move(pointer, px, py, time, threshold);
            if (end) select = End(pointer, px, py, threshold, false) || select;
            return select;
        }
        internal bool CancelMissingOwner(bool ownerPresent)
        {
            if (Pointer < 0 || ownerPresent) return false;
            Reset();
            return true;
        }
        internal bool End(int pointer, float px, float py, float threshold, bool cancel)
        {
            if (pointer != Pointer || Pointer == -1) return false;
            float dx = px - x, dy = py - y;
            bool tap = !cancel && !cancelled && !fired && dx * dx + dy * dy <= threshold * threshold;
            Reset();
            return tap;
        }
        internal void Reset() { Pointer = -1; Slot = -1; cancelled = fired = false; }
    }
}
