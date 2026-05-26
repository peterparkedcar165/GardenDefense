using UnityEngine;
using System.Collections;

public class Level2 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 200, startHealth = 200;
    private int maxWave = 18;
    public GameObject workerAnt, soldierAnt, scoutAnt;
    public GameObject weatherManager;
    public float nextWaveTimer;
    public float restInterval = 5f;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    protected override void Start()
    {
        if (WeatherManager.instance) WeatherManager.instance.weather = WeatherType.Sunny;
        FertilizerSelectionUI.instance?.Configure(fertilizerPool);
        GameManager.instance?.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 1);
        SaveManager.instance.CompleteLevel(2);
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
        SaveManager.instance.CompleteLevel(2);
        Debug.Log("Level 2 completed");
    }

    // spawns exactly spawnCount bugs, cycling through the provided pool in order
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
        // phase 1 waves 1 to 4 setup
        // worker ant only, soldier ant joins wave 3

        if (wave == 1)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 8;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        } else if (wave == 2)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        } else if (wave == 3)
        {
            // soldier ant joins
            waitTime = 2f; spawnInterval = 3f; spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 4)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        // phase 2 waves 5 to 10 rising pressure
        // scout ant joins wave 7

        } else if (wave == 5)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 6)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 7)
        {
            // scout ant joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 8)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 9)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 10)
        {
            waitTime = 2f; spawnInterval = 1.75f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        // phase 3 waves 11 to 16 tough
        // faster intervals, higher counts

        } else if (wave == 11)
        {
            waitTime = 2f; spawnInterval = 1.5f; spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 12)
        {
            waitTime = 2f; spawnInterval = 1.25f; spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 13)
        {
            waitTime = 1f; spawnInterval = 1f; spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 14)
        {
            waitTime = 1f; spawnInterval = 1f; spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 15)
        {
            waitTime = 0f; spawnInterval = 1f; spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 16)
        {
            // final wave
            waitTime = 0f; spawnInterval = 1f; spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));
        } else if (wave == 17)
        {
            // final wave
            waitTime = 0f; spawnInterval = 0.75f; spawnCount = 32;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));
        } else if (wave == 18)
        {
            // final wave
            waitTime = 0f; spawnInterval = 0.75f; spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { workerAnt, soldierAnt, scoutAnt };

    void SpawnWorkerAnt()  { Spawn(workerAnt); }
    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
