using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public Transform spawnPoint;
    public Transform[] leadInWaypoints; // empty = joins main path immediately
}

public abstract class SpawnManager : MonoBehaviour
{
    public SpawnEntry[] spawnEntries;

    protected virtual void Start() { }

    public virtual GameObject[] GetInsectPrefabs() => System.Array.Empty<GameObject>();

    // wave: the wave this insect belongs to (Insect.spawnWave), so two waves running at once (an early
    // call) keep their own health scaling and mulch counts. 0 leaves it to GameManager.currentWave
    protected void Spawn(GameObject insectPrefab, int wave = 0)
    {
        if (spawnEntries == null || spawnEntries.Length == 0)
        {
            Debug.LogWarning("SpawnManager: no spawnEntries configured on " + gameObject.name);
            return;
        }
        SpawnFromEntry(insectPrefab, spawnEntries[Random.Range(0, spawnEntries.Length)], wave);
    }

    private void SpawnFromEntry(GameObject insectPrefab, SpawnEntry entry, int wave)
    {
        if (entry.spawnPoint == null)
        {
            Debug.LogWarning("SpawnManager: spawnEntry has no spawnPoint assigned.");
            return;
        }

        Vector3 pos = entry.spawnPoint.position + (Vector3)new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.2f, 0.2f));
        GameObject go = Instantiate(insectPrefab, pos, Quaternion.identity);

        // build full path: lead-in waypoints (if any) + shared main path
        Transform[] mainWaypoints = PathManager.instance != null ? PathManager.instance.waypoints : new Transform[0];
        Transform[] leadIn        = entry.leadInWaypoints ?? new Transform[0];

        Transform[] fullPath = new Transform[leadIn.Length + mainWaypoints.Length];
        leadIn.CopyTo(fullPath, 0);
        mainWaypoints.CopyTo(fullPath, leadIn.Length);

        Insect insect = go.GetComponent<Insect>();
        if (insect != null)
        {
            insect.spawnWave = wave;
            // counted for its wave's guaranteed mulch the moment it exists, not a frame later in its
            // Start: a wave can now report its spawning done right after its last spawn, and that
            // last insect must already be counted then (Insect.Start's call does nothing after this)
            MulchManager.instance?.RegisterInsect(insect);
            insect.SetPath(fullPath);
            OnInsectSpawned(insect);
        }
    }

    protected virtual void OnInsectSpawned(Insect insect) { }

    protected virtual void Update() { }
}
