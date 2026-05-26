using UnityEngine;

public class Termite : Insect
{
    private TermiteData TData => data as TermiteData;

    private float scanTimer = 0f;
    private const float scanInterval = 0.3f;
    private const float buffDuration = 0.35f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity   = Aggressivity.High;
        targetingRange = 1f;
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
