using System.Diagnostics;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 debugger / reverse-engineering tool detector. Process names come from the
// official static initializer, which builds a thirteen-element array in this exact order.
public static class DebugDetect
{
    private static readonly string[] SuspiciousProcessNames =
    {
        "cheatengine", "x64dbg", "x32dbg", "ollydbg", "ida", "ida64", "idaq", "idaq64",
        "dnspy", "reflector", "frida-server", "windbg", "processhacker"
    };

    private static bool? _cached;
    private static string _cachedReason;

    // 060006af: once a result exists it is reused, so a positive hit is sticky.
    public static bool Detect(out string reason)
    {
        if (_cached.HasValue)
        {
            reason = _cachedReason;
            return _cached.Value;
        }
        bool found = DetectCore(out reason);
        _cached = found;
        _cachedReason = reason;
        return found;
    }

    // 060006b0: a managed debugger is reported first, then each process supplied by
    // the platform runtime is matched against the suspicious list.
    private static bool DetectCore(out string reason)
    {
        if (Debugger.IsAttached)
        {
            reason = "debugger-attached";
            return true;
        }
        foreach (Process process in GetProcessesForRuntime())
        {
            string name = (process.ProcessName ?? string.Empty).ToLowerInvariant();
            foreach (string suspicious in SuspiciousProcessNames)
            {
                if (name.Contains(suspicious))
                {
                    reason = "proc:" + suspicious;
                    return true;
                }
            }
        }
        reason = null;
        return false;
    }

    private static Process[] GetProcessesForRuntime()
    {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        // Official macOS 1.5.8 IL2CPP returns no process IDs on both architectures.
        // System.Process.GetProcesses converts that null result to an empty array.
        // Mono enumerates real processes instead; preserve the official API result.
        // Evidence: docs/contracts/VERSION_UPGRADE_1_5_8_MACOS_PROCESS_ENUMERATION.md.
        return System.Array.Empty<Process>();
#else
        return Process.GetProcesses();
#endif
    }

}

}
