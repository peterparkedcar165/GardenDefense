using UnityEngine;
using UnityEngine.Audio;

// shared looping background sounds for a biome
// create one asset per biome and assign it to every LevelConfig in that biome
[CreateAssetMenu(fileName = "AmbienceProfile", menuName = "Garden Defense/Ambience Profile")]
public class AmbienceProfile : ScriptableObject
{
    [Tooltip("route to the ambient mixer group so the ambient volume bar controls it")]
    public AudioMixerGroup output;
    public AmbienceEntry[] sounds;
}

[System.Serializable]
public class AmbienceEntry
{
    public AudioClip clip;
    [Tooltip("per clip volume, lower this for louder files")]
    [Range(0f, 1f)] public float volume = 1f;
}
