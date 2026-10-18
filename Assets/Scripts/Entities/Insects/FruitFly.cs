using UnityEngine;

public class FruitFly : FlyingInsect
{
    private FruitFlyData FData => data as FruitFlyData;

    [SerializeField] private GameObject fruitFlyPrefab;

    // set to true on companion fruit flies so they don't spawn their own companions
    public bool isCompanion = false;

    private float scanTimer = 0f;
    private const float scanInterval = 0.3f;
    private const float buffDuration = 0.35f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
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
        if (fruitFlyPrefab == null) return;
        int count = FData?.companionCount ?? 2;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.7f, 0.7f), Random.Range(-0.4f, 0.4f), 0f);
            GameObject go = Instantiate(fruitFlyPrefab, transform.position + offset, Quaternion.identity);
            FruitFly companion = go.GetComponent<FruitFly>();
            if (companion == null) continue;
            // mark as companion before its Start() runs so it won't spawn more companions
            companion.isCompanion = true;
            companion.SetPath(waypoints);
            companion.currentWaypointIndex = currentWaypointIndex;
        }
    }

    private void UpdateSwarm()
    {
        float range = FData?.swarmRange ?? 3f;
        int nearbyCount = 0;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || insect == this || !insect.IsAlive) continue;
            if (insect is FruitFly && Vector3.Distance(transform.position, insect.transform.position) <= range)
                nearbyCount++;
        }

        if (nearbyCount == 0) return;

        FruitFlySwarmEffect existing = GetEffect<FruitFlySwarmEffect>();
        if (existing != null)
            existing.Refresh(nearbyCount, this);
        else
            ApplyEffect(new FruitFlySwarmEffect(this, buffDuration, nearbyCount, this));
    }

    public override string GetDescription() =>
        $"Increases Movement Speed by <color=green><b>{FruitFlySwarmEffect.speedBonusPerLevel:F1}</b></color> for every other Fruit Fly within a " +
        $"<b>{(FData?.swarmRange ?? 3f):F0}</b>-radius." + FlyingLine() + AggressivityLine();
}
