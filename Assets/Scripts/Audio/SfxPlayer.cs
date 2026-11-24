using UnityEngine;

// shared player for one-off combat sfx (attack unleash, impact/hit reactions). gives each call
// its own free AudioSource so overlapping sounds keep independent random pitch, routed
// through the same mixer group as GameManager's own one-shot UI sounds (GameVolume)
public static class SfxPlayer
{
    public static void Play(SoundEffect sfx, Vector3 position)
    {
        if (sfx == null || sfx.clips == null) return;
        // every alwaysPlay clip, plus one random pick among the rest. clips with nothing assigned
        // are skipped, so an emptied slot never makes the random pick go silent
        SoundClip pick = null;
        int pool = 0;
        foreach (SoundClip entry in sfx.clips)
        {
            if (entry == null || entry.clip == null) continue;
            if (entry.alwaysPlay) PlayClip(entry, position);
            else if (Random.Range(0, ++pool) == 0) pick = entry;
        }
        if (pick != null) PlayClip(pick, position);
    }

    // reused one shot sources instead of a new object per sound. a source is only picked again
    // once it reports it is done playing and was not started this frame, so a sound is never cut
    // off. when every source is busy a new one is made, so there is no cap, same as before. the
    // objects live in the active scene like before, so a scene change still ends them with it
    private static readonly System.Collections.Generic.List<AudioSource> voices =
        new System.Collections.Generic.List<AudioSource>();
    private static readonly System.Collections.Generic.List<int> voiceStartFrames =
        new System.Collections.Generic.List<int>();

    private static AudioSource GetFreeVoice()
    {
        int frame = Time.frameCount;
        for (int i = voices.Count - 1; i >= 0; i--)
        {
            AudioSource voice = voices[i];
            if (voice == null)
            {
                // its scene was unloaded
                voices.RemoveAt(i);
                voiceStartFrames.RemoveAt(i);
                continue;
            }
            if (voiceStartFrames[i] == frame || voice.isPlaying) continue;
            voiceStartFrames[i] = frame;
            return voice;
        }

        AudioSource made = new GameObject("Sfx").AddComponent<AudioSource>();
        made.playOnAwake = false;
        voices.Add(made);
        voiceStartFrames.Add(frame);
        return made;
    }

    private static void PlayClip(SoundClip entry, Vector3 position)
    {
        if (entry == null || entry.clip == null) return;

        AudioSource source = GetFreeVoice();
        source.transform.position = position;
        source.clip = entry.clip;
        source.pitch = Random.Range(entry.minPitch, entry.maxPitch);
        source.volume = entry.volume;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup =
            GameManager.instance != null && GameManager.instance.audioSource != null
                ? GameManager.instance.audioSource.outputAudioMixerGroup
                : null;
        source.Play();
    }

    // starts a looping sfx (e.g. a skill's ongoing hum) parented to followTarget so it moves with
    // it. always one random clip with something assigned, alwaysPlay doesn't apply to loops. caller owns the returned AudioSource and must pass it to StopLooping when done - there's
    // no automatic lifetime here, unlike Play(), since a loop's duration isn't known up front
    public static AudioSource PlayLooping(SoundEffect sfx, Transform followTarget)
    {
        if (sfx == null || sfx.clips == null) return null;
        SoundClip entry = null;
        int pool = 0;
        foreach (SoundClip c in sfx.clips)
            if (c != null && c.clip != null && Random.Range(0, ++pool) == 0) entry = c;
        if (entry == null) return null;

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
