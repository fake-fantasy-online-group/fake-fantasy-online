using System;
using System.IO;
using System.Text;

namespace FakeFantasyOnline.Editor
{
/// <summary>
/// Moves resource directories, without enumerating their contents, out of Unity's import tree.
/// Original bytes remain in a durable backup until restoration, including after a failed build.
/// </summary>
public sealed class ResourceFreeBuildStage : IDisposable
{
    private readonly DirectoryStage streamingAssets;
    private readonly DirectoryStage resources;

    public ResourceFreeBuildStage(string assetsRoot, string backupRoot, string configName, string configJson)
    {
        if (string.IsNullOrEmpty(configName) || Path.GetFileName(configName) != configName)
            throw new ArgumentException("Bootstrap config must be a file name.", nameof(configName));
        if (string.IsNullOrWhiteSpace(configJson))
            throw new ArgumentException("Bootstrap config must not be empty.", nameof(configJson));
        assetsRoot = Path.GetFullPath(assetsRoot);
        backupRoot = Path.GetFullPath(backupRoot);
        if (backupRoot == assetsRoot || backupRoot.StartsWith(assetsRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new IOException("Resource backup must be outside Assets.");
        // Check every destination before moving anything, including interrupted builds.
        DirectoryStage.Validate(Path.Combine(assetsRoot, "StreamingAssets"), Path.Combine(backupRoot, "StreamingAssets"));
        DirectoryStage.Validate(Path.Combine(assetsRoot, "Resources"), Path.Combine(backupRoot, "Resources"));
        try
        {
            streamingAssets = new DirectoryStage(Path.Combine(assetsRoot, "StreamingAssets"), Path.Combine(backupRoot, "StreamingAssets"));
            resources = new DirectoryStage(Path.Combine(assetsRoot, "Resources"), Path.Combine(backupRoot, "Resources"));
            File.WriteAllText(Path.Combine(assetsRoot, "StreamingAssets", configName), configJson, new UTF8Encoding(false));
        }
        catch { Dispose(); throw; }
    }

    public void Dispose()
    {
        try { resources?.Dispose(); }
        finally { streamingAssets?.Dispose(); }
    }

    private sealed class DirectoryStage : IDisposable
    {
        private readonly string root;
        private readonly string backup;
        private readonly bool existed;
        private bool active;

        internal static void Validate(string root, string backup)
        {
            if (Directory.Exists(backup) || File.Exists(backup))
                throw new IOException("Interrupted resource-free build: restore " + backup + " to " + root + " before retrying; do not delete original resources.");
            if (File.Exists(root) || (Directory.Exists(root) && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0))
                throw new IOException("Resource staging requires real directories, not files or symbolic links: " + root);
        }

        internal DirectoryStage(string root, string backup)
        {
            this.root = root;
            this.backup = backup;
            existed = Directory.Exists(root);
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (existed) Directory.Move(root, backup);
            active = true;
            try { Directory.CreateDirectory(root); }
            catch { Dispose(); throw; }
        }

        public void Dispose()
        {
            if (!active) return;
            // Keep only the tiny generated stage for recovery/inspection; never recursively
            // delete a source tree, or traverse any local-resource symlink children.
            if (Directory.Exists(root))
                Directory.Move(root, backup + ".generated-" + Guid.NewGuid().ToString("N"));
            if (existed) Directory.Move(backup, root);
            active = false;
        }
    }
}
}
