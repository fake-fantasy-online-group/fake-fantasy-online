using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class ItemGestureContract
{
    static int checks;
    static void Check(bool b, string name) { checks++; if (!b) throw new Exception(name); }
    static void Main()
    {
        var g = new MobileItemGesture();
        g.Begin(1, 7, 10, 10, 0);
        Check(!g.Move(1, 10, 10, .49f, 12), "no early hold");
        Check(g.End(1, 10, 10, 12, false), "single tap selects");
        Check(!g.End(1, 10, 10, 12, false), "release cannot repeat");
        g.Begin(1, 8, 10, 10, 0);
        Check(g.Move(1, 10, 10, .5f, 12), "hold selects");
        Check(!g.Move(1, 10, 10, 1, 12), "hold only once");
        Check(!g.End(1, 10, 10, 12, false), "hold release suppresses click");
        g.Begin(1, 9, 10, 10, 0);
        Check(!g.Move(1, 30, 10, .6f, 12), "drag cancels hold");
        Check(!g.End(1, 10, 10, 12, false), "drag out and back cancels tap");
        g.Begin(1, 9, 10, 10, 0); g.Begin(2, 10, 10, 10, 0);
        Check(g.Pointer == 1 && g.Slot == 9, "second finger cannot steal");
        Check(!g.End(2, 10, 10, 12, false) && g.Pointer == 1, "second release cannot end");
        Check(!g.End(1, 10, 10, 12, true), "cancelled touch cannot select");
        g.Begin(1, 9, 10, 10, 0); g.Reset();
        Check(!g.End(1, 10, 10, 12, false), "receipt or world change invalidates release");
        g.Begin(-2, 3, 0, 0, 0);
        Check(g.End(-2, 0, 0, 12, false), "editor mouse uses same state machine");
        g.Begin(4, 1, 0, 0, 0);
        Check(!g.Sample(4, 0, 0, .6f, 12, true, true), "cancel at hold threshold must not select");
        Check(g.Pointer == -1, "cancel sample clears owner");
        g.Begin(4, 1, 0, 0, 0);
        Check(!g.CancelMissingOwner(true) && g.Pointer == 4, "live owner retained");
        Check(g.CancelMissingOwner(false) && g.Pointer == -1, "missing raw owner released");
        g.Begin(5, 2, 0, 0, 1);
        Check(g.Sample(5, 0, 0, 1.1f, 12, true, false), "next finger works after lost owner");
        g.Begin(-2, 1, 0, 0, 0);
        Check(!g.CancelMissingOwner(false), "raw touch absence does not cancel editor mouse");
        g.Reset();
        g.Begin(6, 1, 0, 0, 0); g.Reset();
        Check(!g.Sample(6, 0, 0, 1, 12, true, false), "pause or viewport reset cannot select stale release");
        Console.WriteLine("PASS " + checks + " production mobile inventory gesture assertions");
    }
}
