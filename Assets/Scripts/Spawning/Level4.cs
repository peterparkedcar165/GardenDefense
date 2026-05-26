using UnityEngine;
using System.Collections;

public class Level4 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 500, startHealth = 200;
    private int maxWave = 25;
    public GameObject workerAnt, soldierAnt, scoutAnt, fruitFly, wasp;
    public GameObject weatherManager;
    public float nextWaveTimer;
    public float restInterval = 2f;

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
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 3);
        SaveManager.instance.CompleteLevel(4);
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
        SaveManager.instance.CompleteLevel(4);
        Debug.Log("Level 4 completed");
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
        // phase 1 waves 1 to 6 setup
        // worker ant only, soldier ant joins wave 3, scout ant joins wave 5, fruit fly joins wave 6

        if (wave == 1)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 9;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        } else if (wave == 2)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 10;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt));

        } else if (wave == 3)
        {
            // soldier ant joins
            waitTime = 2f; spawnInterval = 3f; spawnCount = 11;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 4)
        {
            waitTime = 2f; spawnInterval = 3f; spawnCount = 12;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt));

        } else if (wave == 5)
        {
            // scout ant joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 13;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt));

        } else if (wave == 6)
        {
            // fruit fly joins
            waitTime = 2f; spawnInterval = 2.5f; spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly));

        // phase 2 waves 7 to 15 rising pressure
        // wasp joins wave 7

        } else if (wave == 7)
        {
            // wasp joins
            waitTime = 2f; spawnInterval = 2.2f; spawnCount = 15;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnWasp));

        } else if (wave == 8)
        {
            waitTime = 2f; spawnInterval = 2f; spawnCount = 17;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnWasp));

        } else if (wave == 9)
        {
            waitTime = 2f; spawnInterval = 1.8f; spawnCount = 18;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 10)
        {
            waitTime = 2f; spawnInterval = 1.6f; spawnCount = 19;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 11)
        {
            waitTime = 2f; spawnInterval = 1.4f; spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 12)
        {
            waitTime = 2f; spawnInterval = 1.2f; spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 13)
        {
            waitTime = 2f; spawnInterval = 1f; spawnCount = 23;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 14)
        {
            waitTime = 2f; spawnInterval = 0.9f; spawnCount = 25;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 15)
        {
            waitTime = 2f; spawnInterval = 0.8f; spawnCount = 27;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        // phase 3 waves 16 to 25 tough
        // faster intervals, all five types, counts ramp steeply

        } else if (wave == 16)
        {
            waitTime = 2f; spawnInterval = 0.75f; spawnCount = 30;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 17)
        {
            waitTime = 2f; spawnInterval = 0.65f; spawnCount = 35;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 18)
        {
            waitTime = 2f; spawnInterval = 0.6f; spawnCount = 40;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 19)
        {
            waitTime = 2f; spawnInterval = 0.55f; spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 20)
        {
            waitTime = 2f; spawnInterval = 0.5f; spawnCount = 50;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 21)
        {
            waitTime = 2f; spawnInterval = 0.5f; spawnCount = 55;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 22)
        {
            waitTime = 2f; spawnInterval = 0.45f; spawnCount = 65;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 23)
        {
            waitTime = 2f; spawnInterval = 0.4f; spawnCount = 75;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 24)
        {
            waitTime = 2f; spawnInterval = 0.3f; spawnCount = 90;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));

        } else if (wave == 25)
        {
            // final wave
            waitTime = 2f; spawnInterval = 0.2f; spawnCount = 120;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            yield return StartCoroutine(SpawnWave(SpawnWorkerAnt, SpawnWorkerAnt, SpawnSoldierAnt, SpawnScoutAnt, SpawnFruitFly, SpawnWasp));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public override GameObject[] GetInsectPrefabs() => new[] { workerAnt, soldierAnt, scoutAnt, fruitFly, wasp };

    void SpawnWorkerAnt()  { Spawn(workerAnt); }
    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }
    void SpawnFruitFly()   { Spawn(fruitFly); }
    void SpawnWasp()       { Spawn(wasp); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
