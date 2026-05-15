using UnityEngine;
using System.Collections;

public class QueenAnt : Ant
{
    [SerializeField] private GameObject[] antPrefabs;
    public float spawnInterval = 12f;
    public int antsPerSpawn = 3;

    private float spawnTimer;
    private bool isSpawning;

    protected override void Awake()
    {
        baseAttackDamage = 30f;
        baseMaxHealth = 1000f;
        baseMovementSpeed = 0.45f;
        baseTenacity = 0.5f;
        basePhysicalResistance = 0.15f;
        baseMagicResistance = 0.15f;
        sunDrop = 30;
        base.Awake();
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
        float interval = 2f / antsPerSpawn;
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

    public override string GetName() => "<b><color=#8B0000>Queen Ant</color></b>";
    public override string GetDescription() => $"The {GetName()} is a formidable matriarch who periodically halts to spawn ants from her body.";
    public override string GetPassiveDescription() => $"Every {spawnInterval}s, stops for 2 seconds and spawns {antsPerSpawn} ants. 15% physical and magic resistance.";
}
