using UnityEngine;

// slow, tanky cave insect that ignores plants but hunts minions and hypnotized insects.
// aggressivity should be set to Low in the InsectData so it never targets plants.
public class Harvestman : Insect
{
    private HarvestmanData HData => data as HarvestmanData;

    protected override Vector3 AimPointOffset => new Vector3(0f, HData?.aimPointHeight ?? 0f, 0f);

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
            Insect minion = CurrentMinion();
            if (minion != null) return minion;
            return base.target;
        }
    }

    // the minion it goes for, worked out once a frame: target is read several times a frame and
    // every read scanned every friendly. worked out again within the frame if that one dies
    private Insect _minion;
    private int _minionFrame = -1;

    private Insect CurrentMinion()
    {
        bool gone = !ReferenceEquals(_minion, null) && (_minion == null || !_minion.IsAlive);
        if (_minionFrame != Time.frameCount || gone)
        {
            _minion = FindNearestMinionInRange();
            _minionFrame = Time.frameCount;
        }
        return _minion;
    }

    private Insect FindNearestMinionInRange()
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
            damage *= 1f + (HData?.minionDamageBonus ?? 0f);
        current.ReceiveAttack(damage, this);
    }
}
