using System;
using System.Globalization;
using System.IO;
using System.Threading;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class MobileControlLayoutContract
{
    private static int checks;
    private static readonly Rect Safe = new Rect(32f, 20f, 960f, 540f);
    private static readonly Rect Button = new Rect(720f, 360f, 64f, 64f);

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .002f;
    private static bool Same(Rect a, Rect b) => Near(a.x, b.x) && Near(a.y, b.y) && Near(a.width, b.width) && Near(a.height, b.height);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void Contained(Rect rect, Rect safe, string message)
    {
        Check(Finite(rect.x) && Finite(rect.y) && Finite(rect.width) && Finite(rect.height) &&
            rect.width > 0f && rect.height > 0f && rect.x >= safe.x - .002f && rect.y >= safe.y - .002f &&
            rect.xMax <= safe.xMax + .002f && rect.yMax <= safe.yMax + .002f, message);
    }
    private static void Reject(string text)
    {
        bool rejected = false;
        try { MobileControlLayoutStore.Parse(text); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected, "Malformed layout rejected as a whole");
    }

    public static void Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ffo-mobile-layout-" + Guid.NewGuid().ToString("N"));
        CultureInfo culture = Thread.CurrentThread.CurrentCulture;
        try
        {
            Geometry();
            Persistence(directory);
            Console.WriteLine("Mobile control layout: " + checks + " checks passed (pure C#, temporary files; no Unity/device runtime).");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = culture;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static void Geometry()
    {
        var layout = new MobileControlLayout();
        Check(MobileControlLayout.ControlCount == 14 && (int)MobileControlId.Sit == 13, "Stable physical controls include appended sit control");
        Check(layout.Count == 0, "Fresh layout contains no arbitrary defaults");
        Check(Same(layout.ResolveRect(MobileControlId.Attack, Button, Safe, 1f), Button), "Uncustomized valid default remains exact");
        Check(layout.Count == 0, "Resolving defaults does not create persisted entries");
        for (int group = 0; group < 4; group++)
        for (int skill = 0; skill < 6; skill++)
            Check(MobileGroupWheel.HotbarSlot(group, skill) == group * 6 + skill, "Existing four by six mapping is unchanged");
        Check(MobileGroupWheel.SlotCount == 24 && MobileGroupWheel.HotbarSlot(4, 0) == -1, "Mobile binding capacity remains 24");

        foreach (Rect safe in new[] { Safe, new Rect(71, 44, 1846, 1002), new Rect(0, 76, 540, 1028), new Rect(18, 10, 20, 10) })
        foreach (Vector2 center in new[] { new Vector2(-10000, -10000), new Vector2(10000, 10000), safe.center })
        foreach (float scale in new[] { -100f, .65f, 1f, 1.5f, 100f })
        {
            Check(layout.Set(MobileControlId.Skill0, center, scale, Button, safe, 1f), "Finite drag/resize accepted");
            Rect resolved = layout.ResolveRect(MobileControlId.Skill0, Button, safe, 1f);
            Contained(resolved, safe, "Entire touch area remains inside safe inset");
            Check(layout.TryGet(MobileControlId.Skill0, out MobileControlPlacement entry) &&
                entry.CenterX >= 0f && entry.CenterX <= 1f && entry.CenterY >= 0f && entry.CenterY <= 1f &&
                entry.Scale >= MobileControlLayout.MinScale && entry.Scale <= MobileControlLayout.MaxScale,
                "Saved placement remains normalized and bounded");
        }
        Check(layout.Count == 1, "Dragging updates one physical slot regardless of group");
        var smallButton = new Rect(100, 100, 48, 48);
        Check(layout.Set(MobileControlId.Pickup, Safe.center, .65f, smallButton, Safe, 1f), "Small control resize");
        Rect pickup = layout.ResolveRect(MobileControlId.Pickup, smallButton, Safe, 1f);
        Check(Near(pickup.width, 44f) && Near(pickup.height, 44f), "Minimum touch target is 44 logical pixels");
        Check(layout.Set(MobileControlId.Joystick, Safe.center, 100f, new Rect(0, 0, 160, 160), Safe, 1f), "Joystick max resize");
        Check(Near(layout.ResolveRect(MobileControlId.Joystick, new Rect(0, 0, 160, 160), Safe, 1f).width, 240), "Joystick has a reasonable size cap");

        Check(layout.SetNormalized(MobileControlId.Attack, .7f, .6f, 1.2f), "Set a representative normalized placement");
        string beforeOrientation = MobileControlLayoutStore.Serialize(layout);
        foreach (Rect safe in new[] { Safe, new Rect(50, 30, 1920, 1080), new Rect(0, 60, 1080, 1800) })
        {
            Rect resolved = layout.ResolveRect(MobileControlId.Attack, Button, safe, 1f);
            Check(Near((resolved.center.x - safe.x) / safe.width, .7f) && Near((resolved.center.y - safe.y) / safe.height, .6f),
                "Orientation/aspect changes preserve normalized center");
            Contained(resolved, safe, "Rotated bounds remain visible");
        }
        Check(MobileControlLayoutStore.Serialize(layout) == beforeOrientation, "Viewport changes never rewrite saved values");
        Rect doubled = layout.ResolveRect(MobileControlId.Pickup, new Rect(0, 0, 96, 96), Safe, 2f);
        Check(Near(doubled.width, 88), "UI scale applies to touch minimum without changing multiplier");

        string beforeInvalid = MobileControlLayoutStore.Serialize(layout);
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Check(!layout.Set(MobileControlId.Attack, new Vector2(invalid, 200), 1f, Button, Safe, 1f), "Reject invalid pointer x");
            Check(!layout.Set(MobileControlId.Attack, new Vector2(200, invalid), 1f, Button, Safe, 1f), "Reject invalid pointer y");
            Check(!layout.Set(MobileControlId.Attack, Safe.center, invalid, Button, Safe, 1f), "Reject invalid size");
            Check(!layout.SetNormalized(MobileControlId.Attack, invalid, .5f, 1f), "Reject nonfinite stored coordinate");
            Check(!layout.SetNormalized(MobileControlId.Attack, .5f, .5f, invalid), "Reject nonfinite stored scale");
            Check(!layout.Set(MobileControlId.Attack, Safe.center, 1f, Button, Safe, invalid), "Reject invalid UI scale edit");
            Contained(layout.ResolveRect(MobileControlId.Attack, Button, Safe, invalid), Safe, "Invalid UI scale resolves safely");
            Contained(layout.ResolveRect(MobileControlId.Pickup, new Rect(invalid, 0, 64, 64), Safe, 1f), Safe, "Invalid defaults resolve safely");
        }
        Check(!layout.Set((MobileControlId)999, Safe.center, 1f, Button, Safe, 1f), "Unknown runtime ID rejected");
        Check(!layout.Set(MobileControlId.Attack, Safe.center, 1f, Button, default(Rect), 1f), "Empty safe area cannot corrupt edits");
        Check(!layout.Set(MobileControlId.Attack, Safe.center, 1f, Button, Safe, 0f), "Zero UI scale cannot corrupt edits");
        Check(Same(layout.ResolveRect(MobileControlId.Attack, Button, default(Rect), 1f), default(Rect)), "Unavailable safe area returns empty finite rect");
        Check(MobileControlLayoutStore.Serialize(layout) == beforeInvalid, "All invalid samples preserve original values");

        MobileControlLayout copy = layout.Clone();
        Check(copy.Set(MobileControlId.Attack, new Vector2(100, 200), 1f, Button, Safe, 1f), "Edit copied values");
        Check(MobileControlLayoutStore.Serialize(layout) == beforeInvalid, "Clone has no mutable alias");
        copy.Clear();
        Check(copy.Count == 0 && layout.Count > 0 && Same(copy.ResolveRect(MobileControlId.Attack, Button, Safe, 1f), Button), "Reset only edits the clone and restores supplied defaults");

        string[] invalidFiles = {
            null, "", " ", "joystick=.5,.5,1", "version=0", "version=2", "version=1.0", "version=1\nversion=1",
            "version=1\nunknown=.5,.5,1", "version=1\nattack=.5,.5,1\nattack=.6,.6,1", "version=1\nAttack=.5,.5,1",
            "version=1\nattack=NaN,.5,1", "version=1\nattack=.5,Infinity,1", "version=1\nattack=.5,.5,-Infinity",
            "version=1\nattack=1e40,.5,1", "version=1\nattack=-.01,.5,1", "version=1\nattack=.5,1.01,1",
            "version=1\nattack=.5,.5,.64", "version=1\nattack=.5,.5,1.51", "version=1\nattack=.5,.5",
            "version=1\nattack=.5,.5,1,2", "version=1\nattack=.5;.5;1", "version=1\nattack", "version=1\npassword=secret",
            new string('a', MobileControlLayoutStore.MaxBytes + 1)
        };
        foreach (string invalid in invalidFiles) Reject(invalid);
        Check(MobileControlLayoutStore.Parse("version=1\n").Count == 0, "Explicit saved reset has no overrides");
        Check(MobileControlLayoutStore.Parse("\uFEFFversion = 1\r\nattack = .5,.6,1\r\n").Count == 1, "BOM/CRLF and ordinary whitespace accepted");
    }

    private static void Persistence(string directory)
    {
        string path = Path.Combine(directory, MobileControlLayoutStore.FileName);
        Check(MobileControlLayoutStore.Load(directory).Count == 0 && !Directory.Exists(directory), "Loading missing layout has no disk side effects");
        var layout = new MobileControlLayout();
        for (int index = 0; index < MobileControlLayout.ControlCount; index++)
            Check(layout.SetNormalized((MobileControlId)index, .1f + index * .05f, .25f, 1.25f), "Populate every stable physical ID");
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
        Check(MobileControlLayoutStore.TrySave(directory, layout, out string error) && error == null, "First save creates independent application-data file");
        string saved = File.ReadAllText(path);
        Check(saved.Contains("attack=0.15,0.25,1.25") && !saved.Contains("password"), "Serialization is invariant and contains only layout values");
        Check(MobileControlLayoutStore.Serialize(MobileControlLayoutStore.Load(directory)) == saved, "All IDs round-trip with invariant values");
        Check(MobileControlLayoutStore.Load(directory).Count == MobileControlLayout.ControlCount, "Every physical control has a persisted key");
        var ids = new[] { MobileControlId.Joystick, MobileControlId.Attack, MobileControlId.Pickup, MobileControlId.GroupWheel,
            MobileControlId.Skill0, MobileControlId.Skill1, MobileControlId.Skill2, MobileControlId.Skill3, MobileControlId.Skill4,
            MobileControlId.Skill5, MobileControlId.Mount, MobileControlId.HealthPotion, MobileControlId.ManaPotion, MobileControlId.Sit };
        string[] keys = { "joystick", "attack", "pickup", "groupwheel", "skill0", "skill1", "skill2", "skill3", "skill4", "skill5",
            "mount", "healthPotion", "manaPotion", "sit" };
        for (int index = 0; index < ids.Length; index++)
        {
            var one = new MobileControlLayout();
            one.SetNormalized(ids[index], .5f, .5f, 1f);
            Check(MobileControlLayoutStore.Serialize(one) == "version=1\n" + keys[index] + "=0.5,0.5,1\n",
                "Control identity is a stable named key, never a group or binding number");
        }
        Check(Directory.GetFiles(directory).Length == 1, "First save leaves no temporary file");
        File.WriteAllText(Path.Combine(directory, "display-settings.ini"), "fps=60\n");

        MobileControlLayout editing = layout.Clone();
        editing.Clear();
        Check(File.ReadAllText(path) == saved, "Resetting an edit copy never saves immediately");
        editing.SetNormalized(MobileControlId.Sit, .2f, .7f, .9f);
        Check(File.ReadAllText(path) == saved, "Moving after reset remains unsaved");
        editing = null;
        Check(MobileControlLayoutStore.Serialize(MobileControlLayoutStore.Load(directory)) == saved, "Discarding edit copy keeps saved layout after restart");

        var replacement = new MobileControlLayout();
        replacement.SetNormalized(MobileControlId.Attack, .6f, .7f, 1.3f);
        Check(MobileControlLayoutStore.TrySave(directory, replacement), "Second save atomically replaces existing file");
        Check(MobileControlLayoutStore.Load(directory).Count == 1 && File.ReadAllText(path) == MobileControlLayoutStore.Serialize(replacement), "Replacement is complete, never merged with stale slots");
        Check(File.ReadAllText(Path.Combine(directory, "display-settings.ini")) == "fps=60\n", "Other settings are untouched");
        Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "Replacement leaves no temporary residue");

        string replacementText = File.ReadAllText(path);
        Check(!MobileControlLayoutStore.TrySave(directory, null, out error) && !string.IsNullOrEmpty(error), "Invalid save reports an actionable failure");
        Check(File.ReadAllText(path) == replacementText, "Failed save leaves the previous complete layout unchanged");
        File.WriteAllText(path + ".interrupted.tmp", "version=1\nattack=");
        Check(MobileControlLayoutStore.Serialize(MobileControlLayoutStore.Load(directory)) == replacementText, "Interrupted staged file is never loaded");
        File.Delete(path + ".interrupted.tmp");

        foreach (string bad in new[] { "version=999\nattack=.5,.5,1", "version=1\nattack=.5,.5,1\nattack=.2,.2,1", "version=1\nattack=NaN,.5,1", new string('x', MobileControlLayoutStore.MaxBytes + 1) })
        {
            File.WriteAllText(path, bad);
            Check(MobileControlLayoutStore.Load(directory).Count == 0, "Invalid disk layout falls back as a whole");
            Check(File.ReadAllText(path) == bad, "Fallback preserves invalid file for recovery until explicit Save");
        }
        File.WriteAllBytes(path, new byte[] { 0xff, 0xfe, 0x00 });
        Check(MobileControlLayoutStore.Load(directory).Count == 0 && new FileInfo(path).Length == 3, "Invalid UTF-8 falls back without rewriting");
        Check(MobileControlLayoutStore.TrySave(directory, new MobileControlLayout()) && File.ReadAllText(path) == "version=1\n", "Explicit saved reset replaces damaged file safely");
        Check(MobileControlLayoutStore.Load(directory).Count == 0, "Saved reset survives restart");

        string notDirectory = Path.Combine(directory, "occupied");
        File.WriteAllText(notDirectory, "preserve");
        Check(!MobileControlLayoutStore.TrySave(notDirectory, layout), "File at requested directory safely fails");
        Check(File.ReadAllText(notDirectory) == "preserve", "Failed directory setup preserves existing data");
        Check(MobileControlLayoutStore.Load(notDirectory).Count == 0, "Unreadable path falls back");
        string blockedDirectory = Path.Combine(directory, "blocked-target");
        string blockedTarget = Path.Combine(blockedDirectory, MobileControlLayoutStore.FileName);
        Directory.CreateDirectory(blockedTarget);
        File.WriteAllText(Path.Combine(blockedTarget, "preserve"), "keep");
        Check(!MobileControlLayoutStore.TrySave(blockedDirectory, layout), "Non-file target fails without destructive overwrite");
        Check(File.ReadAllText(Path.Combine(blockedTarget, "preserve")) == "keep" && Directory.GetFiles(blockedDirectory).Length == 0,
            "Failed final atomic move preserves target and cleans staging");
        foreach (string invalidDirectory in new[] { null, "", "   ", "invalid\0directory" })
        {
            Check(!MobileControlLayoutStore.TrySave(invalidDirectory, layout), "Invalid directory safely rejects save");
            Check(MobileControlLayoutStore.Load(invalidDirectory).Count == 0, "Invalid directory safely falls back");
        }
    }
}
