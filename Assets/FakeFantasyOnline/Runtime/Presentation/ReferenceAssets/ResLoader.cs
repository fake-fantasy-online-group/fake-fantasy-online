using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal interface IResLoaderUiByteSource
{
    bool Exists(string candidatePath, string logicalPath);

    byte[] ReadAllBytes(string candidatePath, string logicalPath);
}

/// <summary>
/// Recovered official 1.3.4 ResLoader owner.
/// </summary>
/// <remarks>
/// Type ownership, nested data shapes, fields, constructor ordering and package-root lifecycle
/// come from token 06000A7B..06000A80/06000AAD/06000AAE. Built Players expose the copied packages
/// at the official searched layout; only edit-mode audits use a repository-path binding because
/// Unity does not dispatch Awake there.
/// </remarks>
public partial class ResLoader : MonoBehaviour
{
    public struct ObjSprite
    {
        public Texture2D tex;
        public int nLeft;
        public int nTop;
        public int clipBy;
        public int clipWidth;
        public int[] clipArr;
        public int frames;
        public string path;
        public int hdScale;
        public int candW;
        public int candH;
    }

    public struct ObjAnim
    {
        public Sprite[] frames;
        public int[] nLeft;
        public int[] nTop;
    }

    public static ResLoader I;

    public PkgArchive Pkg { get; private set; }

    public PkgArchive Pkg24 { get; private set; }

    public PkgArchive PkgFf21 { get; private set; }

    private readonly Dictionary<string, Texture2D> texCache;
    private readonly Dictionary<int, string> objPathCache;
    private static string[] _res24Dirs;
    private static readonly Dictionary<string, string> _res24Cache;
    public static long Stat24Reads;
    public static long StatReads;
    public static long StatMisses;
    private static Dictionary<string, string> respatchIdx;
    private readonly Dictionary<string, int> frameCntCache;
    private readonly Dictionary<string, Texture2D> uiCache;
    private readonly Dictionary<string, DimgDecoder.Result> npcFullCache;
    private readonly Dictionary<string, ObjAnim> npcAnimCache;
    private readonly ArtBudget<string> npcFullBudget;
    private readonly ArtBudget<string> npcAnimBudget;
    private readonly Dictionary<int, ObjSprite> objCache;
    private readonly Dictionary<int, ObjAnim> objAnimCache;
    private readonly Dictionary<int, Texture2D> contourCache;
    private const float ContourScale = 1024f;
    private readonly Dictionary<int, string> set2city;
    private IResLoaderUiByteSource independentUiByteSource;

    static ResLoader()
    {
        _res24Cache = new Dictionary<string, string>();
    }

    public ResLoader()
    {
        texCache = new Dictionary<string, Texture2D>();
        objPathCache = new Dictionary<int, string>();
        frameCntCache = new Dictionary<string, int>();
        uiCache = new Dictionary<string, Texture2D>();
        npcFullCache = new Dictionary<string, DimgDecoder.Result>();
        npcAnimCache = new Dictionary<string, ObjAnim>();
        npcFullBudget = new ArtBudget<string>("NPC单帧", 50331648L, 600f);
        npcAnimBudget = new ArtBudget<string>("NPC待机动画", 67108864L, 600f);
        mapImgBudget = new ArtBudget<string>("\u9010\u56fe\u5927\u56fe", 67108864L, 1000000000f);
        objCache = new Dictionary<int, ObjSprite>();
        objAnimCache = new Dictionary<int, ObjAnim>();
        contourCache = new Dictionary<int, Texture2D>();
        set2city = new Dictionary<int, string>();
    }

