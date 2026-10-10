using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Coordinate and viewport equations recovered from GameBootstrap.BuildWorld and CameraFollow.
/// </summary>
public static class ReferenceWorldCameraMath
{
    public const float CameraZ = -10f;

    public static Vector3 MapPixelToWorld(
        float mapX,
        float mapY,
        float worldWidth,
        float worldHeight,
        float pixelsPerUnit)
    {
        ValidateWorld(worldWidth, worldHeight, pixelsPerUnit);
        return new Vector3(
            (mapX - worldWidth * 0.5f) / pixelsPerUnit,
            (worldHeight * 0.5f - mapY) / pixelsPerUnit,
            0f);
    }

    public static int PixelScaleForViewport(
        int viewportHeight,
        float pixelsPerUnit,
        float dpiScale)
    {
        ValidateViewport(viewportHeight, pixelsPerUnit, dpiScale);
        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                viewportHeight / (pixelsPerUnit * 10f * dpiScale)));
    }

    public static float OrthographicSizeForViewport(
        int viewportHeight,
        float pixelsPerUnit,
        float dpiScale,
        int pixelScale)
    {
        ValidateViewport(viewportHeight, pixelsPerUnit, dpiScale);
        if (pixelScale < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelScale));
        }

        return viewportHeight /
               (2f * pixelsPerUnit * pixelScale * dpiScale);
    }

    public static Vector3 FollowPosition(
        Vector3 target,
        float orthographicSize,
        float aspect,
        float pixelsPerUnit,
        float worldHalfWidth,
        float worldHalfHeight,
        bool isoDiamond,
        int viewportWidth,
        int viewportHeight)
    {
        if (!(orthographicSize > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(orthographicSize));
        }

        if (!(aspect > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(aspect));
        }

        if (!(pixelsPerUnit > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        }

        if (!(worldHalfWidth > 0f) || !(worldHalfHeight > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(worldHalfWidth));
        }

        if (viewportWidth < 1 || viewportHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        }

        float x = target.x;
        float y = target.y;
        float halfViewWidth = orthographicSize * aspect;
        if (isoDiamond)
        {
            float room = 1f - halfViewWidth / worldHalfWidth -
                         orthographicSize / worldHalfHeight;
            float normalized = Mathf.Abs(x) / worldHalfWidth +
                               Mathf.Abs(y) / worldHalfHeight;
            if (room <= 0f)
            {
                x = 0f;
                y = 0f;
            }
            else if (normalized > room && normalized > 0f)
            {
                float scale = room / normalized;
                x *= scale;
                y *= scale;
            }
        }
        else
        {
            x = halfViewWidth >= worldHalfWidth
                ? 0f
                : Mathf.Clamp(
                    x,
                    halfViewWidth - worldHalfWidth,
                    worldHalfWidth - halfViewWidth);
            y = orthographicSize >= worldHalfHeight
                ? 0f
                : Mathf.Clamp(
                    y,
                    orthographicSize - worldHalfHeight,
                    worldHalfHeight - orthographicSize);
        }

        x = Mathf.Round(x * pixelsPerUnit) / pixelsPerUnit;
        y = Mathf.Round(y * pixelsPerUnit) / pixelsPerUnit;

        float pixelDensity = viewportHeight / (2f * orthographicSize);
        if ((viewportWidth & 1) != 0)
        {
            x -= 0.5f / pixelDensity;
        }

        if ((viewportHeight & 1) != 0)
        {
            y -= 0.5f / pixelDensity;
        }

        return new Vector3(x, y, CameraZ);
    }

    private static void ValidateWorld(
        float worldWidth,
        float worldHeight,
        float pixelsPerUnit)
    {
        if (!(worldWidth > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(worldWidth));
        }

        if (!(worldHeight > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(worldHeight));
        }

        if (!(pixelsPerUnit > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        }
    }

    private static void ValidateViewport(
        int viewportHeight,
        float pixelsPerUnit,
        float dpiScale)
    {
        if (viewportHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportHeight));
        }

        if (!(pixelsPerUnit > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));
        }

        if (!(dpiScale > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(dpiScale));
        }
    }
}

/// <summary>
/// Source-owned translation of the normal first-map CameraFollow branch.
/// </summary>
/// <remarks>
/// CameraFollow tokens 060000C8 through 060000CA establish the default values and immediate
/// LateUpdate follow. GameBootstrap.BuildWorld overwrites the map dimensions and target. The
/// normal first-map path keeps wheel zoom and the isometric-diamond clamp disabled.
/// </remarks>
public sealed class ReferenceWorldCameraFollow : MonoBehaviour
{
    public const bool DefaultWheelZoom = false;
    public const float DefaultZoomSpeed = 4f;
    public const float DefaultMinimumSize = 2f;
    public const float DefaultMaximumSize = 40f;
    public const float DefaultFollow = 8f;
    public const float DefaultPixelsPerUnit = 100f;
    public const float DefaultDpiScale = 1f;

