using System;

namespace FakeFantasyOnline.Application
{

public static class ClientConfig
{
    [Serializable]
    public class Srv
    {
        public string name;
        public string host;
        public int port;
        public string zone;
        public bool pvp;
        public int busy;
    }

    [Serializable]
    public class Dispatch
    {
        public Srv[] servers;
    }

    [Serializable]
    public class MFile
    {
        public string path;
    }

    [Serializable]
    public class Manifest
    {
        public string version;
        public MFile[] files;
    }

    [Serializable]
    public class DevCfg
    {
        public string host;
        public int port;
    }

    public const string WorkerBase = "http://127.0.0.1:8088";
    public const string DispatchUrl = "http://127.0.0.1:8088/dispatch.json";
    public const string ManifestUrlWin = "";
    public const string ManifestUrlMac = "";
}

}
