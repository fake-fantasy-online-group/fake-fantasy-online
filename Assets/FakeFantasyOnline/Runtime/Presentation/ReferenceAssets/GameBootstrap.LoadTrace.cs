using System;
using System.IO;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{
    // 060003f5 writes -99f to +0x04; nullable +0x08 keeps its null default.
    private static bool? _traceHotkey;

    // 060003b9. Only the command-line and marker probes have managed-object catches.
    private static bool TraceHotkeyOn
    {
        get
        {
            if (_traceHotkey.HasValue) return _traceHotkey.Value;

            bool enabled = UnityEngine.Application.isEditor;
            if (!enabled)
            {
                try
                {
                    foreach (string argument in Environment.GetCommandLineArgs())
                    {
                        if (string.Equals(argument, "-ffotrace", StringComparison.OrdinalIgnoreCase))
                        {
                            enabled = true;
                            break;
                        }
                    }
                }
                catch
                {
                }
            }

            if (!enabled)
            {
                try
                {
                    string root = Path.GetDirectoryName(UnityEngine.Application.dataPath);
                    enabled = root != null &&
                        (File.Exists(Path.Combine(root, "ffotrace")) ||
                         File.Exists(Path.Combine(root, "ffotrace.txt"))) ||
                        File.Exists(Path.Combine(UnityEngine.Application.persistentDataPath, "ffotrace")) ||
                        File.Exists(Path.Combine(UnityEngine.Application.persistentDataPath, "ffotrace.txt"));
                }
                catch
                {
                    enabled = false;
                }
            }

            if (enabled && !UnityEngine.Application.isEditor)
            {
                Debug.LogWarning("[追踪] 诊断已启用(启动参数或 ffotrace 标记文件): Ctrl+F9 开/关一段 60s 采样。");
            }

            _traceHotkey = enabled;
            return enabled;
        }
    }

    // The diagnostic portion of official 060003ba, called after ArtUploadClock.BeginFrame.
    private static void UpdateTraceDiagnosticsForIndependentRuntime()
    {
        if (LoadTrace.On && Time.unscaledTime - _srScanAt >= 1f)
        {
            _srScanAt = Time.unscaledTime;
            SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            int count = 0;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer != null && renderer.enabled && renderer.sprite != null)
                    count = unchecked(count + 1);
            }
            LoadTrace.NoteSpriteRenderers(count);
        }

        // Command/Meta/Apple are identical enum values in the frozen Unity assembly.
        if (TraceHotkeyOn && Input.GetKeyDown(KeyCode.F9) &&
            (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
             Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)))
        {
            LoadTrace.Toggle();
        }
    }
}

}