    [SerializeField] private Transform target;
    [SerializeField] private bool wheelZoom = DefaultWheelZoom;
    [SerializeField] private float zoomSpeed = DefaultZoomSpeed;
    [SerializeField] private float minimumSize = DefaultMinimumSize;
    [SerializeField] private float maximumSize = DefaultMaximumSize;
    [SerializeField] private float follow = DefaultFollow;
    [SerializeField] private float pixelsPerUnit = DefaultPixelsPerUnit;
    [SerializeField] private float worldHalfWidth;
    [SerializeField] private float worldHalfHeight;
    [SerializeField] private bool isoDiamond;

    private Camera cameraComponent;

    public Transform Target => target;
    public bool WheelZoom => wheelZoom;
    public float ZoomSpeed => zoomSpeed;
    public float MinimumSize => minimumSize;
    public float MaximumSize => maximumSize;
    public float Follow => follow;
    public float PixelsPerUnit => pixelsPerUnit;
    public float WorldHalfWidth => worldHalfWidth;
    public float WorldHalfHeight => worldHalfHeight;
    public bool IsoDiamond => isoDiamond;
    public int AppliedPixelScale { get; private set; } = 1;

    public void Configure(
        Transform value,
        float valuePixelsPerUnit,
        float worldWidth,
        float worldHeight,
        bool valueIsoDiamond)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (!(valuePixelsPerUnit > 0f) || !(worldWidth > 0f) || !(worldHeight > 0f))
        {
            throw new ArgumentOutOfRangeException(nameof(valuePixelsPerUnit));
        }

        EnsureCamera();
        target = value;
        pixelsPerUnit = valuePixelsPerUnit;
        worldHalfWidth = worldWidth / (2f * valuePixelsPerUnit);
        worldHalfHeight = worldHeight / (2f * valuePixelsPerUnit);
        maximumSize = worldHalfHeight;
        isoDiamond = valueIsoDiamond;
    }

    public int RefreshForViewport(int viewportWidth, int viewportHeight, float dpiScale)
    {
        if (target == null)
        {
            throw new InvalidOperationException("CameraFollow has no target.");
        }

        if (viewportWidth < 1 || viewportHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        }

        EnsureCamera();
        float aspect = viewportWidth / (float)viewportHeight;
        cameraComponent.aspect = aspect;

        // The locked first-world constructor leaves wheelZoom false. Its normal LateUpdate branch
        // replaces the initial size with an integral-art-pixel viewport size every frame.
        AppliedPixelScale = ReferenceWorldCameraMath.PixelScaleForViewport(
            viewportHeight,
            pixelsPerUnit,
            dpiScale);
        float size = ReferenceWorldCameraMath.OrthographicSizeForViewport(
            viewportHeight,
            pixelsPerUnit,
            dpiScale,
            AppliedPixelScale);
        cameraComponent.orthographicSize = size;
        cameraComponent.transform.position = ReferenceWorldCameraMath.FollowPosition(
            target.position,
            size,
            aspect,
            pixelsPerUnit,
            worldHalfWidth,
            worldHalfHeight,
            isoDiamond,
            viewportWidth,
            viewportHeight);
        return AppliedPixelScale;
    }

    private void Awake()
    {
        EnsureCamera();
    }

    private void LateUpdate()
    {
        if (target == null || Screen.width < 1 || Screen.height < 1)
        {
            return;
        }

        RefreshForViewport(Screen.width, Screen.height, DefaultDpiScale);
    }

    private void EnsureCamera()
    {
        if (cameraComponent == null)
        {
            cameraComponent = GetComponent<Camera>();
        }

        if (cameraComponent == null)
        {
            throw new InvalidOperationException(
                "ReferenceWorldCameraFollow requires a Camera on the same GameObject.");
        }
    }
}

/// <summary>
/// Exact normal camera defaults applied by the recovered BuildWorld branch.
/// </summary>
public static class ReferenceWorldCameraSetup
{
    public const float InitialOrthographicSize = 5f;

    public static void Configure(Camera camera, ReferenceGsmMap map)
    {
        if (camera == null)
        {
            throw new ArgumentNullException(nameof(camera));
        }

        if (map == null)
        {
            throw new ArgumentNullException(nameof(map));
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.orthographic = true;
        camera.orthographicSize = InitialOrthographicSize;
        camera.transparencySortMode = TransparencySortMode.CustomAxis;
        camera.transparencySortAxis = Vector3.up;
        GameBootstrap.SetPendingSkyForIndependentRuntime(camera, map.HasSky
            ? map.SkyColorUnity
            : Color.black);
    }
}

}
