using UnityEngine;

public class Termite : Insect
{
    private TermiteData TData => data as TermiteData;

    [SerializeField] private GameObject termitePrefab;

    // set to true on companion termites so they don't spawn their own companions
    public bool isCompanion = false;

    private float scanTimer = 0f;
    private const float scanInterval = 0.3f;
    private const float buffDuration = 0.35f;
    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity   = Aggressivity.High;
    }

    protected override void Start()
    {
        base.Start();
        if (!isCompanion)
            SpawnCompanions();
    }

    protected override void Update()
    {
        base.Update();
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            UpdateSwarm();
        }
    }

    private void SpawnCompanions()
    {
        if (termitePrefab == null) return;
        int count = TData?.companionCount ?? 2;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.15f, 0.15f), 0f);
            GameObject go = Instantiate(termitePrefab, transform.position + offset, Quaternion.identity);
            Termite companion = go.GetComponent<Termite>();
            if (companion == null) continue;
            // mark as companion before its Start() runs so it won't spawn more companions
            companion.isCompanion = true;
            companion.SetPath(waypoints);
            companion.currentWaypointIndex = currentWaypointIndex;
        }
    }

    private void UpdateSwarm()
    {
        float range = TData?.swarmRange ?? 3f;
        int nearbyCount = 0;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || insect == this || !insect.IsAlive) continue;
            if (insect is Termite && Vector3.Distance(transform.position, insect.transform.position) <= range)
                nearbyCount++;
        }

        if (nearbyCount == 0) return;

        TermiteSwarmEffect existing = GetEffect<TermiteSwarmEffect>();
        if (existing != null)
            existing.Refresh(nearbyCount, this);
        else
            ApplyEffect(new TermiteSwarmEffect(this, buffDuration, nearbyCount, this));
    }
}
