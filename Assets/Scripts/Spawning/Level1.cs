using UnityEngine;
using System.Collections;
using TMPro;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave;
    private int startSunCount = 350, startHealth = 20;
    private int maxWave = 8;
    public GameObject workerAnt, soldierAnt, scoutAnt, carpenterAnt;
    public GameObject weatherManager;
    public TextMeshProUGUI waveCountText, nextWaveTimerText;
    public float nextWaveTimer;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;

    protected override void Start()
    {
        WeatherManager.instance.weather = WeatherType.Sunny;

        GameManager.instance.InitiateLevel(startSunCount, startHealth);
        waveCountText.text = $"Wave: {wave}/{maxWave}";
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
           waveCountText.text = $"Wave: {wave}/{maxWave}";
            yield return StartCoroutine(Wave(wave)); 

            if (wave < maxWave)
            {
                yield return StartCoroutine(RestPeriod(10f)); // resting between waves
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
            spawnCount = 14;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 2) 
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 16;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 3) 
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 20;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 4) 
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 24;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 5)
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 28;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 6) 
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 32;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 7)
        {
            waitTime = 1f;
            spawnInterval = 2f;
            spawnCount = 34;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 8)
        {
            waitTime = 1f;
            spawnInterval = 1f;
            spawnCount = 40;
            nextWaveTimer = waitTime + ((spawnCount - 1) * spawnInterval) + 10f; // wave + rest

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
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

    void SpawnCarpenterAnt()
    {
        Spawn(carpenterAnt);
    }

    protected override void Update()
{
    if (nextWaveTimer > 0)
    {
        nextWaveTimer -= Time.deltaTime;
        nextWaveTimerText.text = $"Next wave in {Mathf.CeilToInt(Mathf.Max(0, nextWaveTimer))}s";
    }
}
}
