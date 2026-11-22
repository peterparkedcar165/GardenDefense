using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// generic level runner - assign a LevelConfig asset and it handles all wave spawning
// replaces hand-written Level scripts for any level that uses the new config system
public class ProceduralLevel : SpawnManager
{
    public LevelConfig config;

    // for a test copy of a level: same config as the real level, but a win saves nothing, so no
    // unlock, currency, plant exp or level ups. off in every real level
    [Tooltip("test copy of a level: a win saves nothing (no unlock, currency or plant exp)")]
    public bool testLevel;

    // the level actually running, readable statically so systems with no scene reference (like
    // FertilizerStatRules, which needs to know what insects can appear) can query it
    public static LevelConfig CurrentConfig;

    private int wave = 0;
    // absolute (scaled) time at which the next wave begins; negative = no next wave
    private float nextWaveTime = -1f;
    // when the running countdown to the next wave began (its half way point unlocks the early call)
    private float countdownStartTime = -1f;
    // insects the newest wave has spawned so far: a clear field only unlocks the early call once
    // that wave has started coming
    private int newestWaveSpawned;
    private bool callNextWaveRequested;
    // waves still spawning: they run side by side once one is called early
    private int wavesRunning;

    public static ProceduralLevel Instance { get; private set; }

    // calling the next wave early (Tommy, 2026-11-21): the timer bar starts it now, like Kingdom Rush.
    // the wave already running keeps playing. it pays 1 Sun per SecondsPerSun seconds skipped and
    // takes 1 second off every plant's skill cooldown per SecondsPerCooldownSecond skipped (Tommy: 1 per
    // 2). it opens at half time (the bar half empty), or earlier once the field is clear after the
    // newest wave began spawning
    public const float SecondsPerSun = 2f;
    public const float SecondsPerCooldownSecond = 2f;
    public bool HasNextWave => wave >= 1 && wave < config.maxWaves && nextWaveTime > 0f && !float.IsPositiveInfinity(nextWaveTime);
    public float SecondsSkipped => HasNextWave ? Mathf.Max(0f, nextWaveTime - Time.time) : 0f;
    public int CallSunReward => Mathf.FloorToInt(SecondsSkipped / SecondsPerSun);
    public float CallCooldownCut => SecondsSkipped / SecondsPerCooldownSecond;
    public int NextWaveNumber => wave + 1;
    public bool PastHalfTime => HasNextWave && Time.time - countdownStartTime >= (nextWaveTime - countdownStartTime) * 0.5f;
    public bool FieldClear => newestWaveSpawned > 0 && Insect.allInsects.Count == 0;
    public float SecondsToHalfTime => HasNextWave ? Mathf.Max(0f, countdownStartTime + (nextWaveTime - countdownStartTime) * 0.5f - Time.time) : 0f;
    public bool CanCallNextWave => GameManager.instance != null && GameManager.instance.HasStarted && !callNextWaveRequested
        && HasNextWave && SecondsSkipped > 0f && (PastHalfTime || FieldClear);

    public bool CallNextWave()
    {
        if (!CanCallNextWave) return false;
        float cut = CallCooldownCut;
        int sun = CallSunReward;
        if (sun > 0) GameManager.instance.AddSun(sun);
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
            if (plant != null && plant.IsAlive) plant.ReduceSkillCooldown(cut);
        callNextWaveRequested = true;
        return true;
    }
    // absolute (scaled) time the current (final) wave's last insect spawns; negative = not the final wave
    private float finalWaveSpawnTime = -1f;

    // Level1PlantInfo triggers once wave 1's 2nd sub-wave has begun and the player has over 18
    // sun (enough to afford the Attack path upgrade it walks them through) - checked every frame
    // from level start since sun accrues passively and may not already be over 18 by then
    private bool triedLevel1PlantInfoTutorial;
    private bool level1Wave1SubWave2Started;
    private bool triedLevel1SkillTutorial;
    // game time wave 3 of level 1 began, for its skill tutorial (4 seconds in). negative until then
    private float level1Wave3StartTime = -1f;


