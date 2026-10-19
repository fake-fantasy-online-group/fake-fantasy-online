using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class GameSettings
{
    internal const string DisplayConfigFileName = "display-settings.ini";
    internal const int MaxDisplayConfigBytes = 16384;
    private static LocalDisplayConfig displayConfig = new LocalDisplayConfig();

    internal sealed class LocalDisplayConfig
    {
        internal int FrameRateMode = DefaultFrameRateMode;
        // Persist "default" rather than a generated number so future defaults never
        // have to guess whether a numeric value was selected by the user.
        internal bool UseDefaultFrameRate = true;
        // Zero preserves the Player's startup window; -1 requests the native display size.
        internal int Width;
        internal int Height;
    }

    internal static string DisplayConfigPath => Path.Combine(UnityEngine.Application.persistentDataPath, DisplayConfigFileName);

    // AfterSceneLoad avoids changing Android's render target during early graphics setup.
    // This also runs for the resource-free startup scene, before GameBootstrap exists.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    internal static void InitializeLocalDisplaySettings()
    {
        loaded = false; // Also reload when entering Play Mode with domain reload disabled.
        EnsureLoaded();
        ApplyFrameRate();
        if (displayConfig.Width == -1)
        {
            int width = Display.main.systemWidth;
            int height = Display.main.systemHeight;
            if (width > 0 && height > 0)
                Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow, 0);
        }
        else if (displayConfig.Width > 0)
        {
            ApplyResolution(displayConfig.Width, displayConfig.Height);
        }
    }

    private static void LoadLocalDisplaySettings()
    {
        displayConfig = new LocalDisplayConfig();
        try
        {
            string path = DisplayConfigPath;
            if (File.Exists(path))
            {
                // Once present, even an invalid file never falls back to stale PlayerPrefs.
                displayConfig = ReadDisplayConfig(path);
            }
            else
            {
                // Existing numeric preferences may have been deliberately selected.
                // Preserve them; only a missing/invalid preference uses platform defaults.
                int legacyFps = PlayerPrefs.GetInt("opt_fps", -1);
                if (legacyFps >= 0 && legacyFps <= 2)
                {
                    displayConfig.FrameRateMode = legacyFps;
                    displayConfig.UseDefaultFrameRate = false;
                }
                if (PlayerPrefs.HasKey("res_w") && PlayerPrefs.HasKey("res_h"))
                {
                    int width = PlayerPrefs.GetInt("res_w");
                    int height = PlayerPrefs.GetInt("res_h");
                    if (IsDisplayResolutionSupported(width, height))
                    {
                        displayConfig.Width = width;
                        displayConfig.Height = height;
                    }
                }
                WriteDisplayConfig(path, displayConfig, false);
            }
        }
        catch (Exception exception) when (IsDisplayConfigFailure(exception))
        {
            // Never log raw file contents, exception messages or user supplied values.
            Debug.LogWarning("Display configuration could not be read or created; using safe display settings. See display-settings.ini.");
        }
        FrameRateMode = displayConfig.FrameRateMode;
    }

    internal static LocalDisplayConfig ParseDisplayConfig(string text)
    {
        if (text == null || text.Length > MaxDisplayConfigBytes)
            throw new InvalidDataException("Invalid display configuration size.");
        var result = new LocalDisplayConfig();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        using (var reader = new StringReader(text.TrimStart('\uFEFF')))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal)) continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) throw new InvalidDataException("Expected a display setting.");
                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                if (!seen.Add(key)) throw new InvalidDataException("Duplicate display setting.");
                if (key == "fps")
                {
                    if (value == "default") continue;
                    if (value == "60") result.FrameRateMode = 0;
                    else if (value == "120") result.FrameRateMode = 1;
                    else if (value == "refresh") result.FrameRateMode = 2;
                    else throw new InvalidDataException("Unsupported frame rate.");
                    result.UseDefaultFrameRate = false;
                }
                else if (key == "resolution")
                {
                    if (value == "default") continue;
                    if (value == "native") { result.Width = result.Height = -1; continue; }
                    bool found = false;
                    foreach (var option in ResOpts)
                    {
                        if (value != option.Item1 + "x" + option.Item2) continue;
                        result.Width = option.Item1;
                        result.Height = option.Item2;
                        found = true;
                        break;
                    }
                    if (!found) throw new InvalidDataException("Unsupported resolution.");
                }
                else throw new InvalidDataException("Unknown display setting.");
            }
        }
        return result;
    }

    internal static LocalDisplayConfig ReadDisplayConfig(string path)
    {
        // Bounded even if another process grows the file while it is being read.
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            byte[] bytes = new byte[MaxDisplayConfigBytes + 1];
            int total = 0;
            while (total < bytes.Length)
            {
                int read = stream.Read(bytes, total, bytes.Length - total);
                if (read == 0) break;
                total += read;
            }
            if (total > MaxDisplayConfigBytes) throw new InvalidDataException("Display configuration too large.");
            return ParseDisplayConfig(new UTF8Encoding(false, true).GetString(bytes, 0, total));
        }
    }

    internal static void WriteDisplayConfig(string path, LocalDisplayConfig settings, bool replaceExisting)
    {
        string fps = settings.UseDefaultFrameRate ? "default" :
            settings.FrameRateMode == 0 ? "60" : settings.FrameRateMode == 2 ? "refresh" : "120";
        string resolution = settings.Width == -1 ? "native" : settings.Width == 0 ? "default" : settings.Width + "x" + settings.Height;
        string text = "# Local display settings. Edit while the game is closed; restart to apply.\n" +
            "# fps: default, 60, 120, refresh (subject to hardware).\n" +
            "# fps=default follows platform defaults: mobile 60, desktop 120.\n" +
            "# resolution: default, native, 800x600, 1024x768, 1280x800, 1600x900,\n" +
            "#             1920x1080, 2560x1440, 3840x2160.\n" +
            "# default keeps the Player startup window; native uses the display size.\n" +
            "# This file contains display settings only. Do not put credentials here.\n" +
            "fps=" + fps + "\nresolution=" + resolution + "\n";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(text);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            // Do not truncate an existing file. A failed atomic replace leaves it intact.
            if (replaceExisting && File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static void SaveLocalDisplayResolution(int width, int height)
    {
        try
        {
            string path = DisplayConfigPath;
            // Preserve the file's fps value, including an edit made since startup.
            var next = File.Exists(path) ? ReadDisplayConfig(path) : new LocalDisplayConfig
            {
                FrameRateMode = FrameRateMode,
                UseDefaultFrameRate = displayConfig.UseDefaultFrameRate && FrameRateMode == DefaultFrameRateMode
            };
            next.Width = width;
            next.Height = height;
            WriteDisplayConfig(path, next, true);
        }
        catch (Exception exception) when (IsDisplayConfigFailure(exception))
        {
            Debug.LogWarning("Resolution applied for this session, but display-settings.ini could not be saved. Existing file left unchanged.");
        }
        displayConfig.Width = width;
        displayConfig.Height = height;
    }

    private static bool IsDisplayConfigFailure(Exception exception) =>
        exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException ||
        exception is ArgumentException || exception is NotSupportedException ||
        exception is System.Security.SecurityException;

    private static bool IsDisplayResolutionSupported(int width, int height) =>
        Array.Exists(ResOpts, option => option.Item1 == width && option.Item2 == height);
}
}
