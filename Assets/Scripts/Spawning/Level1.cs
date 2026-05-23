using UnityEngine;
using System.Collections;
using TMPro;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 350, startHealth = 200;
    private int maxWave = 8;
    public GameObject workerAnt, scoutAnt;
    public GameObject weatherManager;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;
    public float restInterval = 5f;

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
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1) // wave 1 - worker ants every 2 seconds for 40 secs
        {
            waitTime = 2f;
            spawnInterval = 3f;
            spawnCount = 23;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 2)
        {
            waitTime = 3f;
            spawnInterval = 2f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 3)
        {
            waitTime = 3f;
            spawnInterval = 2.0f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 4)
        {
            waitTime = 3f;
            spawnInterval = 2.0f;
            spawnCount = 53;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 5)
        {
            waitTime = 3f;
            spawnInterval = 2.0f;
            spawnCount = 45;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 6)
        {
            waitTime = 3f;
            spawnInterval = 1.5f;
            spawnCount = 38;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 7)
        {
            waitTime = 3f;
            spawnInterval = 1.5f;
            spawnCount = 53;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float waveDuration7 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waveDuration7 / 3f, spawnInterval);
            yield return new WaitForSeconds(waveDuration7);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 8)
        {
            waitTime = 3f;
            spawnInterval = 1.5f;
            spawnCount = 68;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + restInterval;
            float waveDuration8 = waitTime + ((spawnCount - 1) * spawnInterval);

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnScoutAnt), waveDuration8 / 3f, spawnInterval);
            yield return new WaitForSeconds(waveDuration8);
            CancelInvoke(nameof(SpawnWorkerAnt));
            CancelInvoke(nameof(SpawnScoutAnt));

            // LEVEL COMPLETION
            yield return new WaitUntil(() => Insect.allInsects.Count == 0);
            yield return new WaitForSeconds(3f);
            SaveManager.instance.CompleteLevel(1);
            Debug.Log("Level 1 completed");
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
