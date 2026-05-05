using UnityEngine;
using System.Collections;
using TMPro;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave, maxWave = 7;
    public GameObject workerAnt, soldierAnt, scoutAnt, carpenterAnt;
    private bool skipRest;
    public TextMeshProUGUI waveCountText;

    protected override void Start()
    {
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
                yield return StartCoroutine(RestPeriod(5f)); // resting between waves
            }
        }

        // Level finishes after loop is finished
    }

    IEnumerator Wave(int wave)
    {
        if (wave == 1) // wave 1 - worker ants every 2 seconds for 40 secs
        {
            InvokeRepeating(nameof(SpawnWorkerAnt), 2f, 2f);
            yield return new WaitForSeconds(40f);
            CancelInvoke(nameof(SpawnWorkerAnt));
        } else if (wave == 2) 
        {
            InvokeRepeating(nameof(SpawnScoutAnt), 0f, 1.5f);
            yield return new WaitForSeconds(30f);
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 3) 
        {
            InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 1f);
            yield return new WaitForSeconds(36f);
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 4) 
        {
            InvokeRepeating(nameof(SpawnScoutAnt), 0f, 1f);
            yield return new WaitForSeconds(45f);
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 5)
        {
            InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 1f);
            yield return new WaitForSeconds(40f);
            CancelInvoke(nameof(SpawnSoldierAnt));
        } else if (wave == 6) 
        {
            InvokeRepeating(nameof(SpawnScoutAnt), 0f, 0.5f);
            yield return new WaitForSeconds(90f);
            CancelInvoke(nameof(SpawnScoutAnt));
        } else if (wave == 7)
        {
            InvokeRepeating(nameof(SpawnScoutAnt), 0f, 0.25f);
            InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 0.25f);
            yield return new WaitForSeconds(45f);
            CancelInvoke(nameof(SpawnScoutAnt));
            CancelInvoke(nameof(SpawnSoldierAnt));
        }
    }

    IEnumerator RestPeriod(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && !skipRest)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        skipRest = false;
    }

    public void SkipToNextWave()
    {
        skipRest = true;
        Debug.Log("Wave rest skipped");
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
