using UnityEngine;

public class Termite : Insect
{
    public float bonusPerTermite = 3f;
    public float swarmRange = 3f;

    private TermiteData TData => data as TermiteData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity   = Aggressivity.High;
        targetingRange = 1f;
        if (TData != null)
        {
            bonusPerTermite = TData.bonusPerTermite;
            swarmRange      = TData.swarmRange;
        }
    }

    public override void UpdateStats()
    {
        int nearbyCount = 0;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || insect == this) continue;
            if (insect is Termite && Vector3.Distance(transform.position, insect.transform.position) <= swarmRange)
                nearbyCount++;
        }
        attackDamageAdder = nearbyCount * bonusPerTermite;
        base.UpdateStats();
    }
}
