using UnityEngine;

// a hand-authored wave: an ordered sequence of sub-waves plus optional background trickle
// spawns. leave both subWaves and trickleSpawns empty to fall back to the procedural
// budget/weight system for this specific wave number (see ProceduralLevel.RunWave).
// waveDuration is not authored directly - it's the sum of every sub-wave's subWaveDuration
[System.Serializable]
public class WaveDefinition
{
    [Tooltip("which wave number this hand-authored definition applies to")]
    public int waveNumber = 1;

    [Tooltip("scripted burst sequence for this wave, run back to back")]
    public SubWaveDefinition[] subWaves;

    [Tooltip("background spawns that run for the wave's whole computed duration, independent of the sub-wave timeline")]
    public TrickleEntry[] trickleSpawns;
}

// one scripted burst within a wave. sub-waves run strictly back to back: this one starts
// right where the previous one's subWaveDuration ends
[System.Serializable]
public class SubWaveDefinition
{
    [Tooltip("computed automatically (see LevelConfig.OnValidate): the last spawn in this sub-wave's finish time, plus delay before next. the next sub-wave starts after this many seconds")]
    public float subWaveDuration;

    public WaveSpawnEntry[] spawns;

    [Tooltip("pause after this sub-wave's last insect spawns, before the next sub-wave begins")]
    public float delayBeforeNext = 3f;
}

// one insect type spawned some number of times within a sub-wave. named WaveSpawnEntry (not
// SpawnEntry) since SpawnManager.cs already defines a SpawnEntry for spawn points/lead-in
// waypoints - unrelated concept, same obvious name
[System.Serializable]
public class WaveSpawnEntry
{
    public InsectData insectData;

    [Min(1)]
    public int count = 1;

    [Tooltip("seconds between each individual spawn of this entry")]
    public float timeBetweenSpawns = 0.5f;

    [Tooltip("offset within the sub-wave before this entry starts spawning")]
    public float startDelay = 0f;
}

// a background spawner that just loops for the wave's whole duration, independent of the
// scripted sub-wave timeline - the simple "one every X seconds" layer
[System.Serializable]
public class TrickleEntry
{
    public InsectData insectData;

    [Tooltip("spawn one every X seconds, for the rest of the wave's duration")]
    public float interval = 5f;

    [Tooltip("offset before this trickle starts, from the wave's start")]
    public float startDelay = 0f;
}
