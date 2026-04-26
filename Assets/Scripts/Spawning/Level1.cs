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

        // wave 1 - worker ants every 2 seconds for 12 secs
        InvokeRepeating(nameof(SpawnWorkerAnt), 2f, 3f);
        yield return new WaitForSeconds(30f);
        CancelInvoke(nameof(SpawnWorkerAnt));

        InvokeRepeating(nameof(SpawnSoldierAnt), 0f, 3f);
        yield return new WaitForSeconds(30);
        CancelInvoke(nameof(SpawnScoutAnt));

        InvokeRepeating(nameof(SpawnScoutAnt), 0f, 2f);
        yield return new WaitForSeconds(32f);
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
