using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Engine-free drag/inertia state for one native list. Positions are list-local
// points; values are content offsets in the same units. Mobile-only callers.
internal sealed class MobileDragScrollTrack
{
    internal const float StopVelocity = 30f;      // points per second
    internal const float Deceleration = 4.5f;     // exponential decay per second
    internal const float VelocityBlend = .5f;
    public int Finger = -1;
    public bool Dragging;
    public float StartX, StartY, LastY, Velocity, LastTime = float.NaN;
    public bool Coasting => Finger < 0 && Math.Abs(Velocity) > StopVelocity;

    public void Cancel() { Finger = -1; Dragging = false; Velocity = 0f; }

    // finger: owning pointer id (or -1 when no pointer is down); began: this is the
    // pointer's first sample; inside: the pointer is within the list region.
    // Returns the new offset. cancelPress reports the frame the drag took ownership
    // so the caller can release any pending native button press.
    public float Update(int finger, bool down, bool began, bool inside, float x, float y, float time,
        float current, float max, float threshold, out bool cancelPress)
    {
        cancelPress = false;
        float dt = float.IsNaN(LastTime) ? 0f : Math.Max(0f, time - LastTime);
        LastTime = time;
        if (max <= 0f) { Cancel(); return 0f; }
        current = Clamp(current, max);
        if (Finger >= 0 && (!down || finger != Finger))
        {
            // Release: a press that never dragged must not coast.
            if (!Dragging) Velocity = 0f;
            Finger = -1; Dragging = false;
        }
        if (down && Finger < 0 && began && inside)
        {
            Finger = finger; StartX = x; StartY = y; LastY = y; Dragging = false; Velocity = 0f;
            return current;
        }
        if (down && Finger >= 0 && finger == Finger)
        {
            float dy = y - LastY;
            if (!Dragging && Math.Abs(y - StartY) > threshold && Math.Abs(y - StartY) >= Math.Abs(x - StartX))
            { Dragging = true; cancelPress = true; dy = y - StartY; }
            if (!Dragging) return current;
            LastY = y;
            float next = Clamp(current - dy, max);
            if (dt > 0f)
            {
                float instant = (next - current) / dt;
                Velocity = Velocity * (1f - VelocityBlend) + instant * VelocityBlend;
            }
            return next;
        }
        if (Finger < 0 && dt > 0f && Math.Abs(Velocity) > StopVelocity)
        {
            float next = current + Velocity * dt;
            Velocity *= (float)Math.Exp(-Deceleration * dt);
            if (next <= 0f || next >= max) { Velocity = 0f; return Clamp(next, max); }
            return next;
        }
        if (Finger < 0) Velocity = 0f;
        return current;
    }

    private static float Clamp(float value, float max) => value < 0f ? 0f : value > max ? max : value;
}
}
