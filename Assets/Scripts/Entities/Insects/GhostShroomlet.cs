using UnityEngine;

// summoned by the Ghost Fungus. it holds at a fixed point and only pursues enemies that are
// within the FUNGUS' attack range (a leash). when no such enemy exists it walks back to its hold
// point. deals ice physical damage, inherits the fungus' stats, and persists (the fungus respawns it)
public class GhostShroomlet : Minion
{
    private GhostFungus owner;
    private Vector3 holdPoint;
    private bool holdSet;

    protected override bool ChasesTarget => true;   // pursues enemies inside the fungus' range

    // called by the Ghost Fungus right after spawn
    public void Configure(GhostFungus owner, Vector3 holdPoint, float attackDamage, float maxHealth, float attackSpeed, float attackRange)
    {
        this.owner     = owner;
        this.holdPoint = holdPoint;
        this.holdSet   = true;
        baseAttackDamage    = attackDamage;
        baseMaxHealth       = maxHealth;
        baseAttackSpeed     = attackSpeed;
        baseAttackRange     = attackRange;
        attackDamageType    = DamageType.Physical;
        attackElementalType = ElementalType.Ice;
        lifetime            = Mathf.Infinity;   // persists; the fungus handles respawns
    }

    // only the nearest enemy that sits within the fungus' attack range is a valid target
    public override IAttackable target
    {
        get
        {
            if (owner == null) return null;
            Vector3 center = owner.transform.position;
            float leash = owner.attackRange;
            Insect nearest = null;
            float nearestDist = Mathf.Infinity;
            foreach (Insect enemy in allInsects)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                if (Vector3.Distance(center, enemy.transform.position) > leash) continue;
                float d = Vector3.Distance(transform.position, enemy.transform.position);
                if (d < nearestDist) { nearestDist = d; nearest = enemy; }
            }
            return nearest;
        }
    }

    // no enemy in the fungus' range: walk back to the hold point
    protected override void FriendlyIdle()
    {
        if (!holdSet) return;
        if (Vector3.Distance(transform.position, holdPoint) <= 0.1f) return;
        Vector3 dir = (holdPoint - transform.position).normalized;
        transform.position += dir * GetMoveSpeed() * Time.deltaTime;
    }

    public override string GetName() => "<color=#B0E0E6>Ghost Shroomlet</color>";
}
