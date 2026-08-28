using UnityEngine;
using System.Collections;

public class QueenAnt : Ant
{
    [SerializeField] private GameObject[] antPrefabs;
    public float spawnInterval = 12f;
    public int antsPerSpawn = 2;

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
        if (HasEffect<HardCrowdControl>()) return;
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
            while (HasEffect<HardCrowdControl>())
                yield return null;
            SpawnRandomAnt();
            yield return new WaitForSeconds(interval);
        }
        isSpawning = false;
    }

    private void SpawnRandomAnt()
    {
        if (antPrefabs == null || antPrefabs.Length == 0) return;
        GameObject prefab = antPrefabs[Random.Range(0, antPrefabs.Length)];
        // spawn the baby's root where the queen's root is (on the path), not at her visual.
        // when she is knocked up her visual is raised, but the root must stay grounded so the
        // baby pathfinds correctly. LaunchAnt then starts the baby's visual at the queen's visual
        GameObject antGO = Instantiate(prefab, transform.position, Quaternion.identity);
        Insect ant = antGO.GetComponent<Insect>();
        if (ant != null)
        {
            // give the spawned ant the queen's full path (including any lead-in waypoints)
            // so it follows the correct route regardless of which spawn lane the queen came from
            ant.SetPath(waypoints);
            ant.currentWaypointIndex = currentWaypointIndex;

            // if the queen herself is hypnotized, her children are born with the same hypnosis.
            // pre-set the team so it is born friendly (no kill rewards), then copy the effect
            HypnotizedEffect hypno = GetEffect<HypnotizedEffect>();
            if (hypno != null)
            {
                ant.SetTeam(Team.Friendly);
                ant.movingBackward = true;
                ant.ApplyEffect(hypno.Clone(ant));
            }

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
        // start the baby's visual at the queen's visual position (raised when she is knocked up),
        // then settle it down to its resting local position. scatter horizontally a little
        Vector3 queenVisualWorld = visual != null ? visual.position : transform.position;
        Vector3 startLocal = queenVisualWorld - antGO.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0f, 0f);
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
