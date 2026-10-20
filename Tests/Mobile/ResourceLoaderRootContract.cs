using System;
using System.IO;
using System.Linq;
using FakeFantasyOnline.Foundation;
namespace UnityEngine
{
    public static class Application
    { public static string dataPath, persistentDataPath, streamingAssetsPath; public static bool isMobilePlatform; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class ResLoader
{
    public static int Main()
    {
        string root = Path.Combine(Path.GetTempPath(), "ffo-loader-root-" + Guid.NewGuid().ToString("N"));
        string install = Path.Combine(root, "Game"), active = Path.Combine(install, "DownloadedResources", "Remote", "generation");
        Directory.CreateDirectory(Path.Combine(active, "data"));
        Directory.CreateDirectory(Path.Combine(install, "data"));
        UnityEngine.Application.dataPath = Path.Combine(install, "Game_Data");
        UnityEngine.Application.persistentDataPath = Path.Combine(root, "user");
        UnityEngine.Application.streamingAssetsPath = Path.Combine(UnityEngine.Application.dataPath, "StreamingAssets");
#if UNITY_ANDROID && !UNITY_EDITOR
        UnityEngine.Application.isMobilePlatform = true;
#endif
        try
        {
            File.WriteAllText(Path.Combine(install, "data", "res_q21-old.pkg"), "old");
            File.WriteAllText(Path.Combine(active, "data", "res_q21-new.pkg"), "new");
            ClientResourcePaths.Activate(active);
#if (UNITY_STANDALONE_WIN || UNITY_ANDROID) && !UNITY_EDITOR
            if (GameRoots().Count() != 1 || GameRoots().Single() != active || FindReleasePkgDir() != Path.Combine(active, "data"))
                throw new Exception("Verified resources must be authoritative, not shadowed by old nearby packages.");
            Directory.Delete(Path.Combine(active, "data"), true);
            if (FindReleasePkgDir() != null) throw new Exception("Missing verified packages must not fall back to old adjacent packages.");
#else
            if (GameRoots().First() != install || FindReleasePkgDir() != Path.Combine(install, "data"))
                throw new Exception("Existing non-Windows/editor search order changed.");
#endif
            Console.WriteLine("PASS loader root platform contract"); return 0;
        }
        finally { ClientResourcePaths.UseBundled(); Directory.Delete(root, true); }
    }
}
}
