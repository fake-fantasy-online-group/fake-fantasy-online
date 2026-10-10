using System.Collections;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SoundManager
{
    public static void Play(int id, int delayMs)
    {
        Ensure();
        if (I == null) return;
        bool from24 = SkillSoundDB.Use24All;
        if (delayMs > 0) I.StartCoroutine(I.DelayPlay(id, delayMs / 1000f, from24));
        else I.PlayId(id, from24);
    }

    private IEnumerator DelayPlay(int id, float sec, bool from24)
    {
        yield return new WaitForSeconds(sec);
        PlayId(id, from24);
    }

    public static void Volume(float v01)
    {
        Ensure();
        I?.SetVolume(v01);
    }

    public static float CurVolume()
    {
        Ensure();
        return I != null ? I.sfxVol : 0.8f;
    }

    public float GetVolume() => sfxVol;

    public void SetVolume(float v01)
    {
        sfxVol = v01 < 0f ? 0f : v01 > 1f ? 1f : v01;
        AudioSource[] sources = pool;
        if (sources != null)
            foreach (AudioSource source in sources)
                if (source != null) source.volume = sfxVol;
        PlayerPrefs.SetFloat("vol_sound", sfxVol);
    }
}

}
