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

    private Transform[][] _cachedAllPaths;

    private Transform[][] GetAllPaths()
    {
        if (_cachedAllPaths != null) return _cachedAllPaths;

        Transform[] main = PathManager.instance != null ? PathManager.instance.waypoints : System.Array.Empty<Transform>();

        if (spawnEntries == null || spawnEntries.Length == 0)
        {
            _cachedAllPaths = new Transform[][] { main };
            return _cachedAllPaths;
        }

        _cachedAllPaths = new Transform[spawnEntries.Length][];
        for (int i = 0; i < spawnEntries.Length; i++)
        {
            Transform[] leadIn = spawnEntries[i].leadInWaypoints ?? System.Array.Empty<Transform>();
            Transform[] full   = new Transform[leadIn.Length + main.Length];
            leadIn.CopyTo(full, 0);
            main.CopyTo(full, leadIn.Length);
            _cachedAllPaths[i] = full;
        }
        return _cachedAllPaths;
    }

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
            insect.SetPath(fullPath, GetAllPaths());
    }

    protected virtual void Update() { }
}
