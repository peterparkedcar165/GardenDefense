using UnityEngine;
using System.Collections;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [SerializeField] private AudioSource sourceA, sourceB;
    [SerializeField] private float defaultCrossfadeDuration = 1.5f;

    private bool aIsActive = true;
    private Coroutine crossfadeCoroutine;

    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Play(AudioClip clip, float crossfadeDuration = -1f, float volume = 1f)
    {
        if (clip == null) return;

        AudioSource active = aIsActive ? sourceA : sourceB;
        if (active.clip == clip && active.isPlaying) return;

        float duration = crossfadeDuration < 0 ? defaultCrossfadeDuration : crossfadeDuration;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(Crossfade(clip, duration, volume));
    }

    public void Stop(float crossfadeDuration = -1f)
    {
        float duration = crossfadeDuration < 0 ? defaultCrossfadeDuration : crossfadeDuration;
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(FadeOut(duration));
    }

    private IEnumerator Crossfade(AudioClip newClip, float duration, float targetVolume)
    {
        AudioSource outgoing = aIsActive ? sourceA : sourceB;
        AudioSource incoming = aIsActive ? sourceB : sourceA;

        incoming.clip = newClip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed = 0f;
        float startVolume = outgoing.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            outgoing.volume = Mathf.Lerp(startVolume, 0f, t);
            incoming.volume = Mathf.Lerp(0f, targetVolume, t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.clip = null;
        incoming.volume = targetVolume;
        aIsActive = !aIsActive;
    }

    // fades both sources: a stop that lands in the middle of a crossfade (which never got to swap
    // the active source) also fades the track that was coming in, instead of leaving it playing at
    // part volume. outside a crossfade the other source is already stopped, so nothing changes
    private IEnumerator FadeOut(float duration)
    {
        float startA = sourceA.volume;
        float startB = sourceB.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            sourceA.volume = Mathf.Lerp(startA, 0f, t);
            sourceB.volume = Mathf.Lerp(startB, 0f, t);
            yield return null;
        }

        sourceA.Stop();
        sourceA.clip = null;
        sourceB.Stop();
        sourceB.clip = null;
    }
}
