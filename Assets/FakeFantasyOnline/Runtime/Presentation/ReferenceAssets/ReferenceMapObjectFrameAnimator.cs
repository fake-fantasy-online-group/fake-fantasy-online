using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Lazily materialized sprites and per-frame local positions for one map object.
/// </summary>
public sealed class ReferenceMapObjectAnimationFrames
{
    public ReferenceMapObjectAnimationFrames(Sprite[] sprites, Vector3[] positions)
    {
        Sprites = sprites ?? Array.Empty<Sprite>();
        Positions = positions ?? Array.Empty<Vector3>();
    }

    public Sprite[] Sprites { get; }
    public Vector3[] Positions { get; }
}

/// <summary>
/// Source translation of the official FrameAnimator used by animated legacy map objects.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ReferenceMapObjectFrameAnimator : MonoBehaviour
{
    public const float DefaultFrameMilliseconds = 100f;
    public const float PlacedObjectFrameMilliseconds = 200f;

    private Func<ReferenceMapObjectAnimationFrames> loader;
    private float frameMilliseconds = DefaultFrameMilliseconds;
    private bool loop = true;
    private float pauseSecondsMinimum;
    private float pauseSecondsMaximum;
    private Sprite[] frames;
    private Vector3[] framePositions;
    private SpriteRenderer spriteRenderer;
    private float accumulatorMilliseconds;
    private int index = -1;
    private bool visible;
    private bool loaded;
    private bool pausing;
    private float currentPauseSeconds;

    public float FrameMilliseconds => frameMilliseconds;
    public bool Loop => loop;
    public bool Visible => visible;
    public bool Loaded => loaded;
    public int FrameCount => frames == null ? 0 : frames.Length;
    public int CurrentFrameIndex => index;
    public float AccumulatorMilliseconds => accumulatorMilliseconds;

    public void Configure(
        Func<ReferenceMapObjectAnimationFrames> frameLoader,
        float millisecondsPerFrame,
        bool shouldLoop,
        float minimumPauseSeconds = 0f,
        float maximumPauseSeconds = 0f)
    {
        loader = frameLoader;
        frameMilliseconds = millisecondsPerFrame;
        loop = shouldLoop;
        pauseSecondsMinimum = minimumPauseSeconds;
        pauseSecondsMaximum = maximumPauseSeconds;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnBecameVisible()
    {
        if (LoadOnFirstVisibility())
            BeginVisibleFrames(UnityEngine.Random.value, -1);
    }

    private void OnBecameInvisible()
    {
        BecomeInvisible();
    }

    private void Update()
    {
        AdvanceMilliseconds(Time.deltaTime * 1000f);
    }

    private void BecomeVisible(float phase, int initialFrameIndex)
    {
        if (LoadOnFirstVisibility()) BeginVisibleFrames(phase, initialFrameIndex);
    }

    private bool LoadOnFirstVisibility()
    {
        visible = true;
        if (loaded) return false;
        loaded = true;
        ReferenceMapObjectAnimationFrames loadedFrames = loader == null ? null : loader();
        if (loadedFrames != null)
        {
            frames = loadedFrames.Sprites;
            framePositions = loadedFrames.Positions;
        }
        return true;
    }

    private void BeginVisibleFrames(float phase, int initialFrameIndex)
    {
        accumulatorMilliseconds = phase * frameMilliseconds;
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        int selected = initialFrameIndex;
        if (selected < 0 || selected >= frames.Length)
        {
            selected = UnityEngine.Random.Range(0, frames.Length);
        }

        ApplyFrame(selected);
    }

    private void BecomeInvisible()
    {
        visible = false;
    }

    private void AdvanceMilliseconds(float deltaMilliseconds)
    {
        if (!visible || frames == null || frames.Length < 2 || !(frameMilliseconds > 0f))
        {
            return;
        }

        accumulatorMilliseconds += deltaMilliseconds;
        if (pausing)
        {
            if (!(accumulatorMilliseconds >= currentPauseSeconds * 1000f))
            {
                return;
            }

            pausing = false;
            accumulatorMilliseconds = 0f;
            return;
        }

        if (!(accumulatorMilliseconds >= frameMilliseconds))
        {
            return;
        }

        int steps = (int)(accumulatorMilliseconds / frameMilliseconds);
        accumulatorMilliseconds -= frameMilliseconds * steps;
        int next = index + steps;
        if (next >= frames.Length)
        {
            if (!(pauseSecondsMaximum > 0f))
            {
                next = loop ? next % frames.Length : frames.Length - 1;
            }
            else
            {
                pausing = true;
                accumulatorMilliseconds = 0f;
                currentPauseSeconds = UnityEngine.Random.Range(
                    pauseSecondsMinimum,
                    pauseSecondsMaximum);
                next = 0;
            }
        }

        ApplyFrame(next);
    }

    private void ApplyFrame(int next)
    {
        if (next == index || spriteRenderer == null || next < 0 ||
            frames == null || next >= frames.Length)
        {
            return;
        }

        index = next;
        Sprite sprite = frames[next];
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }

        if (framePositions != null && next < framePositions.Length)
        {
            transform.localPosition = framePositions[next];
        }
    }
}

}
