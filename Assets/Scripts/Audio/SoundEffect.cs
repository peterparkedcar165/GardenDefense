using UnityEngine;

// one clip plus its own volume/pitch tuning - kept per-clip (not shared across the whole
// SoundEffect) since variations of the same sound are often recorded/mixed at different loudness
[System.Serializable]
public class SoundClip
{
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.5f, 2f)] public float minPitch = 0.95f;
    [Range(0.5f, 2f)] public float maxPitch = 1.05f;
}

// a pool of clip variations for one sfx slot (attack unleash, impact/hit reactions) - a random
// entry is picked each time instead of the exact same sample playing on every hit.
// played via SfxPlayer.Play
[System.Serializable]
public class SoundEffect
{
    public SoundClip[] clips;
}
