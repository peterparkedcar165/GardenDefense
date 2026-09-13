using UnityEngine;
using System.Collections;

// shared player for one-off combat sfx (attack unleash, impact/hit reactions). spawns a
// short-lived AudioSource per call so overlapping sounds keep independent random pitch, routed
// through the same mixer group as GameManager's own one-shot UI sounds (GameVolume)
public static class SfxPlayer
{
    public static void Play(SoundEffect sfx, Vector3 position)
    {
        if (sfx == null || sfx.clips == null || sfx.clips.Length == 0) return;
        SoundClip entry = sfx.clips[Random.Range(0, sfx.clips.Length)];
        if (entry == null || entry.clip == null) return;

        GameObject tempAudio = new GameObject("Sfx");
        tempAudio.transform.position = position;
        AudioSource source = tempAudio.AddComponent<AudioSource>();
        source.clip = entry.clip;
        source.pitch = Random.Range(entry.minPitch, entry.maxPitch);
        source.volume = entry.volume;
        source.spatialBlend = 0f;
        if (GameManager.instance != null && GameManager.instance.audioSource != null)
            source.outputAudioMixerGroup = GameManager.instance.audioSource.outputAudioMixerGroup;
        source.Play();
        tempAudio.AddComponent<SfxCleanup>();
    }

    // destroys this object once its AudioSource actually reports it's done, instead of
    // pre-computing an expected duration and waiting that long - isPlaying reflects real
    // playback directly, so this can't drift out of sync with the clip regardless of
    // pitch/Time.timeScale/rounding, and it never destroys the object early or leaves it lingering
    private class SfxCleanup : MonoBehaviour
    {
        private IEnumerator Start()
        {
            AudioSource source = GetComponent<AudioSource>();
            while (source != null && source.isPlaying)
                yield return null;
            Destroy(gameObject);
        }
    }

    // starts a looping sfx (e.g. a skill's ongoing hum) parented to followTarget so it moves with
    // it. caller owns the returned AudioSource and must pass it to StopLooping when done - there's
    // no automatic lifetime here, unlike Play(), since a loop's duration isn't known up front
    public static AudioSource PlayLooping(SoundEffect sfx, Transform followTarget)
    {
        if (sfx == null || sfx.clips == null || sfx.clips.Length == 0) return null;
        SoundClip entry = sfx.clips[Random.Range(0, sfx.clips.Length)];
        if (entry == null || entry.clip == null) return null;

        GameObject obj = new GameObject("SfxLoop");
        if (followTarget != null) obj.transform.SetParent(followTarget, false);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.clip = entry.clip;
        source.loop = true;
        source.pitch = Random.Range(entry.minPitch, entry.maxPitch);
        source.volume = entry.volume;
        source.spatialBlend = 0f;
        if (GameManager.instance != null && GameManager.instance.audioSource != null)
            source.outputAudioMixerGroup = GameManager.instance.audioSource.outputAudioMixerGroup;
        source.Play();
        return source;
    }

    public static void StopLooping(AudioSource loop)
    {
        if (loop == null) return;
        loop.Stop();
        Object.Destroy(loop.gameObject);
    }
}
