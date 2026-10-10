using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned reconstruction of the official ordinary-combat SoundManager cone and the shared
/// playback owner used by the complete SkillSoundDB id set.
/// </summary>
/// <remarks>
/// The observable mapping and lifecycle are locked to CombatWorld tokens 0600014D..0600014F,
/// PlayerController.PlayAttack token 06000873 and SoundManager tokens 06000BC3..06000BDD.
/// The official binary remains development evidence only; playback uses the complete committed
/// normal sound-effect directory.
/// </remarks>
public sealed class ReferenceWorldCombatSound : MonoBehaviour
{
    public const string VolumePreferenceKey = "vol_sound";
    public const float DefaultVolume = 0.8f;
    public const int AudioSourcePoolSize = 8;
    public const byte SuppressHitSoundFlag =
        FakeFantasyOnline.Application.WorldCombatResultState.SuppressHitSoundFlag;

    private const string SoundDirectoryName = "sndeffect";
    private const string SoundDirectory24Name = "sndeffect24_pcm";
    private const string SoundExtension = ".wav";

    private readonly Dictionary<int, AudioClip> clips =
        new Dictionary<int, AudioClip>();
    private readonly List<UnityWebRequest> activeRequests =
        new List<UnityWebRequest>();

    private AudioSource[] sources;
    private string soundDirectory = string.Empty;
    private string soundDirectory24 = string.Empty;
    private string referenceRoot = string.Empty;
    private float soundVolume = DefaultVolume;
    private int poolIndex;
    private bool configured;

    public IReadOnlyList<AudioSource> Sources => sources;
    public string SoundDirectory => soundDirectory;
    public bool Configured => configured;
    public int CachedClipCount => clips.Count;
    public int PoolIndex => poolIndex;
    public int LastRequestedSoundId { get; private set; }
    public int LastEmittedSoundId { get; private set; }
    public int EmissionCount { get; private set; }
    public ReferenceWorldCombatSoundFailure LastFailure { get; private set; }

    private void Awake()
    {
        EnsureSources();
    }

    public bool Configure(string sourceRoot)
    {
        EnsureSources();
        if (string.IsNullOrWhiteSpace(sourceRoot))
        {
            LastFailure = ReferenceWorldCombatSoundFailure.InvalidConfiguration;
            return false;
        }

        string candidateRoot;
        string candidateDirectory;
        try
        {
            candidateRoot = Path.GetFullPath(sourceRoot);
            candidateDirectory = Path.GetFullPath(
                Path.Combine(candidateRoot, SoundDirectoryName));
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is NotSupportedException ||
            exception is PathTooLongException)
        {
            LastFailure = ReferenceWorldCombatSoundFailure.InvalidConfiguration;
            return false;
        }

        if (configured)
        {
            bool unchanged = string.Equals(
                referenceRoot,
                candidateRoot,
                StringComparison.Ordinal);
            LastFailure = unchanged
                ? ReferenceWorldCombatSoundFailure.None
                : ReferenceWorldCombatSoundFailure.InvalidConfiguration;
            return unchanged;
        }

        if (!Directory.Exists(candidateDirectory))
        {
            // Source-owned assets that arrive grouped under ReferenceClient still reach the
            // official leaf through this one-level-up fallback; Player staging copies the same
            // canonical directory for native SoundManager.ResolveDir.
            string parentDirectory = Path.GetDirectoryName(candidateRoot);
            if (!string.IsNullOrEmpty(parentDirectory))
                candidateDirectory = Path.Combine(parentDirectory, SoundDirectoryName);
        }
        if (!Directory.Exists(candidateDirectory))
        {
            LastFailure = ReferenceWorldCombatSoundFailure.MissingSoundDirectory;
            return false;
        }

        // Official SoundManager.Init (0x06000d6d) resolves both leaves and LoadAndPlay
        // (0x06000d7e) tries the normal leaf before the 24-bit one, so this owner resolves the
        // second leaf beside the same root as well. It stays optional: only sndeffect decides
        // whether configuration succeeds, and the normal directory always wins on a conflict.
        string candidateDirectory24 = ResolveSiblingDirectory(
            candidateRoot,
            SoundDirectory24Name);

        referenceRoot = candidateRoot;
        soundDirectory = candidateDirectory;
        soundDirectory24 = candidateDirectory24;
        configured = true;
        LastFailure = ReferenceWorldCombatSoundFailure.None;
        return true;
    }

