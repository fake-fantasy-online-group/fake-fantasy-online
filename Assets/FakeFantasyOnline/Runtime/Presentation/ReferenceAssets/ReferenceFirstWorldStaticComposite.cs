using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Owns the exact first-map floor and regular GSO composition behind a recovered camera target.
/// </summary>
/// <remarks>
/// This boundary deliberately does not create a player, world entity stream, music, or HUD. The
/// caller supplies the camera target so the next recovered local-player factory can become that
/// target without replacing the camera/floor/object implementation.
/// </remarks>
public sealed partial class ReferenceFirstWorldStaticComposite : IDisposable
{
    public const float PixelsPerUnit = 100f;

    private readonly ReferenceGsmMap map;
    private readonly ReferenceResourceLoader resources;
    private readonly Transform root;
    private readonly Camera camera;
    private readonly GameBootstrap bootstrap;
    private readonly Transform cameraTarget;

    private GameObject floorRootObject;
    private GameObject cloudRootObject;
    private FloorRenderer floorRenderer;
    private CloudLayer cloudLayer;
    private ReferenceLegacyMapObjectRenderer objectRenderer;
    private CameraFollow cameraFollow;
    private bool floorBuilt;
    private bool built;
    private bool disposed;

    public ReferenceFirstWorldStaticComposite(
        ReferenceGsmMap map,
        ReferenceTileImageLibrary tiles,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceResourceLoader resources,
        Transform root,
        Camera camera,
        Transform cameraTarget)
        : this(map, tiles, shapes, resources, root, camera, GameBootstrap.I)
    {
        this.cameraTarget = cameraTarget == null
            ? throw new ArgumentNullException(nameof(cameraTarget))
            : cameraTarget;
    }

    internal ReferenceFirstWorldStaticComposite(
        ReferenceGsmMap map,
        ReferenceTileImageLibrary tiles,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceResourceLoader resources,
        Transform root,
        Camera camera,
        GameBootstrap bootstrap)
    {
        this.bootstrap = bootstrap;
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.root = root == null ? throw new ArgumentNullException(nameof(root)) : root;
        // This is the selected scene camera, including a normal no-camera result.
        this.camera = camera;

        if (!map.Ok && !map.HasCover && !map.HasTextureBackgroundForIndependentRuntime)
        {
            throw new ArgumentException(
                "The first-world static composite requires a parsed terrain map or cover.",
                nameof(map));
        }
    }

    public bool Built => built;
    // Observe the native producer; do not compute or publish a parallel camera scale.
    public int AppliedPixelScale => ArtDetail.PixelScale;
    public FloorRenderer FloorRenderer => floorRenderer;
    public CloudLayer CloudLayer => cloudLayer;
    public ReferenceLegacyMapObjectRenderer ObjectRenderer => objectRenderer;
    public CameraFollow CameraFollow => cameraFollow;

    internal GameBootstrap.BuildWorldCheckpoints BuildCheckpointsForIndependentRuntime { get; set; }

    public bool BuildForViewport(int viewportWidth, int viewportHeight, float dpiScale)
    {
        return BuildFloorForIndependentRuntime() &&
            CompleteForPlayerForIndependentRuntime(cameraTarget);
    }

    internal bool BuildFloorForIndependentRuntime(bool globalsAlreadyConfigured = false)
    {
        ThrowIfDisposed();
        if (built || floorBuilt)
            return false;

        ReferenceWorldRenderOptions options = ReferenceWorldRenderSettings.OfficialDefaults();
        if (options.UseIsoDepth || options.UseIsoY || options.EnableOcclusionFade)
        {
            throw new InvalidOperationException(
                "Locked first-frame render defaults unexpectedly enabled a dormant branch.");
        }

        if (!globalsAlreadyConfigured)
            ReferenceWorldRenderSettings.ConfigureIsoYGlobals(map.MapHeight, PixelsPerUnit);
        if (camera != null) ReferenceWorldCameraSetup.Configure(camera, map);
        floorRootObject = new GameObject(map.HasTextureBackgroundForIndependentRuntime ? "Map" : "Floor");
        if (map.HasTextureBackgroundForIndependentRuntime)
            floorRootObject.transform.parent = root;
        else
            floorRootObject.transform.SetParent(root, false);

        if (map.Ok)
        {
            floorRenderer = floorRootObject.AddComponent<FloorRenderer>();
            floorRenderer.Init(map, resources.Core.Pkg, camera, PixelsPerUnit,
                map.MapWidth, map.MapHeight);

        }
        else if (!BuildCoverBackground())
        {
            return false;
        }

        if (BuildCheckpointsForIndependentRuntime != null)
            BuildCheckpointsForIndependentRuntime.FloorMs =
                BuildCheckpointsForIndependentRuntime.Clock.ElapsedMilliseconds;

        // BuildWorld's extra cloud flag means a missing parsed map, not !map.ok.
        // The supplied camera/root/ppu are the existing world composition inputs.
        GsmMap cloudMap = map.Native;
        if (camera != null && cloudMap != null && cloudMap.hasSky &&
            (cloudMap.skyColor & 0x00ffffffu) != 0)
        {
            cloudRootObject = new GameObject("Clouds");
            cloudRootObject.transform.parent = root;
            cloudLayer = cloudRootObject.AddComponent<CloudLayer>();
            cloudLayer.Init(
                cloudMap.cloudType,
                cloudMap.cloudDensity,
                cloudMap.cloudDir,
                cloudMap.cloudFdir,
                PixelsPerUnit,
                camera,
                cloudRootObject.transform,
                -10050);
            // Init is void. Empty/missing cloud resources return normally and do not
            // reject the world; field-color/object composition continues below.
        }

        if (map.Native.hasFieldColor && map.Native.fcColors != null)
            bootstrap.BuildFieldColor(map.Native,
                bootstrap.WorldRootForMapConstructionForIndependentRuntime,
                bootstrap.pixelsPerUnit, map.MapWidth, map.MapHeight);

        floorBuilt = true;
        return true;
    }

