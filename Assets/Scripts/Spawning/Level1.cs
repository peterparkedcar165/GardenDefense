using UnityEngine;

public class Level1 : SpawnManager
{
    public float levelTime;
    public int wave;
    public Transform spawnPoint;
    public GameObject workerAntPrefab;
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
        CancelInvoke(nameof(SpawnWorkerAnt));
    }

    protected virtual void Spawn(Insect insect)
    {
        Instantiate(insect, spawnPoint. position, Quaternion.identity);
    }


    void Update()
    {
       
    }
}
