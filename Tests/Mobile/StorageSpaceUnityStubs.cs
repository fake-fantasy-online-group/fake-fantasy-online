using System;
using System.Collections.Generic;

namespace UnityEngine
{
// Deterministic JNI substitutes. These tests do not query an Android device.
public static class StorageSpaceStub
{
    public static readonly List<string> Events = new List<string>();
    public static long Available = 8L * 1024 * 1024 * 1024;
    public static int AttachResult, LiveObjects;
    public static bool ConstructorThrows, QueryThrows;
    public static string Path;
    public static void Reset()
    {
        Events.Clear(); AttachResult = LiveObjects = 0;
        ConstructorThrows = QueryThrows = false; Path = null;
    }
}
public sealed class AndroidJavaException : Exception
{
    public AndroidJavaException(string text) : base(text) { }
}
public static class AndroidJNI
{
    public static int AttachCurrentThread()
    { StorageSpaceStub.Events.Add("attach"); return StorageSpaceStub.AttachResult; }
    public static int DetachCurrentThread()
    {
        if (StorageSpaceStub.LiveObjects != 0) throw new Exception("Java objects must be disposed before detach");
        StorageSpaceStub.Events.Add("detach"); return 0;
    }
}
public sealed class AndroidJavaObject : IDisposable
{
    public AndroidJavaObject(string type, params object[] args)
    {
        if (type != "android.os.StatFs" || args.Length != 1 || !(args[0] is string))
            throw new Exception("Unexpected JNI storage query");
        StorageSpaceStub.Path = (string)args[0];
        StorageSpaceStub.Events.Add("construct");
        if (StorageSpaceStub.ConstructorThrows) throw new AndroidJavaException("invalid path");
        StorageSpaceStub.LiveObjects++;
    }
    public T Call<T>(string method, params object[] args)
    {
        if (method != "getAvailableBytes" || typeof(T) != typeof(long) || args.Length != 0)
            throw new Exception("Storage query must use 64-bit available bytes");
        StorageSpaceStub.Events.Add("query");
        if (StorageSpaceStub.QueryThrows) throw new AndroidJavaException("query failed");
        return (T)(object)StorageSpaceStub.Available;
    }
    public void Dispose()
    { StorageSpaceStub.LiveObjects--; StorageSpaceStub.Events.Add("dispose"); }
}
}
