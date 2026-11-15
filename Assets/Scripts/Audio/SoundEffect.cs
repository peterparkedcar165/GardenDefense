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
// entry is picked each time instead of the exact same sample playing on every hit. with
// playAllTogether ticked, every entry plays at once instead, layered into one sound (each with its
// own volume and pitch). played via SfxPlayer.Play
[System.Serializable]
public class SoundEffect
{
    public SoundClip[] clips;
    [Tooltip("play every clip at the same time, layered, instead of one random clip")]
    public bool playAllTogether;
}
