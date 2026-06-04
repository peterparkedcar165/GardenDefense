using UnityEngine;

// the Ghost Fungus skill's hypnosis. it is a permanent HypnotizedEffect (turns the insect friendly
// forever) plus: bonus health and attack damage, and its melee attacks become ice physical, are
// credited to the plant, and slow the victim's attack speed
public class FungalHypnosisEffect : HypnotizedEffect
{
    private readonly Plant plant;
    private readonly float healthMultiplier, attackMultiplier, slowPercent, slowDuration;

    public FungalHypnosisEffect(Entity target, Plant plant, float healthMultiplier, float attackMultiplier, float slowPercent, float slowDuration)
        : base(target, plant)
    {
        this.plant            = plant;
        this.healthMultiplier = healthMultiplier;
        this.attackMultiplier = attackMultiplier;
        this.slowPercent      = slowPercent;
        this.slowDuration     = slowDuration;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Fungal Hypnosis", new Color(0.7f, 0.4f, 1f));

        Insect insect = target as Insect;
        if (insect == null) return;

        insect.maxHealthMultiplier   += healthMultiplier;
        insect.attackDamageMultiplier += attackMultiplier;
        insect.UpdateStats();
        insect.health = insect.maxHealth;   // top up to the boosted maximum

        insect.attackDamageType    = DamageType.Physical;
        insect.attackElementalType = ElementalType.Ice;
        insect.attackSourceOverride = plant;
        insect.attackSlowPercent    = slowPercent;
        insect.attackSlowDuration   = slowDuration;
    }

    // only runs if the marker is explicitly removed (it never expires on its own); undo the boosts,
    // then let the base revert the team back to enemy
    public override void OnExpire()
    {
        Insect insect = target as Insect;
        if (insect != null)
        {
            insect.maxHealthMultiplier    -= healthMultiplier;
            insect.attackDamageMultiplier -= attackMultiplier;
            insect.attackSourceOverride = null;
            insect.attackSlowPercent    = 0f;
            insect.attackSlowDuration   = 0f;
        }
        base.OnExpire();
    }

    public override string GetName() => "<color=#B266FF>Fungal Hypnosis</color>";
    public override string GetDescription() => "Turned against its own with ghostly ice; its strikes chill enemies, slowing their attacks.";
}
