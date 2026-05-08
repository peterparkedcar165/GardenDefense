using UnityEngine;

public abstract class SpawnManager : MonoBehaviour
{
    public Transform spawnPoint;
    protected virtual void Start()
    {
    
    }

    protected void StopAllSpawning()
    {
        CancelInvoke();
    }

    protected void Spawn(GameObject insect)
    {
        // Debug.Log("Spawning at: " + spawnPoint.position);
        Instantiate(insect, spawnPoint.position + (Vector3) new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)), Quaternion.identity);
    }


    // protected virtual void SpawnWorkerAnt()
    // {
    //      Instantiate(workerAntPrefab, spawnPoint.position, Quaternion.identity);
    // }
    // protected virtual void SpawnSoldierAnt()
    // {
    //     Instantiate(soldierAntPrefab, spawnPoint.position, Quaternion.identity);
    // }
    // protected virtual void SpawnScoutAnt()
    // {
    //     // Instantiate(scoutAntPrefab, spawnPoint.position, Quaternion.identity);
    // }

    // protected virtual void SpawnCarpenterAnt()
    // {
    //     // Instantiate(carpenterAntPrefab, spawnPoint.position, Quaternion.identity);
    // }
    protected virtual void Update()
    {
       
    }
}
