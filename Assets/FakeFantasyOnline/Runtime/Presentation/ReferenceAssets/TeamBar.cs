using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class TeamBar
    {
        public static TeamBar I;
        private const float SLOT_W = 44f;
        private const float SLOT_H = 34f;
        private const float BASE_Y = 110f;
        private Texture2D slotBg;
        private Texture2D hpFill;
        private bool tried;
        private float slideX = -44f;
        private static readonly List<int> mateIdx = new List<int>();

        // Official 0x06000e99.
        public static void Ensure()
        {
            if (I != null) return;
            var root = new GameObject("TeamBar");
            I = root.AddComponent<TeamBar>();
            DontDestroyOnLoad(root);
        }

        // Official 0x06000e9a. Preserve Party order and the original indices for profession lookup.
        private static List<(int, string, int, int, int, int, int, int, bool)> Mates()
        {
            var result = new List<(int, string, int, int, int, int, int, int, bool)>();
            mateIdx.Clear();
            for (int i = 0; i < SocialClient.Party.Count; i++)
            {
                if (SocialClient.IsSelf(i)) continue;
                result.Add(SocialClient.Party[i]);
                mateIdx.Add(i);
                if (result.Count >= 9) break;
            }
            return result;
        }

        // Official 0x06000e9b. Scale is based on screen height, not UIScale.Factor.
        public static bool MouseOver()
        {
            if (I == null) return false;
            var mates = Mates();
            if (mates.Count == 0) return false;
            float scale = Screen.height / 600f;
            Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            return new Rect(I.slideX * scale, BASE_Y * scale, SLOT_W * scale, mates.Count * SLOT_H * scale).Contains(mouse);
        }

        // Official 0x06000e9c.
        private void Load()
        {
            tried = true;
            ResLoader loader = ResLoader.I;
            if (loader == null) { tried = false; return; }
            slotBg = loader.LoadGsa("res/effect/duiyouzhuangtai_di_new.gsa");
            hpFill = loader.LoadGsa("res/effect/duiyou_hp_new.gsa");
        }

    }
}
