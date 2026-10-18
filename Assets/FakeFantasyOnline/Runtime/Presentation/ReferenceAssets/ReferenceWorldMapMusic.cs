using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official-code-first recovery of the complete 1.5.8 MusicManager owner.
/// </summary>
/// <remarks>
/// The complete official identities are frozen by the module's ARM64 translation,
/// independent x86_64 verification and exact third-comparison packets. Playback lifecycle
/// now also retires stale map audio and guards failed/obsolete loads. The official
/// binary remains development evidence only; this source owner reads source-owned OGG bytes.
/// </remarks>
public class MusicManager : MonoBehaviour
{
    [Serializable]
    private class Entry
    {
        public string file;
        public string music;
    }

    [Serializable]
    private class Data
    {
        public string login;
        public Entry[] maps;
    }

    public static MusicManager I;

    private AudioSource src;
    private string loginTrack = "M001";
    private readonly Dictionary<string, string> mapMus =
        new Dictionary<string, string>();
    private string cur = string.Empty;
    private string musicDir;
    private Coroutine loading;
    private UnityWebRequest activeReq;
    private UnityWebRequest inflightReq;
    private AudioClip curClip;
    private long selectionVersion;
    private bool originalTableAttempted;
    private float musicVol = 0.6f;

    private void Awake()
    {
        if (I)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        // Independent editor lifecycle adapter: the real Player always executes the official
        // persistence call; Unity forbids that call only for edit-mode construction audits.
        if (UnityEngine.Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }
        src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        musicVol = PlayerPrefs.GetFloat("vol_music", 0.6f);
        src.volume = musicVol;
        musicDir = ResolveMusicDir();

        string text = GameTables.LoadText("mapmusic");
        if (text == null)
        {
            return;
        }

        Data data = JsonUtility.FromJson<Data>(text);
        if (data == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(data.login))
        {
            loginTrack = data.login;
        }

        Entry[] maps = data.maps;
        if (maps == null || maps.Length <= 0)
        {
            return;
        }

        for (int index = 0; index < maps.Length; index++)
        {
            Entry entry = maps[index];
            if (entry != null && !string.IsNullOrEmpty(entry.file))
            {
                mapMus[entry.file] = entry.music;
            }
        }
    }

    private string ResolveMusicDir()
    {
        string primary = Path.Combine(ResourceFileRoot.Path, "Music");
        if (Directory.Exists(primary))
        {
            return primary;
        }

        if (UnityEngine.Application.isMobilePlatform) return primary;
        return Path.GetFullPath(
            Path.Combine(UnityEngine.Application.dataPath, "../../game_res/audio/Music"));
    }

    public void Play(string clipName)
    {
        // A map without usable music must not inherit the outgoing map's clip.
        // Keep a shared, already selected track running across floors/instances.
        if (!string.IsNullOrEmpty(clipName) && clipName == cur)
            return;

        StopSelection();
        if (!HasTrackFile(clipName)) return;
        cur = clipName;
        loading = StartCoroutine(LoadAndPlay(clipName, selectionVersion));
    }

    private void StopSelection()
    {
        ++selectionVersion;
        cur = string.Empty;
        if (loading != null)
        {
            StopCoroutine(loading);
            loading = null;
        }
        DisposeInflight();
        if (src)
        {
            src.Stop();
            src.clip = null;
        }
        if (curClip)
        {
            Destroy(curClip);
            curClip = null;
        }
        if (activeReq != null)
        {
            activeReq.Dispose();
            activeReq = null;
        }
    }

    private void DisposeInflight()
    {
        if (inflightReq != null)
        {
            inflightReq.Dispose();
            inflightReq = null;
        }
    }

