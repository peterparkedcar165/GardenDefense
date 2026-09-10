using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// generic level runner - assign a LevelConfig asset and it handles all wave spawning
// replaces hand-written Level scripts for any level that uses the new config system
public class ProceduralLevel : SpawnManager
{
    public LevelConfig config;

    private int wave = 0;
    // absolute (scaled) time at which the next wave begins; negative = no next wave
    private float nextWaveTime = -1f;

    protected override void Start()
    {
        if (WeatherManager.instance)
        {
            WeatherManager.instance.SetBaseWeather(config.weather);
            WeatherManager.instance.temperature = config.temperature;
        }

        FertilizerSelectionUI.instance?.Configure(config.fertilizerPool);
        Plant.pathLevelCap = config.maxUpgradeLevel;
        StartAmbience();
        GameManager.instance?.InitiateLevel(config.startSunCount, config.startHealth);
        GameHUD.instance?.SetWaveCount(wave, config.maxWaves);

        StartCoroutine(RunWaves());
    }

    // spawns one looping audio source per ambience clip, destroyed with the scene
    private void StartAmbience()
    {
        if (config.ambience == null) return;
        foreach (var entry in config.ambience.sounds)
        {
            if (entry.clip == null) continue;
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.clip = entry.clip;
            src.volume = entry.volume;
            src.outputAudioMixerGroup = config.ambience.output;
            src.loop = true;
            src.playOnAwake = false;
            src.Play();
        }
    }

    // ── main loop ─────────────────────────────────────────────────────────────

