using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    /// <summary>
    /// An independent, bounded, non-secret layout file beneath the caller's application
    /// data directory. Loading, editing, cancelling and resetting never write preferences.
    /// </summary>
    internal static class MobileControlLayoutStore
    {
        internal const string FileName = "mobile-control-layout.cfg";
        internal const int Version = 1;
        internal const int MaxBytes = 4096;
        private static readonly string[] Keys = {
            "joystick", "attack", "pickup", "groupwheel",
            "skill0", "skill1", "skill2", "skill3", "skill4", "skill5",
            "mount", "healthPotion", "manaPotion", "sit"
        };

        internal static MobileControlLayout Load(string directory)
        {
            try
            {
                string path = LayoutPath(directory);
                if (!File.Exists(path)) return new MobileControlLayout();
                // Bound the read itself, even if an external process grows the file.
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] bytes = new byte[MaxBytes + 1];
                    int total = 0;
                    while (total < bytes.Length)
                    {
                        int read = stream.Read(bytes, total, bytes.Length - total);
                        if (read == 0) break;
                        total += read;
                    }
                    if (total > MaxBytes) return new MobileControlLayout();
                    return Parse(new UTF8Encoding(false, true).GetString(bytes, 0, total));
                }
            }
            catch (Exception exception) when (StorageFailure(exception))
            {
                // Leave the unreadable/invalid file intact. A later explicit Save may
                // replace it; Cancel and default fallback must not destroy user data.
                return new MobileControlLayout();
            }
        }

        internal static bool TrySave(string directory, MobileControlLayout layout) =>
            TrySave(directory, layout, out _);

        internal static bool TrySave(string directory, MobileControlLayout layout, out string error)
        {
            string temporary = null;
            error = null;
            try
            {
                byte[] bytes = new UTF8Encoding(false, true).GetBytes(Serialize(layout));
                string path = LayoutPath(directory);
                Directory.CreateDirectory(directory);
                temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                // Same-directory replacement matches the project's existing display and
                // update settings stores. Never delete/truncate the previous valid file.
                // If a platform cannot replace atomically, fail and retain edit mode.
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                temporary = null;
                return true;
            }
            catch (Exception exception) when (StorageFailure(exception))
            {
                // Do not expose paths, file contents or raw system exception messages.
                error = "布局保存失败，请检查存储空间后重试。";
                return false;
            }
            finally
            {
                if (temporary != null)
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    catch (Exception exception) when (StorageFailure(exception)) { }
                }
            }
        }

        internal static string Serialize(MobileControlLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            var text = new StringBuilder("version=1\n");
            for (int index = 0; index < Keys.Length; index++)
            {
                if (!layout.TryGet((MobileControlId)index, out MobileControlPlacement placement)) continue;
                text.Append(Keys[index]).Append('=')
                    .Append(placement.CenterX.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(placement.CenterY.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(placement.Scale.ToString("R", CultureInfo.InvariantCulture)).Append('\n');
            }
            return text.ToString();
        }

        internal static MobileControlLayout Parse(string text)
        {
            if (text == null || text.Length > MaxBytes) throw new InvalidDataException("Invalid layout size.");
            var layout = new MobileControlLayout();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            bool versionSeen = false;
            using (var reader = new StringReader(text.TrimStart('\uFEFF')))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0) continue;
                    int separator = line.IndexOf('=');
                    if (separator <= 0) throw new InvalidDataException("Invalid layout entry.");
                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    if (!seen.Add(key)) throw new InvalidDataException("Duplicate layout entry.");
                    if (key == "version")
                    {
                        if (value != "1") throw new InvalidDataException("Unsupported layout version.");
                        versionSeen = true;
                        continue;
                    }
                    int index = Array.IndexOf(Keys, key);
                    string[] parts = value.Split(',');
                    if (index < 0 || parts.Length != 3 ||
                        !TryFloat(parts[0], out float x) || !TryFloat(parts[1], out float y) ||
                        !TryFloat(parts[2], out float scale) ||
                        !layout.SetNormalized((MobileControlId)index, x, y, scale))
                        throw new InvalidDataException("Invalid layout placement.");
                }
            }
            if (!versionSeen) throw new InvalidDataException("Missing layout version.");
            return layout;
        }

        private static bool TryFloat(string value, out float result) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

        private static string LayoutPath(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Missing application data directory.");
            return Path.Combine(directory, FileName);
        }

        private static bool StorageFailure(Exception exception) =>
            exception is IOException || exception is InvalidDataException || exception is UnauthorizedAccessException ||
            exception is ArgumentException || exception is NotSupportedException || exception is SecurityException;
    }
}
