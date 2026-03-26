using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public float levelTime;
    public Transform spawnPoint;
    public GameObject workerAntPrefab, soldierAntPrefab;
    void Start()
    {
        // invoke repeating of Object, starting at ..., every ...
        InvokeRepeating(nameof(SpawnWorkerAnt/*method*/), 2f/*starts at*/, 2f /*repeats every*/);
        Invoke(nameof(StopSpawning),10f);
       // Invoke(nameof(StopSpawning), 30f); // stops after ... seconds
        InvokeRepeating(nameof(SpawnSoldierAnt), 11f, 5f);
    }

    void StopSpawning()
    {
        CancelInvoke(nameof(Spawn));
    }

    protected virtual void Spawn(Insect insect)
    {
        Instantiate(insect, spawnPoint. position, Quaternion.identity);
    }


    protected virtual void SpawnWorkerAnt()
    {
         Instantiate(workerAntPrefab, spawnPoint.position, Quaternion.identity);
    }
    protected virtual void SpawnSoldierAnt()
    {
        Instantiate(soldierAntPrefab, spawnPoint.position, Quaternion.identity);
    }
    protected virtual void SpawnScoutAnt()
    {
        // Instantiate(scoutAntPrefab, spawnPoint.position, Quaternion.identity);
    }

    protected virtual void SpawnCarpenterAnt()
    {
        // Instantiate(carpenterAntPrefab, spawnPoint.position, Quaternion.identity);
    }
    void Update()
    {
       
    }
}
