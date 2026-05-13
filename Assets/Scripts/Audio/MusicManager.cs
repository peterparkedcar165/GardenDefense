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

    public void Play(AudioClip clip, float crossfadeDuration = -1f)
    {
        if (clip == null) return;

        AudioSource active = aIsActive ? sourceA : sourceB;
        if (active.clip == clip && active.isPlaying) return;

        float duration = crossfadeDuration < 0 ? defaultCrossfadeDuration : crossfadeDuration;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(Crossfade(clip, duration));
    }

    public void Stop(float crossfadeDuration = -1f)
    {
        float duration = crossfadeDuration < 0 ? defaultCrossfadeDuration : crossfadeDuration;
        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(FadeOut(duration));
    }

    private IEnumerator Crossfade(AudioClip newClip, float duration)
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
            incoming.volume = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.clip = null;
        incoming.volume = 1f;
        aIsActive = !aIsActive;
    }

    private IEnumerator FadeOut(float duration)
    {
        AudioSource active = aIsActive ? sourceA : sourceB;
        float startVolume = active.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            active.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        active.Stop();
        active.clip = null;
    }
}
