using UnityEngine;
using System.Collections;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave;
    public GameObject workerAnt, soldierAnt, scoutAnt, carpenterAnt;

    protected override void Start()
    {
        StartCoroutine(Wave1());
    }
    IEnumerator Wave1()
    {
        wave++;

        // wave 1 - worker ants every 2 seconds for 40 secs
        InvokeRepeating(nameof(SpawnWorkerAnt), 2f, 2f);
        yield return new WaitForSeconds(40f);
        CancelInvoke(nameof(SpawnWorkerAnt));

        // wave 2 - scout ants every 1 seconds for 15 seconds
        InvokeRepeating(nameof(SpawnScoutAnt), 0f, 1.5f);
        yield return new WaitForSeconds(30f);
        CancelInvoke(nameof(SpawnScoutAnt));

        InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 1f);
        yield return new WaitForSeconds(36f);
        CancelInvoke(nameof(SpawnSoldierAnt));

        InvokeRepeating(nameof(SpawnScoutAnt), 0f, 1f);
        yield return new WaitForSeconds(45f);
        CancelInvoke(nameof(SpawnScoutAnt));

        InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 1f);
        yield return new WaitForSeconds(40f);
        CancelInvoke(nameof(SpawnSoldierAnt));

        InvokeRepeating(nameof(SpawnScoutAnt), 0f, 0.5f);
        yield return new WaitForSeconds(90f);
        CancelInvoke(nameof(SpawnScoutAnt));

        InvokeRepeating(nameof(SpawnScoutAnt), 0f, 0.25f);
        InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 0.25f);
        yield return new WaitForSeconds(45f);
        CancelInvoke(nameof(SpawnScoutAnt));
        CancelInvoke(nameof(SpawnSoldierAnt));
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