    private static bool UseInstalledResourceRoot()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // A verified Windows download must not be shadowed by older adjacent packages.
        if (!string.IsNullOrEmpty(FakeFantasyOnline.Foundation.ClientResourcePaths.InstalledRoot)) return true;
#endif
        return UnityEngine.Application.isMobilePlatform;
    }

    private static IEnumerable<string> GameRoots()
    {
        if (UseInstalledResourceRoot())
        {
            yield return ResourceFileRoot.Path;
            yield break;
        }
        yield return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
        yield return UnityEngine.Application.persistentDataPath;
        yield return ResourceFileRoot.Path;
        yield return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../.."));
    }

    private static IEnumerable<string> EnumRootSub(string sub)
    {
        foreach (string root in GameRoots())
        {
            yield return Path.Combine(root, sub);
        }
    }

    private static IEnumerable<string> CandidateDirs()
    {
        foreach (string root in GameRoots())
        {
            yield return Path.Combine(root, "data");
            yield return Path.Combine(root, "Q21/data");
        }
    }

    private static string FindPackRoot()
    {
        foreach (string root in GameRoots())
        {
            string[] candidates = { "game_res", "res_pack_out" };
            for (int index = 0; index < candidates.Length; index++)
            {
                string candidate = Path.Combine(root, candidates[index]);
                if (Directory.Exists(Path.Combine(candidate, "q21")) &&
                    Directory.Exists(Path.Combine(candidate, "05diff")))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string FindReleasePkgDir()
    {
        if (UseInstalledResourceRoot())
        {
            string installed = Path.Combine(ResourceFileRoot.Path, "data");
            return Directory.Exists(installed) ? installed : null;
        }
        List<string> candidates = new List<string>();
        string dataPath = UnityEngine.Application.dataPath;
        string[] relatives = { "", "..", "../..", "../../.." };
        for (int index = 0; index < relatives.Length; index++)
        {
            string candidate = dataPath;
            if (relatives[index].Length != 0)
            {
                candidate = Path.GetFullPath(Path.Combine(dataPath, relatives[index]));
            }

            candidates.Add(candidate);
            candidates.Add(Path.Combine(candidate, "data"));
        }

        candidates.Add(UnityEngine.Application.persistentDataPath);
        candidates.Add(Path.Combine(UnityEngine.Application.persistentDataPath, "data"));
        candidates.Add(ResourceFileRoot.Path);
        candidates.Add(Path.Combine(ResourceFileRoot.Path, "data"));

        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) &&
                Directory.Exists(candidate) &&
                Directory.GetFiles(candidate, "res_q21*.pkg").Length > 0)
            {
                return candidate;
            }
        }

        return null;
    }

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
        ArtDetail.OnBudgetScaleChanged += ApplyResBudgetScale;
        ApplyResBudgetScale();

        string packRoot = FindPackRoot();
        string releasePkgDir = FindReleasePkgDir();
        if (releasePkgDir != null &&
            (File.Exists(Path.Combine(releasePkgDir, "USE_PKG")) || packRoot == null))
        {
            string whiteListPath = Path.Combine(releasePkgDir, "05paths.txt");
            List<string> q21Packages = new List<string>(
                Directory.GetFiles(releasePkgDir, "res_q21*.pkg"));
            q21Packages.Sort();

            List<string> patches = new List<string>();
            string[] patchPatterns = { "hd_patch*.pkg", "zpatch*.pkg" };
            for (int index = 0; index < patchPatterns.Length; index++)
            {
                patches.AddRange(Directory.GetFiles(releasePkgDir, patchPatterns[index]));
            }

            patches.Sort();
            patches.Reverse();

            string diffPackage = Path.Combine(releasePkgDir, "res_05diff.pkg");
            List<string> primaryPackages = new List<string>(patches);
            primaryPackages.Add(diffPackage);
            primaryPackages.AddRange(q21Packages);

            List<string> nativePackages = new List<string>(patches);
            nativePackages.AddRange(q21Packages);

            Pkg = new PkgArchive { Tag = "05" };
            Pkg.MountPkgs(primaryPackages);
            Pkg24 = new PkgArchive { Tag = "q21" };
            Pkg24.MountPkgs(nativePackages);

            string ff21Package = Path.Combine(releasePkgDir, "res_ff21.pkg");
            if (File.Exists(ff21Package))
            {
                PkgFf21 = new PkgArchive { Tag = "ff21" };
                List<string> ff21Packages = new List<string>(patches);
                ff21Packages.Add(ff21Package);
                PkgFf21.MountPkgs(ff21Packages);
            }

            if (!File.Exists(whiteListPath))
            {
                Debug.LogWarning("[ResLoader] 发行包缺 05paths.txt 白名单, 05视角未过滤");
            }

            if (!File.Exists(ff21Package))
            {
                Debug.LogWarning(
                    "[ResLoader] 发行包缺 res_ff21.pkg, 461个NPC/手修传送将丢失(地图可能坏)");
            }

            return;
        }

        if (packRoot != null)
        {
            string whiteListPath = Path.Combine(packRoot, "05paths.txt");
            Pkg = new PkgArchive { Tag = "05" };
            Pkg.MountLooseDirs(new[]
            {
                Path.Combine(packRoot, "05diff"),
                Path.Combine(packRoot, "q21")
            });
            Pkg24 = new PkgArchive { Tag = "q21" };
            Pkg24.MountLooseDirs(new[] { Path.Combine(packRoot, "q21") });

            if (!File.Exists(whiteListPath))
            {
                Debug.LogWarning(
                    "[ResLoader] 缺 05paths.txt 白名单, 05视角未过滤(含q21独有文件)");
            }

            return;
        }

        Debug.LogWarning("[ResLoader] 未找到 res_pack_out, 回退原 pkg 挂载");
        Pkg = new PkgArchive { Tag = "05" };
        Pkg.Mount(CandidateDirs(), "objects*.pkg");
        Pkg24 = new PkgArchive { Tag = "q21" };
        Pkg24.Mount(EnumRootSub("Q21/data"), "objects*.pkg");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-audit binding only. Built Players use the complete recovered Awake mount path.
    /// </summary>
    internal void BindIndependentRuntime(
        PkgArchive pkg,
        PkgArchive pkg24,
        PkgArchive pkgFf21,
        string[] res24Dirs)
    {
        Pkg = pkg;
        Pkg24 = pkg24;
        PkgFf21 = pkgFf21;
        _res24Dirs = res24Dirs;
        _res24Cache.Clear();
    }
