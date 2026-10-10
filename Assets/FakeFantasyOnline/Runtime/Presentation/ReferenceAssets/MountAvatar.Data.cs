using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class MountAvatar
{
    // Original nested reference type 0x020000e4, consumed by the shared cache and upload jobs.
    public class Data
    {
        public Sprite[,] stand;
        public Sprite[,] walk;
        public Sprite[,] standTop;
        public Sprite[,] walkTop;
        public bool mine;
        public Sprite[,] standFx;
        public Sprite[,] walkFx;
        public Vector2[,] standFxOff;
        public Vector2[,] walkFxOff;
        public bool topAdditive;
        public bool fxAdditive;
        public Vector2[][,] standSeatSaddle;
        public Vector2[][,] walkSeatSaddle;
        public Vector2[,] standTopOff;
        public Vector2[,] walkTopOff;
        public int dirs = 5;
        public int seatCount = 1;
        public bool lowDetail;
        public int boundCy;
        public int boundW;
        public int belowAnchor;
        public float standInterval = 0.075f;
        public float walkInterval = 0.071f;
        public bool ok;
        internal int key;
        internal int model;
        internal float ppu;
        internal bool dirsFromWalk;
        internal int refCount;
        internal float lastUse;
        internal long bytes;
        internal Job job;
        internal int fillVersion;

        public bool Warming => job != null;
        public int FillVersion => fillVersion;

        public Vector2[,] Saddle(bool moving, int seat)
        {
            Vector2[][,] saddles = moving && walkSeatSaddle != null && walkSeatSaddle.Length != 0
                ? walkSeatSaddle : standSeatSaddle;
            if (saddles == null || saddles.Length == 0) return null;
            return saddles[Mathf.Clamp(seat, 0, saddles.Length - 1)];
        }

        public void Touch(bool walking, int dir)
        {
            lastUse = Time.realtimeSinceStartup;
            Job current = job;
            if (current != null)
            {
                Volatile.Write(ref current.hintWalk, walking);
                Volatile.Write(ref current.hintDir, dir);
            }
        }

        internal void DisposeNow()
        {
            HashSet<Texture2D> seen = new HashSet<Texture2D>();
            Kill(stand);
            Kill(walk);
            Kill(standTop);
            Kill(walkTop);
            Kill(standFx);
            Kill(walkFx);
            walkFx = null;
            standFx = null;
            walkTop = null;
            standTop = null;
            walk = null;
            stand = null;
            job = null;
            bytes = 0;
            ok = false;

            void Kill(Sprite[,] grid)
            {
                if (grid == null) return;
                foreach (Sprite sprite in grid)
                {
                    if (!sprite) continue;
                    Texture2D texture = sprite.texture;
                    Object.Destroy(sprite);
                    if (texture && seen.Add(texture)) Object.Destroy(texture);
                }
            }
        }
    }
}

}
