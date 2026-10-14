using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Independent-runtime boundary around the recovered official <see cref="ResLoader"/>.
/// </summary>
/// <remarks>
/// Repository path binding, Unity component lifetime and official-method forwarding are
/// decision-free. Explicitly named migration methods remain unaccepted recovery debt until their
/// recoverable official owners replace them. Package precedence and recovered cache decisions
/// remain on <see cref="ResLoader"/>.
/// </remarks>
public sealed class ReferenceResourceLoader : IDisposable
{
    private const string StreamingAssetsDirectory = "ReferenceClient";

    private static ReferenceResourceLoader persistentStreamingOwner;

    private readonly string rootDirectory;
    private readonly string rootPrefix;
    private readonly string mappedUiCandidateRoot;
    private readonly string mappedUiCandidatePrefix;
    private readonly ResLoader core;
    private readonly GameObject coreOwner;
    private readonly bool persistent;
    private readonly Dictionary<string, Texture2D> adapterTextureCache =
        new Dictionary<string, Texture2D>();
    private bool disposed;

    private sealed class IndependentUiByteSource : IResLoaderUiByteSource
    {
        private readonly ReferenceResourceLoader owner;

        public IndependentUiByteSource(ReferenceResourceLoader owner)
        {
            this.owner = owner;
        }

        public bool Exists(string candidatePath, string logicalPath)
        {
            string mappedPath;
            return owner.TryMapUiCandidate(candidatePath, logicalPath, out mappedPath)
                ? File.Exists(mappedPath)
                : File.Exists(candidatePath);
        }

        public byte[] ReadAllBytes(string candidatePath, string logicalPath)
        {
            string mappedPath;
            return File.ReadAllBytes(
                owner.TryMapUiCandidate(candidatePath, logicalPath, out mappedPath)
                    ? mappedPath
                    : candidatePath);
        }
    }

    public ReferenceResourceLoader(string rootDirectory)
        : this(rootDirectory, false)
    {
    }

    private ReferenceResourceLoader(string rootDirectory, bool persistent)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A reference resource root is required.", nameof(rootDirectory));
        }

        this.rootDirectory = Path.GetFullPath(rootDirectory);
        rootPrefix = this.rootDirectory.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        // Official StreamingAssets/gameassets root. It is the single root GameTables reads and
        // the only place the byte-locked official loose tables are supplied from.
        mappedUiCandidateRoot = Path.GetFullPath(
            Path.Combine(ResourceFileRoot.Path, "gameassets"));
        mappedUiCandidatePrefix = mappedUiCandidateRoot.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        this.persistent = persistent;

        coreOwner = new GameObject("ResLoader");
        coreOwner.SetActive(false);
        core = coreOwner.AddComponent<ResLoader>();
        core.BindIndependentUiByteSource(new IndependentUiByteSource(this));
        if (UnityEngine.Application.isPlaying)
        {
            // Awake owns package availability. Missing FF21, loose mounts and legacy mounts
            // are original states; keep the published owner and its reached package prefix.
            // Content authority and world consumers validate the data they actually require.
            coreOwner.SetActive(true);
        }
#if UNITY_EDITOR
        else
        {
            PkgArchive pkg;
            PkgArchive pkg24;
            PkgArchive pkgFf21;
            ReferencePackageMountAdapter.OpenIfComplete(
                Path.Combine(this.rootDirectory, "packages"),
                out pkg,
                out pkg24,
                out pkgFf21);

            // Edit-mode audits do not dispatch MonoBehaviour.Awake. This assigns the same public
            // singleton input consumed by the recovered static Read24Res owner, without adding a
            // second resource decision path.
            ResLoader.I = core;

            // This is an editor-only translation from the repository layout into the three
            // official archive views. Built Players mount through the recovered Awake itself.
            core.BindIndependentRuntime(
                pkg,
                pkg24,
                pkgFf21,
                new[] { Path.Combine(this.rootDirectory, "res24") });
        }
