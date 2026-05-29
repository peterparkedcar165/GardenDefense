using UnityEngine;

// one entry = one insect type that can appear in a level
// the insect's prefab and threat stats come from its InsectData asset
[System.Serializable]
public class LevelInsectEntry
{
    [Tooltip("the insect's data asset - prefab and threat values are pulled from here")]
    public InsectData data;

    [Tooltip("which wave this insect first appears on (0 or 1 = from the start)")]
    public int unlockWave = 1;

    [Tooltip("fraction of the wave budget given to this insect type. values are auto-normalized so they do not need to sum to 1")]
    [Range(0f, 1f)]
    public float budgetWeight = 0.1f;

    [Tooltip("seconds into the wave before this type starts spawning")]
    public float startDelay = 0f;
}

// elite entries add a spawn count that scales with level progress.
// kept as a subclass so these fields only appear on the elite roster, not the normal one
[System.Serializable]
public class LevelEliteEntry : LevelInsectEntry
{
    [Tooltip("how many spawn on the first elite wave this is eligible for")]
    [Min(1)]
    public int minSpawnCount = 1;

    [Tooltip("how many spawn on the final wave. count scales linearly between min and max as the level progresses")]
    [Min(1)]
    public int maxSpawnCount = 1;
}
