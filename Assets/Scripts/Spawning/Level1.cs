using UnityEngine;
using System.Collections;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 150, startHealth = 200;
    private int maxWave = 10;
    public GameObject workerAnt, scoutAnt;
    public GameObject weatherManager;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;
    public float restInterval = 8f;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    protected override void Start()
    {
        if (WeatherManager.instance) WeatherManager.instance.weather = WeatherType.Sunny;
        FertilizerSelectionUI.instance?.Configure(fertilizerPool);
        GameManager.instance?.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.CompleteLevel(1);
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        nextWaveTimer = 10f;
        yield return new WaitForSeconds(10f);

        while (wave < maxWave)
        {
            wave++;
            GameManager.instance.currentWave = wave;
            GameHUD.instance?.SetWaveCount(wave, maxWave);
            yield return StartCoroutine(Wave(wave));

            if (wave < maxWave)
                yield return StartCoroutine(RestPeriod(restInterval));
        }

        yield return new WaitUntil(() => Insect.allInsects.Count == 0);
        yield return new WaitForSeconds(3f);
        SaveManager.instance.CompleteLevel(1);
        Debug.Log("Level 1 completed");
    }

    // Spawns exactly spawnCount bugs, cycling through the provided pool in order.
    IEnumerator SpawnWave(params System.Action[] pool)
    {
        yield return new WaitForSeconds(waitTime);
        for (int i = 0; i < spawnCount; i++)
        {
            pool[i % pool.Length]();
            if (i < spawnCount - 1)
                yield return new WaitForSeconds(spawnInterval);
        }
    }

    IEnumerator Wave(int wave)
    {
        // ── PHASE 1 · Waves 1-3 · Setup ──────────────────────────────────────
        // WorkerAnt only. spawnCount = exact bug count.

        if (wave == 1)
        {
            waitTime = 2f; spawnInterval = 4f; spawnCount = 6;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        } else if (wave == 2)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 8;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        } else if (wave == 3)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        // ── PHASE 2 · Waves 4-7 · Rising pressure ────────────────────────────
        // ScoutAnt joins. Pool cycles so the ratio is exact.
        // [W, W, S] → 2 Workers per 1 Scout

        } else if (wave == 4)
        {
            // ScoutAnt joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnScoutAnt));

        } else if (wave == 5)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnScoutAnt));

        } else if (wave == 6)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnScoutAnt));

        } else if (wave == 7)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnScoutAnt));

        // ── PHASE 3 · Waves 8-10 · Tough ─────────────────────────────────────
        // Faster intervals, higher counts.

        } else if (wave == 8)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnScoutAnt));

        } else if (wave == 9)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnScoutAnt));

        } else if (wave == 10)
        {
            // Final wave
            waitTime = 2f; spawnInterval = 2f; spawnCount = 24;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnScoutAnt));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { workerAnt, scoutAnt };

    void SpawnWorkerAnt() { Spawn(workerAnt); }
    void SpawnScoutAnt()  { Spawn(scoutAnt); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
