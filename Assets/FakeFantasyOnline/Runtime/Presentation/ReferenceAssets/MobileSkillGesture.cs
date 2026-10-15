namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Pure thumb intent: crossing the scaled slop is irreversible until release/cancel.
    internal sealed class MobileSkillGesture
    {
        internal bool Active { get; private set; }
        internal bool Dragged { get; private set; }
        private float startX, startY, threshold;
        internal void Begin(float x, float y, float scale)
        { Active = true; Dragged = false; startX = x; startY = y; threshold = 12f * System.Math.Max(.1f, scale); }
        internal void Sample(float x, float y)
        {
            if (!Active) return;
            float dx = x - startX, dy = y - startY;
            if (dx * dx + dy * dy >= threshold * threshold) Dragged = true;
        }
        internal bool AllowsRelease(bool smartCast, bool selfSkill, bool canceled) =>
            Active && !canceled && (Dragged || smartCast || selfSkill);
        internal void Reset() { Active = false; Dragged = false; }
    }
}
