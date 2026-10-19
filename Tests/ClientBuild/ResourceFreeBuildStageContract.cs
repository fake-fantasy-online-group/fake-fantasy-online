using System;
using System.IO;
using System.Runtime.InteropServices;
using FakeFantasyOnline.Editor;

public static class ResourceFreeBuildStageContract
{
    [DllImport("libc", EntryPoint = "symlink", SetLastError = true)]
    private static extern int Symlink(string target, string link);
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }
    private static void ExpectFailure(Action action, string message)
    {
        try { action(); }
        catch (IOException) { assertions++; return; }
        throw new Exception(message);
    }
    private static string CreateEmbeddedBackground(string assets)
    {
        string image = Path.Combine(assets, ResourceFreeBuildStage.EmbeddedBackgroundRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(image));
        File.WriteAllText(image, "source-owned-artwork-fixture");
        File.WriteAllText(image + ".meta", "source-owned-import-settings");
        string root = Path.Combine(assets, ResourceFreeBuildStage.EmbeddedResourcesRelativePath);
        foreach (string folder in new[] { "FakeFantasyOnline", "FakeFantasyOnline/Bootstrap", "FakeFantasyOnline/Registration", "FakeFantasyOnline/MobileControls" })
        {
            Directory.CreateDirectory(Path.Combine(root, folder));
            File.WriteAllText(Path.Combine(root, folder + ".meta"), "source-owned-folder-settings");
        }
        foreach (string relative in new[] { "ReferenceEffectGL.shader", "ReferenceIsoDepthY.shader", "ReferenceNameTag.shader",
            "Registration/WqyRegistrationSubset.txt", "Registration/WqyRegistrationLicense.txt", "Registration/WqyRegistrationNotice.txt", "MobileControls/Sit.png" })
        {
            File.WriteAllText(Path.Combine(root, "FakeFantasyOnline/" + relative), "source-owned-text-fixture");
            File.WriteAllText(Path.Combine(root, "FakeFantasyOnline/" + relative + ".meta"), "source-owned-import-settings");
        }
        return image;
    }
    public static void Main(string[] args)
    {
        if (args.Length == 1)
        {
            ResourceFreeBuildStage.ValidateEmbeddedBootstrapAssets(args[0]);
            Check(true, "The actual source-owned Resources tree satisfies the build whitelist");
        }
        string temp = Path.Combine(Path.GetTempPath(), "ffo-stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // The .NET test host needs its standard code-page provider registered; Unity's
            // Mono Editor supplies CP936 itself. No downloaded game table participates.
            var providerType = Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages", false);
            if (providerType != null)
            {
                var register = typeof(System.Text.Encoding).GetMethod("RegisterProvider");
                register.Invoke(null, new[] { providerType.GetProperty("Instance").GetValue(null, null) });
            }
            byte[] gbk = GbkBuildTable.Generate();
            Check(gbk.Length == 47880, "GBK table must have the exact runtime layout");
            UnityEngine.Resources.Table = gbk;
            Check(FakeFantasyOnline.Presentation.ReferenceAssets.Gbk.GetString(new byte[] { 0xd6, 0xd0, 0xce, 0xc4 }) == "中文", "Real runtime Gbk consumer must decode generated Chinese table");
            Check(FakeFantasyOnline.Presentation.ReferenceAssets.Gbk.GetString(new byte[] { 0xc4, 0xe3, 0xba, 0xc3 }) == "你好", "Generated table must decode another Chinese phrase");
            Check(FakeFantasyOnline.Presentation.ReferenceAssets.Gbk.GetString(new byte[] { 0x81, 0x40 }) == "丂", "First GBK extension maps correctly");
            string assets = Path.Combine(temp, "Assets");
            string backup = Path.Combine(temp, "Backup");
            string streaming = Path.Combine(assets, "StreamingAssets");
            string resources = Path.Combine(assets, "Resources");
            Directory.CreateDirectory(streaming);
            Directory.CreateDirectory(resources);
            string embedded = CreateEmbeddedBackground(assets);
            File.WriteAllText(Path.Combine(streaming, "private.pkg"), "original-private-resource");
            File.WriteAllText(Path.Combine(resources, "private.txt"), "original-resource");
            var stage = new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}");
            Check(Directory.GetFiles(streaming).Length == 1, "Only bootstrap marker must remain in StreamingAssets");
            Check(Directory.GetFiles(resources).Length == 0, "Resources must be isolated without a recursive file scan");
            Check(File.ReadAllText(embedded) == "source-owned-artwork-fixture", "Exact source-owned background must remain in Unity's import tree during resource-free builds");
            Check(File.Exists(embedded + ".meta"), "Embedded background import settings must remain available");
            Check(File.ReadAllText(Path.Combine(backup, "StreamingAssets/private.pkg")) == "original-private-resource", "Original StreamingAssets must remain recoverable");
            stage.Dispose();
            stage.Dispose();
            Check(File.ReadAllText(Path.Combine(streaming, "private.pkg")) == "original-private-resource", "StreamingAssets bytes must be restored");
            Check(File.ReadAllText(Path.Combine(resources, "private.txt")) == "original-resource", "Resources bytes must be restored");
            Check(!File.Exists(Path.Combine(streaming, "resource-bootstrap.json")), "Generated config must not pollute the restored source tree");
            Check(File.ReadAllText(embedded) == "source-owned-artwork-fixture", "Staging must not move or rewrite the embedded background");
            // Failure after setup follows the exact using/finally restoration path used by a build.
            try { using (new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}")) { throw new InvalidOperationException("build failed"); } }
            catch (InvalidOperationException) { }
            Check(File.Exists(Path.Combine(streaming, "private.pkg")), "Failed builds must restore source resources");
            Check(File.Exists(embedded), "Failed builds must retain the embedded background");
            string unreviewed = Path.Combine(Path.GetDirectoryName(embedded), "other.png");
            File.WriteAllText(unreviewed, "private-artwork-fixture");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Embedded whitelist must reject an extra image");
            Check(File.Exists(Path.Combine(streaming, "private.pkg")), "Reject an invalid embedded whitelist before moving any private resource");
            File.Delete(unreviewed);
            Directory.CreateDirectory(unreviewed);
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Embedded whitelist must reject an extra nested directory");
            Directory.Delete(unreviewed);
            string siblingArt = Path.Combine(assets, ResourceFreeBuildStage.EmbeddedResourcesRelativePath, "FakeFantasyOnline/unreviewed.png");
            File.WriteAllText(siblingArt, "private-sibling-artwork");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Source Resources must reject unreviewed artwork beside Bootstrap too");
            File.Delete(siblingArt);
            string siblingDirectory = Path.Combine(assets, ResourceFreeBuildStage.EmbeddedResourcesRelativePath, "Unreviewed");
            Directory.CreateDirectory(siblingDirectory);
            File.WriteAllText(Path.Combine(siblingDirectory, "private.png"), "private-nested-artwork");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Source Resources must reject unknown directory trees without traversing their contents");
            Directory.Delete(siblingDirectory, true);
            string glyphs = Path.Combine(assets, ResourceFreeBuildStage.EmbeddedResourcesRelativePath, "FakeFantasyOnline/Registration/WqyRegistrationSubset.txt");
            Check(File.ReadAllText(glyphs) == "source-owned-text-fixture", "Minimal registration glyph source and license stay in the Player import tree");
            string sitIcon = Path.Combine(assets, ResourceFreeBuildStage.EmbeddedResourcesRelativePath, "FakeFantasyOnline/MobileControls/Sit.png");
            Check(File.Exists(sitIcon), "Approved sit icon stays in the Player import tree");
            File.Delete(sitIcon);
            ExpectFailure(() => ResourceFreeBuildStage.ValidateEmbeddedBootstrapAssets(assets), "Missing sit icon must fail closed");
            File.WriteAllBytes(sitIcon, new byte[ResourceFreeBuildStage.MaxEmbeddedControlIconBytes + 1]);
            ExpectFailure(() => ResourceFreeBuildStage.ValidateEmbeddedBootstrapAssets(assets), "Control icon must respect its smaller size limit");
            File.WriteAllText(sitIcon, "source-owned-text-fixture");
            string otherIcon = Path.Combine(Path.GetDirectoryName(sitIcon), "unreviewed.png");
            File.WriteAllText(otherIcon, "not-approved");
            ExpectFailure(() => ResourceFreeBuildStage.ValidateEmbeddedBootstrapAssets(assets), "Adding sit icon must not permit other control artwork");
            File.Delete(otherIcon);
            File.Delete(embedded + ".meta");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Missing import settings must fail before staging");
            CreateEmbeddedBackground(assets);
            using (var image = new FileStream(embedded, FileMode.Open, FileAccess.Write))
                image.SetLength(ResourceFreeBuildStage.MaxEmbeddedBackgroundBytes + 1);
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Oversized embedded background must fail closed");
            File.WriteAllText(embedded, "");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Empty embedded background must fail closed");
            File.Delete(embedded);
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Missing embedded background must fail closed");
            Check(File.Exists(Path.Combine(streaming, "private.pkg")), "Missing background must not move or alter the private resource tree");
            CreateEmbeddedBackground(assets);
            if (Path.DirectorySeparatorChar == '/')
            {
                string target = Path.Combine(temp, "linked-artwork-fixture");
                File.WriteAllText(target, "unreviewed-external-artwork");
                File.Delete(embedded);
                Check(Symlink(target, embedded) == 0, "Create linked-artwork fixture");
                ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Symlinked startup artwork must be rejected");
                File.Delete(embedded);
                CreateEmbeddedBackground(assets);
                File.Delete(embedded + ".meta");
                Check(Symlink(target, embedded + ".meta") == 0, "Create linked-import-settings fixture");
                ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Symlinked import settings must be rejected");
                File.Delete(embedded + ".meta");
                CreateEmbeddedBackground(assets);
            }
            Directory.CreateDirectory(Path.Combine(backup, "Resources"));
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Interrupted backup must prevent staging");
            Check(File.Exists(Path.Combine(streaming, "private.pkg")), "Validate all backups before moving the first resource directory");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, Path.Combine(assets, "Backup"), "resource-bootstrap.json", "{}"), "Never stage resources inside Assets");
            string clean = Path.Combine(temp, "CleanAssets");
            string cleanBackground = CreateEmbeddedBackground(clean);
            using (new ResourceFreeBuildStage(clean, Path.Combine(temp, "CleanBackup"), "resource-bootstrap.json", "{}"))
            {
                Check(File.Exists(Path.Combine(clean, "StreamingAssets/resource-bootstrap.json")), "Clean checkout with no private resource directories must work");
                Check(File.Exists(cleanBackground), "Resource-free first startup must retain the same bundled background");
            }
            Check(!Directory.Exists(Path.Combine(clean, "Resources")), "Absent Resources must remain absent after restoration");
            Check(!Directory.Exists(Path.Combine(clean, "StreamingAssets")), "Absent StreamingAssets must remain absent after restoration");
            // A failed restore must keep original bytes, restore the other directory, and
            // allow a retry once the external obstruction is removed.
            string restoreAssets = Path.Combine(temp, "RestoreAssets");
            string restoreBackup = Path.Combine(temp, "RestoreBackup");
            CreateEmbeddedBackground(restoreAssets);
            Directory.CreateDirectory(Path.Combine(restoreAssets, "Resources"));
            Directory.CreateDirectory(Path.Combine(restoreAssets, "StreamingAssets"));
            File.WriteAllText(Path.Combine(restoreAssets, "Resources/private.txt"), "restore-original");
            File.WriteAllText(Path.Combine(restoreAssets, "StreamingAssets/private.pkg"), "restore-streaming");
            var obstructed = new ResourceFreeBuildStage(restoreAssets, restoreBackup, "resource-bootstrap.json", "{}");
            string blockedRoot = Path.Combine(restoreAssets, "Resources");
            Directory.Delete(blockedRoot);
            File.WriteAllText(blockedRoot, "external-obstruction");
            ExpectFailure(() => obstructed.Dispose(), "A restore obstruction must be reported");
            Check(File.ReadAllText(Path.Combine(restoreBackup, "Resources/private.txt")) == "restore-original", "Restore failure must retain recoverable original resource bytes");
            Check(File.ReadAllText(Path.Combine(restoreAssets, "StreamingAssets/private.pkg")) == "restore-streaming", "Restore failure in Resources must still restore StreamingAssets");
            File.Delete(blockedRoot);
            obstructed.Dispose();
            Check(File.ReadAllText(Path.Combine(restoreAssets, "Resources/private.txt")) == "restore-original", "Dispose can retry the failed restore after removing an obstruction");
            Console.WriteLine("PASS resource-free staging contract: " + assertions + " assertions (no Unity build)");
        }
        finally { Directory.Delete(temp, true); }
    }
}

// Engine-free adapter for the unchanged Gbk consumer; this is not a Unity runtime test.
namespace UnityEngine
{
    public sealed class TextAsset { public byte[] bytes; }
    public static class Resources
    {
        public static byte[] Table;
        public static T Load<T>(string path) where T : class
        { return path == "gbk_table" ? new TextAsset { bytes = Table } as T : null; }
    }
    public static class Debug { public static void LogWarning(string message) { throw new Exception(message); } }
}
