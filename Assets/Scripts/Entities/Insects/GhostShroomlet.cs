using UnityEngine;

// summoned by the Ghost Fungus. it holds at a fixed point and only pursues enemies that are
// within the FUNGUS' attack range (a leash). when no such enemy exists it walks back to its hold
// point. deals ice physical damage, inherits the fungus' stats, and persists (the fungus respawns it).
// the hover/select range circle is handled generically by Minion (rangeCircle + owner)
public class GhostShroomlet : Minion
{
    private const float RegenPercent  = 0.05f;   // out of combat: regenerate 5% of max health
    private const float RegenInterval = 2f;      // ...per 2 seconds

    private GhostFungus _fungus;
    private Vector3 holdPoint;
    private bool holdSet;
    private float detectionRange = 2.5f;

    protected override bool ChasesTarget => true;   // pursues enemies it spots within the fungus' leash

    // core stats follow the fungus live, so they grow as it is upgraded
    public override void UpdateStats()
    {
        if (_fungus != null)
        {
            baseAttackDamage = _fungus.ShroomletAttackDamage;
            baseMaxHealth    = _fungus.ShroomletHealth;
            baseAttackSpeed  = _fungus.ShroomletAttackSpeed;
        }
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();
        RegenerateOutOfCombat();
    }

    // while it has no enemy to fight, slowly heal back up
    private void RegenerateOutOfCombat()
    {
        if (isDying || health >= maxHealth) return;
        if (target != null) return;   // in combat
        health = Mathf.Min(maxHealth, health + maxHealth * (RegenPercent / RegenInterval) * Time.deltaTime);
        UpdateHealthBar();
    }

    // called by the Ghost Fungus right after spawn
    public void Configure(GhostFungus fungus, Vector3 holdPoint, float attackDamage, float maxHp, float attackSpeed, float attackRange, float detectionRange, float moveSpeed)
    {
        owner               = fungus;            // Minion.owner: leash + range-circle visibility
        _fungus             = fungus;            // typed reference for live stat syncing
        this.holdPoint      = holdPoint;
        this.holdSet        = true;
        this.detectionRange = detectionRange;
        baseAttackDamage    = attackDamage;
        baseMaxHealth       = maxHp;
        baseAttackSpeed     = attackSpeed;
        baseAttackRange     = attackRange;
        baseMovementSpeed   = moveSpeed;        // without this it can't actually chase or return
        attackDamageType    = DamageType.Physical;
        attackElementalType = ElementalType.Ice;
        lifetime            = Mathf.Infinity;   // persists; the fungus handles respawns

        // set health now so IsAlive is true immediately (Start runs next frame); otherwise the
        // fungus sees the fresh shroomlet as "dead" for a frame and over-spawns into its slot
        UpdateStats();
        health = maxHealth;

        SetRangeCircle(detectionRange);          // sizes + hides the range circle (shown on hover/select)
    }

    // the nearest enemy this shroomlet can SEE (within its detection range) and is allowed to chase
    // (within the fungus' attack range, the leash). measuring detection from itself makes it react
    // proactively instead of waiting for an enemy to walk on top of it
    public override IAttackable target
    {
        get
        {
            Vector3 fungusPos = owner != null ? owner.transform.position : transform.position;
            float leash = owner != null ? owner.attackRange : Mathf.Infinity;
            // sticky: keep fighting one enemy it can see and is allowed to chase, ignoring passers-by
            return StickyEnemy(e =>
                Vector3.Distance(transform.position, e.transform.position) <= detectionRange &&
                Vector3.Distance(fungusPos, e.transform.position) <= leash);
        }
    }

    // no enemy in range: walk back to the hold point
    protected override void FriendlyIdle()
    {
        if (!holdSet) return;
        if (Vector3.Distance(transform.position, holdPoint) <= 0.1f) return;
        Vector3 dir = (holdPoint - transform.position).normalized;
        transform.position += dir * GetMoveSpeed() * Time.deltaTime;
    }

    public override string GetName() => "<color=#B0E0E6>Ghost Shroomlet</color>";
}
