using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class MountAvatar
{
    private static void StartJob(Data d)
    {
        int stand = NpcActions.Find(d.model, "Stand");
        int walk = NpcActions.Find(d.model, "Walk");
        Job job = new Job { d = d };
        Volatile.Write(ref job.hintDir, 0);
        Volatile.Write(ref job.hintWalk, false);
        if (stand > 0) job.layers.Add(new LayerTask { actId = stand, suffix = "", walk = false, kind = 0 });
        if (walk > 0) job.layers.Add(new LayerTask { actId = walk, suffix = "", walk = true, kind = 0 });
        if (stand > 0) job.layers.Add(new LayerTask { actId = stand, suffix = "t", walk = false, kind = 1 });
        if (walk > 0) job.layers.Add(new LayerTask { actId = walk, suffix = "t", walk = true, kind = 1 });
        long total = CacheBytes;
        long highWater = (long)((float)BudgetBytes * HighWaterFrac);
        GameSettings.EnsureLoaded();
        bool mineGlow = d.mine && !GameSettings.HideMyPetGlow;
        d.lowDetail = !mineGlow && total > highWater;
        if (GameSettings.AnyRideGlowWanted && (mineGlow || total <= highWater))
        {
            if (stand > 0) job.layers.Add(new LayerTask { actId = stand, suffix = "e", walk = false, kind = 2 });
            if (walk > 0) job.layers.Add(new LayerTask { actId = walk, suffix = "e", walk = true, kind = 2 });
        }
        d.topAdditive = false;
        d.fxAdditive = false;
        d.job = job;
        if (job.layers.Count == 0)
        {
            d.job = null;
            d.ok = false;
            if (_missWarned.Add(-d.model))
                Debug.LogWarning(string.Format("[MountAvatar] ★★model={0} 没有任何可用美术层 ⇒ 骑上去完全不可见(原因见上一条 ★坐骑本体…)。", d.model));
        }
    }

    private static byte[] ReadGsn(int actId, string suffix)
    {
        string path = string.Format("avatar/npc/npc{0}{1}.gsn", actId, suffix);
        return ResLoader.Read24Res(path) ?? ResLoader.I?.Pkg?.Read(path);
    }

    private static void Tick()
    {
        ArtUploadClock.BeginFrame();
        _mountUploadedThisFrame = 0;
        ArtUploadClock.Resume();
        try { TickInner(); }
        finally { ArtUploadClock.Pause(); }
    }

    private static void TickInner()
    {
        float now = Time.realtimeSinceStartup;
        int uploadBytes = MaxUploadBytesPerFrame;
        int textureCount = MaxTexPerFrame;
        bool readThisFrame = false;
        long pending = 0;
        foreach (var pair in cache)
            if (pair.Value.job != null)
                pending += Interlocked.CompareExchange(ref pair.Value.job.pendingBytes, 0, 0);
        bool allowSchedule = pending <= MaxPendingBytes;
        foreach (var pair in cache)
        {
            Data d = pair.Value;
            Job job = d.job;
            if (job == null) continue;
            PumpUploads(d, job, ref uploadBytes, ref textureCount);
            if (allowSchedule && (job.running == null || job.running.IsCompleted))
            {
                job.running = null;
                ScheduleNext(d, job, ref readThisFrame);
            }
        }
        EvictIfNeeded(now);
    }

    private static void FinishJob(Data d, Job job)
    {
        if (!job.ready.IsEmpty) return;
        foreach (LayerTask layer in job.layers)
            if (layer.geom != null) layer.geom.src = null;
        d.job = null;
    }

    private static void PumpUploads(Data d, Job job, ref int uploadBytes, ref int textureCount)
    {
        while (textureCount > 0 && uploadBytes > 0)
        {
            if (_mountUploadedThisFrame > 0 && ArtUploadClock.Over) break;
            if (!job.ready.TryDequeue(out Ready ready)) break;
            Interlocked.Add(ref job.pendingBytes, unchecked(-ready.w * ready.h * 4));
            Sprite[,] grid = GridOf(d, ready.L);
            if (grid == null || ready.rgba == null) continue;
            Texture2D texture = null;
            try
            {
                _mountUploadedThisFrame++;
                texture = DimgDecoder.TexFromRgba(ready.w, ready.h, ready.rgba, ready.scale, false);
                if (!texture) continue;
                Vector2 pivot = new Vector2(0f, 1f);
                if (ready.L.kind == 0)
                {
                    DimgDecoder.AnimGeom geom = ready.L.geom;
                    DimgDecoder.FrameGeom frame = geom.f[ready.idx];
                    pivot = new Vector2((geom.boundCx - frame.nLeft) * ready.scale / (float)ready.w,
                        1f - (geom.boundCy - frame.nTop) * ready.scale / (float)ready.h);
                }
                if (ready.dir < grid.GetLength(0) && ready.f < grid.GetLength(1))
                    grid[ready.dir, ready.f] = Sprite.Create(texture,
                        new Rect(0f, 0f, ready.w, ready.h), pivot, d.ppu * ready.scale);
                else UnityEngine.Object.Destroy(texture);
                int bytes = unchecked(ready.w * ready.h * 4);
                d.bytes += bytes;
                uploadBytes -= bytes;
                textureCount--;
                d.fillVersion++;
            }
            catch (Exception ex)
            {
                if (texture) UnityEngine.Object.Destroy(texture);
                if (_texFailWarned.Add(ready.L.actId * 4 + ready.L.kind))
                    Debug.LogWarning(string.Format("[MountAvatar] 建纹理失败 act={0}{1} dir={2} f={3} ",
                        ready.L.actId, ready.L.suffix, ready.dir, ready.f) +
                        string.Format("{0}x{1} scale={2} rgba={3}B: {4}",
                            ready.w, ready.h, ready.scale, ready.rgba.Length, ex.Message) +
                        " —— 该帧不画(本层只报一次), 请把这行发回来");
            }
        }
    }

    private static void ScheduleNext(Data d, Job job, ref bool readThisFrame)
    {
        foreach (LayerTask layer in job.layers)
        {
            if (layer.dead || layer.geom != null || layer.triedBytes) continue;
            if (readThisFrame) break;
            readThisFrame = true;
            layer.triedBytes = true;
            byte[] data;
            try { data = ReadGsn(layer.actId, layer.suffix); }
            catch (Exception ex)
            {
                layer.dead = true;
                Debug.LogWarning(string.Format("[MountAvatar] 读 gsn 失败 act={0}{1}: {2}",
                    layer.actId, layer.suffix, ex.Message));
                continue;
            }
            if (data == null)
            {
                layer.dead = true;
                if (layer.kind == 0 && _missWarned.Add(layer.actId))
                    Debug.LogWarning(string.Format("[MountAvatar] ★坐骑本体读不到 model={0} act={1}{2} ",
                        d.model, layer.actId, layer.suffix) +
                        string.Format("—— Read24Res(q21/ff21) 与 05主源 Pkg 都没有 avatar/npc/npc{0}{1}.gsn。",
                            layer.actId, layer.suffix) +
                        " 这只坐骑骑上去会完全不可见。多半是打包漏了该文件, 不是渲染问题。");
                continue;
            }
            DimgDecoder.AnimGeom geom = DimgDecoder.ParseAnimGeom(data);
            if (!geom.ok || geom.f == null)
            {
                layer.dead = true;
                if (layer.kind == 0 && _missWarned.Add(layer.actId))
                    Debug.LogWarning(string.Format("[MountAvatar] ★坐骑本体解不动 model={0} act={1}{2} ",
                        d.model, layer.actId, layer.suffix) +
                        string.Format("({0}B, 头 {1}) ", data.Length,
                            data.Length >= 8 ? Encoding.ASCII.GetString(data, 4, 4) : "?") +
                        "—— ParseAnimGeom 不认。这只坐骑骑上去会完全不可见。");
                continue;
            }
            layer.geom = geom;
            if (layer.kind == 0) ApplyBodyGeom(d, layer);
            else
            {
                LayerTask body = BodyTaskFor(job, layer.walk);
                if (body == null || body.geom == null || !body.geom.ok)
                {
                    layer.dead = true;
                    layer.geom = null;
                    continue;
                }
                ApplyOverlayGeom(d, layer, body.geom);
            }
            return;
        }

        LayerTask chosen = null;
        int chosenDir = -1;
        for (int pass = 0; pass < 2 && chosen == null; pass++)
        {
            bool hintWalk = Volatile.Read(ref job.hintWalk);
            foreach (LayerTask layer in job.layers)
            {
                if (layer.dead || layer.geom == null || layer.dirDone == null ||
                    (pass == 0 && layer.walk != hintWalk)) continue;
                int dir = Mathf.Clamp(Volatile.Read(ref job.hintDir), 0, layer.dirs - 1);
                if (!layer.dirDone[dir])
                {
                    chosen = layer;
                    chosenDir = dir;
                    break;
                }
                for (int i = 0; i < layer.dirs; i++)
                {
                    if (layer.dirDone[i]) continue;
                    chosen = layer;
                    chosenDir = i;
                    break;
                }
                if (chosen != null) break;
            }
        }
        if (chosen == null)
        {
            FinishJob(d, job);
            return;
        }
        chosen.dirDone[chosenDir] = true;
        LayerTask selected = chosen;
        int direction = chosenDir;
        job.running = Task.Run(() =>
        {
            for (int f = 0; f < selected.fpd; f++)
            {
                int index = direction * selected.fpd + f;
                if (index >= selected.geom.f.Length) break;
                DimgDecoder.FrameGeom frame = selected.geom.f[index];
                if (frame.srcLen <= 0) continue;
                byte[] rgba = DimgDecoder.FrameToRgba(selected.geom, index);
                if (rgba == null) continue;
                Interlocked.Add(ref job.pendingBytes, unchecked(frame.w * frame.h * 4));
                job.ready.Enqueue(new Ready
                {
                    L = selected, dir = direction, f = f, idx = index,
                    w = frame.w, h = frame.h, rgba = rgba,
                    scale = Mathf.Max(1, selected.geom.scale)
                });
            }
        });
    }
}

internal static partial class GameSettings
{
    internal static bool AnyRideGlowWanted
    {
        get { EnsureLoaded(); return !HideMyPetGlow || !HideOtherPetGlow; }
    }
}

}
