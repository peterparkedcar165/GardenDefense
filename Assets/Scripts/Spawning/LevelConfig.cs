using UnityEngine;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Garden Defense/Level Config")]
public class LevelConfig : ScriptableObject
{
    [Header("level settings")]
    [Tooltip("used for save data (CompleteLevel calls)")]
    public int levelNumber;
    public WeatherEntry[] weather;
    public TemperatureType temperature = TemperatureType.Normal;
    public int maxWaves = 40;
    public int startSunCount = 350;
    public int startHealth = 200;
    [Tooltip("highest upgrade level plants can reach in this level")]
    public int maxUpgradeLevel = 5;

    [Header("wave duration")]
    [Tooltip("duration in seconds of the very first wave")]
    public float minWaveDuration = 20f;
    [Tooltip("duration in seconds of the final wave")]
    public float maxWaveDuration = 32f;
    [Tooltip("shape of the duration ramp from wave 1 to final wave. x axis = wave progress (0 to 1), y axis = 0 to 1 (lerped between min and max)")]
    public AnimationCurve waveDurationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("rest period")]
    public float restDuration = 12f;

    [Header("threat budget")]
    [Tooltip("total threat spawned on wave 1")]
    public float minThreatBudget = 15f;
    [Tooltip("total threat spawned on the final wave")]
    public float maxThreatBudget = 120f;
    [Tooltip("shape of the budget ramp. x axis = wave progress (0 to 1), y axis = 0 to 1 (lerped between min and max)")]
    public AnimationCurve budgetCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("insect roster")]
    [Tooltip("all insect types available in this level. each entry defines when it unlocks and how much of the budget it uses")]
    public LevelInsectEntry[] insects;

    [Header("hand-authored waves")]
    [Tooltip("optional exact spawn scripting for specific wave numbers. a wave number with no entry here (or an entry with no sub-waves/trickle spawns) falls back to the procedural budget system above")]
    public WaveDefinition[] waves;

    [Header("elite waves")]
    [Tooltip("a bonus elite insect spawns every N waves (set to 0 to disable)")]
    public int eliteWaveInterval = 5;
    [Tooltip("which insect(s) spawn as the elite bonus. startDelay controls when in the wave they appear")]
    public LevelEliteEntry[] eliteInsects;

    [Header("fertilizers")]
    public FertilizerData[] fertilizerPool;

    [Header("ambience")]
    [Tooltip("looping background sounds, share one profile asset across a biome")]
    public AmbienceProfile ambience;

    private void OnValidate()
    {
        SyncSubWaveDurations();
        SyncMaxWaves();
    }

    // each sub-wave's duration is derived, not authored: the finish time of its own last spawn,
    // plus the pause before the next sub-wave. keeps it from silently desyncing as spawns are edited
    private void SyncSubWaveDurations()
    {
        if (waves == null) return;
        foreach (WaveDefinition wave in waves)
        {
            if (wave?.subWaves == null) continue;
            foreach (SubWaveDefinition sub in wave.subWaves)
            {
                if (sub == null) continue;

                float spawnFinish = 0f;
                if (sub.spawns != null)
                {
                    foreach (WaveSpawnEntry entry in sub.spawns)
                    {
                        if (entry == null || entry.insectData == null || entry.count <= 0) continue;
                        float entryFinish = entry.startDelay + entry.timeBetweenSpawns * Mathf.Max(0, entry.count - 1);
                        if (entryFinish > spawnFinish) spawnFinish = entryFinish;
                    }
                }
                sub.subWaveDuration = spawnFinish + sub.delayBeforeNext;
            }
        }
    }

    // keeps maxWaves synced to the highest hand-authored wave number, so the level always runs
    // through its last scripted wave. uses the highest waveNumber rather than waves.Length since
    // authoring is sparse (e.g. only wave 15 and wave 30 defined)
    private void SyncMaxWaves()
    {
        if (waves == null || waves.Length == 0) return;

        int highest = 0;
        foreach (WaveDefinition wave in waves)
        {
            if (wave != null && wave.waveNumber > highest)
                highest = wave.waveNumber;
        }
        if (highest > 0)
            maxWaves = highest;
    }
}