    internal bool CompleteForPlayerForIndependentRuntime(Transform cameraTarget)
    {
        ThrowIfDisposed();
        if (built || !floorBuilt)
            return false;
        if (cameraTarget == null)
            throw new ArgumentNullException(nameof(cameraTarget));

        if (camera != null)
        {
            camera.transform.position = new Vector3(
                cameraTarget.position.x,
                cameraTarget.position.y,
                ReferenceWorldCameraMath.CameraZ);

            // BuildWorld 060003d4 gets/adds on the selected camera GameObject. The
            // component result is a raw-reference null branch, not Unity object equality.
            cameraFollow = camera.gameObject.GetComponent<CameraFollow>();
            if (ReferenceEquals(cameraFollow, null))
                cameraFollow = camera.gameObject.AddComponent<CameraFollow>();

            // Preserve the ARM binding order. The x86 compiler moves ppu before maxSize;
            // its maxSize store is FOUR bytes and must not overwrite the reused follow.
            // No constructor defaults, private camera, or explicit lifecycle calls here.
            cameraFollow.target = cameraTarget.gameObject.transform;
            float halfHeight = (map.MapHeight / PixelsPerUnit) * 0.5f;
            cameraFollow.maxSize = halfHeight;
            cameraFollow.ppu = PixelsPerUnit;
            cameraFollow.worldHalfW = (map.MapWidth / PixelsPerUnit) * 0.5f;
            cameraFollow.worldHalfH = halfHeight;
            cameraFollow.isoDiamond = false;
        }

        if (BuildCheckpointsForIndependentRuntime != null)
            BuildCheckpointsForIndependentRuntime.LocalPlayerMs =
                BuildCheckpointsForIndependentRuntime.Clock.ElapsedMilliseconds;

        if (GameBootstrap.MapObjectsAdmittedForIndependentRuntime(map))
        {
            objectRenderer = new ReferenceLegacyMapObjectRenderer(
                resources.Core, bootstrap, createMapObjectRoot: true);
            // PlaceMapObjects is void. A normal return, including native object
            // admission rejection, continues to the original post-object suffix.
            objectRenderer.PlaceMapObjectsForIndependentRuntime(bootstrap, map.Native, root,
                ReferenceEquals(bootstrap, null) ? PixelsPerUnit : bootstrap.pixelsPerUnit,
                map.MapWidth, map.MapHeight);
        }
        if (BuildCheckpointsForIndependentRuntime != null)
            BuildCheckpointsForIndependentRuntime.ObjectsMs =
                BuildCheckpointsForIndependentRuntime.Clock.ElapsedMilliseconds;

        // Unity's original CameraFollow.LateUpdate is the sole live viewport/art-scale
        // writer. BuildWorld has no explicit initial refresh or lifecycle invocation.
        built = true;
        return true;
    }

    /// <summary>
    /// Reads the currently bound camera for explicit diagnostics. This does not apply a
    /// synthetic viewport, run CameraFollow, refresh the floor, or publish art scale.
    /// </summary>
    public Camera InspectCameraForDiagnostics()
    {
        ThrowIfDisposed();
        return camera;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        // Retire the component with its scene root; native OnDisable only clears Any.
        floorRenderer = null;

        if (objectRenderer != null)
        {
            objectRenderer.Dispose();
            objectRenderer = null;
        }

        DestroyOwned(coverSprite);
        if (ownsCoverTexture) DestroyOwned(coverTexture);
        coverSprite = null;
        coverTexture = null;
        DestroyOwned(floorRootObject);
        // Unity owns the single automatic LateUpdate and calls native OnDestroy when
        // this Clouds hierarchy is retired; do not run a second loop or release its
        // freshly decoded source textures through the resource adapter.
        DestroyOwned(cloudRootObject);
        floorRootObject = null;
        cloudRootObject = null;
        cloudLayer = null;

        // Borrowed from the camera, outside this map-root hierarchy. This boundary
        // must not add target clears, resets or component destruction to the binding.
        // The external scene-camera owner still determines the camera's lifetime.
        cameraFollow = null;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceFirstWorldStaticComposite));
        }
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
