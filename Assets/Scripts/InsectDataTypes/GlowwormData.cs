using UnityEngine;

[CreateAssetMenu(fileName = "GlowwormData", menuName = "Scriptable Objects/InsectData/Glowworm")]
public class GlowwormData : InsectData
{
    [Header("Glowworm Glow")]
    [Tooltip("outer radius of the glowworm's own light (also used for darkness detection)")]
    public float glowRadius    = 2f;
    [Tooltip("intensity of the glowworm's own light")]
    public float glowIntensity = 0.8f;

    [Header("Glowworm Heal")]
    [Tooltip("radius in which injured insects are detected and targeted")]
    public float healRange = 4f;
    [Tooltip("seconds after a heal finishes before the next one can begin")]
    public float healCooldown = 3f;
    [Tooltip("seconds from cast start until the heal lands")]
    public float castDuration = 1.5f;
    [Tooltip("flat health restored by the heal")]
    public float healAmount = 20f;
    [Tooltip("additional heal as a fraction of the target's max health (0.1 = 10%)")]
    public float healPercent = 0.1f;

    [Header("Regenerative Glow Buff")]
    [Tooltip("duration of the Regenerative Glow buff applied after the heal")]
    public float regenerationDuration = 6f;
    [Tooltip("health restored per second by the Regenerative Glow buff")]
    public float regenerationHealing = 8f;
}
