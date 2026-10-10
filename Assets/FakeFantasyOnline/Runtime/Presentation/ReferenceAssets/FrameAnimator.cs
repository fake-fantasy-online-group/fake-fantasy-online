using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 FrameAnimator, tokens 060003a9–060003ae.
public class FrameAnimator : MonoBehaviour
{
    public Func<(Sprite[], Vector3[])> loader;
    public float frameMs = 100f;
    public bool loop = true;
    public float pauseSecMin;
    public float pauseSecMax;

    private Sprite[] frames;
    private Vector3[] framePos;
    private SpriteRenderer sr;
    private float acc;
    private int idx = -1;
    private bool visible;
    private bool loaded;
    private bool pausing;
    private float curPause;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnBecameVisible()
    {
        visible = true;
        if (loaded) return;
        loaded = true;
        var load = loader;
        if (load != null)
        {
            var loadedFrames = load();
            frames = loadedFrames.Item1;
            framePos = loadedFrames.Item2;
        }
        acc = UnityEngine.Random.value * frameMs;
        if (frames != null && frames.Length != 0)
            Apply(UnityEngine.Random.Range(0, frames.Length));
    }

    private void OnBecameInvisible()
    {
        visible = false;
    }

    private void Update()
    {
        if (!visible || frames == null || frames.Length <= 1 || frameMs <= 0f) return;
        acc += Time.deltaTime * 1000f;
        if (pausing)
        {
            if (acc < curPause * 1000f) return;
            pausing = false;
            acc = 0f;
            return;
        }

        if (acc < frameMs) return;
        int steps = (int)(acc / frameMs);
        acc -= frameMs * steps;
        int next = idx + steps;
        if (next >= frames.Length)
        {
            if (pauseSecMax > 0f)
            {
                pausing = true;
                acc = 0f;
                curPause = UnityEngine.Random.Range(pauseSecMin, pauseSecMax);
                Apply(0);
                return;
            }
            next = loop ? next % frames.Length : frames.Length - 1;
        }
        Apply(next);
    }

    private void Apply(int i)
    {
        if (i == idx || sr == null || i < 0 || frames == null || i >= frames.Length)
            return;
        idx = i;
        if (frames[i] != null) sr.sprite = frames[i];
        if (framePos != null && i < framePos.Length)
            transform.localPosition = framePos[i];
    }
}

}