#endif
    }

    public static ReferenceResourceLoader OpenStreamingAssets()
    {
        if (persistentStreamingOwner == null || persistentStreamingOwner.disposed)
        {
            persistentStreamingOwner = new ReferenceResourceLoader(
                Path.Combine(
                    ResourceFileRoot.Path,
                    StreamingAssetsDirectory),
                true);
        }

        return persistentStreamingOwner;
    }

    internal string RootDirectory => rootDirectory;

    internal ResLoader Core => core;

    internal PkgArchive PrimaryPackageForIndependentRuntime
    {
        get
        {
            ThrowIfDisposed();
            return core.Pkg;
        }
    }

    public bool UsesCompletePackageSet =>
        core.Pkg != null && core.Pkg24 != null && core.PkgFf21 != null;

    internal IReadOnlyList<string> FindPrimaryPackagePaths(
        string directoryPrefix,
        string suffix)
    {
        ThrowIfDisposed();
        return core.Pkg == null
            ? Array.Empty<string>()
            : core.Pkg.FindAll(directoryPrefix, suffix);
    }

    public byte[] ReadMap(string logicalPath)
    {
        ThrowIfDisposed();
        return core.ReadMap(logicalPath);
    }

    public byte[] ReadRes(string logicalPath)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.ReadRes(logicalPath);
    }

    public Texture2D LoadGsa(string logicalPath)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.LoadGsa(logicalPath);
    }

    public Texture2D LoadGsa2021First(string logicalPath)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.LoadGsa2021First(logicalPath);
    }

    public int GsaFrameCount(string logicalPath)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return 0;
        }

        return core.GsaFrameCount(logicalPath);
    }

    public Texture2D LoadGsaFrame(string logicalPath, int frameIndex)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.LoadGsaFrame(logicalPath, frameIndex);
    }

    public Texture2D LoadGsaFrame2021First(string logicalPath, int frameIndex)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.LoadGsaFrame2021First(logicalPath, frameIndex);
    }

    public Texture2D LoadGsaFrameA(string logicalPath, int frameIndex)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.LoadGsaFrameA(logicalPath, frameIndex);
    }

    public Texture2D LoadNpcTex(string spriteName)
    {
        ThrowIfDisposed();
        return core.LoadNpcTex(spriteName);
    }

    public DimgDecoder.Result LoadNpcFull(string spriteName)
    {
        ThrowIfDisposed();
        return core.LoadNpcFull(spriteName);
    }

    public DimgDecoder.Result LoadNpcFull(string spriteName, int storedDir)
    {
        ThrowIfDisposed();
        return core.LoadNpcFull(spriteName, storedDir);
    }

    public void BuildCityTable(IEnumerable<int> imageIds)
    {
        ThrowIfDisposed();
        core.BuildCityTable(imageIds);
    }

    internal string ResolveObjectPath(int imageId)
    {
        ThrowIfDisposed();
        return core.ResolveObjectPath(imageId);
    }

    /// <summary>
    /// Migration adapter for GSO consumers whose exact official ResLoader owner has not yet been
    /// restored. It reads only bytes selected by the recovered core.
    /// </summary>
    public Texture2D LoadGsoFirst(string logicalPath)
    {
        ThrowIfDisposed();

        string key = "@adapter:gso-first:" + logicalPath;
        Texture2D cached;
        if (adapterTextureCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        Texture2D decoded = null;
        byte[] encoded = core.ReadRes(logicalPath);
        ReferenceGsoImage image;
        if (encoded != null &&
            ReferenceGsoDecoder.TryDecode(encoded, out image) &&
            image != null && image.InitialFrame != null &&
            image.InitialFrame.Width > 0 && image.InitialFrame.Height > 0)
        {
            ReferenceGsoFrame frame = image.InitialFrame;
            Color32[] bottomUp = FlipRows(frame.Pixels, frame.Width, frame.Height);
            decoded = CreateTexture(
                logicalPath,
                frame.Width,
                frame.Height,
                bottomUp,
                FilterMode.Point,
                TextureWrapMode.Clamp);
            GsaTextureScale.Register(decoded, image.Scale);
        }

        adapterTextureCache[key] = decoded;
        return decoded;
    }

    /// <summary>
    /// Migration adapter for DIMG container consumers outside the restored official method cone.
    /// </summary>
    public Texture2D LoadDimgFirstVisible(string logicalPath)
    {
        ThrowIfDisposed();

        string key = "@adapter:dimg-first-visible:" + logicalPath;
        Texture2D cached;
        if (adapterTextureCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        Texture2D decoded = null;
        byte[] encoded = core.ReadRes(logicalPath);
        int sourceScale = HdMagic.DimgScaleOf(encoded);
        int frameCount = DimgDecoder.FrameCount(encoded);
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            DimgFrame frame;
            if (!DimgDecoder.TryDecodeFrame(encoded, frameIndex, out frame) || frame == null)
            {
                decoded = null;
                break;
            }

            if (!frame.HasPixels)
            {
                continue;
            }

            decoded = CreateTexture(
                logicalPath,
                frame.Width,
                frame.Height,
                FlipRows(frame.Pixels, frame.Width, frame.Height),
                FilterMode.Point,
                TextureWrapMode.Clamp);
            GsaTextureScale.Register(decoded, sourceScale);
            break;
        }

        adapterTextureCache[key] = decoded;
        return decoded;
    }

    /// <summary>
    /// Migration adapter retained until the official NetClient.LoadItemIcon owner is restored.
    /// </summary>
    public Texture2D LoadItemIconFirstVisible(string logicalPath)
    {
        Texture2D texture = LoadGsoFirst(logicalPath);
        return texture ? texture : LoadDimgFirstVisible(logicalPath);
    }

    /// <summary>
    /// Decision-free independent-runtime entry into the recovered official LoadUI owner.
    /// </summary>
    public Texture2D LoadUiPng(string logicalPath)
    {
        ThrowIfDisposed();
        if (!IsSafeLogicalPath(logicalPath))
        {
            return null;
        }

        return core.LoadUI(logicalPath);
    }

    /// <summary>
    /// Adapter for reviewed Unity Texture2D migration inputs outside the official package set.
    /// </summary>
    public Texture2D LoadReviewedPng(string logicalPath)
    {
        return LoadPng(
            logicalPath,
            "@adapter:reviewed-png:",
            FilterMode.Bilinear,
            TextureWrapMode.Repeat,
            true);
    }

    internal byte[] ReadSourceOwned(string logicalPath)
    {
        ThrowIfDisposed();
        byte[] bytes = core.ReadRes(logicalPath);
        if (bytes != null)
        {
            return bytes;
        }

        string filePath;
        if (!TryResolveSourceOwned(logicalPath, out filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            return File.ReadAllBytes(filePath);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (disposed || persistent)
        {
            return;
        }

        disposed = true;
        ReleaseAdapterTextures();
        if (ReferenceEquals(ResLoader.I, core))
        {
            ResLoader.I = null;
        }

        if (!coreOwner)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(coreOwner);
            return;
        }
#endif
        UnityEngine.Object.Destroy(coreOwner);
    }

    private Texture2D LoadPng(
        string logicalPath,
        string cachePrefix,
        FilterMode filterMode,
        TextureWrapMode wrapMode,
        bool linear)
    {
        ThrowIfDisposed();

        string key = cachePrefix + logicalPath;
        Texture2D cached;
        if (adapterTextureCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] encoded = ReadSourceOwned(logicalPath);
        Texture2D decoded = null;
        if (encoded != null)
        {
            Texture2D candidate = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear);
            if (ImageConversion.LoadImage(candidate, encoded, false))
            {
                candidate.name = Path.GetFileNameWithoutExtension(logicalPath);
                candidate.filterMode = filterMode;
                candidate.wrapMode = wrapMode;
                decoded = candidate;
            }
            else
            {
                DestroyTexture(candidate);
            }
        }

        adapterTextureCache[key] = decoded;
        return decoded;
    }

    private bool TryResolve(string logicalPath, out string filePath)
    {
        return TryResolveUnder(rootDirectory, rootPrefix, logicalPath, out filePath);
    }

    /// <summary>
    /// Source-owned inputs resolve below the independent <c>ReferenceClient</c> staging root,
    /// except the official loose <c>tables</c> tree. Those byte-locked originals live in the
    /// single official <c>StreamingAssets/gameassets</c> root that <see cref="GameTables"/>
    /// reads, so Editor, QA and Player share one copy instead of a second relocated one.
    /// </summary>
    private bool TryResolveSourceOwned(string logicalPath, out string filePath)
    {
        return IsOfficialTablesPath(logicalPath)
            ? TryResolveUnder(
                mappedUiCandidateRoot,
                mappedUiCandidatePrefix,
                logicalPath,
                out filePath)
            : TryResolve(logicalPath, out filePath);
    }

    private static bool IsOfficialTablesPath(string logicalPath)
    {
        return !string.IsNullOrEmpty(logicalPath) &&
               logicalPath.Replace('\\', '/').StartsWith("tables/", StringComparison.Ordinal);
    }

    private static bool TryResolveUnder(
        string root,
        string prefix,
        string logicalPath,
        out string filePath)
    {
        filePath = null;
        if (string.IsNullOrWhiteSpace(logicalPath) ||
            Path.IsPathRooted(logicalPath))
        {
            return false;
        }

        string normalized = logicalPath.Replace('\\', '/');
        string[] segments = normalized.Split('/');
        for (int index = 0; index < segments.Length; index++)
        {
            string segment = segments[index];
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                return false;
            }
        }

        string candidate = Path.GetFullPath(
            Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!candidate.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        filePath = candidate;
        return true;
    }

    private bool TryMapUiCandidate(
        string candidatePath,
        string logicalPath,
        out string mappedPath)
    {
        mappedPath = null;
        if (string.IsNullOrEmpty(candidatePath))
        {
            return false;
        }

        string fullCandidate = Path.GetFullPath(candidatePath);
        string expectedCandidate = Path.GetFullPath(
            Path.Combine(mappedUiCandidateRoot, logicalPath));
        if (!string.Equals(fullCandidate, expectedCandidate, StringComparison.Ordinal) ||
            !fullCandidate.StartsWith(mappedUiCandidatePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string relativeTail = fullCandidate.Substring(mappedUiCandidatePrefix.Length);
        string candidate = Path.GetFullPath(Path.Combine(rootDirectory, relativeTail));
        if (!candidate.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        mappedPath = candidate;
        return true;
    }

    private bool IsSafeLogicalPath(string logicalPath)
    {
        string ignored;
        return TryResolve(logicalPath, out ignored);
    }

    private void ReleaseAdapterTextures()
    {
        HashSet<Texture2D> released = new HashSet<Texture2D>();
        foreach (Texture2D texture in adapterTextureCache.Values)
        {
            if (texture && released.Add(texture))
            {
                DestroyTexture(texture);
            }
        }

        adapterTextureCache.Clear();
    }

    private static Color32[] FlipRows(Color32[] pixels, int width, int height)
    {
        Color32[] bottomUp = new Color32[pixels.Length];
        for (int sourceY = 0; sourceY < height; sourceY++)
        {
            Array.Copy(
                pixels,
                sourceY * width,
                bottomUp,
                (height - sourceY - 1) * width,
                width);
        }

        return bottomUp;
    }

    private static Texture2D CreateTexture(
        string logicalPath,
        int width,
        int height,
        Color32[] pixels,
        FilterMode filterMode,
        TextureWrapMode wrapMode)
    {
        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false,
            false);
        texture.name = Path.GetFileNameWithoutExtension(logicalPath);
        texture.filterMode = filterMode;
        texture.wrapMode = wrapMode;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (!texture)
        {
            return;
        }

#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(texture);
            return;
        }
#endif
        UnityEngine.Object.Destroy(texture);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceResourceLoader));
        }
    }
}

}