    public bool TryPlaySwing(int weaponCType)
    {
        int soundId = SwingSoundId(weaponCType);
        return soundId == 0 || TryPlayId(soundId);
    }

    public bool TryPlayHit(int weaponCType, bool critical)
    {
        return TryPlayId(HitSoundId(weaponCType, critical));
    }

    public void SetVolume(float value)
    {
        EnsureSources();
        soundVolume = Mathf.Clamp01(value);
        for (int index = 0; index < sources.Length; index++)
        {
            if (sources[index] != null)
            {
                sources[index].volume = soundVolume;
            }
        }

        SoundManager.Volume(soundVolume);
    }

    public float GetVolume()
    {
        return SoundManager.CurVolume();
    }

    internal bool TryPlayId(int soundId)
    {
        if (!configured || !IsValidSoundId(soundId))
        {
            LastFailure = ReferenceWorldCombatSoundFailure.InvalidConfiguration;
            return false;
        }

        LastRequestedSoundId = soundId;

        AudioClip clip;
        if (clips.TryGetValue(soundId, out clip))
        {
            if (clip == null)
            {
                return false;
            }

            LastFailure = ReferenceWorldCombatSoundFailure.None;
            return Emit(soundId, clip);
        }

        string filePath;
        if (!TryResolveSoundPath(soundId, out filePath) || !File.Exists(filePath))
        {
            clips[soundId] = null;
            LastFailure = ReferenceWorldCombatSoundFailure.MissingSoundFile;
            return false;
        }

        // Editor audits exercise the exact request/decode/apply methods below. A live Player owns
        // coroutine timing, matching the official lazy-load then emit-on-completion behavior.
        if (UnityEngine.Application.isPlaying && isActiveAndEnabled)
        {
            StartCoroutine(LoadAndPlay(soundId));
        }

        LastFailure = ReferenceWorldCombatSoundFailure.None;
        return true;
    }

    internal bool RequestPlayId(int soundId, int delayMilliseconds)
    {
        if (!configured || !IsValidSoundId(soundId) || delayMilliseconds < 0)
        {
            return false;
        }

        if (delayMilliseconds == 0)
        {
            TryPlayId(soundId);
            return true;
        }

        if (UnityEngine.Application.isPlaying && isActiveAndEnabled)
        {
            StartCoroutine(DelayPlay(soundId, delayMilliseconds / 1000f));
        }
        return true;
    }

    internal bool TryCreateLoadRequest(
        int soundId,
        out UnityWebRequest request)
    {
        request = null;
        string filePath;
        if (!configured || !IsValidSoundId(soundId) ||
            !TryResolveSoundPath(soundId, out filePath) || !File.Exists(filePath))
        {
            return false;
        }

        request = UnityWebRequestMultimedia.GetAudioClip(
            new Uri(Path.GetFullPath(filePath)).AbsoluteUri,
            AudioType.WAV);
        if (!(request.downloadHandler is DownloadHandlerAudioClip))
        {
            request.Dispose();
            request = null;
            return false;
        }

        return true;
    }

