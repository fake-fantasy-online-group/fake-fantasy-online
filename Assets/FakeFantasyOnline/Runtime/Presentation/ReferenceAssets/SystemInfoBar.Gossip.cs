using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Native 1.5.8 system-request and local gossip-notice owner.
    public sealed partial class SystemInfoBar : MonoBehaviour
    {
        public struct Req
        {
            public int reqId;
            public byte kind;
            public int fromId;
            public string fromName;
            public string text;
            public float anim;
            public bool local;
        }

        public static SystemInfoBar I;
        internal int ListSoundCountForIndependentRuntime { get; private set; }
        private readonly List<Req> reqs = new List<Req>();
        private readonly List<Req> gossip = new List<Req>();
        private readonly Texture2D[] frames = new Texture2D[4];
        internal IReadOnlyList<Req> RequestsForIndependentRuntime => reqs;
        internal IReadOnlyList<Req> GossipForIndependentRuntime => gossip;

        // Official 0x06000e32.
        public static void Ensure()
        {
            if (I != null) return;
            var go = new GameObject("SystemInfoBar");
            I = go.AddComponent<SystemInfoBar>();
            DontDestroyOnLoad(go);
        }

        // Official 0x06000e34, ARM 0x348c58. Only a newly appended notice makes a sound.
        public static void AddGossipNotice(string from)
        {
            Ensure();
            from = from ?? "";
            if (I.gossip.Exists(q => q.fromName == from)) return;
            if (I.reqs.Exists(q => q.kind == 4 && q.fromName == from)) return;
            I.gossip.Add(new Req { reqId = -1, kind = 4, fromName = from,
                text = from + " 和你说话", anim = 0f, local = true });
            SoundManager.Play(1009);
        }

        // Official 0x06000e35 and predicate 0x06000e41. Does not create the owner.
        public static void ClearGossipNotice(string from)
        {
            if (I == null) return;
            I.gossip.RemoveAll(q => q.fromName == (from ?? ""));
        }

        // Official 0x06000e37.
        internal static Rect IconRect(Req q, int index, float scale)
        {
            float animation = Mathf.Clamp01(q.anim);
            float start = 447 + 26 * index;
            float end = 184 + 26 * index;
            return new Rect((start + (end - start) * animation) * scale, 70f * scale, 24f * scale, 24f * scale);
        }

        // Official 0x06000e3a. Original ARM getter thunk 0x104dd0c resolves unscaledDeltaTime.
        private void Update()
        {
            float step = Time.unscaledDeltaTime / 0.3f;
            for (int i = 0; i < reqs.Count; i++)
            {
                if (reqs[i].anim < 1f)
                {
                    Req q = reqs[i];
                    q.anim = Mathf.Min(1f, q.anim + step);
                    reqs[i] = q;
                }
            }
            for (int i = 0; i < gossip.Count; i++)
            {
                if (gossip[i].anim < 1f)
                {
                    Req q = gossip[i];
                    q.anim = Mathf.Min(1f, q.anim + step);
                    gossip[i] = q;
                }
            }
        }

        // Host-only projection and lifecycle: no duplicate notification storage or decisions.
        internal static void ProjectRequestsForIndependentRuntime(List<Req> rows)
        {
            Ensure();
            I.reqs.Clear();
            I.reqs.AddRange(rows);
        }
    }
}
