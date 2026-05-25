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

    protected void StopAllSpawning()
    {
        CancelInvoke();
    }

    protected void Spawn(GameObject insectPrefab)
    {
        if (spawnEntries == null || spawnEntries.Length == 0)
        {
            Debug.LogWarning("SpawnManager: no spawnEntries configured on " + gameObject.name);
            return;
        }
        SpawnFromEntry(insectPrefab, spawnEntries[Random.Range(0, spawnEntries.Length)]);
    }

    protected void SpawnAt(GameObject insectPrefab, int entryIndex)
    {
        if (spawnEntries == null || entryIndex < 0 || entryIndex >= spawnEntries.Length)
        {
            Debug.LogWarning("SpawnManager: invalid entryIndex " + entryIndex);
            return;
        }
        SpawnFromEntry(insectPrefab, spawnEntries[entryIndex]);
    }

    private void SpawnFromEntry(GameObject insectPrefab, SpawnEntry entry)
    {
        if (entry.spawnPoint == null)
        {
            Debug.LogWarning("SpawnManager: spawnEntry has no spawnPoint assigned.");
            return;
        }

        Vector3 pos = entry.spawnPoint.position + (Vector3)new Vector2(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
        GameObject go = Instantiate(insectPrefab, pos, Quaternion.identity);

        // build full path: lead-in waypoints (if any) + shared main path
        Transform[] mainWaypoints = PathManager.instance != null ? PathManager.instance.waypoints : new Transform[0];
        Transform[] leadIn        = entry.leadInWaypoints ?? new Transform[0];

        Transform[] fullPath = new Transform[leadIn.Length + mainWaypoints.Length];
        leadIn.CopyTo(fullPath, 0);
        mainWaypoints.CopyTo(fullPath, leadIn.Length);

        Insect insect = go.GetComponent<Insect>();
        if (insect != null)
            insect.SetPath(fullPath);
    }

    protected virtual void Update() { }
}
