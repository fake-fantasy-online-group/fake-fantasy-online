using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class LocalDisplayConfigContract
{
    private static int checks;
    private static string root;
    private static string ConfigPath => GameSettings.DisplayConfigPath;
    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }
    private static void Reset(bool mobile = false)
    {
        UnityEngine.Application.persistentDataPath = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(UnityEngine.Application.persistentDataPath);
        UnityEngine.Application.isMobilePlatform = mobile;
        UnityEngine.Application.targetFrameRate = -123;
        QualitySettings.vSyncCount = -123;
        PlayerPrefs.Values.Clear();
        PlayerPrefs.Writes = 0;
        Screen.Calls = 0;
        Screen.currentResolution = new Resolution { refreshRateRatio = new RefreshRate { value = 144 } };
        Display.main.systemWidth = 2560;
        Display.main.systemHeight = 1440;
        WindowCenter.Calls = 0;
        WindowCenter.MaxWidth = 2400;
        WindowCenter.MaxHeight = 1300;
        Debug.Warnings.Clear();
    }
    private static void Start(string text = null)
    {
        if (text != null) File.WriteAllText(ConfigPath, text);
        GameSettings.InitializeLocalDisplaySettings();
    }
    private static void ExpectRejected(string text)
    {
        bool threw = false;
        try { GameSettings.ParseDisplayConfig(text); }
        catch (InvalidDataException) { threw = true; }
        Check(threw, "Reject invalid config: " + (text == null ? "null" : text.Substring(0, Math.Min(60, text.Length))));
    }
    public static void Main()
    {
        root = Path.Combine(Path.GetTempPath(), "ffo-display-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var attribute = typeof(GameSettings).GetMethod("InitializeLocalDisplaySettings", BindingFlags.Static | BindingFlags.NonPublic)
                .GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Check(attribute != null && attribute.Type == RuntimeInitializeLoadType.AfterSceneLoad, "Startup applies after scene graphics setup");
            Reset();
            PlayerPrefs.Values["opt_weather"] = 0;
            PlayerPrefs.Values["unrelated"] = 765;
            Start();
            Check(GameSettings.FrameRateMode == 1 && UnityEngine.Application.targetFrameRate == 120, "Fresh desktop install retains 120 mode");
            Check(Screen.Calls == 0, "Fresh install preserves Player window");
            Check(!GameSettings.Weather && PlayerPrefs.Values["unrelated"] == 765 && PlayerPrefs.Writes == 0, "Loading does not modify other preferences");
            Check(File.Exists(ConfigPath), "Creates editable template");
            var template = GameSettings.ReadDisplayConfig(ConfigPath);
            Check(template.FrameRateMode == 1 && template.UseDefaultFrameRate && template.Width == 0 && template.Height == 0, "Template retains platform-default provenance and startup size");
            Check(new DirectoryInfo(UnityEngine.Application.persistentDataPath).GetFiles().Length == 1, "No temporary-file residue");

            foreach (bool mobile in new[] { false, true })
            foreach (double hz in new[] { 60d, 90d, 120d, 144d })
            {
                Reset(mobile);
                Screen.currentResolution = new Resolution { refreshRateRatio = new RefreshRate { value = hz } };
                Start();
                int expected = mobile ? 0 : 1;
                Check(GameSettings.DefaultFrameRateMode == expected && GameSettings.FrameRateMode == expected, "Fresh startup chooses platform default mode");
                Check(UnityEngine.Application.targetFrameRate == (mobile ? 60 : 120), "Mobile requests 60 independently of refresh rate; desktop keeps 120");
                Check(QualitySettings.vSyncCount == (int)Math.Max(1, Math.Min(4, Math.Round(hz / (mobile ? 60 : 120)))), "Historical vSync divisor retained");
                Check(Screen.Calls == 0 && WindowCenter.Calls == 0, "Frame default never lowers native resolution or alters startup window");
                Check(PlayerPrefs.Writes == 0, "Fresh defaults do not rewrite unrelated preferences");
                Check(File.ReadAllText(ConfigPath).Contains("\nfps=default\n"), "Fresh template marks generated defaults explicitly");
                template = GameSettings.ReadDisplayConfig(ConfigPath);
                Check(template.FrameRateMode == expected && template.UseDefaultFrameRate, "Generated default round-trips");
                GameSettings.ApplyRes(2);
                Check(GameSettings.ReadDisplayConfig(ConfigPath).UseDefaultFrameRate && File.ReadAllText(ConfigPath).Contains("\nfps=default\n"), "Resolution-only save preserves default marker");
                GameSettings.InitializeLocalDisplaySettings();
                Check(GameSettings.FrameRateMode == expected && GameSettings.ResIndex() == 2, "Reload retains platform default and saved resolution");
            }

            foreach (bool mobile in new[] { false, true })
            foreach (int legacy in new[] { -3, 0, 1, 2, 99 })
            {
                Reset(mobile);
                PlayerPrefs.Values["opt_fps"] = legacy;
                PlayerPrefs.Values["res_w"] = 1600;
                PlayerPrefs.Values["res_h"] = 900;
                Start();
                bool valid = legacy >= 0 && legacy <= 2;
                Check(GameSettings.FrameRateMode == (valid ? legacy : mobile ? 0 : 1), "Valid legacy fps migrates; invalid preference uses platform default");
                Check(GameSettings.ReadDisplayConfig(ConfigPath).UseDefaultFrameRate == !valid, "Legacy explicit mode is never relabeled as generated default");
                Check(Screen.Width == 1600 && Screen.Height == 900, "Valid legacy resolution migrates once file is missing");
                Check(PlayerPrefs.Writes == 0, "Legacy preferences are left recoverable");
            }
            Reset();
            PlayerPrefs.Values["res_w"] = -1;
            PlayerPrefs.Values["res_h"] = 99999;
            Start();
            Check(Screen.Calls == 0, "Invalid legacy resolution uses Player default");

            foreach (bool mobile in new[] { false, true })
            foreach (string fps in new[] { "60", "120", "refresh" })
            foreach (var resolution in GameSettings.ResOpts)
            {
                Reset(mobile);
                PlayerPrefs.Values["opt_fps"] = 2;
                PlayerPrefs.Values["res_w"] = 800;
                PlayerPrefs.Values["res_h"] = 600;
                Start("# comment\r\n; comment\r\nfps = " + fps + "\r\nresolution = " + resolution.Item1 + "x" + resolution.Item2 + "\r\n");
                int expected = fps == "60" ? 0 : fps == "120" ? 1 : 2;
                Check(GameSettings.FrameRateMode == expected, "Config wins over legacy fps");
                Check(!GameSettings.ReadDisplayConfig(ConfigPath).UseDefaultFrameRate, "Explicit file mode keeps provenance");
                Check(GameSettings.ResOpts[GameSettings.ResIndex()] == resolution, "Config wins over legacy resolution");
                bool native = resolution.Item1 >= 2560 || resolution.Item2 >= 1440;
                Check(Screen.Width == (native ? 2560 : resolution.Item1) && Screen.Height == (native ? 1440 : resolution.Item2), "Resolution constrained to display");
                Check(Screen.Mode == (native || mobile ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed), "Desktop window/fullscreen and mobile fullscreen modes");
            }

            foreach (bool mobile in new[] { false, true })
            {
                Reset(); UnityEngine.Application.isMobilePlatform = mobile;
                Start("fps=refresh\nresolution=native\n");
                Check(UnityEngine.Application.targetFrameRate == (mobile ? 144 : -1), "Refresh follows display on Android, vSync on desktop");
                Check(Screen.Width == 2560 && Screen.Height == 1440 && Screen.Mode == FullScreenMode.FullScreenWindow, "Native uses display dimensions");
                Reset(); UnityEngine.Application.isMobilePlatform = mobile;
                Start("fps=60\nresolution=1280x800\n");
                Check(UnityEngine.Application.targetFrameRate == 60, "60 target preserved across platforms");
                Check(Screen.Mode == (mobile ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed), "Mobile never requests windowed mode");
                Check(WindowCenter.Calls == (mobile ? 0 : 1), "Mobile does not consult desktop window sizing");
                Reset(mobile);
                string previousTemplate = "# Local display settings. Edit while the game is closed; restart to apply.\n" +
                    "# fps: 60, 120, refresh (display-paced modes, subject to hardware).\nfps=120\nresolution=default\n";
                Start(previousTemplate);
                Check(GameSettings.FrameRateMode == 1 && UnityEngine.Application.targetFrameRate == 120, "Ambiguous older generated 120 is preserved like an explicit choice");
                Check(File.ReadAllText(ConfigPath) == previousTemplate, "Old numeric config is not silently rewritten");

                foreach (string text in new[] { "", "# defaults only\n", "resolution=default\n", "fps=default\nresolution=default\n" })
                {
                    Reset(mobile); PlayerPrefs.Values["opt_fps"] = mobile ? 1 : 0;
                    Start(text);
                    Check(GameSettings.FrameRateMode == (mobile ? 0 : 1), "Missing fps/default key follows platform rather than stale preference");
                    Check(GameSettings.ReadDisplayConfig(ConfigPath).UseDefaultFrameRate && Screen.Calls == 0, "Default key preserves native startup size and default provenance");
                    Check(File.ReadAllText(ConfigPath) == text, "Loading an existing default config does not rewrite it");
                }

                Reset(mobile); PlayerPrefs.Values["opt_fps"] = 1;
                Start("fps=default\nresolution=default\n");
                Check(GameSettings.FrameRateMode == (mobile ? 0 : 1), "Explicit default reset overrides old preference");
                File.Delete(ConfigPath);
                GameSettings.InitializeLocalDisplaySettings();
                Check(GameSettings.FrameRateMode == 1 && !GameSettings.ReadDisplayConfig(ConfigPath).UseDefaultFrameRate, "Deleting config conservatively re-imports old preference rather than resetting it");
            }
            foreach (double hz in new[] { 59.94d, 60d, 90d, 120d, 144d, 240d })
            {
                Reset(true);
                Screen.currentResolution = new Resolution { refreshRateRatio = new RefreshRate { value = hz } };
                Start("fps=refresh\nresolution=default\n");
                Check(GameSettings.FrameRateMode == 2 && UnityEngine.Application.targetFrameRate == (int)Math.Round(hz), "Explicit refresh is not clamped by mobile default");
            }
            foreach (double hz in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0d, 24d })
            {
                Reset(); UnityEngine.Application.isMobilePlatform = true;
                Screen.currentResolution = new Resolution { refreshRateRatio = new RefreshRate { value = hz } };
                Start("fps=refresh\nresolution=default\n");
                Check(UnityEngine.Application.targetFrameRate == 60 && GameSettings.TargetFrameMs > 0 && !float.IsNaN(GameSettings.TargetFrameMs), "Invalid refresh reports recover safely");
            }
            Check(GameSettings.ParseDisplayConfig("\uFEFFfps=60\n").FrameRateMode == 0, "UTF-8 BOM accepted");
            Check(GameSettings.ParseDisplayConfig("").FrameRateMode == 0, "Absent keys default safely on mobile");
            foreach (string invalid in new[] { null, "fps=0", "fps=-1", "fps=144", "fps=NaN", "fps=DEFAULT", "fps=default\nfps=120", "fps=60\nfps=120", "resolution=0x0", "resolution=-1x600", "resolution=999999999x999999999", "resolution=1920X1080", "resolution=1920x1080\nresolution=native", "secret=abc", "[display]", "fps", "fps=60=120", "fps=60 # inline", new string('a', GameSettings.MaxDisplayConfigBytes + 1) }) ExpectRejected(invalid);
            foreach (bool mobile in new[] { false, true })
            {
                foreach (string invalid in new[] { "fps=bad\nresolution=1280x800", "fps=60\nresolution=bad", "fps=60\nunknown=secret-content" })
                {
                    Reset(mobile); PlayerPrefs.Values["opt_fps"] = mobile ? 1 : 0; Start(invalid);
                    Check(GameSettings.FrameRateMode == (mobile ? 0 : 1) && Screen.Calls == 0, "Invalid file resets whole display config to platform defaults, not stale prefs");
                    Check(UnityEngine.Application.targetFrameRate == (mobile ? 60 : 120), "Malformed file still applies platform frame budget");
                    Check(File.ReadAllText(ConfigPath) == invalid, "Invalid file stays available for correction");
                    Check(Debug.Warnings.Count == 1 && !Debug.Warnings[0].Contains("secret-content"), "Warning omits file contents");
                }
                Reset(mobile); File.WriteAllBytes(ConfigPath, new byte[] { 0xff, 0xfe, 0x00 }); Start();
                Check(GameSettings.FrameRateMode == (mobile ? 0 : 1) && Debug.Warnings.Count == 1, "Malformed UTF-8 safe platform fallback");
                Reset(mobile); File.WriteAllBytes(ConfigPath, new byte[GameSettings.MaxDisplayConfigBytes + 1]); Start();
                Check(GameSettings.FrameRateMode == (mobile ? 0 : 1) && Debug.Warnings.Count == 1, "Oversized disk file safe platform fallback");
                Reset(mobile); Directory.CreateDirectory(ConfigPath); Start();
                Check(GameSettings.FrameRateMode == (mobile ? 0 : 1) && Directory.Exists(ConfigPath), "Unwritable config path does not block startup or overwrite directory");
            }

            foreach (bool mobile in new[] { false, true })
            foreach (string fps in new[] { "default", "60", "120", "refresh" })
            {
                Reset(mobile); Start("fps=" + fps + "\nresolution=default\n");
                int expectedMode = GameSettings.FrameRateMode;
                int expectedTarget = UnityEngine.Application.targetFrameRate;
                GameSettings.ApplyRes(2);
                Check(File.ReadAllText(ConfigPath).Contains("\nfps=" + fps + "\n"), "Saving resolution preserves every fps mode");
                var roundTrip = GameSettings.ReadDisplayConfig(ConfigPath);
                Check(roundTrip.FrameRateMode == expectedMode && roundTrip.UseDefaultFrameRate == (fps == "default"), "File save/load preserves effective fps and provenance");
                File.Delete(ConfigPath);
                GameSettings.ApplyRes(3);
                Check(File.ReadAllText(ConfigPath).Contains("\nfps=" + fps + "\n"), "Recreating missing file during resolution save preserves current fps intent");
                GameSettings.InitializeLocalDisplaySettings();
                Check(GameSettings.FrameRateMode == expectedMode && UnityEngine.Application.targetFrameRate == expectedTarget, "Startup reload after save respects every fps mode");
            }

            Reset(); Start("fps=60\nresolution=default\n");
            string before = File.ReadAllText(ConfigPath);
            GameSettings.Weather = false; GameSettings.Save();
            Check(File.ReadAllText(ConfigPath) == before && PlayerPrefs.GetInt("opt_weather") == 0, "Saving unrelated settings never rewrites display file");
            Check(!PlayerPrefs.HasKey("opt_fps") && !PlayerPrefs.HasKey("res_w") && !PlayerPrefs.HasKey("res_h"), "No competing display PlayerPrefs writes");
            File.WriteAllText(ConfigPath, "fps=refresh\nresolution=native\n");
            GameSettings.ApplyRes(2);
            var saved = GameSettings.ReadDisplayConfig(ConfigPath);
            Check(saved.FrameRateMode == 2 && saved.Width == 1280 && saved.Height == 800, "Existing settings window saves same file and preserves newly edited fps");
            Check(GameSettings.FrameRateMode == 0 && GameSettings.ResIndex() == 2, "External fps changes require restart; current resolution updates immediately");
            GameSettings.InitializeLocalDisplaySettings();
            Check(GameSettings.FrameRateMode == 2 && Screen.Width == 1280, "Restart reloads both configured values");
            before = File.ReadAllText(ConfigPath);
            bool overwriteBlocked = false;
            try { GameSettings.WriteDisplayConfig(ConfigPath, new GameSettings.LocalDisplayConfig(), false); }
            catch (IOException) { overwriteBlocked = true; }
            Check(overwriteBlocked && File.ReadAllText(ConfigPath) == before, "Template creation never overwrites existing file");
            Check(new DirectoryInfo(UnityEngine.Application.persistentDataPath).GetFiles().Length == 1, "Failed create cleans up temp file");
            File.WriteAllText(ConfigPath, "fps=typo");
            GameSettings.ApplyRes(3);
            Check(File.ReadAllText(ConfigPath) == "fps=typo" && Screen.Width == 1600, "Invalid existing file retained, resolution still applies this session");
            int calls = Screen.Calls;
            GameSettings.ApplyRes(-1); GameSettings.ApplyRes(999); GameSettings.ApplyResolution(0, -1);
            Check(Screen.Calls == calls, "Invalid programmatic indices/dimensions ignored safely");
            Console.WriteLine("PASS local display configuration: " + checks + " checks; actual parser/file/startup/settings code with Unity APIs stubbed.");
        }
        finally { Directory.Delete(root, true); }
    }
}

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { AfterSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeLoadType Type;
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { Type = type; }
    }
    public static class Application { public static string persistentDataPath; public static bool isMobilePlatform; public static int targetFrameRate; }
    public enum FullScreenMode { Windowed, FullScreenWindow }
    public struct RefreshRate { public double value; }
    public struct Resolution { public RefreshRate refreshRateRatio; }
    public static class Screen
    {
        public static Resolution currentResolution;
        public static int Calls, Width, Height;
        public static FullScreenMode Mode;
        public static void SetResolution(int width, int height, FullScreenMode mode, int hz) { Calls++; Width = width; Height = height; Mode = mode; }
    }
    public sealed class Display { public static Display main = new Display(); public int systemWidth, systemHeight; }
    public static class QualitySettings { public static int vSyncCount; }
    public static class Mathf
    {
        public static int Clamp(int x, int min, int max) => Math.Max(min, Math.Min(max, x));
        public static float Max(float x, float y) => Math.Max(x, y);
        public static int RoundToInt(float x) => (int)Math.Round(x);
    }
    public static class PlayerPrefs
    {
        public static Dictionary<string, int> Values = new Dictionary<string, int>();
        public static int Writes;
        public static int GetInt(string key, int fallback = 0) => Values.TryGetValue(key, out var value) ? value : fallback;
        public static bool HasKey(string key) => Values.ContainsKey(key);
        public static void SetInt(string key, int value) { Writes++; Values[key] = value; }
        public static void Save() { }
    }
    public static class Debug { public static List<string> Warnings = new List<string>(); public static void LogWarning(string value) { Warnings.Add(value); } }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class WindowCenter
    {
        public static int Calls, MaxWidth, MaxHeight;
        public static bool TryGetMaxClient(out int width, out int height) { Calls++; width = MaxWidth; height = MaxHeight; return true; }
    }
}
