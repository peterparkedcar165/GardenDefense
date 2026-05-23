using UnityEngine;
using System.Collections;
using TMPro;

public class Level2 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 350, startHealth = 200;
    private int maxWave = 10;
    public GameObject workerAnt, soldierAnt, scoutAnt;
    public GameObject weatherManager;
    public float nextWaveTimer;
    public float restInterval = 15f;

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
        yield return new WaitForSeconds(10f); // initial setup time

        while (wave < maxWave)
        {
           wave++;
           GameManager.instance.currentWave = wave;
           GameHUD.instance?.SetWaveCount(wave, maxWave);
            yield return StartCoroutine(Wave(wave)); 

            if (wave < maxWave)
            {
                yield return StartCoroutine(RestPeriod(restInterval)); // resting between waves
            }
        }

        // Level finishes after loop is finished

        // LEVEL COMPLETION
            yield return new WaitUntil(() => Insect.allInsects.Count == 0);
            yield return new WaitForSeconds(3f);
            SaveManager.instance.CompleteLevel(2);
            Debug.Log("Level 2 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1) // wave 1 - worker ants every 2 seconds for 40 secs
        {
            waitTime = 3f;
            spawnInterval = 3f;
            spawnCount = 30;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 2)
        {
            waitTime = 3f;
            spawnInterval = 2.5f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 3)
        {
            waitTime = 3f;
            spawnInterval = 3f;
            spawnCount = 23;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 4)
        {
            waitTime = 3f;
            spawnInterval = 2.5f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd4 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd4 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd4);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));

        } else if (wave == 5)
        {
            waitTime = 3f;
            spawnInterval = 2.25f;
            spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd5 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd5 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd5);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 6)
        {
            waitTime = 3f;
            spawnInterval = 2f;
            spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd6 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd6 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd6);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 7)
        {
            waitTime = 3f;
            spawnInterval = 2f;
            spawnCount = 53;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd7 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd7 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd7);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 8)
        {
            waitTime = 3f;
            spawnInterval = 2.0f;
            spawnCount = 68;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));

        } else if (wave == 9)
        {
            waitTime = 3f;
            spawnInterval = 2.0f;
            spawnCount = 83;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd9 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd9 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd9);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 10)
        {
            waitTime = 3f;
            spawnInterval = 2.0f;
            spawnCount = 98;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd10 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd10 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd10);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        yield return new WaitForSeconds(duration);
    }


// SPAWNING OF SPECIFIC TYPES
    void SpawnWorkerAnt() {
        Spawn(workerAnt);
    }

    void SpawnSoldierAnt() {
        Spawn(soldierAnt);
    }
    
    void SpawnScoutAnt()
    {
        Spawn(scoutAnt);
    }

    protected override void Update()
{
    if (nextWaveTimer > 0)
    {
        nextWaveTimer -= Time.deltaTime;
        GameHUD.instance?.SetNextWaveTimer(nextWaveTimer);
    }
}
}
