using System;
using System.Collections.Generic;
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
    // Unity includes nested Resources in every Player. Keep this reviewed source asset
    // outside the private root Assets/Resources that staging moves out of the import tree.
    public const string EmbeddedResourcesRelativePath = "FakeFantasyOnline/Runtime/Presentation/Resources";
    public const string EmbeddedBackgroundRelativePath =
        EmbeddedResourcesRelativePath + "/FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary.png";
    public const long MaxEmbeddedBackgroundBytes = 4 * 1024 * 1024;
    public const long MaxEmbeddedControlIconBytes = 512 * 1024;
    private static readonly HashSet<string> EmbeddedResourceDirectories = new HashSet<string>(StringComparer.Ordinal)
    {
        "FakeFantasyOnline", "FakeFantasyOnline/Bootstrap", "FakeFantasyOnline/Registration", "FakeFantasyOnline/MobileControls"
    };
    private static readonly HashSet<string> EmbeddedResourceFiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "FakeFantasyOnline.meta",
        "FakeFantasyOnline/ReferenceEffectGL.shader", "FakeFantasyOnline/ReferenceEffectGL.shader.meta",
        "FakeFantasyOnline/ReferenceIsoDepthY.shader", "FakeFantasyOnline/ReferenceIsoDepthY.shader.meta",
        "FakeFantasyOnline/ReferenceNameTag.shader", "FakeFantasyOnline/ReferenceNameTag.shader.meta",
        "FakeFantasyOnline/Bootstrap.meta",
        "FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary.png", "FakeFantasyOnline/Bootstrap/TaoistCloudSanctuary.png.meta",
        "FakeFantasyOnline/MobileControls.meta",
        "FakeFantasyOnline/MobileControls/Sit.png", "FakeFantasyOnline/MobileControls/Sit.png.meta",
        "FakeFantasyOnline/Registration.meta",
        "FakeFantasyOnline/Registration/WqyRegistrationSubset.txt", "FakeFantasyOnline/Registration/WqyRegistrationSubset.txt.meta",
        "FakeFantasyOnline/Registration/WqyRegistrationLicense.txt", "FakeFantasyOnline/Registration/WqyRegistrationLicense.txt.meta",
        "FakeFantasyOnline/Registration/WqyRegistrationNotice.txt", "FakeFantasyOnline/Registration/WqyRegistrationNotice.txt.meta"
    };
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
        ValidateEmbeddedBootstrapAssets(assetsRoot);
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

    public static void ValidateEmbeddedBootstrapAssets(string assetsRoot)
    {
        assetsRoot = Path.GetFullPath(assetsRoot);
        string source = assetsRoot;
        // Inspect only the exact, source-owned path. Never enumerate private resource trees.
        foreach (string segment in EmbeddedResourcesRelativePath.Split('/'))
        {
            source = Path.Combine(source, segment);
            if ((!File.Exists(source) && !Directory.Exists(source)) ||
                (File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Missing or linked source-owned Resources directory: " + source);
        }
        ValidateSourceResourceDirectory(source, "");
        foreach (string relative in EmbeddedResourceFiles)
        {
            long limit = relative == "FakeFantasyOnline/MobileControls/Sit.png" ? MaxEmbeddedControlIconBytes :
                relative.EndsWith(".png", StringComparison.Ordinal) ? MaxEmbeddedBackgroundBytes :
                relative.EndsWith(".meta", StringComparison.Ordinal) ? 16 * 1024 : 64 * 1024;
            RequireSmallRegularFile(Path.Combine(source, relative), limit);
        }
    }

    private static void ValidateSourceResourceDirectory(string root, string relative)
    {
        string directory = relative.Length == 0 ? root : Path.Combine(root, relative);
        foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
        {
            string name = relative.Length == 0 ? Path.GetFileName(entry) : relative + "/" + Path.GetFileName(entry);
            if ((File.GetAttributes(entry) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked source-owned resource is not allowed: " + entry);
            if (Directory.Exists(entry))
            {
                if (!EmbeddedResourceDirectories.Contains(name))
                    throw new IOException("Unreviewed source-owned Resources directory: " + entry);
                ValidateSourceResourceDirectory(root, name);
            }
            else if (!EmbeddedResourceFiles.Contains(name))
                throw new IOException("Unreviewed source-owned Resources file: " + entry);
        }
    }

    private static void RequireSmallRegularFile(string path, long limit)
    {
        if (!File.Exists(path) || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Missing or linked embedded startup file: " + path);
        long length = new FileInfo(path).Length;
        if (length <= 0 || length > limit)
            throw new IOException("Embedded startup file exceeds its reviewed size boundary: " + path);
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