    private IEnumerator LoadAndPlay(string clipName, long version)
    {
        UnityWebRequest req;
        if (!TryCreateLoadRequest(clipName, out req))
        {
            if (version == selectionVersion) cur = string.Empty;
            yield break;
        }
        inflightReq = req;
        yield return req.SendWebRequest();

        // Track name alone is insufficient for A -> B -> A: an old A completion
        // must not replace the newer A selection or clear its in-flight request.
        if (version != selectionVersion)
            yield break; // StopSelection already disposed this obsolete request.
        inflightReq = null;
        loading = null;
        if (string.IsNullOrEmpty(req.error))
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip != null && src != null)
            {
                clip.name = clipName;
                src.clip = clip;
                src.Play();
                curClip = clip;
                activeReq = req;
                yield break;
            }
        }

        req.Dispose();
        cur = string.Empty; // A failed request may be retried on the next entry.
    }

    private void OnDestroy()
    {
        if (I != this) return;
        StopSelection();
        I = null;
    }

    public void PlayLogin()
    {
        Play(loginTrack);
    }

    public void SetVolume(float v01)
    {
        float clamped = 0f;
        if (v01 >= 0f)
        {
            clamped = v01 > 1f ? 1f : v01;
        }

        musicVol = clamped;
        if (src)
        {
            src.volume = musicVol;
        }

        PlayerPrefs.SetFloat("vol_music", musicVol);
    }

    public float GetVolume()
    {
        return musicVol;
    }

    private void EnsureOriginalMapMusic()
    {
        if (originalTableAttempted || ResLoader.I == null) return;
        originalTableAttempted = true;
        try
        {
            MapDef.EnsureLoaded(ResLoader.I.Pkg);
            byte[] bytes = ResLoader.Read24Res("zipbin/ov_music.bin");
            if (bytes == null || !AddOriginalMapMusic(bytes))
                Debug.LogWarning("[Music] Original map music table is missing or invalid; keeping explicit JSON mappings.");
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Music] Cannot read original map music table: " + exception.Message);
        }
    }

    // Original ov_music: little-endian magic 0x5566, version 1, 20-byte rows,
    // each containing an int map ID and a 16-byte, NUL-padded ASCII track name.
    // Join through authored map.def filenames; never guess a dungeon soundtrack.
    internal bool AddOriginalMapMusic(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 16) return false;
        var additions = new Dictionary<string, string>();
        using (var reader = new BinaryReader(new MemoryStream(bytes), Encoding.ASCII))
        {
            int magic = reader.ReadInt32();
            int version = reader.ReadInt32();
            int stride = reader.ReadInt32();
            int count = reader.ReadInt32();
            if (magic != 0x5566 || version != 1 || stride != 20 || count < 0 ||
                16L + (long)count * stride != bytes.Length) return false;
            for (int i = 0; i < count; ++i)
            {
                int mapId = reader.ReadInt32();
                byte[] name = reader.ReadBytes(16);
                int length = Array.IndexOf(name, (byte)0);
                if (length < 0) length = name.Length;
                for (int n = 0; n < length; ++n)
                    if (!((name[n] >= 'A' && name[n] <= 'Z') ||
                          (name[n] >= 'a' && name[n] <= 'z') ||
                          (name[n] >= '0' && name[n] <= '9') ||
                          name[n] == '_' || name[n] == '-')) return false;
                MapDef.Entry descriptor;
                if (mapId <= 0 || length == 0 || !MapDef.TryGet(mapId, out descriptor) ||
                    string.IsNullOrEmpty(descriptor.file) || mapMus.ContainsKey(descriptor.file)) continue;
                string track = Encoding.ASCII.GetString(name, 0, length);
                string previous;
                if (additions.TryGetValue(descriptor.file, out previous) && previous != track)
                    return false; // An ambiguous source mapping is not a soundtrack choice.
                additions[descriptor.file] = track;
            }
        }
        foreach (var entry in additions) mapMus.Add(entry.Key, entry.Value);
        return true;
    }

    public void PlayMap(string mapFile)
    {
        EnsureOriginalMapMusic();
        string track = null;
        if (!string.IsNullOrEmpty(mapFile)) mapMus.TryGetValue(mapFile, out track);
        Play(track);
    }

    // Independent-runtime adapter surface. These members only bind the repository's immutable
    // source-owned layout and expose read-only diagnostics; every selection/playback decision
    // remains in the recovered methods above.

    public AudioSource Source => src;
    public string LoginTrack => loginTrack;
    public string CurrentTrack => cur;
    public string MusicDirectory => musicDir;
    public string CurrentFilePath => string.IsNullOrEmpty(cur) || string.IsNullOrEmpty(musicDir)
        ? string.Empty
        : Path.Combine(musicDir, cur + ".ogg");
    public int MapCount => mapMus.Count;
    public bool HasInflightRequest => inflightReq != null;
    public bool HasActiveRequest => activeReq != null;
    public string CurrentClipName => curClip ? curClip.name : string.Empty;

    public bool ConfigureIndependentRuntime(string referenceRoot)
    {
        if (string.IsNullOrWhiteSpace(referenceRoot))
        {
            return false;
        }

        // Unity invokes Awake in a Player. Editor construction audits add the component outside
        // play mode, so the independent lifecycle seam invokes that same recovered owner once.
        if (!src)
        {
            Awake();
        }
        if (!src)
        {
            return false;
        }

        if (!Directory.Exists(musicDir))
        {
            string candidate;
            try
            {
                candidate = Path.GetFullPath(Path.Combine(referenceRoot, "music"));
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                return false;
            }

            if (!Directory.Exists(candidate))
            {
                return false;
            }

            musicDir = candidate;
        }

        EnsureOriginalMapMusic();
        return mapMus.Count > 0 && HasTrackFile(loginTrack);
    }

    public bool TryPlayLogin()
    {
        if (!HasTrackFile(loginTrack))
        {
            return false;
        }

        PlayLogin();
        return cur == loginTrack;
    }

    public bool TryPlayMap(string mapFile)
    {
        EnsureOriginalMapMusic();
        string track;
        if (string.IsNullOrEmpty(mapFile) || !mapMus.TryGetValue(mapFile, out track) ||
            !HasTrackFile(track))
        {
            Play(null);
            return false;
        }

        PlayMap(mapFile);
        return cur == track;
    }

    public bool TryGetMapTrack(string mapFile, out string track)
    {
        return mapMus.TryGetValue(mapFile, out track);
    }

    internal bool TryCreateLoadRequest(
        string track,
        out UnityWebRequest request)
    {
        request = null;
        if (!HasTrackFile(track))
        {
            return false;
        }

        request = UnityWebRequestMultimedia.GetAudioClip(
            new Uri(Path.Combine(musicDir, track + ".ogg")).AbsoluteUri,
            AudioType.OGGVORBIS);
        DownloadHandlerAudioClip handler =
            request.downloadHandler as DownloadHandlerAudioClip;
        if (handler == null)
        {
            request.Dispose();
            request = null;
            return false;
        }

        handler.streamAudio = true;
        return true;
    }

    internal bool TryApplyCompletedRequest(
        string track,
        UnityWebRequest request,
        bool startPlayback)
    {
        if (request == null || !request.isDone || !string.IsNullOrEmpty(request.error) ||
            cur != track)
        {
            return false;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        if (clip == null || src == null)
        {
            return false;
        }

        clip.name = track;
        AudioClip oldClip = curClip;
        src.Stop();
        src.clip = clip;
        if (startPlayback)
        {
            src.Play();
        }
        curClip = clip;
        if (oldClip && oldClip != clip)
        {
            DestroyImmediate(oldClip);
        }

        if (activeReq != null)
        {
            activeReq.Dispose();
        }

        activeReq = request;
        return true;
    }

    private bool HasTrackFile(string track)
    {
        return !string.IsNullOrEmpty(track) &&
            !string.IsNullOrEmpty(musicDir) &&
            File.Exists(Path.Combine(musicDir, track + ".ogg"));
    }
}

}
