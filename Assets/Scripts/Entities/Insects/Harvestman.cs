using UnityEngine;

// slow, tanky cave insect that ignores plants but hunts minions and hypnotized insects.
// aggressivity should be set to Low in the InsectData so it never targets plants.
public class Harvestman : Insect
{
    protected override Vector3 AimPointOffset => new Vector3(0f, data?.aimPointHeight ?? 0f, 0f);

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override IAttackable target
    {
        get
        {
            // prioritize minions over the base target (which is null for Low aggressivity)
            IAttackable minion = FindNearestMinionInRange();
            if (minion != null) return minion;
            return base.target;
        }
    }

    private IAttackable FindNearestMinionInRange()
    {
        Insect nearest = null;
        float nearestDist = Mathf.Infinity;
        foreach (Insect insect in Insect.friendlyInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (!CanReach(this, insect)) continue;
            float dist = Vector3.Distance(transform.position, insect.transform.position);
            if (dist <= targetingRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearest = insect;
            }
        }
        return nearest;
    }

    public override void Attack()
    {
        IAttackable current = target;
        if (current == null) return;
        float damage = attackDamage;
        if (current is Insect)
            damage *= 1f + (data?.minionDamageBonus ?? 0f);
        current.ReceiveAttack(damage, this);
    }
}
