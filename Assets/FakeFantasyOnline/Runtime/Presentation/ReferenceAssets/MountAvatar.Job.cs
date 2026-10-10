using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class MountAvatar
{
    internal class Job
    {
        public Data d;
        public List<LayerTask> layers = new List<LayerTask>();
        public bool hintWalk;
        public int hintDir;
        public Task running;
        public readonly ConcurrentQueue<Ready> ready = new ConcurrentQueue<Ready>();
        public int pendingBytes;
    }

    internal class LayerTask
    {
        public int actId;
        public string suffix;
        public bool walk;
        public int kind;
        public DimgDecoder.AnimGeom geom;
        public bool triedBytes;
        public bool[] dirDone;
        public int dirs;
        public int fpd;
        public bool dead;
    }

    internal struct Ready
    {
        public LayerTask L;
        public int dir;
        public int f;
        public int idx;
        public int w;
        public int h;
        public byte[] rgba;
        public int scale;
    }
}

}
