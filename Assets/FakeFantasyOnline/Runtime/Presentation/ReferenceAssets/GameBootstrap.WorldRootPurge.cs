using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameBootstrap
{
    private void PurgeWorldRootsForIndependentRuntime()
    {
        var keep = new HashSet<string>
        {
            "ResLoader", "MusicManager", "SoundManager", "CursorManager",
            "NetClient", "GameBootstrap", "Main Camera", "EventSystem"
        };
        var probe = new GameObject("__ddolprobe__");
        DontDestroyOnLoad(probe);
        GameObject[] persistentRoots = probe.scene.GetRootGameObjects();
        Destroy(probe);
        Purge(persistentRoots);
        Purge(SceneManager.GetActiveScene().GetRootGameObjects());
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        void Purge(GameObject[] roots)
        {
            foreach (GameObject root in roots)
            {
                if (root != null && root != gameObject && !keep.Contains(root.name))
                    Destroy(root);
            }
        }
    }
}
}