    IEnumerator RunWaves()
    {
        // wave 0 is the placement phase: the level sits paused waiting for the player to press
        // Start (see GameManager.HasStarted), so there's no fixed countdown here - wave 1 begins
        // the instant they do, with no leftover delay
        nextWaveTime = float.PositiveInfinity;
        yield return new WaitUntil(() => GameManager.instance.HasStarted);

        while (wave < config.maxWaves)
        {
            wave++;
            GameManager.instance.currentWave = wave;
            GameHUD.instance?.SetWaveCount(wave, config.maxWaves);
            yield return StartCoroutine(RunWave(wave));

            if (wave < config.maxWaves)
                yield return StartCoroutine(RestPeriod(config.restDuration));
        }

        nextWaveTime = -1f;
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

        // hand-authored waves take priority - a wave number with no definition, or one with
        // no sub-waves/trickle spawns, falls straight through to the procedural budget system
        WaveDefinition scripted = FindWaveDefinition(waveNumber);
        bool useScripted = scripted != null &&
            ((scripted.subWaves != null && scripted.subWaves.Length > 0) ||
             (scripted.trickleSpawns != null && scripted.trickleSpawns.Length > 0));

        float waveDuration;
        var streams = new List<Coroutine>();

        if (useScripted)
        {
            // duration is derived from the content itself (latest sub-wave finish time), not a
            // separately-tuned number that would silently desync as the script changes. a
            // trickle-only wave (no sub-waves) has nothing to derive a duration from, so it
            // falls back to the same curve the procedural system uses
            waveDuration = ComputeScriptedDuration(scripted);
            if (waveDuration <= 0f)
                waveDuration = Mathf.Lerp(config.minWaveDuration, config.maxWaveDuration, config.waveDurationCurve.Evaluate(t));

            float cumulativeStart = 0f;
            if (scripted.subWaves != null)
            {
                foreach (SubWaveDefinition sub in scripted.subWaves)
                {
                    streams.Add(StartCoroutine(RunSubWave(sub, cumulativeStart)));
                    cumulativeStart += sub.delayBeforeNext;
                }
            }
            if (scripted.trickleSpawns != null)
                foreach (TrickleEntry trickle in scripted.trickleSpawns)
                    streams.Add(StartCoroutine(RunTrickle(trickle, waveDuration)));
        }
        else
        {
            waveDuration = Mathf.Lerp(
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
        }

        // elite wave bonus - one elite is chosen by weighted random among unlocked entries
        bool isEliteWave = config.eliteWaveInterval > 0
                        && waveNumber % config.eliteWaveInterval == 0;
        if (isEliteWave && config.eliteInsects != null)
        {
            LevelEliteEntry chosenElite = PickWeightedElite(waveNumber);
            if (chosenElite != null)
                streams.Add(StartCoroutine(
                    SpawnEliteGroup(chosenElite, waveDuration, t)));
        }

        // schedule the next wave: this wave's remaining spawn time plus the rest after it.
        // the final wave has no successor, so clear the countdown
        nextWaveTime = waveNumber < config.maxWaves
            ? Time.time + waveDuration + config.restDuration
            : -1f;
        yield return new WaitForSeconds(waveDuration);

        // stop any streams still running (they self-terminate via elapsed check but
        // this cleans up edge cases where the last interval overshoots wave end)
        foreach (var s in streams)
            if (s != null) StopCoroutine(s);
    }

    // ── elite selection ───────────────────────────────────────────────────────

    // picks one elite entry by weighted random from those unlocked by this wave
    // higher budgetWeight = more likely to be chosen
    LevelEliteEntry PickWeightedElite(int waveNumber)
    {
        // build eligible pool
        var pool = new List<LevelEliteEntry>();
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

    // ── hand-authored waves ───────────────────────────────────────────────────

    private WaveDefinition FindWaveDefinition(int waveNumber)
    {
        if (config.waves == null) return null;
        foreach (WaveDefinition w in config.waves)
            if (w != null && w.waveNumber == waveNumber)
                return w;
        return null;
    }

    // the wave's duration is derived from its content rather than authored separately, so it
    // can never silently desync from the sub-waves as they're edited: it's the latest point at
    // which any sub-wave finishes its own last spawn, accounting for each sub-wave's start
    // offset (cumulative sum of delayBeforeNext before it)
    private float ComputeScriptedDuration(WaveDefinition def)
    {
        float duration = 0f;
        float cumulativeStart = 0f;
        if (def.subWaves == null) return 0f;

        foreach (SubWaveDefinition sub in def.subWaves)
        {
            float subFinish = 0f;
            if (sub.spawns != null)
            {
                foreach (WaveSpawnEntry entry in sub.spawns)
                {
                    if (entry.insectData == null || entry.count <= 0) continue;
                    float entryFinish = entry.startDelay + entry.timeBetweenSpawns * Mathf.Max(0, entry.count - 1);
                    if (entryFinish > subFinish) subFinish = entryFinish;
                }
            }

            float subWaveAbsoluteFinish = cumulativeStart + subFinish;
            if (subWaveAbsoluteFinish > duration) duration = subWaveAbsoluteFinish;

            cumulativeStart += sub.delayBeforeNext;
        }
        return duration;
    }

    // waits startOffset (this sub-wave's own start time within the wave), then fires every
    // spawn entry in this sub-wave concurrently - each entry runs its own count/timing
    // independently, so multiple entries in one sub-wave can overlap too
    IEnumerator RunSubWave(SubWaveDefinition sub, float startOffset)
    {
        if (startOffset > 0f)
            yield return new WaitForSeconds(startOffset);

        if (sub.spawns == null) yield break;
        foreach (WaveSpawnEntry entry in sub.spawns)
            StartCoroutine(RunWaveSpawnEntry(entry));
    }

    IEnumerator RunWaveSpawnEntry(WaveSpawnEntry entry)
    {
        if (entry.insectData == null || entry.insectData.insectPrefab == null || entry.count <= 0)
            yield break;

        if (entry.startDelay > 0f)
            yield return new WaitForSeconds(entry.startDelay);

        for (int i = 0; i < entry.count; i++)
        {
            Spawn(entry.insectData.insectPrefab);
            if (i < entry.count - 1)
                yield return new WaitForSeconds(entry.timeBetweenSpawns);
        }
    }

    // background spawner: loops for the wave's whole (computed) duration, independent of
    // whatever the scripted sub-waves are doing
    IEnumerator RunTrickle(TrickleEntry trickle, float waveDuration)
    {
        if (trickle.insectData == null || trickle.insectData.insectPrefab == null)
            yield break;

        if (trickle.startDelay > 0f)
            yield return new WaitForSeconds(trickle.startDelay);

        float elapsed = trickle.startDelay;
        while (elapsed < waveDuration)
        {
            Spawn(trickle.insectData.insectPrefab);
            yield return new WaitForSeconds(trickle.interval);
            elapsed += trickle.interval;
        }
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
    IEnumerator SpawnEliteGroup(LevelEliteEntry entry, float waveDuration, float t)
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
        float remaining = nextWaveTime < 0f ? -1f : nextWaveTime - Time.time;
        GameHUD.instance?.SetNextWaveTimer(remaining);
    }
}
