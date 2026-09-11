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

        // AudioSource playback always runs in real (unscaled) time regardless of Time.timeScale,
        // but a delayed Object.Destroy call is scaled by it - at higher game speeds that would
        // destroy this object, and cut the clip short, long before it's actually finished
        // playing. cleanup runs on real time too so it always matches actual playback length
        SfxCleanup cleanup = tempAudio.AddComponent<SfxCleanup>();
        cleanup.unscaledLifetime = entry.clip.length / Mathf.Max(source.pitch, 0.01f);
    }

    private class SfxCleanup : MonoBehaviour
    {
        public float unscaledLifetime;

        private IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(unscaledLifetime);
            Destroy(gameObject);
        }
    }
}
