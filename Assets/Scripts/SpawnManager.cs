using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public float levelTime;
    public Transform spawnPoint;
    public GameObject workerAntPrefab;
    void Start()
    {
        // invoke repeating of Object, starting at ..., every ...
        InvokeRepeating(nameof(SpawnWorkerAnt), 2f, 2f);
       // Invoke(nameof(StopSpawning), 30f); // stops after ... seconds
    }

    void StopSpawning()
    {
        CancelInvoke(nameof(SpawnWorkerAnt));
    }

    
    void SpawnWorkerAnt()
    {
         Instantiate(workerAntPrefab, spawnPoint.position, Quaternion.identity);
    }
    void Update()
    {
       
    }
}