    protected override void Start()
    {
        CurrentConfig = config;
        Instance = this;
        Plant.ResetPendingExpForNewLevel(); // discard any exp left over from a game-over'd attempt

        if (WeatherManager.instance)
        {
            WeatherManager.instance.SetBaseWeather(config.weather);
            WeatherManager.instance.temperature = config.temperature;
        }

        FertilizerSelectionUI.instance?.Configure(config.fertilizers);
        FertilizerManager.instance?.ResetForLevel();
        MulchManager.instance?.ResetForLevel();
        Plant.pathLevelCap = config.maxUpgradeLevel;
        StartAmbience();
        GameManager.instance?.InitiateLevel(config.startSunCount, config.startHealth);
        GameHUD.instance?.SetWaveCount(wave, config.maxWaves);

        Debug.Log($"[Tutorial] ProceduralLevel.Start - levelNumber={config.levelNumber}, TutorialManager.instance={(TutorialManager.instance != null ? "OK" : "NULL")}");
        if (config.levelNumber == 1)
            TutorialManager.instance?.TryShow(TutorialIds.Level1PlantSelection, TutorialContent.Level1PlantSelection, TutorialSpeaker.AcornKnight);

        StartCoroutine(RunWaves());
    }

    private void OnDestroy()
    {
        if (CurrentConfig == config) CurrentConfig = null;
        if (Instance == this) Instance = null;
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
            if (config.levelNumber == 1 && wave == 3) level1Wave3StartTime = Time.time;
            GameHUD.instance?.SetWaveCount(wave, config.maxWaves);
            FertilizerManager.instance?.NotifyWaveStarted(wave);
            MulchManager.instance?.NotifyWaveStarted(wave);
            newestWaveSpawned = 0;
            callNextWaveRequested = false;
            // the wave runs on its own (RunWaveToEnd), so a wave called early plays alongside it.
            // starting it sets nextWaveTime: its length plus the rest after it
            StartCoroutine(RunWaveToEnd(wave));

            // the countdown to the next wave, cut short when the player calls it early
            if (wave < config.maxWaves)
                yield return new WaitUntil(() => callNextWaveRequested || Time.time >= nextWaveTime);
        }

