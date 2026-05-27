using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// generic level runner - assign a LevelConfig asset and it handles all wave spawning
// replaces hand-written Level scripts for any level that uses the new config system
public class ProceduralLevel : SpawnManager
{
    public LevelConfig config;

    private int wave = 0;
    private float nextWaveTimer = 0f;

    protected override void Start()
    {
        if (WeatherManager.instance)
        {
            WeatherManager.instance.weather = config.weather;
            WeatherManager.instance.temperature = config.temperature;
        }

        FertilizerSelectionUI.instance?.Configure(config.fertilizerPool);
        GameManager.instance?.InitiateLevel(config.startSunCount, config.startHealth);
        GameHUD.instance?.SetWaveCount(wave, config.maxWaves);

        // mark this level visible in save data and mark the prerequisite complete
        SaveManager.instance.saveData.highestLevelUnlocked =
            Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, config.levelNumber);
        if (config.levelNumber > 1)
            SaveManager.instance.CompleteLevel(config.levelNumber - 1);

        StartCoroutine(RunWaves());
    }

    // ── main loop ─────────────────────────────────────────────────────────────

    IEnumerator RunWaves()
    {
        nextWaveTimer = 10f;
        yield return new WaitForSeconds(10f);

        while (wave < config.maxWaves)
        {
            wave++;
            GameManager.instance.currentWave = wave;
            GameHUD.instance?.SetWaveCount(wave, config.maxWaves);
            yield return StartCoroutine(RunWave(wave));

            if (wave < config.maxWaves)
                yield return StartCoroutine(RestPeriod(config.restDuration));
        }

        yield return new WaitUntil(() => Insect.allInsects.Count == 0);
        yield return new WaitForSeconds(3f);
        SaveManager.instance.CompleteLevel(config.levelNumber);
        Debug.Log("level " + config.levelNumber + " completed");
    }

    // ── wave runner ───────────────────────────────────────────────────────────

    IEnumerator RunWave(int waveNumber)
    {
        float t = config.maxWaves > 1
            ? (float)(waveNumber - 1) / (config.maxWaves - 1)
            : 1f;

        float waveDuration = Mathf.Lerp(
            config.minWaveDuration,
            config.maxWaveDuration,
            config.waveDurationCurve.Evaluate(t));

        float budget = Mathf.Lerp(
            config.minThreatBudget,
            config.maxThreatBudget,
            config.budgetCurve.Evaluate(t));

        // collect insects unlocked by this wave
        var available = new List<LevelInsectEntry>();
        foreach (var e in config.insects)
            if (e.data != null && e.unlockWave <= waveNumber)
                available.Add(e);

        // normalize weights
        float totalWeight = 0f;
        foreach (var e in available) totalWeight += e.budgetWeight;

        // start one spawn stream per insect type
        var streams = new List<Coroutine>();

        if (totalWeight > 0f)
        {
            foreach (var entry in available)
            {
                if (entry.budgetWeight <= 0f) continue;

                float share    = budget * (entry.budgetWeight / totalWeight);
                float spawns   = share / Mathf.Max(entry.data.threatValue, 0.01f);
                // clamp interval: at least 0.5s between spawns of the same type
                float interval = Mathf.Max(0.5f, waveDuration / Mathf.Max(spawns, 0.01f));

                streams.Add(StartCoroutine(
                    SpawnStream(entry.data.insectPrefab, entry.startDelay, interval, waveDuration)));
            }
        }

        // elite wave bonus - one elite is chosen by weighted random among unlocked entries
        bool isEliteWave = config.eliteWaveInterval > 0
                        && waveNumber % config.eliteWaveInterval == 0;
        if (isEliteWave && config.eliteInsects != null)
        {
            LevelInsectEntry chosenElite = PickWeightedElite(waveNumber);
            if (chosenElite != null)
                streams.Add(StartCoroutine(
                    SpawnEliteGroup(chosenElite, waveDuration, t)));
        }

        nextWaveTimer = waveDuration + config.restDuration;
        yield return new WaitForSeconds(waveDuration);

        // stop any streams still running (they self-terminate via elapsed check but
        // this cleans up edge cases where the last interval overshoots wave end)
        foreach (var s in streams)
            if (s != null) StopCoroutine(s);
    }

    // ── elite selection ───────────────────────────────────────────────────────

    // picks one elite entry by weighted random from those unlocked by this wave
    // higher budgetWeight = more likely to be chosen
    LevelInsectEntry PickWeightedElite(int waveNumber)
    {
        // build eligible pool
        var pool = new List<LevelInsectEntry>();
        float total = 0f;
        foreach (var e in config.eliteInsects)
        {
            if (e.data == null) continue;
            if (e.unlockWave > waveNumber) continue;
            pool.Add(e);
            total += e.budgetWeight;
        }

        if (pool.Count == 0 || total <= 0f) return null;

        // weighted random roll
        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var e in pool)
        {
            cumulative += e.budgetWeight;
            if (roll <= cumulative) return e;
        }

        // fallback: return last (handles floating point edge cases)
        return pool[pool.Count - 1];
    }

    // ── spawn helpers ─────────────────────────────────────────────────────────

    // repeatedly spawns a single insect type for the duration of a wave
    IEnumerator SpawnStream(GameObject prefab, float startDelay, float interval, float waveDuration)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float elapsed = startDelay;
        while (elapsed < waveDuration)
        {
            Spawn(prefab);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
    }

    // spawns the elite group for an elite wave
    // count scales between minSpawnCount and maxSpawnCount based on wave progress (t = 0 to 1)
    // if count > 1, spreads the spawns evenly across the remaining wave time
    IEnumerator SpawnEliteGroup(LevelInsectEntry entry, float waveDuration, float t)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(entry.minSpawnCount, entry.maxSpawnCount, t));
        count = Mathf.Max(1, count);

        if (entry.startDelay > 0f)
            yield return new WaitForSeconds(entry.startDelay);

        if (count <= 1)
        {
            Spawn(entry.data.insectPrefab);
        }
        else
        {
            // spread multiple spawns evenly across the time remaining after startDelay
            float remaining = waveDuration - entry.startDelay;
            float gap = remaining / count;
            for (int i = 0; i < count; i++)
            {
                Spawn(entry.data.insectPrefab);
                if (i < count - 1)
                    yield return new WaitForSeconds(gap);
            }
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    // ── SpawnManager overrides ─────────────────────────────────────────────────

    public override GameObject[] GetInsectPrefabs()
    {
        var prefabs = new List<GameObject>();

        foreach (var e in config.insects)
            if (e.data != null && e.data.insectPrefab != null)
                if (!prefabs.Contains(e.data.insectPrefab))
                    prefabs.Add(e.data.insectPrefab);

        if (config.eliteInsects != null)
            foreach (var e in config.eliteInsects)
                if (e.data != null && e.data.insectPrefab != null)
                    if (!prefabs.Contains(e.data.insectPrefab))
                        prefabs.Add(e.data.insectPrefab);

        return prefabs.ToArray();
    }

    protected override void Update()
    {
        if (nextWaveTimer > 0f)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