#endif

    internal void BindIndependentUiByteSource(IResLoaderUiByteSource source)
    {
        independentUiByteSource = source;
    }

    public byte[] ReadMap(string path)
    {
        byte[] bytes = Pkg == null ? null : Pkg.Read(path);
        if (bytes != null)
        {
            return bytes;
        }

        return Pkg24 == null ? null : Pkg24.Read(path);
    }

    public byte[] ReadNpc(string mapName)
    {
        string path = "map/" + mapName + ".npc";
        byte[] bytes = Pkg == null ? null : Pkg.Read(path);
        return bytes ?? Read24Res(path);
    }

    public byte[] ReadRes(string pkgPath)
    {
        StatReads++;
        byte[] bytes = LoadRespatch(pkgPath);
        if (bytes != null)
        {
            return bytes;
        }

        bytes = Pkg == null ? null : Pkg.Read(pkgPath);
        if (bytes != null)
        {
            return bytes;
        }

        bytes = Read24Res(pkgPath);
        if (bytes != null)
        {
            return bytes;
        }

        bytes = Pkg24 == null ? null : Pkg24.Read(pkgPath);
        if (bytes != null)
        {
            return bytes;
        }

        StatMisses++;
        return null;
    }

    private static void EnsureRespatchIndex()
    {
        if (respatchIdx != null)
        {
            return;
        }

        respatchIdx = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string directory in UiPackDirs())
        {
            string patchDirectory = Path.Combine(directory, "respatch");
            if (!Directory.Exists(patchDirectory))
            {
                continue;
            }

            string[] files = Directory.GetFiles(
                patchDirectory, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string key = file.Substring(patchDirectory.Length + 1).Replace('\\', '/');
                if (!respatchIdx.ContainsKey(key))
                {
                    respatchIdx[key] = file;
                }
            }
        }

        if (respatchIdx.Count > 0)
        {
            Debug.Log("[ResLoader] respatch 补丁层: " + respatchIdx.Count.ToString() +
                " 个散文件覆盖 pkg");
        }
    }

    public byte[] LoadRespatch(string pkgPath)
    {
        EnsureRespatchIndex();
        if (respatchIdx.Count == 0 || string.IsNullOrEmpty(pkgPath))
        {
            return null;
        }

        string file;
        if (!respatchIdx.TryGetValue(pkgPath.Replace('\\', '/'), out file))
        {
            return null;
        }

        try
        {
            return File.ReadAllBytes(file);
        }
        catch (Exception error)
        {
            Debug.LogWarning("[ResLoader] respatch 读失败 " + file + ": " + error.Message);
        }

        return null;
    }

    public byte[] ReadRes24First(string pkgPath)
    {
        byte[] bytes = LoadRespatch(pkgPath);
        if (bytes != null)
        {
            return bytes;
        }

        bytes = Read24Res(pkgPath);
        if (bytes != null)
        {
            return bytes;
        }

        if (Pkg24 != null)
        {
            bytes = Pkg24.Read(pkgPath);
            if (bytes != null)
            {
                return bytes;
            }
        }

        if (Pkg != null)
        {
            return Pkg.Read(pkgPath);
        }

        return null;
    }

    public Texture2D LoadTex(string pkgPath, int frame = 0)
    {
        Texture2D cached;
        if (texCache.TryGetValue(pkgPath, out cached))
        {
            return cached;
        }

        byte[] data = ReadRes(pkgPath);
        Texture2D texture = null;
        if (data != null)
        {
            DimgDecoder.Result decoded = DimgDecoder.Decode(data, frame);
            if (decoded.ok)
            {
                texture = decoded.tex;
                GsaTextureScale.Register(texture, decoded.scale);
            }
        }

        texCache[pkgPath] = texture;
        return texture;
    }

    public Texture2D LoadTex24First(string pkgPath, int frame = 0)
    {
        string key = "@24f:" + pkgPath;
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = ReadRes24First(pkgPath);

        Texture2D texture = null;
        if (data != null)
        {
            DimgDecoder.Result decoded = DimgDecoder.Decode(data, frame);
            if (decoded.ok)
            {
                texture = decoded.tex;
                GsaTextureScale.Register(texture, decoded.scale);
            }
        }

        texCache[key] = texture;
        return texture;
    }

    public static byte[] Read24Res(string relPath)
    {
        Stat24Reads++;
        string filePath;
        if (_res24Cache.TryGetValue(relPath, out filePath))
        {
            if (!string.IsNullOrEmpty(filePath))
            {
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
        }
        else
        {
            filePath = string.Empty;
            if (_res24Dirs != null)
            {
                for (int index = 0; index < _res24Dirs.Length; index++)
                {
                    string candidate = Path.Combine(
                        _res24Dirs[index],
                        relPath.Replace('/', Path.DirectorySeparatorChar));
                    if (File.Exists(candidate))
                    {
                        filePath = candidate;
                        break;
                    }
                }
            }

            _res24Cache[relPath] = filePath;
            if (!string.IsNullOrEmpty(filePath))
            {
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
        }

        ResLoader loader = I;
        byte[] bytes = loader == null || loader.PkgFf21 == null
            ? null
            : loader.PkgFf21.Read(relPath);
        if (bytes != null)
        {
            return bytes;
        }

        return loader == null || loader.Pkg24 == null
            ? null
            : loader.Pkg24.Read(relPath);
    }

    public Texture2D LoadGsa(string pkgPath)
    {
        Texture2D cached;
        if (texCache.TryGetValue(pkgPath, out cached))
        {
            if (cached != null) MapImgTouch(pkgPath);
            return cached;
        }

        byte[] data = ReadRes(pkgPath);
        Texture2D texture = data == null ? null : GsaDecoder.Decode(data);
        texCache[pkgPath] = texture;
        MapImgNote(pkgPath, texture);
        return texture;
    }

    public Texture2D LoadGsa2021First(string pkgPath)
    {
        string key = "@21:" + pkgPath;
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = Read24Res(pkgPath);
        if (data == null)
        {
            data = ReadRes(pkgPath);
        }

        Texture2D texture = data == null ? null : GsaDecoder.Decode(data);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadGsaFrame2021First(string pkgPath, int frame)
    {
        string key = "@21f:" + pkgPath + "#" +
            frame.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = Read24Res(pkgPath);
        if (data == null)
        {
            data = ReadRes(pkgPath);
        }

        Texture2D texture = data == null ? null : GsaDecoder.DecodeFrame(data, frame);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadGsaLayer(string pkgPath, int layer)
    {
        string key = pkgPath + "@L" + layer.ToString();
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = ReadRes(pkgPath);
        Texture2D texture = data == null ? null : GsaDecoder.DecodeLayer(data, layer);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadGsa24(string pkgPath)
    {
        string key = "@24:" + pkgPath;
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = Pkg24 == null ? null : Pkg24.Read(pkgPath);
        if (data == null && Pkg != null)
        {
            data = Pkg.Read(pkgPath);
        }

        Texture2D texture = data == null ? null : GsaDecoder.Decode(data);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadGsaFrame24(string pkgPath, int frame)
    {
        string key = "@24:" + pkgPath + "#" + frame.ToString();
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = Pkg24 == null ? null : Pkg24.Read(pkgPath);
        if (data == null && Pkg != null)
        {
            data = Pkg.Read(pkgPath);
        }

        Texture2D texture = data == null ? null : GsaDecoder.DecodeFrame(data, frame);
        texCache[key] = texture;
        return texture;
    }

    public int GsaFrameCount(string pkgPath)
    {
        int cached;
        if (frameCntCache.TryGetValue(pkgPath, out cached))
        {
            return cached;
        }

        byte[] encoded = ReadRes(pkgPath);
        int frameCount = GsaDecoder.FrameCount(encoded);
        frameCntCache[pkgPath] = frameCount;
        return frameCount;
    }

    public Texture2D LoadGsaFrame(string pkgPath, int frame)
    {
        string key = pkgPath + "#" +
            frame.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = ReadRes(pkgPath);
        Texture2D texture = data == null ? null : GsaDecoder.DecodeFrame(data, frame);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadGsaFrameA(string pkgPath, int frame)
    {
        string key = pkgPath + "#A#" +
            frame.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = ReadRes(pkgPath);
        Texture2D texture = data == null
            ? null
            : GsaDecoder.DecodeFrameAssembled(data, frame);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadGsaFrameA2021First(string pkgPath, int frame)
    {
        string key = "@21A:" + pkgPath + "#" + frame.ToString();
        Texture2D cached;
        if (texCache.TryGetValue(key, out cached))
        {
            return cached;
        }

        byte[] data = Read24Res(pkgPath);
        if (data == null)
        {
            data = ReadRes(pkgPath);
        }

        Texture2D texture = data == null
            ? null
            : GsaDecoder.DecodeFrameAssembled(data, frame);
        texCache[key] = texture;
        return texture;
    }

    public Texture2D LoadUI(string relPath)
    {
        Texture2D result;
        if (uiCache.TryGetValue(relPath, out result))
        {
            return result;
        }

        result = null;
        foreach (string directory in UiPackDirs())
        {
            string filePath = Path.Combine(directory, relPath);
            if (!UiFileExists(filePath, relPath))
            {
                continue;
            }

            Texture2D candidate = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            byte[] bytes = ReadUiFile(filePath, relPath);
            if (ImageConversion.LoadImage(candidate, bytes))
            {
                candidate.filterMode = FilterMode.Point;
                candidate.wrapMode = TextureWrapMode.Clamp;
                result = candidate;
            }

            break;
        }

        uiCache[relPath] = result;
        return result;
    }

    private static IEnumerable<string> UiPackDirs()
    {
        yield return Path.Combine(
            ResourceFileRoot.Path,
            "gameassets");
    }

    public byte[] LoadUIBytes(string relPath)
    {
        foreach (string directory in UiPackDirs())
        {
            string filePath = Path.Combine(directory, relPath);
            if (!UiFileExists(filePath, relPath))
            {
                continue;
            }

            return ReadUiFile(filePath, relPath);
        }

        return null;
    }

    private bool UiFileExists(string candidatePath, string logicalPath)
    {
        return independentUiByteSource == null
            ? File.Exists(candidatePath)
            : independentUiByteSource.Exists(candidatePath, logicalPath);
    }

    private byte[] ReadUiFile(string candidatePath, string logicalPath)
    {
        return independentUiByteSource == null
            ? File.ReadAllBytes(candidatePath)
            : independentUiByteSource.ReadAllBytes(candidatePath, logicalPath);
    }

    public Texture2D LoadNpcTex(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
        {
            return null;
        }

        return LoadNpcFull(spriteName, 0).tex;
    }

    public DimgDecoder.Result LoadNpcFull(string spriteName)
    {
        return LoadNpcFull(spriteName, 0);
    }

    public DimgDecoder.Result LoadNpcFull(string spriteName, int storedDir)
    {
        if (string.IsNullOrEmpty(spriteName) || Pkg == null)
        {
            return default(DimgDecoder.Result);
        }

        string cacheKey = spriteName + "#" + storedDir.ToString();
        DimgDecoder.Result cached;
        if (npcFullCache.TryGetValue(cacheKey, out cached))
        {
            npcFullBudget.Touch(cacheKey);
            return cached;
        }

        string path = "avatar/npc/" + spriteName + "01.gsn";
        byte[] data = Pkg.Read(path);
        DimgDecoder.Result result = default(DimgDecoder.Result);
        if (data == null)
        {
            npcFullCache[cacheKey] = result;
            return result;
        }

        int wantFrame = 0;
        if (storedDir > 0 && data.Length >= 24)
        {
            int frames = BitConverter.ToInt32(data, 16);
            int dirs = BitConverter.ToInt32(data, 20);
            if (dirs >= 2 && storedDir < dirs)
            {
                wantFrame = (frames / dirs) * storedDir;
            }
        }

        result = DimgDecoder.Decode(data, wantFrame);
        npcFullCache[cacheKey] = result;
        long bytes = result.tex
            ? (long)result.tex.width * result.tex.height * 4L
            : 0L;
        npcFullBudget.Note(cacheKey, bytes);
        return result;
    }

    public long NpcArtBytes
    {
        get { return npcFullBudget.Bytes + npcAnimBudget.Bytes; }
    }

    public bool NpcArtOverBudget
    {
        get { return npcFullBudget.OverBudget || npcAnimBudget.OverBudget; }
    }

    public string NpcArtStat()
    {
        return npcFullBudget.Stat() + " · " + npcAnimBudget.Stat();
    }

    public void CollectNpcTextures(Action<Texture2D> sink)
    {
        if (sink == null)
        {
            return;
        }

        foreach (DimgDecoder.Result value in npcFullCache.Values)
        {
            if (value.tex)
            {
                sink(value.tex);
            }
        }

        foreach (ObjAnim value in npcAnimCache.Values)
        {
            CollectAnimTextures(value, sink);
        }
    }

    public void CollectObjTextures(Action<Texture2D> sink)
    {
        if (sink == null)
        {
            return;
        }

        foreach (ObjSprite value in objCache.Values)
        {
            if (value.tex)
            {
                sink(value.tex);
            }
        }

        foreach (ObjAnim value in objAnimCache.Values)
        {
            CollectAnimTextures(value, sink);
        }

        foreach (Texture2D value in contourCache.Values)
        {
            if (value)
            {
                sink(value);
            }
        }
    }

    public void CollectUiTextures(Action<Texture2D> sink)
    {
        if (sink == null)
        {
            return;
        }

        foreach (Texture2D value in texCache.Values)
        {
            if (value)
            {
                sink(value);
            }
        }

        foreach (Texture2D value in uiCache.Values)
        {
            if (value)
            {
                sink(value);
            }
        }
    }

    public (int, int) EvictNpcArtAtSafePoint()
    {
        int full = npcFullBudget.EvictAtSafePoint(DropNpcFull);
        int anim = npcAnimBudget.EvictAtSafePoint(DropNpcAnim);
        return (full, anim);
    }

    private void DropNpcFull(string key)
    {
        DimgDecoder.Result value;
        if (npcFullCache.TryGetValue(key, out value) && value.tex)
        {
            Destroy(value.tex);
        }

        npcFullCache.Remove(key);
    }

    private void DropNpcAnim(string key)
    {
        ObjAnim value;
        if (npcAnimCache.TryGetValue(key, out value) && value.frames != null)
        {
            foreach (Sprite sprite in value.frames)
            {
                if (sprite)
                {
                    if (sprite.texture)
                    {
                        Destroy(sprite.texture);
                    }

                    Destroy(sprite);
                }
            }
        }

        npcAnimCache.Remove(key);
    }

    private static void CollectAnimTextures(ObjAnim value, Action<Texture2D> sink)
    {
        if (value.frames == null)
        {
            return;
        }

        foreach (Sprite sprite in value.frames)
        {
            if (sprite && sprite.texture)
            {
                sink(sprite.texture);
            }
        }
    }
}

}
