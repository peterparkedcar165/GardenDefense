using UnityEngine;
using System.Collections;
using TMPro;

public class Level7 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 750, startHealth = 200;
    private int maxWave = 18;
    public GameObject workerAnt, soldierAnt, scoutAnt, fruitFly, wasp, queenAnt;
    public GameObject weatherManager;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;

    [Header("Fertilizers")]
    [SerializeField] private FertilizerData[] fertilizerPool;

    protected override void Start()
    {
        WeatherManager.instance.weather = WeatherType.Clear;
        FertilizerSelectionUI.instance.Configure(fertilizerPool);
        GameManager.instance.InitiateLevel(startSunCount, startHealth);
        GameHUD.instance?.SetWaveCount(wave, maxWave);
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 6);
        SaveManager.instance.CompleteLevel(7);
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
                yield return StartCoroutine(RestPeriod(10f));
        }

        yield return new WaitUntil(() => Insect.allInsects.Count == 0);
        yield return new WaitForSeconds(3f);
        SaveManager.instance.CompleteLevel(7);
        Debug.Log("Level 7 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1)
        {
            waitTime = 2f;
            spawnInterval = 1.75f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 2)
        {
            waitTime = 2f;
            spawnInterval = 1.5f;
            spawnCount = 28;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 3)
        {
            waitTime = 1f;
            spawnInterval = 1.75f;
            spawnCount = 22;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));

        } else if (wave == 4)
        {
            waitTime = 1f;
            spawnInterval = 1.5f;
            spawnCount = 28;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnWasp));

        } else if (wave == 5)
        {
            waitTime = 1f;
            spawnInterval = 2.25f;
            spawnCount = 28;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));

        } else if (wave == 6)
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 32;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));

        } else if (wave == 7)
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 32;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 3f, 12f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 8)
        {
            waitTime = 1f;
            spawnInterval = 1.75f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 2f, 10f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 9)
        {
            waitTime = 1f;
            spawnInterval = 1.25f;
            spawnCount = 42;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 2f, 8f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 10)
        {
            waitTime = 1f;
            spawnInterval = 1f;
            spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 2f, 6f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 11)
        {
            waitTime = 0.5f;
            spawnInterval = 0.75f;
            spawnCount = 48;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 12)
        {
            waitTime = 0.5f;
            spawnInterval = 0.6f;
            spawnCount = 52;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 4f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 13)
        {
            waitTime = 0.5f;
            spawnInterval = 0.5f;
            spawnCount = 55;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 14)
        {
            waitTime = 0.5f;
            spawnInterval = 0.4f;
            spawnCount = 58;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3.5f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 15)
        {
            waitTime = 0.5f;
            spawnInterval = 0.35f;
            spawnCount = 60;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 16)
        {
            waitTime = 0.5f;
            spawnInterval = 0.3f;
            spawnCount = 62;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 17)
        {
            waitTime = 0.5f;
            spawnInterval = 0.25f;
            spawnCount = 65;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));

        } else if (wave == 18)
        {
            waitTime = 0.5f;
            spawnInterval = 0.2f;
            spawnCount = 70;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnFruitFly), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnWasp), waitTime, spawnInterval * 2f);
            InvokeRepeating(nameof(SpawnQueenAnt), 1f, 3f);
            yield return new WaitForSeconds(waitTime + ((spawnCount - 1) * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
            CancelInvoke(nameof(SpawnFruitFly));
            CancelInvoke(nameof(SpawnWasp));
            CancelInvoke(nameof(SpawnQueenAnt));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    void SpawnWorkerAnt()  { Spawn(workerAnt); }
    void SpawnSoldierAnt() { Spawn(soldierAnt); }
    void SpawnScoutAnt()   { Spawn(scoutAnt); }
    void SpawnFruitFly()   { Spawn(fruitFly); }
    void SpawnWasp()       { Spawn(wasp); }
    void SpawnQueenAnt()   { Spawn(queenAnt); }

    protected override void Update()
    {
        if (nextWaveTimer > 0)
        {
            nextWaveTimer -= Time.deltaTime;
            GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
        }
    }
}
