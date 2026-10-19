using System;
using System.IO;
using FakeFantasyOnline.Editor;

public static class ResourceFreeBuildStageContract
{
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
    public static void Main()
    {
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
            File.WriteAllText(Path.Combine(streaming, "private.pkg"), "original-private-resource");
            File.WriteAllText(Path.Combine(resources, "private.txt"), "original-resource");
            var stage = new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}");
            Check(Directory.GetFiles(streaming).Length == 1, "Only bootstrap marker must remain in StreamingAssets");
            Check(Directory.GetFiles(resources).Length == 0, "Resources must be isolated without a recursive file scan");
            Check(File.ReadAllText(Path.Combine(backup, "StreamingAssets/private.pkg")) == "original-private-resource", "Original StreamingAssets must remain recoverable");
            stage.Dispose();
            stage.Dispose();
            Check(File.ReadAllText(Path.Combine(streaming, "private.pkg")) == "original-private-resource", "StreamingAssets bytes must be restored");
            Check(File.ReadAllText(Path.Combine(resources, "private.txt")) == "original-resource", "Resources bytes must be restored");
            Check(!File.Exists(Path.Combine(streaming, "resource-bootstrap.json")), "Generated config must not pollute the restored source tree");
            // Failure after setup follows the exact using/finally restoration path used by a build.
            try { using (new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}")) { throw new InvalidOperationException("build failed"); } }
            catch (InvalidOperationException) { }
            Check(File.Exists(Path.Combine(streaming, "private.pkg")), "Failed builds must restore source resources");
            Directory.CreateDirectory(Path.Combine(backup, "Resources"));
            ExpectFailure(() => new ResourceFreeBuildStage(assets, backup, "resource-bootstrap.json", "{}"), "Interrupted backup must prevent staging");
            Check(File.Exists(Path.Combine(streaming, "private.pkg")), "Validate all backups before moving the first resource directory");
            ExpectFailure(() => new ResourceFreeBuildStage(assets, Path.Combine(assets, "Backup"), "resource-bootstrap.json", "{}"), "Never stage resources inside Assets");
            string clean = Path.Combine(temp, "CleanAssets");
            using (new ResourceFreeBuildStage(clean, Path.Combine(temp, "CleanBackup"), "resource-bootstrap.json", "{}"))
                Check(File.Exists(Path.Combine(clean, "StreamingAssets/resource-bootstrap.json")), "Clean checkout with no resource directories must work");
            Check(!Directory.Exists(Path.Combine(clean, "Resources")), "Absent Resources must remain absent after restoration");
            Check(!Directory.Exists(Path.Combine(clean, "StreamingAssets")), "Absent StreamingAssets must remain absent after restoration");
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
