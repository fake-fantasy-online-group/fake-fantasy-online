using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Exact CloudLayer count, direction, parallax, wrapping, and alpha equations from 1.3.4.
/// </summary>
public static class ReferenceWorldCloudMath
{
    public const int MinimumDensity = 64;
    public const int MinimumCloudCount = 6;
    public const int MaximumCloudCount = 60;
    public const float CountWidth = 1600f;
    public const float CountHeight = 1200f;
    public const float ParallaxCompensation = 0.9f;
    public const float PulseTimeScale = 0.4f;

    public static int CountForDensity(int density)
    {
        int admittedDensity = density > MinimumDensity ? density : MinimumDensity;
        int columns = Mathf.CeilToInt(CountWidth / admittedDensity);
        int rows = Mathf.CeilToInt(CountHeight / admittedDensity);
        return Mathf.Clamp(
            columns * rows,
            MinimumCloudCount,
            MaximumCloudCount);
    }

    public static Vector2 Velocity(float directionDegrees, float speed)
    {
        float radians = directionDegrees * Mathf.Deg2Rad;
        return new Vector2(
            Mathf.Cos(radians) * speed,
            -Mathf.Sin(radians) * speed);
    }

    public static Vector2 AdvanceOffset(
        Vector2 offset,
        Vector2 velocity,
        float deltaTime,
        Vector2 cameraDelta,
        float windowWidth,
        float windowHeight)
    {
        offset += velocity * deltaTime -
                  cameraDelta * ParallaxCompensation;
        float halfWidth = windowWidth * 0.5f;
        float halfHeight = windowHeight * 0.5f;
        if (offset.x > halfWidth)
        {
            offset.x -= windowWidth;
        }
        else if (offset.x < -halfWidth)
        {
            offset.x += windowWidth;
        }

        if (offset.y > halfHeight)
        {
            offset.y -= windowHeight;
        }
        else if (offset.y < -halfHeight)
        {
            offset.y += windowHeight;
        }

        return offset;
    }

    public static float Alpha(float baseAlpha, float phase, float pulse, float time)
    {
        return Mathf.Clamp01(
            baseAlpha + Mathf.Sin(time * PulseTimeScale + phase) * pulse);
    }
}

internal static class ReferenceWorldCloudAssets
{
    private const string RootPrefix = "object/YD/";
    private const string TypedPrefix = "object/YD/YD_";
    private const string Suffix = ".gso";

    private static readonly Dictionary<int, string> CityNames =
        new Dictionary<int, string>
        {
            { 1, "FY" },
            { 2, "XL" },
            { 3, "TD" },
            { 4, "TY" },
            { 5, "CL" },
            { 6, "DFYW" },
            { 7, "SW" },
            { 8, "YL" },
            { 9, "PL" },
            { 10, "TT" },
            { 11, "SJ" },
            { 12, "HJ" },
            { 13, "SMYW" },
            { 14, "HS" },
            { 15, "YM" },
            { 16, "GC" },
            { 17, "MY" },
            { 18, "TH" },
            { 19, "XFYW" },
            { 20, "TTD" },
            { 21, "YLD" },
            { 22, "PLSJ" },
            { 23, "SWS" },
            { 24, "DXWG" },
            { 25, "CLMG" },
            { 26, "TYMG" },
            { 27, "TJJ" },
            { 28, "JXMG" },
            { 29, "TJFB" },
            { 30, "JZSN" },
            { 31, "HYYSC" },
            { 32, "HYYHS" },
            { 33, "CLY" },
            { 34, "HDMZS" },
            { 35, "JLG" },
            { 36, "HSTX" },
            { 37, "SHLG" },
            { 38, "MC" },
            { 39, "ZHG" },
            { 40, "LHZC" }
        };

    public static IReadOnlyList<string> Paths(
        ReferenceResourceLoader resources,
        int cloudType)
    {
        if (resources == null)
        {
            throw new ArgumentNullException(nameof(resources));
        }

        string city;
        if (CityNames.TryGetValue(cloudType, out city))
        {
            IReadOnlyList<string> typed = resources.FindPrimaryPackagePaths(
                TypedPrefix + city + "_",
                Suffix);
            if (typed.Count > 0)
            {
                return typed;
            }
        }

        return resources.FindPrimaryPackagePaths(RootPrefix, Suffix);
    }
}

