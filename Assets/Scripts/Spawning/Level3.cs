using UnityEngine;
using System.Collections;
using TMPro;

public class Level3 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 450, startHealth = 200;
    private int maxWave = 12;
    public GameObject workerAnt, soldierAnt, scoutAnt, fruitFly;
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
        SaveManager.instance.saveData.highestLevelUnlocked = Mathf.Max(SaveManager.instance.saveData.highestLevelUnlocked, 2);
        SaveManager.instance.CompleteLevel(3);
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
            SaveManager.instance.CompleteLevel(3);
            Debug.Log("Level 3 completed");
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1) // wave 1 - worker ants every 2 seconds for 40 secs
        {
            waitTime = 2f;
            spawnInterval = 2f;
            spawnCount = 30;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 2)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 3)
        {
            waitTime = 2f;
            spawnInterval = 2f;
            spawnCount = 30;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 4)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 5)
        {
            waitTime = 2f;
            spawnInterval = 2.5f;
            spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd5 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), wd5 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd5);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 6)
        {
            waitTime = 2f;
            spawnInterval = 3f;
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
            waitTime = 2f;
            spawnInterval = 2.75f;
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
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 75;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd8 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), wd8 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd8);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));

        } else if (wave == 9)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 60;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd9 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), wd9 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd9);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 10)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 68;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd10 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd10 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd10);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));

        } else if (wave == 11)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 68;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd11 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd11 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd11);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 12)
        {
            waitTime = 2f;
            spawnInterval = 2.0f;
            spawnCount = 68;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float wd12 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), wd12 / 3f, spawnInterval);
            yield return new WaitForSeconds(wd12);
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

    void SpawnFruitFly()
    {
        Spawn(fruitFly);
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
