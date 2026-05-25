using UnityEngine;
using System.Collections;

public class Level3 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 250, startHealth = 200;
    private int maxWave = 22;
    public GameObject workerAnt, soldierAnt, scoutAnt, fruitFly;
    public GameObject weatherManager;
    public float nextWaveTimer;
    public float restInterval = 12f;

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
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 2);
        SaveManager.instance.CompleteLevel(3);
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
        SaveManager.instance.CompleteLevel(3);
        Debug.Log("Level 3 completed");
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
        // phase 1 waves 1 to 5 setup
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

        } else if (wave == 5)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        // phase 2 waves 6 to 13 rising pressure
        // scout ant joins wave 9, fruit fly joins wave 12

        } else if (wave == 6)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 7)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 8)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 9)
        {
            // scout ant joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 10)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 11)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 12)
        {
            // fruit fly joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 13)
        {
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        // phase 3 waves 14 to 22 tough
        // faster intervals, all four types, counts ramp steeply

        } else if (wave == 14)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 15)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 16)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 17)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 18)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 24;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 19)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 26;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 20)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 28;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 21)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 30;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        } else if (wave == 22)
        {
            // final wave
            waitTime = 2f; spawnInterval = 2f; spawnCount = 32;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { workerAnt, soldierAnt, scoutAnt, fruitFly };

    void SpawnWorkerAnt()  { Spawn(workerAnt); }
    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }
    void SpawnFruitFly()   { Spawn(fruitFly); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
