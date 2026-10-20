using System;
using System.IO;
using System.Reflection;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class MobileComfortableLayoutContract
{
    private static int checks;
    private static readonly MobileControlId[] Captured = {
        MobileControlId.Attack, MobileControlId.Pickup, MobileControlId.GroupWheel,
        MobileControlId.Skill0, MobileControlId.Skill1, MobileControlId.Skill2,
        MobileControlId.Skill3, MobileControlId.Skill4, MobileControlId.Skill5,
        MobileControlId.HealthPotion, MobileControlId.ManaPotion
    };
    // Independently recorded safe-area centers after the three midpoint alignments.
    private static readonly float[,] Phone = {
        { .910651147f, .796296300f, 96f, 1.3344183f },
        { .758417400f, .740722300f, 48f, 1f },
        { .821482000f, .624948400f, 48f, 1f },
        { .752339400f, .862527460f, 64f, 1f },
        { .815175831f, .862527460f, 64f, 1f },
        { .818847358f, .734838963f, 64f, 1f },
        { .878643632f, .614132970f, 64f, 1f },
        { .939076543f, .487941116f, 64f, 1f },
        { .943750100f, .614132970f, 64f, 1f },
        { .814712048f, .474373043f, 52f, 1f },
        { .869944200f, .474373043f, 52f, 1f }
    };
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    private static bool Near(float a, float b) => Math.Abs(a - b) < .002f;
    private static bool Same(Rect a, Rect b) => Near(a.x, b.x) && Near(a.y, b.y) && Near(a.width, b.width) && Near(a.height, b.height);
    private static bool Within(Rect a, Rect b) => a.x >= b.x - .002f && a.y >= b.y - .002f && a.xMax <= b.xMax + .002f && a.yMax <= b.yMax + .002f;
    private static bool Overlap(Rect a, Rect b) => a.x < b.xMax - .002f && a.xMax > b.x + .002f && a.y < b.yMax - .002f && a.yMax > b.y + .002f;
    private static void Reload()
    {
        MobileClientPresentation.CancelLayoutEdit();
        typeof(MobileClientPresentation).GetField("savedControlLayout", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
        Time.frameCount++;
    }
    private static void SetPhone()
    {
        Screen.width = 2616; Screen.height = 1212;
        Screen.safeArea = new Rect(104, 0, 2512, 1212);
        UIScale.User = 1f;
        MobileClientPresentation.ChatExpanded = false;
    }
    private static void PhoneDefaults()
    {
        var safe = MobileClientPresentation.SafeAreaGui;
        float scale = 1212f / 540f;
        for (int i = 0; i < Captured.Length; i++)
        {
            Rect actual = MobileClientPresentation.ResolveControlRect(Captured[i]);
            Check(Near(actual.center.x, safe.x + Phone[i, 0] * safe.width) && Near(actual.center.y, Phone[i, 1] * safe.height), "Captured phone center " + Captured[i]);
            Check(Near(actual.width, Phone[i, 2] * Phone[i, 3] * scale) && Near(actual.height, actual.width), "Captured phone touch size " + Captured[i]);
        }
        Rect joystick = MobileClientPresentation.JoystickRect;
        Check(Near(joystick.center.x, 104f + 100f * scale) && Near(joystick.center.y, 1212f - 110f * scale) && Near(joystick.width, 160f * scale), "Joystick retains original center and touch size");
        Check(Near(MobileClientPresentation.SkillRect(0).center.y, MobileClientPresentation.SkillRect(1).center.y), "Lower skill row aligned");
        Check(Near(MobileClientPresentation.SkillRect(3).center.y, MobileClientPresentation.SkillRect(5).center.y), "Upper skill row aligned");
        Check(Near(MobileClientPresentation.HealthPotionRect.center.y, MobileClientPresentation.ManaPotionRect.center.y), "Potion row aligned");
    }
    public static void Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mobile-comfortable-" + Guid.NewGuid().ToString("N"));
        Application.persistentDataPath = directory;
        try
        {
            SetPhone();
            Check(!MobileClientPresentation.Enabled, "Desktop remains disabled without explicit simulation");
            Check(!MobileClientPresentation.IsPointerOverUi(new Vector2(2392, 247)) && !MobileClientPresentation.GameplayInputBlocked, "Desktop input gates unchanged");
            Check(!Directory.Exists(directory), "Desktop checks do not load or write mobile layout");
            Application.isMobilePlatform = true;
            PhoneDefaults();
            Check(!Directory.Exists(directory), "Default resolution never creates a preferences file");
            Check(MobileControlLayoutStore.Load(directory).Count == 0, "New install has no synthetic overrides");

            // First drag must retain the larger attack size and not multiply it again.
            Rect attack = MobileClientPresentation.AttackRect;
            MobileClientPresentation.BeginLayoutEdit();
            MobileClientPresentation.SetControlCenter(MobileControlId.Attack, new Vector2(1900, 800));
            Check(Near(MobileClientPresentation.AttackRect.width, attack.width), "First drag retains default attack multiplier");
            MobileClientPresentation.CancelLayoutEdit();
            Check(Same(MobileClientPresentation.AttackRect, attack), "Cancel first drag restores default");

            // Missing controls use defaults, while every explicit v1 center/scale keeps
            // its previous meaning, including 1x and the original phone's 1.3344183x.
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, MobileControlLayoutStore.FileName);
            foreach (bool compact in new[] { false, true })
            foreach (float multiplier in new[] { .65f, 1f, 1.3344183f, 1.5f })
            {
                SetPhone();
                if (compact) { Screen.width = 940; Screen.height = 540; Screen.safeArea = new Rect(0, 0, 940, 540); UIScale.User = 2f; }
                var custom = new MobileControlLayout();
                foreach (MobileControlId id in Enum.GetValues(typeof(MobileControlId)))
                    custom.SetNormalized(id, .53f, .58f, multiplier);
                string saved = MobileControlLayoutStore.Serialize(custom);
                File.WriteAllText(path, saved);
                Reload();
                foreach (MobileControlId id in Enum.GetValues(typeof(MobileControlId)))
                {
                    Rect baseline = MobileClientPresentation.DefaultControlRect(id);
                    Rect expected = custom.ResolveRect(id, baseline, MobileClientPresentation.SafeAreaGui, MobileClientPresentation.Scale);
                    Check(Same(MobileClientPresentation.ResolveControlRect(id), expected), "Saved v1 placement bypasses new fallback " + id + " / " + multiplier);
                }
                Check(File.ReadAllText(path) == saved, "Loading and resolving never rewrites custom config");
            }
            SetPhone();
            File.WriteAllText(path, "version=1\nattack=.53,.58,1\n");
            Reload();
            Check(Near(MobileClientPresentation.AttackRect.width, 96f * MobileClientPresentation.Scale), "Saved 1x attack is not multiplied by new default");
            Check(Near(MobileClientPresentation.PickupRect.center.x, 104f + .7584174f * 2512f), "Partial custom layout gets new defaults only for missing controls");
            string original = File.ReadAllText(path);
            MobileClientPresentation.BeginLayoutEdit();
            MobileClientPresentation.ResetLayoutEdit();
            PhoneDefaults();
            Check(File.ReadAllText(path) == original, "Reset preview does not write");
            MobileClientPresentation.CancelLayoutEdit();
            Check(Near(MobileClientPresentation.AttackRect.width, 96f * MobileClientPresentation.Scale) && File.ReadAllText(path) == original, "Cancel reset restores exact custom layout");
            MobileClientPresentation.BeginLayoutEdit();
            MobileClientPresentation.ResetLayoutEdit();
            Check(MobileClientPresentation.SaveLayoutEdit(out string error), "Explicit save commits reset: " + error);
            Check(File.ReadAllText(path) == "version=1\n", "Saved reset remains empty overrides, not a frozen default snapshot");
            Reload(); PhoneDefaults();

            // Existing adaptive scaling/safe-area path, no new orientation behavior.
            foreach (var dims in new[] { new[] { 2616, 1212 }, new[] { 2340, 1080 }, new[] { 1280, 720 }, new[] { 1920, 1200 }, new[] { 1024, 768 }, new[] { 812, 375 }, new[] { 640, 360 } })
            foreach (float preference in new[] { .6f, .8f, 1f, 1.2f, 1.5f, 2f })
            foreach (bool notch in new[] { false, true })
            {
                Screen.width = dims[0]; Screen.height = dims[1]; UIScale.User = preference;
                Screen.safeArea = notch ? new Rect(44, 10, dims[0] - 52, dims[1] - 34) : new Rect(0, 0, dims[0], dims[1]);
                Rect safe = MobileClientPresentation.SafeAreaGui; float scale = MobileClientPresentation.Scale;
                Check(safe.y == (notch ? 24f : 0f), "Safe-area bottom-left converted to GUI top-left");
                Check(MobileClientPresentation.DefaultControlScale(MobileControlId.Skill0) >= .8767f, "Current scale cap keeps compact fit in nonoverlapping range");
                for (int i = 0; i < MobileControlLayout.ControlCount; i++)
                {
                    var id = (MobileControlId)i;
                    Rect rect = MobileClientPresentation.ResolveControlRect(id);
                    Check(Within(rect, safe), "Default touch region inside safe area " + id);
                    Check(rect.width >= 44f * scale - .002f && rect.height >= 44f * scale - .002f, "Default retains minimum touch size " + id);
                    Check(!Overlap(rect, MobileClientPresentation.ChatRect) && !Overlap(rect, MobileClientPresentation.MinimapRect), "Default avoids chat/map " + id);
                    for (int j = 0; j < i; j++) Check(!Overlap(rect, MobileClientPresentation.ResolveControlRect((MobileControlId)j)), "Default touch regions remain disjoint " + id + "/" + j);
                }
                Rect chat = MobileClientPresentation.ChatRect;
                Check(Near(MobileClientPresentation.MountRect.center.x, chat.x - 36f * scale) && Near(MobileClientPresentation.MountRect.center.y, chat.y + 26f * scale), "Mount retains original default anchor");
                Check(Near(MobileClientPresentation.SitRect.center.x, chat.x - 36f * scale) && Near(MobileClientPresentation.SitRect.center.y, chat.y - 38f * scale), "Meditate retains original default anchor");
                Check(Near(MobileClientPresentation.JoystickRect.width, 160f * scale), "Joystick size unchanged by compact fit");
            }
            Screen.width = 940; Screen.height = 540; Screen.safeArea = new Rect(0, 0, 940, 540); UIScale.User = 2f;
            Check(Near(MobileClientPresentation.PickupRect.width, 44f) && Near(MobileClientPresentation.PageRect.width, 44f), "Compact pickup/wheel use actual 44-unit touch floor, not smaller fitted artwork");
            Check(Near(MobileClientPresentation.SkillRect(0).x - MobileClientPresentation.ChatRect.xMax, 8f), "Compact cluster keeps eight-unit chat gap");
            Check(File.ReadAllText(path) == "version=1\n", "Viewport changes never rewrite persisted defaults");
            var layout = new MobileControlLayout();
            Rect basis = new Rect(50, 50, 96, 96), bounds = new Rect(0, 0, 500, 500);
            foreach (float bad in new[] { float.NaN, float.PositiveInfinity, .1f, 2f })
                Check(Same(layout.ResolveRect(MobileControlId.Attack, basis, bounds, 1f, bad), basis), "Invalid fallback scale safely uses baseline");
            Console.WriteLine("PASS comfortable mobile defaults: " + checks + " assertions; production C# with geometry/application stubs, no Unity or device runtime.");
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
