using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity art owner (06000539..0600054F, 0600056A..0600057B,
/// 060005A7 and nested 060005A8..060005B2).
/// </summary>
public partial class MonsterEntity
{
    private sealed class ArtLayer
    {
        public int actId;
        public string suffix;
        public bool glow;
        public DimgDecoder.AnimGeom geom;
        public int dirs;
        public int fpd;
        public int gdirs;
        public int gfpd;
        public bool[] dirDone;
        public bool dead;

        public bool AllDone
        {
            get
            {
                if (dirDone == null || dirDone.Length < 1)
                {
                    return true;
                }

                for (int index = 0; index < dirDone.Length; index++)
                {
                    if (!dirDone[index])
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }

    private sealed class ArtJob
    {
        public int actId;
        public int model;
        public string mname;
        public float ppu;
        public ArtLayer body;
        public ArtLayer glow;
        public Sprite[,] grid;
        public Sprite[,] glowGrid;
        public bool wantGlow;
        public bool glowTried;
        public bool forHeadTop;
        public bool preferNative;
        public readonly ConcurrentQueue<ArtReady> ready =
            new ConcurrentQueue<ArtReady>();
        public Task running;
        public int pendingBytes;
        public int hintDir;
        // Official 1.5.8 reads ArtJob.cancelled (offset 0x68) with a full fence
        // after the load in PumpArtJobsInner/UploadReady/the decode task, and
        // writes it with the fence before the store in CancelArtJobs 06000604,
        // i.e. the field is volatile on both architectures.
        public volatile bool cancelled;
        public long bytes;
        public int frames;
        public readonly Stopwatch sw = Stopwatch.StartNew();
    }

    private struct ArtReady
    {
        public ArtLayer L;
        public int dir;
        public int f;
        public int idx;
        public int w;
        public int h;
        public int scale;
        public byte[] rgba;
        public bool is4444;
    }

    private sealed class ArtDriver : MonoBehaviour
    {
        private void Update()
        {
            PumpArtJobs();
            EvictGridsIfNeeded();
        }
    }

    private const int ArtUploadBytesPerFrame = 8388608;
    private const int ArtTexPerFrame = 20;
    // Official 1.5.8 has no second stopwatch here: PumpArtJobs 060005fa only calls
    // ArtUploadClock.BeginFrame/Resume/Pause around PumpArtJobsInner and UploadReady
    // 060005fc reads ArtUploadClock.get_Over (06000b7f) as its only clock.
    private static int _uploadedThisFrame;
    private const int ArtPendingBytesMax = 25165824;
    private const long AsyncMinDecodedBytes = 6291456L;
    private const long GlowHighWaterBytes = 301989888L;
    private const long NativeArtWaterBytes = 167772160L;
    private const bool NativeArtDowngrade = false;
    private static int _glowVer;

    internal static float TraceLastReadMs;
    internal static long TraceLastFileBytes;
    internal static int TraceLastScale;
    internal static long TraceLastNative;

    private const long GridBudgetBase = 671088640L;
    private static bool _gridBudgetHooked;
    private const float GridMinAgeSec = 20f;
    private static int _evictedActs;
    private static long _evictedBytes;
    private static int _gridGen;
    private List<int> _heldActs;
    private static int _evictTick;
    private static ArtDriver _artDriver;

    public int model;
    public string mname;
    private bool moving;
    private int standAct;
    private int walkAct;


    internal static int TraceArtJobs => _artJobs.Count;

    internal static int GlowVer
    {
        get => _glowVer;
        set => _glowVer = value;
    }

    private static void BumpGlowVer()
    {
        _glowVer = unchecked(_glowVer + 1);
    }

    private static long DecodedActualBytes(DimgDecoder.AnimGeom geom)
    {
        if (geom == null || geom.f == null)
        {
            return 0L;
        }

        long bytes = 0L;
        for (int index = 0; index < geom.f.Length; index++)
        {
            DimgDecoder.FrameGeom frame = geom.f[index];
            if (frame.srcLen > 0)
            {
                bytes += (long)frame.w * frame.h * 4L;
            }
        }

        return bytes;
    }

    private static Sprite[,] BeginGridJob(
        int actId,
        float pixelsPerUnit,
        int modelId,
        string monsterName,
        bool wantGlow,
        bool forHeadTop,
        bool isMine,
        out byte[] rawOut)
    {
        rawOut = null;
        if (_artJobs.ContainsKey(actId))
        {
            return null;
        }

        bool preferNative = ArtDetail.PreferNativeArt;
        TraceLastReadMs = 0f;
        TraceLastFileBytes = 0L;
        TraceLastScale = 0;
        TraceLastNative = 0L;
        long started = LoadTrace.Now;
        byte[] bytes = ReadNpcGsn(actId, string.Empty, preferNative);
        TraceLastReadMs = LoadTrace.Ms(started);
        if (bytes == null)
        {
            return null;
        }

        TraceLastFileBytes = bytes.Length;
        rawOut = bytes;
        DimgDecoder.AnimGeom geom = DimgDecoder.ParseAnimGeom(bytes);
        if (!geom.ok || geom.f == null || geom.src == null)
        {
            return null;
        }

        NoteBoundW(actId, geom.boundW);
        int scale = Math.Max(1, geom.scale);
        List<int> widths = new List<int>(geom.f.Length);
        for (int index = 0; index < geom.f.Length; index++)
        {
            widths.Add(geom.f[index].w / scale);
        }

        NoteFrameWidths(actId, widths);
        long decodedBytes = DecodedActualBytes(geom);
        TraceLastScale = geom.scale;
        TraceLastNative = decodedBytes;
        if (decodedBytes < AsyncMinDecodedBytes)
        {
            return null;
        }

        int directions = Math.Max(1, geom.dirs);
        int framesPerDirection = Math.Max(1, geom.frames / directions);
        bool keepGlow = wantGlow;
        if (!(isMine && !GameSettings.HideMyPetGlow) && keepGlow &&
            GridCacheBytes > GlowHighWaterBytes)
        {
            keepGlow = false;
        }

        ArtLayer body = new ArtLayer
        {
            actId = actId,
            suffix = string.Empty,
            geom = geom,
            dirs = directions,
            fpd = framesPerDirection,
            dirDone = new bool[directions],
        };
        ArtJob job = new ArtJob
        {
            actId = actId,
            model = modelId,
            mname = monsterName,
            ppu = pixelsPerUnit,
            body = body,
            grid = new Sprite[directions, framesPerDirection],
            wantGlow = keepGlow,
            forHeadTop = forHeadTop,
            preferNative = preferNative,
        };
        _artJobs.Add(actId, job);
        EnsureArtDriver();
        return job.grid;
    }

    private static void PumpArtJobs()
    {
        if (_artJobs.Count < 1)
        {
            return;
        }

        ArtUploadClock.BeginFrame();
        _uploadedThisFrame = 0;
        ArtUploadClock.Resume();
        PumpArtJobsInner();
        ArtUploadClock.Pause();
    }

    private void ArtHint()
    {
        int actId = moving ? walkAct : standAct;
        if (actId < 1)
        {
            return;
        }

        ArtJob job;
        if (_artJobs.TryGetValue(actId, out job) && job != null)
        {
            job.hintDir = curDir;
        }
    }

    private static void UploadReady(ArtJob job, ref int budget, ref int quota)
    {
        while (budget > 0 && quota > 0)
        {
            // Official 1.5.8 reads ArtUploadClock.get_Over (06000b7f) here, the same clock
            // PumpArtJobs 060005fa started with BeginFrame/Resume.
            if (_uploadedThisFrame > 0 && ArtUploadClock.Over)
            {
                return;
            }

            ArtReady ready;
            if (!job.ready.TryDequeue(out ready))
            {
                return;
            }

            // Official 1.5.8 accounts the decoded size, not the buffer length:
            // Interlocked.Add(ref j.pendingBytes, -(w * h << (is4444 ? 1 : 2))) runs before
            // the cancelled/L/dead/rgba gate (arm64 0x484cf8 / x86_64 0x49c316).
            int byteCount = (ready.w * ready.h) << (ready.is4444 ? 1 : 2);
            Interlocked.Add(ref job.pendingBytes, -byteCount);
            if (job.cancelled || ready.L == null || ready.L.dead ||
                ready.rgba == null)
            {
                continue;
            }

            Sprite[,] target = ready.L.glow ? job.glowGrid : job.grid;
            if (target == null || ready.dir < 0 || ready.f < 0 ||
                ready.dir >= target.GetLength(0) || ready.f >= target.GetLength(1))
            {
                continue;
            }

            // Official 1.5.8 counts a dequeued item as an upload only after every gate above
            // and before TintIfColorless (arm64 0x484d8c / x86_64 0x49c3b9).
            _uploadedThisFrame++;

            if (!ready.L.glow)
            {
                TintIfColorless(job, ready.rgba, ready.is4444);
            }

            bool keepReadable = !ready.L.glow &&
                !_colorlessModel.ContainsKey(job.model);
            Texture2D texture = ready.is4444
                ? DimgDecoder.TexFrom4444(
                    ready.w,
                    ready.h,
                    ready.rgba,
                    ready.scale,
                    keepReadable)
                : DimgDecoder.TexFromRgba(
                    ready.w,
                    ready.h,
                    ready.rgba,
                    ready.scale,
                    keepReadable);
            if (texture == null)
            {
                if (_texFailWarned.Add(job.actId))
                {
                    UnityEngine.Debug.LogWarning(
                        "MonsterEntity texture upload failed: " + job.actId);
                }
                continue;
            }

            Sprite sprite = null;
            if (!ready.L.glow)
            {
                DimgDecoder.FrameGeom frame = ready.L.geom.f[ready.idx];
                sprite = AnchorSpriteAt(
                    texture,
                    ready.L.geom.boundCx,
                    ready.L.geom.boundCy,
                    frame.nLeft,
                    frame.nTop,
                    ready.scale,
                    job.ppu);
                if (job.forHeadTop && sprite != null)
                {
                    HeadTopFromRgba(job, ready, sprite);
                }
            }
            else
            {
                int baseIndex = ready.f + job.body.fpd * ready.dir;
                // Official 1.5.8 drops the texture instead of throwing when the body frame
                // index is out of range: negative or >= f.Length falls through to
                // Object.Destroy (arm64 0x484eac/0x484ec8 -> 0x484c44,
                // x86_64 0x49c4ed/0x49c510 -> 0x49c75a) and then continues the loop.
                if (baseIndex >= 0 && baseIndex < job.body.geom.f.Length)
                {
                    DimgDecoder.FrameGeom baseFrame = job.body.geom.f[baseIndex];
                    DimgDecoder.FrameGeom glowFrame = ready.L.geom.f[ready.idx];
                    sprite = AnchorSpriteAt(
                        texture,
                        job.body.geom.boundCx,
                        job.body.geom.boundCy,
                        glowFrame.nLeft + baseFrame.nLeft,
                        glowFrame.nTop + baseFrame.nTop,
                        ready.scale,
                        job.ppu);
                }
            }

            if (sprite == null)
            {
                DestroyOwned(texture);
                continue;
            }

            target[ready.dir, ready.f] = sprite;
            if (ready.L.glow)
            {
                Sprite baseSprite = job.grid == null
                    ? null
                    : job.grid[ready.dir, ready.f];
                if (baseSprite != null)
                {
                    _glowOf[baseSprite] = sprite;
                    BumpGlowVer();
                }
            }

            budget -= byteCount;
            quota--;
            job.bytes += byteCount;
            job.frames++;
            if (LoadTrace.On)
            {
                LoadTrace.Upload(1, byteCount);
            }
        }
    }

    private static void TintIfColorless(ArtJob job, byte[] rgba, bool is4444)
    {
        bool colorless;
        if (!_colorlessModel.TryGetValue(job.model, out colorless))
        {
            int verdict = ColorlessVerdict(rgba, is4444);
            if (verdict < 0)
            {
                return;
            }

            colorless = verdict != 0;
            _colorlessModel[job.model] = colorless;
            if (colorless)
            {
                _recoloredModel.Add(job.model);
                if (job.frames > 0)
                {
                    UnityEngine.Debug.LogWarning(string.Format(
                        "[MonsterArt] 剪影怪判定晚于上传 model={0} {1}: 已有 {2} 帧未染色上屏 → 补染。" +
                        "(说明先解到的那个方向首帧内容太少; 表现为黑剪影闪一下)",
                        job.model,
                        job.mname,
                        job.frames));
                    RecolorGrid(job.grid, new HashSet<Texture2D>());
                }
            }
        }

        if (!colorless || rgba == null)
        {
            return;
        }

        if (is4444)
        {
            for (int index = 1; index < rgba.Length; index += 2)
            {
                byte high = rgba[index];
                if (high > 15)
                {
                    rgba[index - 1] = (byte)((MaskColor.g & 240) | (MaskColor.b >> 4));
                    rgba[index] = (byte)((high & 240) | (MaskColor.r >> 4));
                }
            }
        }
        else
        {
            for (int index = 3; index < rgba.Length; index += 4)
            {
                if (rgba[index] != 0)
                {
                    rgba[index - 3] = MaskColor.r;
                    rgba[index - 2] = MaskColor.g;
                    rgba[index - 1] = MaskColor.b;
                }
            }
        }
    }

    private static void RecolorGrid(
        Sprite[,] grid,
        HashSet<Texture2D> done)
    {
        if (grid == null)
        {
            return;
        }

        for (int direction = grid.GetLowerBound(0);
             direction <= grid.GetUpperBound(0);
             direction++)
        {
            for (int frame = grid.GetLowerBound(1);
                 frame <= grid.GetUpperBound(1);
                 frame++)
            {
                Sprite sprite = grid[direction, frame];
                Texture2D texture = sprite == null ? null : sprite.texture;
                if (texture == null || !texture.isReadable ||
                    done == null || !done.Add(texture))
                {
                    continue;
                }

                Color32[] pixels = texture.GetPixels32();
                for (int index = 0; index < pixels.Length; index++)
                {
                    if (pixels[index].a != 0)
                    {
                        pixels[index].r = MaskColor.r;
                        pixels[index].g = MaskColor.g;
                        pixels[index].b = MaskColor.b;
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();
            }
        }
    }

    private static void HeadTopFromRgba(ArtJob job, ArtReady ready, Sprite sprite)
    {
        if (_headTopCache.ContainsKey(job.model) || ready.rgba == null ||
            ready.w < 1 || ready.h < 1)
        {
            return;
        }

        int required = ready.w * ready.h * (ready.is4444 ? 2 : 4);
        if (required > ready.rgba.Length)
        {
            return;
        }

        int opaqueRow = -1;
        for (int row = ready.h - 1; row >= 0 && opaqueRow < 0; row--)
        {
            int offset = row * ready.w * (ready.is4444 ? 2 : 4) +
                         (ready.is4444 ? 1 : 3);
            int stride = ready.is4444 ? 2 : 4;
            for (int x = 0; x < ready.w; x++, offset += stride)
            {
                if (ready.rgba[offset] > (ready.is4444 ? 15 : 16))
                {
                    opaqueRow = row;
                    break;
                }
            }
        }

        if (opaqueRow < 0 || sprite == null)
        {
            return;
        }

        float spritePpu = sprite.pixelsPerUnit > 0.001f
            ? sprite.pixelsPerUnit
            : job.ppu;
        float pixelsAbovePivot = opaqueRow + 1f - sprite.pivot.y;
        if (pixelsAbovePivot > 0f && spritePpu > 0.001f)
        {
            _headTopCache[job.model] = pixelsAbovePivot / spritePpu;
        }
    }

    private static int ColorlessVerdict(byte[] rgba, bool is4444)
    {
        if (rgba == null || rgba.Length < (is4444 ? 2 : 4))
        {
            return -1;
        }

        int visible = 0;
        int colored = 0;
        if (is4444)
        {
            for (int highIndex = 1; highIndex < rgba.Length; highIndex += 14)
            {
                byte high = rgba[highIndex];
                if (high > 15)
                {
                    visible++;
                    if (rgba[highIndex - 1] != 0 || (high & 15) != 0)
                    {
                        colored++;
                    }
                }
            }
        }
        else
        {
            for (int alphaIndex = 3; alphaIndex < rgba.Length; alphaIndex += 28)
            {
                if (rgba[alphaIndex] > 16)
                {
                    visible++;
                    if (rgba[alphaIndex - 3] > 16 ||
                        rgba[alphaIndex - 2] > 16 ||
                        rgba[alphaIndex - 1] > 16)
                    {
                        colored++;
                    }
                }
            }
        }

        return visible >= 31 && colored * 100 / visible < 3 ? 1 :
            visible >= 31 ? 0 : -1;
    }

    private static void ScheduleNextArt(ArtJob job)
    {
        if (job == null)
        {
            return;
        }

        if (job.body != null && !job.body.dead && !job.body.AllDone)
        {
            StartDirTask(job, job.body);
            return;
        }

        if (job.body != null && job.body.geom != null)
        {
            job.body.geom.src = null;
        }

        if (!job.ready.IsEmpty)
        {
            return;
        }

        if (job.wantGlow && !job.glowTried)
        {
            job.glowTried = true;
            OpenGlowLayer(job);
            return;
        }

        if (job.glow != null && !job.glow.dead && !job.glow.AllDone)
        {
            StartDirTask(job, job.glow);
            return;
        }

        FinishArtJob(job);
    }

    private static void OpenGlowLayer(ArtJob job)
    {
        if (job == null || job.body == null || job.body.geom == null ||
            _glowGridCache.ContainsKey(job.actId))
        {
            return;
        }

        byte[] bytes = ReadNpcGsn(job.actId, "e", job.preferNative);
        DimgDecoder.AnimGeom geom = bytes == null
            ? null
            : DimgDecoder.ParseAnimGeom(bytes);
        if (geom == null || !geom.ok || geom.f == null)
        {
            _glowGridCache[job.actId] = null;
            return;
        }

        int glowDirections = Math.Max(1, geom.dirs);
        int glowFramesPerDirection = Math.Max(1, geom.frames / glowDirections);
        job.glowGrid = new Sprite[job.body.dirs, job.body.fpd];
        job.glow = new ArtLayer
        {
            actId = job.actId,
            suffix = "e",
            glow = true,
            geom = geom,
            dirs = job.body.dirs,
            fpd = job.body.fpd,
            gdirs = glowDirections,
            gfpd = glowFramesPerDirection,
            dirDone = new bool[job.body.dirs],
        };
        _glowGridCache[job.actId] = job.glowGrid;
    }

    private static void StartDirTask(ArtJob job, ArtLayer layer)
    {
        if (job == null || layer == null || layer.dirDone == null ||
            layer.dirDone.Length < 1)
        {
            return;
        }

        int direction = Mathf.Clamp(job.hintDir, 0, layer.dirs - 1);
        if (layer.dirDone[direction])
        {
            direction = -1;
            for (int index = 0; index < layer.dirs; index++)
            {
                if (!layer.dirDone[index])
                {
                    direction = index;
                    break;
                }
            }
        }

        if (direction < 0)
        {
            return;
        }

        layer.dirDone[direction] = true;
        int selectedDirection = direction;
        job.running = Task.Run(() =>
        {
            for (int frameNumber = 0; frameNumber < layer.fpd; frameNumber++)
            {
                if (job.cancelled)
                {
                    return;
                }

                int sourceIndex;
                if (!layer.glow)
                {
                    sourceIndex = frameNumber + layer.fpd * selectedDirection;
                }
                else
                {
                    int glowDirection = Math.Min(selectedDirection, layer.gdirs - 1);
                    sourceIndex = frameNumber % layer.gfpd + layer.gfpd * glowDirection;
                }

                if (sourceIndex < 0 || sourceIndex >= layer.geom.f.Length)
                {
                    continue;
                }

                DimgDecoder.FrameGeom frame = layer.geom.f[sourceIndex];
                if (frame.srcLen < 1 || layer.glow && (frame.flags & 256) == 0)
                {
                    continue;
                }

                bool is4444 = DimgDecoder.FrameIs4444(layer.geom, sourceIndex);
                byte[] rgba = is4444
                    ? DimgDecoder.FrameTo4444(layer.geom, sourceIndex)
                    : DimgDecoder.FrameToRgba(layer.geom, sourceIndex);
                if (rgba == null)
                {
                    continue;
                }

                Interlocked.Add(ref job.pendingBytes, rgba.Length);
                job.ready.Enqueue(new ArtReady
                {
                    L = layer,
                    dir = selectedDirection,
                    f = frameNumber,
                    idx = sourceIndex,
                    w = frame.w,
                    h = frame.h,
                    scale = Math.Max(1, layer.geom.scale),
                    rgba = rgba,
                    is4444 = is4444,
                });
            }
        });
    }

    private static void FinishArtJob(ArtJob job)
    {
        if (!job.ready.IsEmpty)
        {
            return;
        }

        if (job.body != null && job.body.geom != null)
        {
            job.body.geom.src = null;
        }

        if (job.glow != null && job.glow.geom != null)
        {
            job.glow.geom.src = null;
        }

        _artJobs.Remove(job.actId);
        if (job.frames == 0)
        {
            UnityEngine.Debug.LogWarning(string.Format(
                "[MonsterArt] ⚠后台解码完成但 0 帧上屏 npc{0}.gsn model={1} {2}",
                job.actId, job.model, job.mname) + string.Format(
                "(方向{0}/每向{1}帧) —— 这只在游戏里会是隐形的, 请把这行发回来。",
                job.body != null ? job.body.dirs : 0,
                job.body != null ? job.body.fpd : 0));
        }

        if (LoadTrace.On)
        {
            LoadTrace.GridDone(
                job.actId,
                job.frames,
                job.bytes,
                (float)job.sw.Elapsed.TotalMilliseconds);
        }
    }

    private static void CancelArtJobs()
    {
        foreach (ArtJob job in _artJobs.Values)
        {
            job.grid = null;
            job.cancelled = true;
            job.glowGrid = null;
        }

        _artJobs.Clear();
    }

    private static void CancelGlowJobs()
    {
        foreach (ArtJob job in _artJobs.Values)
        {
            job.wantGlow = false;
            job.glowTried = true;
            if (job.glow != null)
            {
                job.glow.dead = true;
            }

            job.glowGrid = null;
        }
    }

    private static void EnsureArtDriver()
    {
        if (_artDriver != null)
        {
            return;
        }

        GameObject driverObject = new GameObject("MonsterArtDecoder");
        driverObject.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(driverObject);
        _artDriver = driverObject.AddComponent<ArtDriver>();
    }

    private static void EnsureGridBudgetHook()
    {
        if (_gridBudgetHooked)
        {
            return;
        }

        _gridBudgetHooked = true;
        ArtDetail.OnBudgetScaleChanged += () =>
            GridBudgetBytes = (long)(ArtDetail.BudgetScale * GridBudgetBase);
        GridBudgetBytes = (long)(ArtDetail.BudgetScale * GridBudgetBase);
    }

    private static void NoteGridUse(int actId, int modelId)
    {
        int count;
        _gridRef.TryGetValue(actId, out count);
        _gridRef[actId] = count + 1;
        _gridLru[actId] = Time.realtimeSinceStartup;
        _actModel[actId] = modelId;
    }

    private void NoteHeld(int actId)
    {
        if (_heldGen != _gridGen)
        {
            if (_heldActs != null)
            {
                _heldActs.Clear();
            }
            _heldGen = _gridGen;
        }

        if (_heldActs == null)
        {
            _heldActs = new List<int>();
        }

        _heldActs.Add(actId);
        NoteGridUse(actId, model);
    }

    private void ReleaseHeldGrids()
    {
        if (_heldActs == null)
        {
            return;
        }

        if (_heldGen == _gridGen)
        {
            float now = Time.realtimeSinceStartup;
            for (int index = 0; index < _heldActs.Count; index++)
            {
                int actId = _heldActs[index];
                int count;
                if (_gridRef.TryGetValue(actId, out count))
                {
                    _gridRef[actId] = Math.Max(0, count - 1);
                    _gridLru[actId] = now;
                }
            }
        }

        _heldActs.Clear();
    }

    private Sprite[,] LoadGrid(int actId)
    {
        if (actId < 1)
        {
            return null;
        }

        EnsureGridBudgetHook();
        if (_gridCache.Count == 0)
        {
            ArtDetail.Adopt();
        }
        NoteHeld(actId);

        Sprite[,] cached;
        if (_gridCache.TryGetValue(actId, out cached))
        {
            if (LoadTrace.On)
                LoadTrace.Grid(actId, model, mname, "缓存命中", 0L, 0, 0L, 0f);
            return cached;
        }

        bool wantGlow = !friendly || AnyPetGlowWanted;
        byte[] raw;
        Sprite[,] grid = BeginGridJob(
            actId,
            ppu,
            model,
            mname,
            wantGlow,
            actId == standAct,
            IsMyPet,
            out raw);
        if (grid == null)
        {
            long started = LoadTrace.Now;
            grid = DecodeGrid(actId, raw);
            if (LoadTrace.On)
            {
                LoadTrace.Grid(actId, model, mname, grid == null ? "读不到" : "同步解码",
                    TraceLastFileBytes, TraceLastScale, TraceLastNative,
                    TraceLastReadMs + LoadTrace.Ms(started));
            }
        }
        else if (LoadTrace.On)
        {
            LoadTrace.Grid(actId, model, mname, "后台开工",
                TraceLastFileBytes, TraceLastScale, TraceLastNative, TraceLastReadMs);
        }

        _gridCache[actId] = grid;
        return grid;
    }

    private Sprite[,] DecodeGrid(int actId, byte[] pre = null)
    {
        byte[] bytes = pre ?? ReadNpcGsn(actId, string.Empty, ArtDetail.PreferNativeArt);
        if (bytes == null) return null;

        DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(bytes);
        if (!anim.ok || anim.f == null) return null;
        if (anim.boundW > 0) _boundWOf[actId] = anim.boundW;

        List<int> widths = new List<int>();
        foreach (DimgDecoder.FrameTex frame in anim.f)
            widths.Add(frame.tex != null ? frame.tex.width / Math.Max(1, anim.scale) : 0);
        NoteFrameWidths(actId, widths);

        int directions = anim.dirs < 2 ? 1 : anim.dirs;
        int framesPerDirection = anim.frames / directions;
        if (framesPerDirection < 2) framesPerDirection = 1;
        Sprite[,] grid = new Sprite[directions, framesPerDirection];
        for (int direction = 0; direction < directions; direction++)
        {
            for (int frameNumber = 0; frameNumber < framesPerDirection; frameNumber++)
            {
                int index = frameNumber + framesPerDirection * direction;
                if (index >= anim.f.Length) continue;
                DimgDecoder.FrameTex frame = anim.f[index];
                if (frame.tex == null) continue;
                grid[direction, frameNumber] = AnchorSpriteAt(frame.tex,
                    anim.boundCx, anim.boundCy, frame.nLeft, frame.nTop, anim.scale, ppu);
            }
        }

        DecodeGlowInto(actId, anim, grid);
        return grid;
    }

    private static Sprite AnchorSpriteAt(
        Texture2D texture,
        int boundCenterX,
        int boundCenterY,
        int frameLeft,
        int frameTop,
        int scale,
        float pixelsPerUnit)
    {
        // Official 1.5.8 AnchorSpriteAt (06000639) has exactly one early return: the Unity-null
        // check Object.op_Equality(texture, null). The dual-architecture comparison list
        // (arm64 0x485fd0 / x86_64 0x49d4c9) contains no comparison against texture.width or
        // texture.height, so the product's extra size guard is removed here.
        if (texture == null)
        {
            return null;
        }

        int safeScale = Math.Max(scale, 1);
        Vector2 pivot = boundCenterX == 0 && boundCenterY == 0
            ? new Vector2(0.5f, 0.06f)
            : new Vector2(
                (float)((boundCenterX - frameLeft) * safeScale) / texture.width,
                1f - (float)((boundCenterY - frameTop) * safeScale) / texture.height);
        // Official arm64 target 0x10168f0 is the 4-parameter overload
        // Sprite.Create(Texture2D, Rect, Vector2, Single); extrude and meshType are not passed
        // (x86_64 0x100dd90 has the same operand shape). Restored as the 4-parameter call.
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            pivot,
            pixelsPerUnit * safeScale);
    }

    private static long TexBytesOf(Texture2D texture)
    {
        if (texture == null)
        {
            return 0L;
        }

        long bytes = (long)texture.width * texture.height;
        TextureFormat format = texture.format;
        bytes *= format == TextureFormat.ARGB4444 || format == TextureFormat.RGBA4444
            ? 2L
            : 4L;
        if (texture.mipmapCount > 1)
        {
            bytes = bytes * 4L / 3L;
        }

        return bytes;
    }

    internal static long GridCacheBytes
    {
        get
        {
            long bytes = 0L;
            foreach (Sprite[,] grid in _gridCache.Values)
            {
                bytes += GridBytes(grid);
            }
            foreach (Sprite[,] grid in _glowGridCache.Values)
            {
                bytes += GridBytes(grid);
            }
            return bytes;
        }
    }

    private static long GridBytes(Sprite[,] grid)
    {
        if (grid == null)
        {
            return 0L;
        }

        long bytes = 0L;
        for (int direction = 0; direction < grid.GetLength(0); direction++)
        {
            for (int frame = 0; frame < grid.GetLength(1); frame++)
            {
                Sprite sprite = grid[direction, frame];
                if (sprite != null)
                {
                    bytes += TexBytesOf(sprite.texture);
                }
            }
        }
        return bytes;
    }

    private static void EvictGridsIfNeeded()
    {
        _evictTick++;
        if (_evictTick < 30)
        {
            return;
        }

        _evictTick = 0;
        long bytes = GridCacheBytes;
        if (bytes <= GridBudgetBytes)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        _evictBuf.Clear();
        foreach (KeyValuePair<int, Sprite[,]> pair in _gridCache)
        {
            int actId = pair.Key;
            int refs;
            float last;
            if (_artJobs.ContainsKey(actId) ||
                _gridRef.TryGetValue(actId, out refs) && refs > 0 ||
                _gridLru.TryGetValue(actId, out last) && now - last < GridMinAgeSec)
            {
                continue;
            }
            _evictBuf.Add(actId);
        }

        _evictBuf.Sort((left, right) =>
        {
            float leftLast;
            float rightLast;
            _gridLru.TryGetValue(left, out leftLast);
            _gridLru.TryGetValue(right, out rightLast);
            return leftLast.CompareTo(rightLast);
        });

        int acts = 0;
        long releasedBytes = 0L;
        for (int index = 0;
             index < _evictBuf.Count && bytes > GridBudgetBytes;
             index++)
        {
            long released = EvictOneGrid(_evictBuf[index]);
            bytes -= released;
            releasedBytes += released;
            acts++;
        }

        _evictedActs += acts;
        _evictedBytes += releasedBytes;
    }

    private static long EvictOneGrid(int actId)
    {
        long bytes = 0L;
        Sprite[,] grid;
        if (_gridCache.TryGetValue(actId, out grid))
        {
            bytes += DestroyGridAndCount(grid, true);
        }
        _gridCache.Remove(actId);

        if (_glowGridCache.TryGetValue(actId, out grid))
        {
            bytes += DestroyGridAndCount(grid, false);
        }
        _glowGridCache.Remove(actId);
        _recoloredAct.Remove(actId);

        int modelId;
        if (_actModel.TryGetValue(actId, out modelId))
        {
            bool modelStillCached = false;
            foreach (KeyValuePair<int, int> pair in _actModel)
            {
                if (pair.Key != actId && pair.Value == modelId &&
                    _gridCache.ContainsKey(pair.Key))
                {
                    modelStillCached = true;
                    break;
                }
            }
            if (!modelStillCached)
            {
                _headTopCache.Remove(modelId);
                _recoloredModel.Remove(modelId);
            }
        }

        _actModel.Remove(actId);
        _gridRef.Remove(actId);
        _gridLru.Remove(actId);
        BumpGlowVer();
        return bytes;
    }

    private static long DestroyGridAndCount(Sprite[,] grid, bool removeGlowMap)
    {
        if (grid == null)
        {
            return 0L;
        }

        long bytes = 0L;
        for (int direction = 0; direction < grid.GetLength(0); direction++)
        {
            for (int frame = 0; frame < grid.GetLength(1); frame++)
            {
                Sprite sprite = grid[direction, frame];
                if (sprite == null)
                {
                    continue;
                }
                if (removeGlowMap)
                {
                    _glowOf.Remove(sprite);
                }
                Texture2D texture = sprite.texture;
                if (texture != null)
                {
                    bytes += (long)texture.width * texture.height * 4L;
                    DestroyOwned(texture);
                }
                DestroyOwned(sprite);
                grid[direction, frame] = null;
            }
        }
        return bytes;
    }

    private static void DestroyGrid(Sprite[,] grid)
    {
        DestroyGridAndCount(grid, false);
    }

    public static void ClearGridCache()
    {
        // Official 1.5.8 0600062a keeps every decoded grid across an ordinary
        // owner boundary: it only clears when a pending art tier still has to be
        // adopted (ArtDetail.AppliedScale != ArtDetail.PixelScale) or when the
        // cache is already over the shared budget.
        if (ArtDetail.AppliedScale == ArtDetail.PixelScale &&
            GridCacheBytes <= GridBudgetBytes)
        {
            return;
        }

        CancelArtJobs();
        foreach (Sprite[,] grid in _gridCache.Values)
        {
            DestroyGrid(grid);
        }
        _gridCache.Clear();
        foreach (Sprite[,] grid in _glowGridCache.Values)
        {
            DestroyGrid(grid);
        }
        _glowGridCache.Clear();
        // Official 1.5.8 0600062a never touches _glowOf (static offset 0xc8) on
        // either architecture: the glow mapping is only edited per sprite on the
        // ordinary eviction path, so a full clear leaves it alone.
        foreach (Sprite sprite in _firstCache.Values)
        {
            if (sprite == null)
            {
                continue;
            }

            Texture2D texture = sprite.texture;
            if (texture != null)
            {
                DestroyOwned(texture);
            }
            DestroyOwned(sprite);
        }
        _firstCache.Clear();
        _headTopCache.Clear();
        _recoloredModel.Clear();
        _recoloredAct.Clear();
        _gridGen++;
        _gridRef.Clear();
        _gridLru.Clear();
        _actModel.Clear();
        ArtDetail.Adopt();
    }

    internal static void NoteBoundW(int actId, int boundWidth)
    {
        if (boundWidth >= 1)
        {
            _boundWOf[actId] = boundWidth;
        }
    }

    internal static int BoundWOf(int actId)
    {
        int value;
        return _boundWOf.TryGetValue(actId, out value) ? value : 0;
    }

    internal static int FrameWOf(int actId)
    {
        int value;
        return _frameWOf.TryGetValue(actId, out value) ? value : 0;
    }

    internal static void NoteFrameWidths(int actId, List<int> widths)
    {
        if (widths == null || widths.Count == 0 || _frameWOf.ContainsKey(actId))
        {
            return;
        }

        widths.Sort();
        int firstPositive = 0;
        while (firstPositive < widths.Count && widths[firstPositive] < 1)
        {
            firstPositive++;
        }
        int positiveCount = widths.Count - firstPositive;
        if (positiveCount >= 1)
        {
            _frameWOf[actId] = widths[firstPositive + (positiveCount >> 1)];
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

#if UNITY_EDITOR
    internal static int GridCacheCountForAudit => _gridCache.Count;

    internal static int GridReferenceCountForAudit(int actId)
    {
        int value;
        return _gridRef.TryGetValue(actId, out value) ? value : 0;
    }

    internal static long GridBudgetBytesForAudit => GridBudgetBytes;

    internal static int EvictedActionCountForAudit => _evictedActs;

    internal static long EvictedBytesForAudit => _evictedBytes;

    internal static int PendingArtJobCountForAudit => _artJobs.Count;

    internal static void SetGridBudgetForAudit(long value)
    {
        GridBudgetBytes = value;
    }

    internal static void AgeAllGridsForAudit(float ageSeconds)
    {
        float stamp = Time.realtimeSinceStartup - ageSeconds;
        foreach (int actId in _gridCache.Keys)
        {
            _gridLru[actId] = stamp;
        }
    }

    internal static void ResetEvictionTickForAudit()
    {
        _evictTick = 0;
    }

    internal static void EvictGridsIfNeededForAudit()
    {
        EvictGridsIfNeeded();
    }

    internal static void PumpArtJobsForAudit()
    {
        PumpArtJobs();
    }

    internal static void ClearGridCacheForAudit()
    {
        ClearGridCache();
    }
#endif
}

}
