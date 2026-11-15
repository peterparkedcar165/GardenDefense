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
    [Tooltip("plays every time, layered on top of the one clip picked at random from the unticked ones")]
    public bool alwaysPlay;
}

// one sfx slot (attack unleash, impact/hit reactions). every clip ticked alwaysPlay plays each
// time, and one of the unticked clips is picked at random on top, so the same sample doesn't repeat
// on every hit. e.g. a bubble blow ticked plus four musical notes unticked: the blow and one random
// note together. each layer keeps its own volume and pitch. played via SfxPlayer.Play
[System.Serializable]
public class SoundEffect
{
    public SoundClip[] clips;
}
