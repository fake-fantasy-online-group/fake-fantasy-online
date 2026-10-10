using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    // 060001fc. Path construction and the success notification are outside the disk-write catch.
    private static string WriteFile()
    {
        string text;
        try
        {
            text = Compose();
        }
        catch (Exception ex)
        {
            text = "生成报告时抛异常: " + ex;
        }

        var dir = OutDir();
        var path = Path.Combine(dir, string.Format("trace_{0}_{1:MMdd_HHmmss}.txt", SafeTag(), DateTime.Now));
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(path, text, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[追踪] 写文件失败 " + path + ": " + ex.Message);
            return null;
        }

        Say("已保存 " + path);
        return path;
    }

    // 060001ff; generated 06000207/06000208 and 06000209-0600020c are carried by
    // the four noncapturing sort lambdas below. Preserve explicit LF characters.
    private static string Compose()
    {
        var sb = new StringBuilder(65536);
        sb.Append("==== fake-fantasy-online 切图加载追踪 ====\n");
        sb.Append(string.Format("客户端 {0} | 平台 {1} | 分辨率 {2}x{3} | 显示器 {4:F0}Hz\n",
            "1.6.0", UnityEngine.Application.platform, Screen.width, Screen.height,
            Screen.currentResolution.refreshRateRatio.value));
        sb.Append(string.Format("美术档: 相机像素缩放级 n={0} | 怪宠读{1} | 跟随开关={2}\n",
            ArtDetail.PixelScale,
            ArtDetail.PreferNativeArt ? "原生1×" : "HD件",
            ArtDetail.FollowCameraScale));
        sb.Append("录制标签 " + _tag + "\n");
        sb.Append(string.Format("开始 {0:yyyy-MM-dd HH:mm:ss} | 时长 {1:F1}s\n", DateTime.Now.AddSeconds(-T), T));
        sb.Append(Env()).Append('\n');
        sb.Append(string.Format("开关: 跟随相机档={0} 4444直传={1}",
                ArtDetail.FollowCameraScale, ArtDetail.Use4444Upload))
            .Append(string.Format(" 预算倍数×{0:0.0}(实际档次{1})\n", ArtDetail.BudgetScale, ArtDetail.AppliedScale));
        sb.Append(GpuFormatProbe()).Append('\n');
        sb.Append(Argb4444SelfCheck()).Append('\n');

        sb.Append("\n---- 一句话总账 ----\n");
        sb.Append(string.Format("帧 {0} 个 / 长帧(>{1:F0}ms) {2} 次 / 最长 {3:F0}ms / GC {4} 次\n",
            _frames, HitchMs, _hitches, _maxFrameMs, _gcTotal));
        sb.Append(string.Format("网络 {0} 条 / {1} / 派发合计 {2:F0}ms / 队列峰值 {3} 条\n",
            _netN, Kb(_netBytes), _netMs, _backlogPeak));
        sb.Append(string.Format("spawn 玩家{0} 宠{1} 怪{2}\n", _spPlayers, _spPets, _spMobs));
        sb.Append(string.Format("移动包 收到{0} → 本人{1} 玩家{2} 怪宠{3}",
                CombatWorld.MoveRecv, CombatWorld.MoveSelf, CombatWorld.MovePlayer, CombatWorld.MoveMob)
            + string.Format(" | ★查不到id{0} | 世界未初始化{1}\n", CombatWorld.MoveNoTarget, CombatWorld.MoveNoWorld));
        if (CombatWorld.MoveNoTarget > 0)
        {
            sb.Append(string.Format("  ★★查不到id的那些(最近{0}个): {1}\n",
                    CombatWorld.NoTargetSample.Count, string.Join(",", CombatWorld.NoTargetSample))
                + "  ⇒ 这些 id 的实体不在客户端表里(spawn 丢了/切图 ClearAll 后没重建/id 对不上), 移动包被静默丢弃。\n");
        }

        sb.Append(string.Format("资源读取 {0} 次 / 原始 {1} / 读盘 {2:F0}ms + 解压 {3:F0}ms = {4:F0}ms\n",
            _pkgN, Mb(_pkgOrig), _pkgReadMs, _pkgInflMs, _pkgReadMs + _pkgInflMs));
        sb.Append(string.Format("DIMG 解码 像素{0}次/{1:F0}ms/{2} · 只解几何{3}次/{4:F0}ms\n",
            _decPxN, _decPxMs, Mb(_decPxBytes), _decGeoN, _decGeoMs));
        sb.Append(string.Format("纸娃娃 {0} 个 / {1:F0}ms\n", _avN, _avMs));

        double scriptMs = 0d;
        for (int i = 0; i < 5; i++) scriptMs += _segTotal[i];
        sb.Append("脚本侧逐段(60s 合计): ");
        for (int i = 0; i < _segTotal.Length; i++)
            sb.Append(string.Format("{0} {1:F0}ms · ", _segName[i], _segTotal[i]));
        sb.Append(string.Format("合计 {0:F0}ms\n", scriptMs));
        sb.Append("  段调用次数/单次均值: ");
        for (int i = 0; i < _segTotal.Length; i++)
        {
            if (_segCalls[i] > 0)
                sb.Append(string.Format("{0} {1}次/{2:F0}µs · ",
                    _segName[i], _segCalls[i], _segTotal[i] / _segCalls[i] * 1000d));
        }

        sb.Append("\n");
        sb.Append(string.Format("  ★引擎侧(帧总时长 − 脚本侧) ≈ {0:F0}ms / 录制 {1:F0}ms", _wallMsTotal - scriptMs, _wallMsTotal)
            + string.Format("  ⇒ 脚本 {0:F0}% / 引擎 {1:F0}%\n",
                _wallMsTotal > 0d ? scriptMs / _wallMsTotal * 100d : 0d,
                _wallMsTotal > 0d ? (_wallMsTotal - scriptMs) / _wallMsTotal * 100d : 0d));
        sb.Append(string.Format("  ★活跃 SpriteRenderer(最后一帧) {0} 个 —— 每层贴不同纹理⇒不能合批, 这个数≈每帧 draw call 下限\n", _srLast));
        sb.Append(string.Format("怪宠网格 主线程 {0:F0}ms / 后台上传 {1} 张 / {2}\n", _gridMainMs, _upN, Mb(_upBytes)));

        sb.Append("\n---- 切图相位 ----\n");
        foreach (var phase in _phases)
            sb.Append(string.Format("  {0,-28} {1,8:F0}ms\n", phase.Item1, phase.Item2));

        sb.Append("\n---- 资源读取: 按来源 ----\n");
        sb.Append("  ⚠这一节是判断\"这份数据代不代表线上\"的关键: 编辑器散文件模式没有 XOR/zlib, 解压列会是 0。\n");
        foreach (var pair in SortByMs(_pkgBy))
            sb.Append(string.Format("  {0,-16} {1,6} 次  {2,9:F0}ms  {3,10}\n",
                pair.Key, pair.Value.Item1, pair.Value.Item2, Mb(pair.Value.Item3)));

        sb.Append("\n---- 资源读取: 最贵的 40 个文件 ----\n");
        var files = new List<KeyValuePair<string, (int, float, int)>>(_pkgTop);
        // 06000209: descending elapsed time, with Single.CompareTo NaN/tie behavior.
        files.Sort((a, b) => b.Value.Item2.CompareTo(a.Value.Item2));
        for (int i = 0; i < files.Count && i < 40; i++)
            sb.Append(string.Format("  {0,8:F0}ms ×{1,-3} {2,9}  {3}\n",
                files[i].Value.Item2, files[i].Value.Item1,
                Kb(files[i].Value.Item3), files[i].Key));
        if (files.Count > 40)
            sb.Append(string.Format("  (其余 {0} 个文件略)\n", files.Count - 40));

        sb.Append("\n---- 资源读取: 按目录聚合 ----\n");
        var directories = new Dictionary<string, (int, float, long)>();
        foreach (var pair in _pkgTop)
        {
            string key = pair.Key;
            int slash = key.LastIndexOf('/');
            string directory = slash > 0 ? key.Substring(0, slash) : "(根)";
            if (!directories.TryGetValue(directory, out var previous)) previous = default;
            directories[directory] = (
                unchecked(previous.Item1 + pair.Value.Item1),
                previous.Item2 + pair.Value.Item2,
                unchecked(previous.Item3 + pair.Value.Item3));
        }

        foreach (var pair in SortByMs(directories))
            sb.Append(string.Format("  {0,9:F0}ms ×{1,-5} {2,10}  {3}\n",
                pair.Value.Item2, pair.Value.Item1, Mb(pair.Value.Item3), pair.Key));

        sb.Append("\n---- 纸娃娃: 最贵的 30 个 ----\n");
        var avatars = new List<(string, string, float, bool)>(_avTop);
        // 0600020a.
        avatars.Sort((a, b) => b.Item3.CompareTo(a.Item3));
        for (int i = 0; i < avatars.Count && i < 30; i++)
            sb.Append(string.Format("  {0,8:F0}ms {1} {2,-18} {3}\n",
                avatars[i].Item3, avatars[i].Item4 ? "ok " : "失败",
                avatars[i].Item1, avatars[i].Item2));

        sb.Append("\n---- 怪/宠美术: 逐套动作 ----\n");
        sb.Append("     主线程ms  墙钟ms  方式      文件      scale 实际像素   纹理    act/model/名字\n");
        var grids = new List<(int, int, string, string, long, int, long, float, float, int, long)>(_gridRows);
        // 0600020b: negative wall time contributes zero; unordered/NaN is retained.
        grids.Sort((a, b) =>
            (b.Item8 + (b.Item9 < 0f ? 0f : b.Item9) * 0.001f).CompareTo(
                a.Item8 + (a.Item9 < 0f ? 0f : a.Item9) * 0.001f));
        foreach (var row in grids)
            sb.Append(string.Format("  {0,9:F0} {1}  {2,-9} {3,8}  ×{4}  {5,8} {6,8}  act{7} model={8} {9}\n",
                row.Item8,
                row.Item9 < 0f ? "     —" : row.Item9.ToString("F0").PadLeft(6),
                row.Item4, Kb(row.Item5), row.Item6, Mb(row.Item7), Mb(row.Item11),
                row.Item1, row.Item2, row.Item3));

        sb.Append("\n---- 网络: 按消息类型 ----\n");
        foreach (var pair in SortByMs(_netBy))
            sb.Append(string.Format("  id={0,-4} {1,6} 条  {2,9:F0}ms  {3,10}\n",
                pair.Key, pair.Value.Item1, pair.Value.Item2, Kb(pair.Value.Item3)));

        sb.Append("\n---- 长帧明细(最多 400 条) ----\n");
        foreach (var line in _hitchTop)
            sb.Append("  ").Append(line).Append('\n');

        sb.Append("\n---- Unity 警告/报错(去重, 按次数排) ----\n");
        if (_logs.Count == 0)
        {
            sb.Append("  (无)\n");
        }
        else
        {
            var logs = new List<KeyValuePair<string, (int, string, LogType)>>(_logs);
            // 0600020c.
            logs.Sort((a, b) => b.Value.Item1.CompareTo(a.Value.Item1));
            foreach (var pair in logs)
                sb.Append(string.Format("  ×{0,-5}[{1}] ", pair.Value.Item1, pair.Value.Item3))
                    .Append(pair.Value.Item2.Replace("\n", " ⏎ "))
                    .Append('\n');
        }

        sb.Append("\n---- 结束时的内存/缓存读数 ----\n");
        try
        {
            sb.Append(ArtCaches.Dump("追踪结束")).Append('\n');
        }
        catch (Exception ex)
        {
            sb.Append("(取不到: " + ex.Message + ")\n");
        }

        try
        {
            sb.Append(ArtCaches.DumpTextures(20)).Append('\n');
        }
        catch (Exception ex)
        {
            sb.Append("(取不到: " + ex.Message + ")\n");
        }

        sb.Append("\n---- 时间线 ----\n");
        foreach (var line in _tl)
            sb.Append(line).Append('\n');
        return sb.ToString();
    }
}
}
