using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed class DealingMark : MonoBehaviour
    {
        private const float FRAME_SEC = 0.1f;
        private static Sprite[] _frames;
        private static bool _tried;
        private static readonly List<DealingMark> _live = new List<DealingMark>();

        private Transform target;
        private float headU;
        private SpriteRenderer sr;
        private int f;
        private float t;

        // Official 1.6.0 token 0x060001d8.
        private static Sprite[] Frames()
        {
            if (_tried) return _frames;

            ResLoader loader = ResLoader.I;
            if (loader == null) return null;

            _tried = true;
            int count = loader.GsaFrameCount("res/effect/dealing.gsa");
            if (count < 1) return null;

            var frames = new List<Sprite>(count);
            for (int index = 0; index < count; index++)
            {
                Texture2D texture = loader.LoadGsaFrame("res/effect/dealing.gsa", index);
                if (texture != null) frames.Add(TexScale.MakeSprite(texture, 100f, 0.5f, 0.5f));
            }
            _frames = frames.Count == 0 ? null : frames.ToArray();
            return _frames;
        }

        // Official 1.6.0 token 0x060001d9.
        public static void Attach(Transform target, float headU)
        {
            if (target == null) return;
            Sprite[] frames = Frames();
            if (frames == null) return;

            foreach (DealingMark existing in _live)
                if (existing != null && existing.target == target) return;

            GameObject root = new GameObject("DealingMark");
            DealingMark created = root.AddComponent<DealingMark>();
            created.target = target;
            created.headU = headU;
            created.sr = root.AddComponent<SpriteRenderer>();
            created.sr.sortingOrder = 30000;
            created.sr.sprite = frames[0];
            _live.Add(created);
        }

        // Official 1.6.0 token 0x060001da.
        public static void Clear()
        {
            foreach (DealingMark mark in _live)
                if (mark != null) Destroy(mark.gameObject);
            _live.Clear();
        }

        // Official 1.6.0 token 0x060001db.
        private void Update()
        {
            if (target == null)
            {
                _live.Remove(this);
                Destroy(gameObject);
                return;
            }

            transform.position = target.position + new Vector3(0f, headU + 0.35f, 0f);
            Sprite[] frames = _frames;
            if (frames == null || frames.Length == 0) return;

            t += Time.deltaTime;
            if (!(t >= FRAME_SEC)) return;

            t -= FRAME_SEC;
            f = (f + 1) % frames.Length;
            sr.sprite = frames[f];
        }
    }
}
