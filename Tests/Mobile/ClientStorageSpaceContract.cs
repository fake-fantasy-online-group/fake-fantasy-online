using System;
using System.IO;
using System.Threading.Tasks;
using FakeFantasyOnline.Bootstrap;
using UnityEngine;

internal static class ClientStorageSpaceContract
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static void Reject(Action action, string events)
    {
        bool rejected = false;
        try { action(); } catch (IOException) { rejected = true; }
        Check(rejected, "Storage probe failure must fail closed, not report unlimited space");
        Check(string.Join(",", StorageSpaceStub.Events) == events, "Failure must release JNI in order");
    }
    public static void Main()
    {
        StorageSpaceStub.Reset();
        var query = ClientStorageSpace.CreateAvailableBytesQuery();
#if UNITY_ANDROID && !UNITY_EDITOR
        Check(query != null, "Android Player must inject a storage probe");
        const string path = "/data/user/0/org.example.game/files/MobileResources/Remote";
        StorageSpaceStub.Available = 9L * 1024 * 1024 * 1024 + 123;
        Check(query(path) == StorageSpaceStub.Available, "Keep 64-bit usable capacity above 4 GiB");
        Check(StorageSpaceStub.Path == path, "Probe the downloader's actual destination path");
        Check(string.Join(",", StorageSpaceStub.Events) == "construct,query,dispose", "Main thread must never be detached");
        StorageSpaceStub.Reset(); StorageSpaceStub.Available = 12345;
        Check(Task.Run(() => query(path)).GetAwaiter().GetResult() == 12345, "Worker obtains live capacity");
        Check(string.Join(",", StorageSpaceStub.Events) == "attach,construct,query,dispose,detach", "Worker attaches and disposes before detaching");
        StorageSpaceStub.Reset(); StorageSpaceStub.Available = 0;
        Check(Task.Run(() => query(path)).GetAwaiter().GetResult() == 0, "Zero capacity remains zero for download admission");
        StorageSpaceStub.Reset(); StorageSpaceStub.Available = -1;
        Reject(() => Task.Run(() => query(path)).GetAwaiter().GetResult(), "attach,construct,query,dispose,detach");
        StorageSpaceStub.Reset(); StorageSpaceStub.AttachResult = -1;
        Reject(() => Task.Run(() => query(path)).GetAwaiter().GetResult(), "attach");
        StorageSpaceStub.Reset(); StorageSpaceStub.ConstructorThrows = true;
        Reject(() => Task.Run(() => query(path)).GetAwaiter().GetResult(), "attach,construct,detach");
        StorageSpaceStub.Reset(); StorageSpaceStub.QueryThrows = true;
        Reject(() => Task.Run(() => query(path)).GetAwaiter().GetResult(), "attach,construct,query,dispose,detach");
        StorageSpaceStub.Reset(); StorageSpaceStub.QueryThrows = true;
        Reject(() => query(path), "construct,query,dispose");
        StorageSpaceStub.Reset(); StorageSpaceStub.Available = 42;
        Check(Task.Run(() => query(path)).GetAwaiter().GetResult() == 42, "Retry queries fresh capacity after prior failures");
        Check(StorageSpaceStub.LiveObjects == 0, "No Java object leaks after success or failures");
#else
        Check(query == null, "Desktop and Android Editor retain Foundation's DriveInfo fallback");
        Check(StorageSpaceStub.Events.Count == 0, "Non-Android Player branch does not invoke JNI");
#endif
        Console.WriteLine("PASS platform storage probe: " + checks + " assertions (JNI substitutes, no device query)");
    }
}