        nextWaveTime = -1f;
        yield return new WaitUntil(() => wavesRunning == 0);
        yield return new WaitUntil(() => Insect.allInsects.Count == 0);
        yield return new WaitForSeconds(3f);
        if (testLevel)
        {
            Debug.Log("test level " + config.levelNumber + " won, nothing saved");
            yield break;
        }
        Plant.BankAllExpForLevelEnd();
        Plant.CommitPendingExpToSave(); // level actually won - exp becomes persistent here
        SaveManager.instance.CompleteLevel(config.levelNumber);
        Debug.Log("level " + config.levelNumber + " completed");
    }

    // ── wave runner ───────────────────────────────────────────────────────────

    // one wave from its first spawn to its end, and what follows its end. several can run at once
    IEnumerator RunWaveToEnd(int waveNumber)
    {
        wavesRunning++;
        yield return StartCoroutine(RunWave(waveNumber));
        FertilizerManager.instance?.NotifyWaveEnded(waveNumber);
        MulchManager.instance?.NotifyWaveEnded(waveNumber);
        // every wave but the last guarantees a mulch where its last insect dies or leaks
        MulchManager.instance?.NotifyWaveSpawningDone(waveNumber, waveNumber >= config.maxWaves);
        wavesRunning--;
    }

    protected override void OnInsectSpawned(Insect insect)
    {
        if (insect != null && insect.spawnWave == wave) newestWaveSpawned++;
    }

    IEnumerator RunWave(int waveNumber)
    {
        float t = config.maxWaves > 1
            ? (float)(waveNumber - 1) / (config.maxWaves - 1)
            : 1f;

        // every wave is hand-authored now - a wave number with no definition (or an entry with
        // no sub-waves/trickle spawns) simply spawns nothing and just runs out its duration
        WaveDefinition scripted = FindWaveDefinition(waveNumber);
        bool useScripted = scripted != null &&
            ((scripted.subWaves != null && scripted.subWaves.Length > 0) ||
             (scripted.trickleSpawns != null && scripted.trickleSpawns.Length > 0));

        float waveDuration;
        var streams = new List<Coroutine>();
        // held at 1 until every spawn stream of this wave is counted, so one that finishes at once
        // can't report the wave done early (ReleaseSpawnStream below)
        spawnStreamsLeft[waveNumber] = 1;
        // tracks the latest point (relative to wave start) any insect actually spawns, for the
        // final wave's "time until last insect" HUD countdown
        float lastSpawnOffset = 0f;

        if (useScripted)
        {
            // duration is derived from the content itself (sum of each sub-wave's own duration),
            // not a separately-tuned number that would silently desync as the script changes. a
            // trickle-only wave (no sub-waves) has nothing to derive a duration from, so it
            // falls back to the same curve the procedural system uses
            waveDuration = ComputeScriptedDuration(scripted);
            if (waveDuration <= 0f)
                waveDuration = Mathf.Lerp(config.minWaveDuration, config.maxWaveDuration, config.waveDurationCurve.Evaluate(t));

            // sub-waves run strictly back to back: each one starts where the previous one's
            // subWaveDuration ends
            float cumulativeStart = 0f;
            if (scripted.subWaves != null)
            {
                for (int i = 0; i < scripted.subWaves.Length; i++)
                {
                    SubWaveDefinition sub = scripted.subWaves[i];
                    if (sub.spawns != null)
                        foreach (WaveSpawnEntry entry in sub.spawns)
                            if (Spawns(entry)) spawnStreamsLeft[waveNumber]++;
                    streams.Add(StartCoroutine(RunSubWave(sub, cumulativeStart, waveNumber, i + 1)));
                    lastSpawnOffset = Mathf.Max(lastSpawnOffset, cumulativeStart + SubWaveSpawnFinish(sub));
                    cumulativeStart += sub.subWaveDuration;
                }
            }
            if (scripted.trickleSpawns != null)
            {
                foreach (TrickleEntry trickle in scripted.trickleSpawns)
                {
                    if (Spawns(trickle)) spawnStreamsLeft[waveNumber]++;
                    streams.Add(StartCoroutine(RunTrickle(trickle, waveDuration, waveNumber)));
                    lastSpawnOffset = Mathf.Max(lastSpawnOffset, LastRepeatingSpawnOffset(trickle.startDelay, trickle.interval, waveDuration));
                }
            }
        }
        else
        {
            // no hand-authored wave for this number - nothing spawns, but the wave still runs
            // out its duration so the game keeps progressing (sun generation, the rest period
            // after, etc.) instead of instantly skipping past it
            waveDuration = Mathf.Lerp(
                config.minWaveDuration,
                config.maxWaveDuration,
                config.waveDurationCurve.Evaluate(t));
        }

        // elite wave bonus - one elite is chosen by weighted random among unlocked entries
        bool isEliteWave = config.eliteWaveInterval > 0
                        && waveNumber % config.eliteWaveInterval == 0;
        if (isEliteWave && config.eliteInsects != null)
        {
            LevelEliteEntry chosenElite = PickWeightedElite(waveNumber);
            if (chosenElite != null)
            {
                spawnStreamsLeft[waveNumber]++;
                streams.Add(StartCoroutine(
                    SpawnEliteGroup(chosenElite, waveDuration, t, waveNumber)));

                int eliteCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(chosenElite.minSpawnCount, chosenElite.maxSpawnCount, t)));
                float eliteLastSpawn = eliteCount <= 1
                    ? chosenElite.startDelay
                    : chosenElite.startDelay + ((waveDuration - chosenElite.startDelay) / eliteCount) * (eliteCount - 1);
                lastSpawnOffset = Mathf.Max(lastSpawnOffset, eliteLastSpawn);
            }
        }

        // schedule the next wave: this wave's remaining spawn time plus the rest after it.
        // the final wave has no successor - instead track when its last insect spawns, for the
        // HUD countdown
        if (waveNumber < config.maxWaves)
        {
            nextWaveTime = Time.time + waveDuration + config.restDuration;
            countdownStartTime = Time.time;
            finalWaveSpawnTime = -1f;
        }
        else
        {
            nextWaveTime = -1f;
            finalWaveSpawnTime = Time.time + lastSpawnOffset;
        }
        ReleaseSpawnStream(waveNumber);
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

    // the wave's duration is derived from its content rather than authored separately: sub-waves
    // run strictly back to back, so it's simply the sum of each sub-wave's own duration
    // (subWaveDuration is kept in sync with its spawns by LevelConfig.OnValidate)
    private float ComputeScriptedDuration(WaveDefinition def)
    {
        float duration = 0f;
        if (def.subWaves != null)
            foreach (SubWaveDefinition sub in def.subWaves)
                duration += sub.subWaveDuration;
        return duration;
    }

    // the point within a sub-wave (relative to the sub-wave's own start) that its last insect
    // spawns - not including the trailing delayBeforeNext pause
    private float SubWaveSpawnFinish(SubWaveDefinition sub)
    {
        float finish = 0f;
        if (sub.spawns == null) return finish;
        foreach (WaveSpawnEntry entry in sub.spawns)
        {
            if (entry.insectData == null || entry.count <= 0) continue;
            float entryFinish = entry.startDelay + entry.timeBetweenSpawns * Mathf.Max(0, entry.count - 1);
            if (entryFinish > finish) finish = entryFinish;
        }
        return finish;
    }

    // the last tick (relative to wave start) of a spawner that repeats every interval seconds,
    // starting at startDelay, for as long as the wave runs
    private float LastRepeatingSpawnOffset(float startDelay, float interval, float waveDuration)
    {
        if (interval <= 0f || waveDuration <= startDelay) return startDelay;
        int ticks = Mathf.FloorToInt((waveDuration - startDelay) / interval);
        return startDelay + ticks * interval;
    }

    // waits startOffset (this sub-wave's own start time within the wave), then fires every
    // spawn entry in this sub-wave concurrently - each entry runs its own count/timing
    // independently, so multiple entries in one sub-wave can overlap too
    IEnumerator RunSubWave(SubWaveDefinition sub, float startOffset, int waveNumber, int subWaveNumber)
    {
        if (startOffset > 0f)
            yield return new WaitForSeconds(startOffset);

        if (config.levelNumber == 1 && waveNumber == 1 && subWaveNumber == 2)
            level1Wave1SubWave2Started = true;

        if (sub.spawns == null) yield break;
        foreach (WaveSpawnEntry entry in sub.spawns)
            StartCoroutine(RunWaveSpawnEntry(entry, waveNumber));
    }

    IEnumerator RunWaveSpawnEntry(WaveSpawnEntry entry, int waveNumber)
    {
        if (!Spawns(entry))
            yield break;

        if (entry.startDelay > 0f)
            yield return new WaitForSeconds(entry.startDelay);

        for (int i = 0; i < entry.count; i++)
        {
            Spawn(entry.insectData.insectPrefab, waveNumber);
            if (i < entry.count - 1)
                yield return new WaitForSeconds(entry.timeBetweenSpawns);
        }
        ReleaseSpawnStream(waveNumber);
    }

    // ── a wave's spawning done ────────────────────────────────────────────────

    // spawn streams (spawn entries, trickles, the elite group) of each wave still to finish. at 0
    // the wave has spawned its last insect, so its guaranteed mulch can drop the moment its last
    // insect dies, instead of after the pause that closes the wave (Tommy, 2026-11-22: the mulch
    // sometimes came late). RunWaveToEnd still reports it at the wave's end in case a stream was cut
    private readonly Dictionary<int, int> spawnStreamsLeft = new Dictionary<int, int>();

    private static bool Spawns(WaveSpawnEntry entry) =>
        entry != null && entry.insectData != null && entry.insectData.insectPrefab != null && entry.count > 0;

    private static bool Spawns(TrickleEntry trickle) =>
        trickle != null && trickle.insectData != null && trickle.insectData.insectPrefab != null;

    private void ReleaseSpawnStream(int waveNumber)
    {
        if (!spawnStreamsLeft.TryGetValue(waveNumber, out int left) || left <= 0) return;
        spawnStreamsLeft[waveNumber] = --left;
        if (left == 0)
            MulchManager.instance?.NotifyWaveSpawningDone(waveNumber, waveNumber >= config.maxWaves);
    }

    // background spawner: loops for the wave's whole (computed) duration, independent of
    // whatever the scripted sub-waves are doing
    IEnumerator RunTrickle(TrickleEntry trickle, float waveDuration, int waveNumber)
    {
        if (!Spawns(trickle))
            yield break;

        if (trickle.startDelay > 0f)
            yield return new WaitForSeconds(trickle.startDelay);

        float elapsed = trickle.startDelay;
        while (elapsed < waveDuration)
        {
            Spawn(trickle.insectData.insectPrefab, waveNumber);
            yield return new WaitForSeconds(trickle.interval);
            elapsed += trickle.interval;
        }
        ReleaseSpawnStream(waveNumber);
    }

    // ── spawn helpers ─────────────────────────────────────────────────────────

    // spawns the elite group for an elite wave
    // count scales between minSpawnCount and maxSpawnCount based on wave progress (t = 0 to 1)
    // if count > 1, spreads the spawns evenly across the remaining wave time
    IEnumerator SpawnEliteGroup(LevelEliteEntry entry, float waveDuration, float t, int waveNumber)
    {
        int count = Mathf.RoundToInt(Mathf.Lerp(entry.minSpawnCount, entry.maxSpawnCount, t));
        count = Mathf.Max(1, count);

        if (entry.startDelay > 0f)
            yield return new WaitForSeconds(entry.startDelay);

        if (count <= 1)
        {
            Spawn(entry.data.insectPrefab, waveNumber);
        }
        else
        {
            // spread multiple spawns evenly across the time remaining after startDelay
            float remaining = waveDuration - entry.startDelay;
            float gap = remaining / count;
            for (int i = 0; i < count; i++)
            {
                Spawn(entry.data.insectPrefab, waveNumber);
                if (i < count - 1)
                    yield return new WaitForSeconds(gap);
            }
        }
        ReleaseSpawnStream(waveNumber);
    }

    // ── SpawnManager overrides ─────────────────────────────────────────────────

    // reads the hand-authored wave slots directly rather than the procedural roster/elite
    // arrays - what's actually placed in a wave's sub-wave spawns is what the player will really
    // fight, so that's what the loadout screen's insect preview should reflect
    public override GameObject[] GetInsectPrefabs()
    {
        var prefabs = new List<GameObject>();

        if (config.waves != null)
            foreach (WaveDefinition wave in config.waves)
            {
                if (wave == null) continue;

                if (wave.subWaves != null)
                    foreach (SubWaveDefinition sub in wave.subWaves)
                        if (sub?.spawns != null)
                            foreach (WaveSpawnEntry spawn in sub.spawns)
                                if (spawn?.insectData != null && spawn.insectData.insectPrefab != null)
                                    if (!prefabs.Contains(spawn.insectData.insectPrefab))
                                        prefabs.Add(spawn.insectData.insectPrefab);

                if (wave.trickleSpawns != null)
                    foreach (TrickleEntry trickle in wave.trickleSpawns)
                        if (trickle?.insectData != null && trickle.insectData.insectPrefab != null)
                            if (!prefabs.Contains(trickle.insectData.insectPrefab))
                                prefabs.Add(trickle.insectData.insectPrefab);
            }

        return prefabs.ToArray();
    }

    protected override void Update()
    {
        if (nextWaveTime < 0f && finalWaveSpawnTime >= 0f)
            GameHUD.instance?.SetFinalWaveTimer(Mathf.Max(0f, finalWaveSpawnTime - Time.time));
        else
            GameHUD.instance?.SetNextWaveTimer(nextWaveTime < 0f ? -1f : nextWaveTime - Time.time);

        if (!triedLevel1PlantInfoTutorial && level1Wave1SubWave2Started && GameManager.instance.SunCount > 18)
        {
            triedLevel1PlantInfoTutorial = true;
            if (TutorialManager.instance != null && TutorialManager.instance.TryShow(TutorialIds.Level1PlantInfo, TutorialContent.Level1PlantInfo,
                    TutorialSpeaker.AcornKnight, () => GameManager.instance.SetPause(false)))
                GameManager.instance.SetPause(true);
        }

        if (!triedLevel1SkillTutorial && level1Wave3StartTime >= 0f && Time.time - level1Wave3StartTime >= 4f)
            TryLevel1SkillTutorial();
    }

    // Level1UseSkillForTheFirstTime (Tommy): from 4 seconds into wave 3, as soon as an Acorn Knight
    // whose skill is still locked is on the field and the sun covers its unlock. that Knight is
    // selected, so his panel with the skill's vines is open for the first page
    private void TryLevel1SkillTutorial()
    {
        TutorialManager tutorials = TutorialManager.instance;
        if (tutorials == null || GameManager.instance == null) return;
        if (tutorials.HasSeen(TutorialIds.Level1UseSkillForTheFirstTime)) { triedLevel1SkillTutorial = true; return; }
        if (tutorials.IsAnyTutorialActive) return;
        AcornKnight knight = null;
        foreach (Plant plant in Plant.allPlants)
            if (plant is AcornKnight k && k.IsAlive && !k.path3Unlocked && GameManager.instance.SunCount >= k.GetPath3Cost())
            {
                knight = k;
                break;
            }
        if (knight == null) return;
        triedLevel1SkillTutorial = true;
        if (tutorials.TryShow(TutorialIds.Level1UseSkillForTheFirstTime, TutorialContent.Level1UseSkillForTheFirstTime,
                TutorialSpeaker.AcornKnight, () => GameManager.instance.SetPause(false)))
        {
            GameManager.instance.SetPause(true);
            PlantUpgradeUI.instance?.ShowPanel(knight);
        }
    }
}