/// <summary>
/// Source translation of the official CloudLayer owner used by GameBootstrap.BuildWorld.
/// </summary>
public sealed class ReferenceWorldCloudLayer : MonoBehaviour
{
    private struct Cloud
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector2 Offset;
        public Vector2 Velocity;
        public float BaseAlpha;
        public float Phase;
        public float Pulse;
    }

    public const int SortingOrder = -10050;

    private readonly List<Cloud> clouds = new List<Cloud>();
    private readonly List<Sprite> ownedSprites = new List<Sprite>();
    private readonly List<string> sourcePaths = new List<string>();

    private Camera cameraComponent;
    private float windowWidth;
    private float windowHeight;
    private Vector3 lastCameraPosition;
    private bool cameraInitialized;
    private bool initialized;

    public int CloudCount => clouds.Count;
    public int SourcePathCount => sourcePaths.Count;
    public IReadOnlyList<string> SourcePaths => sourcePaths.AsReadOnly();
    public float WindowWidth => windowWidth;
    public float WindowHeight => windowHeight;
    public bool Initialized => initialized;

    public bool Initialize(
        ReferenceGsmSky sky,
        float pixelsPerUnit,
        Camera camera,
        Transform parent,
        ReferenceResourceLoader resources,
        int viewportWidth,
        int viewportHeight)
    {
        if (initialized || sky == null || !(pixelsPerUnit > 0f) ||
            camera == null || parent == null || resources == null ||
            viewportWidth < 1 || viewportHeight < 1)
        {
            return false;
        }

        cameraComponent = camera;
        windowWidth = viewportWidth / pixelsPerUnit;
        windowHeight = viewportHeight / pixelsPerUnit;

        IReadOnlyList<string> paths = ReferenceWorldCloudAssets.Paths(
            resources,
            sky.CloudType);
        List<Texture2D> textures = new List<Texture2D>();
        for (int index = 0; index < paths.Count; index++)
        {
            Texture2D texture = resources.LoadGsoFirst(paths[index]);
            if (texture == null)
            {
                continue;
            }

            textures.Add(texture);
            sourcePaths.Add(paths[index]);
        }

        if (textures.Count == 0)
        {
            cameraComponent = null;
            return false;
        }

        int count = ReferenceWorldCloudMath.CountForDensity(sky.CloudDensity);
        float minimumDirection = -sky.CloudDirection * 0.5f;
        float maximumDirection = sky.CloudDirection * 0.5f;
        for (int index = 0; index < count; index++)
        {
            Texture2D texture = textures[UnityEngine.Random.Range(0, textures.Count)];
            GameObject cloudObject = new GameObject("cloud" + index.ToString());
            cloudObject.transform.parent = parent;

            SpriteRenderer renderer = cloudObject.AddComponent<SpriteRenderer>();
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
            renderer.sprite = sprite;
            renderer.sortingOrder = SortingOrder;
            ownedSprites.Add(sprite);

            float scale = UnityEngine.Random.Range(0.7f, 1.4f);
            cloudObject.transform.localScale = new Vector3(scale, scale, 1f);
            float direction = UnityEngine.Random.Range(
                minimumDirection,
                maximumDirection) + sky.CloudFineDirection;
            float speed = UnityEngine.Random.Range(0.15f, 0.4f);
            Cloud cloud = new Cloud
            {
                Transform = cloudObject.transform,
                Renderer = renderer,
                Offset = new Vector2(
                    UnityEngine.Random.Range(-windowWidth * 0.5f, windowWidth * 0.5f),
                    UnityEngine.Random.Range(-windowHeight * 0.5f, windowHeight * 0.5f)),
                Velocity = ReferenceWorldCloudMath.Velocity(direction, speed),
                BaseAlpha = UnityEngine.Random.Range(0.35f, 0.6f),
                Phase = UnityEngine.Random.Range(0f, 6.28f),
                Pulse = UnityEngine.Random.Range(0.08f, 0.18f)
            };
            clouds.Add(cloud);
        }

        initialized = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!initialized || cameraComponent == null || clouds.Count == 0)
        {
            return;
        }

        Vector3 cameraPosition = cameraComponent.transform.position;
        Vector3 previous = cameraInitialized ? lastCameraPosition : cameraPosition;
        cameraInitialized = true;
        lastCameraPosition = cameraPosition;
        Vector2 cameraDelta = new Vector2(
            cameraPosition.x - previous.x,
            cameraPosition.y - previous.y);
        float deltaTime = Time.deltaTime;
        float time = Time.time;
        for (int index = 0; index < clouds.Count; index++)
        {
            Cloud cloud = clouds[index];
            cloud.Offset = ReferenceWorldCloudMath.AdvanceOffset(
                cloud.Offset,
                cloud.Velocity,
                deltaTime,
                cameraDelta,
                windowWidth,
                windowHeight);
            cloud.Transform.position = new Vector3(
                cameraPosition.x + cloud.Offset.x,
                cameraPosition.y + cloud.Offset.y,
                0f);

            Color color = cloud.Renderer.color;
            color.a = ReferenceWorldCloudMath.Alpha(
                cloud.BaseAlpha,
                cloud.Phase,
                cloud.Pulse,
                time);
            cloud.Renderer.color = color;
            clouds[index] = cloud;
        }
    }

    private void OnDestroy()
    {
        for (int index = 0; index < ownedSprites.Count; index++)
        {
            DestroyOwned(ownedSprites[index]);
        }

        ownedSprites.Clear();
        sourcePaths.Clear();
        clouds.Clear();
        cameraComponent = null;
        initialized = false;
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

}
