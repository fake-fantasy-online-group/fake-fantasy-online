using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SoundManager : MonoBehaviour
{
    private static SoundManager I;
    private string sndDir;
    private string snd24Dir;
    private readonly Dictionary<int, AudioClip> cache = new Dictionary<int, AudioClip>();
    private readonly Dictionary<int, AudioClip> cache24 = new Dictionary<int, AudioClip>();
    private AudioSource[] pool;
    private int poolIdx;
    private float sfxVol = 0.8f;

    public static void Play(int id)
    {
        Ensure();
        if (I != null) I.PlayId(id, false);
    }

    private static void Ensure()
    {
        if (I != null) return;
        GameObject root = new GameObject("SoundManager");
        Object.DontDestroyOnLoad(root);
        I = root.AddComponent<SoundManager>();
        I.Init();
    }

    private void Init()
    {
        sndDir = ResolveDir("sndeffect");
        snd24Dir = ResolveDir("sndeffect24_pcm");
        pool = new AudioSource[8];
        sfxVol = PlayerPrefs.GetFloat("vol_sound", 0.8f);
        for (int i = 0; i < pool.Length; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = sfxVol;
            pool[i] = source;
        }
    }

    private static string ResolveDir(string leaf)
    {
        string path = Path.Combine(ResourceFileRoot.Path, leaf);
        if (Directory.Exists(path) || UnityEngine.Application.isMobilePlatform) return path;
        return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../game_res/audio", leaf));
    }

    private static string PickPath(string dir, int id)
    {
        if (string.IsNullOrEmpty(dir)) return null;
        string path = Path.Combine(dir, id.ToString() + ".wav");
        if (File.Exists(path)) return path;
        path = Path.Combine(dir, id.ToString() + ".WAV");
        return File.Exists(path) ? path : null;
    }

    private void PlayId(int id, bool from24)
    {
        Dictionary<int, AudioClip> selected = from24 ? cache24 : cache;
        if (selected.TryGetValue(id, out AudioClip clip))
        {
            if (clip != null) pool[unchecked(poolIdx++) % pool.Length].PlayOneShot(clip);
            return;
        }
        StartCoroutine(LoadAndPlay(id, from24));
    }

    private IEnumerator LoadAndPlay(int id, bool from24, System.Func<bool> stillValid = null)
    {
        Dictionary<int, AudioClip> selected = from24 ? cache24 : cache;
        string first = PickPath(from24 ? snd24Dir : sndDir, id);
        string second = from24 ? null : PickPath(snd24Dir, id);
        AudioClip clip = null;
        foreach (string path in new[] { first, second })
        {
            if (path == null) continue;
            // Official 0x06000d7e is String.Concat("file://", path); no Uri/GetFullPath adaptation.
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
                "file://" + path, AudioType.WAV))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success) continue;
                clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null) break;
            }
        }
        selected[id] = clip;
        if (clip != null && (stillValid == null || stillValid())) Emit(clip);
    }

    private void Emit(AudioClip clip) => pool[unchecked(poolIdx++) % pool.Length].PlayOneShot(clip);
}

}
