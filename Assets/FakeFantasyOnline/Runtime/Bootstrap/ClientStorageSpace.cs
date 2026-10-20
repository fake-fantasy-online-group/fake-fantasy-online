using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Platform storage probe injected into engine-free downloaders.</summary>
internal static class ClientStorageSpace
{
    // Create on Unity's main thread, alongside the downloader. The returned query is
    // called after its storage directory exists and may run on a Task worker thread.
    internal static Func<string, long> CreateAvailableBytesQuery()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int mainThread = Thread.CurrentThread.ManagedThreadId;
        return path => AndroidAvailableBytes(path, mainThread);
#else
        // Keep the existing DriveInfo implementation on desktop and in the Editor.
        return null;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static long AndroidAvailableBytes(string path, int mainThread)
    {
        bool attachedWorker = false;
        try
        {
            if (Thread.CurrentThread.ManagedThreadId != mainThread)
            {
                if (AndroidJNI.AttachCurrentThread() != 0)
                    throw new IOException("Unable to query available storage: Android JVM thread attachment failed.");
                attachedWorker = true;
            }
            // StatFs reports bytes available to this application, excluding reserved
            // blocks. Query the actual destination filesystem afresh for every attempt.
            using (var stat = new AndroidJavaObject("android.os.StatFs", path))
            {
                long available = stat.Call<long>("getAvailableBytes");
                if (available < 0) throw new IOException("Android returned invalid available storage bytes.");
                return available;
            }
        }
        catch (AndroidJavaException exception)
        {
            throw new IOException("Unable to query available storage on Android.", exception);
        }
        finally
        {
            // Dispose Java references before detaching only our worker invocation.
            // Never detach the Unity main thread, which owns the UI's JVM context.
            if (attachedWorker) AndroidJNI.DetachCurrentThread();
        }
    }
#endif
}
}
