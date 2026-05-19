using UnityEngine;
using System.Collections;

public class QueenAnt : Ant
{
    [SerializeField] private GameObject[] antPrefabs;
    public float spawnInterval = 12f;
    public int antsPerSpawn = 5;

    private float spawnTimer;
    private bool isSpawning;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        transform.localScale = Vector3.one * 1f;
    }

    protected override void Update()
    {
        base.Update();
        if (!isSpawning)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                StartCoroutine(SpawnRoutine());
            }
        }
    }

    protected override void Move()
    {
        if (isSpawning) return;
        base.Move();
    }

    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        float interval = 1f / antsPerSpawn;
        for (int i = 0; i < antsPerSpawn; i++)
        {
            SpawnRandomAnt();
            yield return new WaitForSeconds(interval);
        }
        isSpawning = false;
    }

    private void SpawnRandomAnt()
    {
        if (antPrefabs == null || antPrefabs.Length == 0) return;
        GameObject prefab = antPrefabs[Random.Range(0, antPrefabs.Length)];
        GameObject antGO = Instantiate(prefab, transform.position, Quaternion.identity);
        Insect ant = antGO.GetComponent<Insect>();
        if (ant != null)
        {
            ant.currentWaypointIndex = currentWaypointIndex;
            StartCoroutine(LaunchAnt(antGO));
        }
    }

    private IEnumerator LaunchAnt(GameObject antGO)
    {
        yield return null; // wait for ant's Start() to run

        if (antGO == null) yield break;
        Transform antVisual = antGO.transform.Find("Visual");
        if (antVisual == null) yield break;

        Vector3 endLocal = antVisual.localPosition;
        Vector3 startLocal = endLocal + new Vector3(Random.Range(-0.5f, 0.5f), 1.5f, 0f);
        antVisual.localPosition = startLocal;

        float duration = 0.6f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (antGO == null) yield break;
            elapsed += Time.deltaTime;
            antVisual.localPosition = Vector3.Lerp(startLocal, endLocal, elapsed / duration);
            yield return null;
        }

        if (antGO != null)
            antVisual.localPosition = endLocal;
    }

}
