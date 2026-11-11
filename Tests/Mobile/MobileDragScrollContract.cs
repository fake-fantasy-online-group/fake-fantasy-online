using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;

static class MobileDragScrollContract
{
    static int checks;
    static void Check(bool ok, string name) { checks++; if (!ok) throw new Exception("FAIL " + name); }
    static float Step(MobileDragScrollTrack t, int finger, bool down, bool began, bool inside, float y, float time, float current, float max, out bool cancel)
        => t.Update(finger, down, began, inside, 100f, y, time, current, max, 12f, out cancel);

    static void Main()
    {
        var t = new MobileDragScrollTrack();
        bool cancel;
        float off = Step(t, 0, true, true, true, 300f, 0f, 50f, 400f, out cancel);
        Check(off == 50f && !cancel && t.Finger == 0 && !t.Dragging, "press inside starts tracking without moving");
        off = Step(t, 0, true, false, true, 292f, .016f, off, 400f, out cancel);
        Check(off == 50f && !cancel && !t.Dragging, "movement under threshold stays a tap");
        off = Step(t, 0, true, false, true, 280f, .032f, off, 400f, out cancel);
        Check(t.Dragging && cancel && Math.Abs(off - 70f) < .01f, "crossing threshold drags by full distance and cancels the press");
        off = Step(t, 0, true, false, false, 240f, .048f, off, 400f, out cancel);
        Check(!cancel && Math.Abs(off - 110f) < .01f, "drag keeps ownership outside the region");
        Check(t.Velocity > 0f, "upward finger builds positive velocity");
        float released = Step(t, -1, false, false, false, 0f, .064f, off, 400f, out cancel);
        Check(t.Finger < 0 && !t.Dragging && released > off, "release coasts in drag direction");
        float v0 = t.Velocity; float last = released;
        for (int i = 0; i < 400 && t.Coasting; i++) last = Step(t, -1, false, false, false, 0f, .064f + (i + 1) * .016f, last, 400f, out cancel);
        Check(!t.Coasting && t.Velocity == 0f || Math.Abs(t.Velocity) <= MobileDragScrollTrack.StopVelocity, "inertia decays to a stop");
        Check(last <= 400f && last >= released, "coasting stays in bounds");

        t = new MobileDragScrollTrack();
        Step(t, 3, true, true, true, 100f, 0f, 0f, 400f, out cancel);
        off = Step(t, 3, true, false, true, 102f, .016f, 0f, 400f, out cancel);
        off = Step(t, -1, false, false, false, 0f, .032f, off, 400f, out cancel);
        off = Step(t, -1, false, false, false, 0f, .048f, off, 400f, out cancel);
        Check(off == 0f && !t.Coasting, "tap never coasts");

        t = new MobileDragScrollTrack();
        off = Step(t, 1, true, true, false, 100f, 0f, 10f, 400f, out cancel);
        off = Step(t, 1, true, false, true, 40f, .016f, off, 400f, out cancel);
        Check(off == 10f && t.Finger < 0, "press that began outside never scrolls");

        t = new MobileDragScrollTrack();
        t.Update(1, true, true, true, 100f, 100f, 0f, 0f, 400f, 12f, out cancel);
        off = t.Update(1, true, false, true, 160f, 80f, .016f, 0f, 400f, 12f, out cancel);
        Check(!t.Dragging && off == 0f, "horizontal gesture is left to the content");

        t = new MobileDragScrollTrack();
        Step(t, 0, true, true, true, 100f, 0f, 20f, 400f, out cancel);
        off = Step(t, 0, true, false, true, 200f, .016f, 20f, 400f, out cancel);
        Check(off == 0f, "drag clamps at the top");
        off = Step(t, 0, true, false, true, 100f, .032f, 0f, 0f, out cancel);
        Check(off == 0f && t.Finger < 0, "non-scrollable list releases tracking");

        t = new MobileDragScrollTrack();
        Step(t, 0, true, true, true, 100f, 0f, 0f, 400f, out cancel);
        Step(t, 0, true, false, true, 60f, .016f, 0f, 400f, out cancel);
        off = Step(t, 5, true, false, true, 20f, .032f, 40f, 400f, out cancel);
        Check(t.Finger < 0 && !t.Dragging, "a different pointer ends the drag");
        Console.WriteLine("PASS mobile drag scroll track: " + checks + " checks");
    }
}
