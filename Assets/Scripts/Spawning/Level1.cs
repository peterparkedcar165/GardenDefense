using UnityEngine;
using System.Collections;
using TMPro;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave, maxWave = 7;
    public GameObject workerAnt, soldierAnt, scoutAnt, carpenterAnt;
    public TextMeshProUGUI waveCountText;

    [Header("Spawning")]
    public float waitTime;
    public float spawnInterval;
    public int spawnCount;

    protected override void Start()
    {
        waveCountText.text = $"Wave: {wave}/{maxWave}";
        StartCoroutine(RunWave());
    }
    IEnumerator RunWave()
    {
        yield return new WaitForSeconds(10f); // initial setup time

        while (wave < maxWave)
        {
           wave++;
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
            spawnInterval = 2f;
            spawnCount = 20;

            InvokeRepeating(nameof(SpawnWorkerAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 2) 
        {
            waitTime = 1f;
            spawnInterval = 1.5f;
            spawnCount = 15;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 3) 
        {
            waitTime = 1f;
            spawnInterval = 1f;
            spawnCount = 18;

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 4) 
        {
            waitTime = 1f;
            spawnInterval = 1f;
            spawnCount = 25;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 5)
        {
            waitTime = 1f;
            spawnInterval = 1f;
            spawnCount = 20;

            InvokeRepeating(nameof(SpawnSoldierAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 6) 
        {
            waitTime = 1f;
            spawnInterval = 0.5f;
            spawnCount = 45;

            InvokeRepeating(nameof(SpawnScoutAnt), waitTime, spawnInterval);
            yield return new WaitForSeconds(waitTime + ((spawnCount -1)  * spawnInterval));
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 7)
        {
            waitTime = 1f;
            spawnInterval = 0.25f;
            spawnCount = 50;

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
       
    }
}
