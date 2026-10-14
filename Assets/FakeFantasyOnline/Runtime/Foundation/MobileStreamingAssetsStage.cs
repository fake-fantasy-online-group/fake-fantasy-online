using System;
using System.IO;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Temporarily exposes only an embedded manifest, preserving original resource directories.</summary>
public sealed class MobileStreamingAssetsStage : IDisposable
{
    private readonly string root;
    private readonly string backup;
    private bool moved;
    public MobileStreamingAssetsStage(string root, string backup, string manifest)
    {
        this.root = Path.GetFullPath(root);
        this.backup = Path.GetFullPath(backup);
        if (Directory.Exists(this.backup) || File.Exists(this.backup))
            throw new IOException("Previous mobile build left resources at " + this.backup +
                ". Restore that directory to " + this.root + " before retrying; do not delete original resources.");
        if (!Directory.Exists(this.root) || (File.GetAttributes(this.root) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("StreamingAssets must be an existing real directory; children may reference local resources.");
        if (this.backup.StartsWith(this.root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new IOException("Resource backup must be outside StreamingAssets.");
        MobileResourceManifest.Parse(manifest);
        Directory.CreateDirectory(Path.GetDirectoryName(this.backup));
        Directory.Move(this.root, this.backup);
        moved = true;
        try
        {
            Directory.CreateDirectory(this.root);
            File.WriteAllText(Path.Combine(this.root, MobileResourceManifest.FileName), manifest,
                new System.Text.UTF8Encoding(false));
        }
        catch { Dispose(); throw; }
    }
    public void Dispose()
    {
        if (!moved) return;
        // Retain Unity-generated metadata with the small temporary stage for inspection.
        // Never recursively delete a source directory or follow its symlink children.
        if (Directory.Exists(root))
            Directory.Move(root, backup + ".generated-" + Guid.NewGuid().ToString("N"));
        Directory.Move(backup, root);
        moved = false;
    }
}
}