    internal bool TryApplyCompletedRequest(
        int soundId,
        UnityWebRequest request,
        bool startPlayback)
    {
        if (!configured || !IsValidSoundId(soundId) ||
            request == null || !request.isDone || !string.IsNullOrEmpty(request.error))
        {
            return false;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
        clips[soundId] = clip;
        if (clip == null)
        {
            LastFailure = ReferenceWorldCombatSoundFailure.AudioLoadFailed;
            return false;
        }

        clip.name = soundId.ToString();
        LastFailure = ReferenceWorldCombatSoundFailure.None;
        return !startPlayback || Emit(soundId, clip);
    }

    internal bool TryResolveSoundPath(int soundId, out string filePath)
    {
        filePath = null;
        if (!configured || !IsValidSoundId(soundId) ||
            string.IsNullOrEmpty(soundDirectory))
        {
            return false;
        }

        string primary;
        bool primaryResolved = TryBuildSoundPath(soundDirectory, soundId, out primary);
        if (primaryResolved && File.Exists(primary))
        {
            filePath = primary;
            return true;
        }

        // Same directory order and file-name rule as the official LoadAndPlay: the normal leaf
        // first, then sndeffect24_pcm. Ids that the official 1.5.8 skillsound.json only ships in
        // the 24-bit leaf (1403, 1430, 3014, 5002, 6030, 8096, 9033, ...) resolve here instead
        // of failing with MissingSoundFile.
        string secondary;
        if (!string.IsNullOrEmpty(soundDirectory24) &&
            TryBuildSoundPath(soundDirectory24, soundId, out secondary) &&
            File.Exists(secondary))
        {
            filePath = secondary;
            return true;
        }

        filePath = primaryResolved ? primary : null;
        return filePath != null;
    }

    private static bool TryBuildSoundPath(
        string directory,
        int soundId,
        out string filePath)
    {
        filePath = null;
        try
        {
            string candidate = Path.GetFullPath(
                Path.Combine(directory, soundId + SoundExtension));
            string prefix = directory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            filePath = candidate;
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is NotSupportedException ||
            exception is PathTooLongException)
        {
            return false;
        }
    }

    private static string ResolveSiblingDirectory(string root, string leaf)
    {
        try
        {
            string candidate = Path.GetFullPath(Path.Combine(root, leaf));
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            string parentDirectory = Path.GetDirectoryName(root);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                candidate = Path.GetFullPath(Path.Combine(parentDirectory, leaf));
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is NotSupportedException ||
            exception is PathTooLongException)
        {
            return string.Empty;
        }

        return string.Empty;
    }

    internal static bool ShouldPlayHit(byte flags)
    {
        return (flags & FakeFantasyOnline.Application.WorldCombatResultState.MissFlag) == 0 &&
               (flags & SuppressHitSoundFlag) == 0;
    }

    // CombatWorld.SwingSnd (0600015e) owns the table; the eight cType entries were read out of
    // the frozen 1.5.8 Mach-O in WB-054 and are byte-identical to the switch this helper used
    // to carry, so this is now a pass-through to the official owner like HitSoundId below.
    internal static int SwingSoundId(int weaponCType) =>
        CombatWorld.SwingSndForIndependentRuntime(weaponCType);

    internal static int HitSoundId(int weaponCType, bool critical) =>
        CombatWorld.HitSoundForIndependentRuntime(weaponCType, critical);

    private static bool IsValidSoundId(int soundId)
    {
        return soundId > 0;
    }

    private void EnsureSources()
    {
        if (sources != null && sources.Length == AudioSourcePoolSize)
        {
            return;
        }

        soundVolume = PlayerPrefs.GetFloat(VolumePreferenceKey, DefaultVolume);
        sources = new AudioSource[AudioSourcePoolSize];
        for (int index = 0; index < sources.Length; index++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.loop = false;
            source.playOnAwake = false;
            source.volume = soundVolume;
            sources[index] = source;
        }
    }

    private IEnumerator LoadAndPlay(int soundId)
    {
        UnityWebRequest request;
        if (!TryCreateLoadRequest(soundId, out request))
        {
            clips[soundId] = null;
            LastFailure = ReferenceWorldCombatSoundFailure.MissingSoundFile;
            yield break;
        }

        activeRequests.Add(request);
        yield return request.SendWebRequest();
        if (!TryApplyCompletedRequest(soundId, request, true))
        {
            clips[soundId] = null;
            LastFailure = ReferenceWorldCombatSoundFailure.AudioLoadFailed;
        }

        activeRequests.Remove(request);
        request.Dispose();
    }

    private IEnumerator DelayPlay(int soundId, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        TryPlayId(soundId);
    }

    private bool Emit(int soundId, AudioClip clip)
    {
        EnsureSources();
        if (clip == null || sources.Length == 0)
        {
            return false;
        }

        AudioSource source = sources[poolIndex % sources.Length];
        poolIndex++;
        if (source == null)
        {
            return false;
        }

        source.PlayOneShot(clip);
        LastEmittedSoundId = soundId;
        EmissionCount++;
        return true;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        for (int index = 0; index < activeRequests.Count; index++)
        {
            activeRequests[index]?.Dispose();
        }

        activeRequests.Clear();
        if (sources != null)
        {
            for (int index = 0; index < sources.Length; index++)
            {
                if (sources[index] != null)
                {
                    sources[index].Stop();
                }
            }
        }

        clips.Clear();
        configured = false;
    }
}

public enum ReferenceWorldCombatSoundFailure
{
    None = 0,
    InvalidConfiguration = 1,
    MissingSoundDirectory = 2,
    MissingSoundFile = 3,
    AudioLoadFailed = 4,
}

}
