using UnityEngine;

// the Ghost Fungus skill's hypnosis. it is a permanent HypnotizedEffect (turns the insect friendly
// forever) plus: bonus health and attack damage, reduced movement speed, and its melee attacks
// become ice physical credited to the plant
public class FungalHypnosisEffect : HypnotizedEffect
{
    private readonly Plant plant;
    private readonly float healthMultiplier, attackMultiplier, moveSlow;
    private readonly bool _reversesAtSpawn;

    public FungalHypnosisEffect(Entity target, Plant plant, float healthMultiplier, float attackMultiplier, float moveSlow)
        : base(target, plant)
    {
        this.plant            = plant;
        this.healthMultiplier = healthMultiplier;
        this.attackMultiplier = attackMultiplier;
        this.moveSlow         = moveSlow;
        _reversesAtSpawn      = plant is GhostFungus gf && gf.IsPath3Maxed;
        elementalType         = ElementalType.Ice;
    }

    protected override string IndicatorLabel => "Fungal Hypnosis";
    protected override Color  IndicatorColor => new Color(0f, 1f, 1f);   // ice color

    public override void OnApply()
    {
        base.OnApply();   // turns the insect (ApplyHypnosis) and spawns the "Fungal Hypnosis" indicator

        Insect insect = target as Insect;
        if (insect == null) return;

        insect.maxHealthMultiplier     += healthMultiplier;
        insect.attackDamageMultiplier  += attackMultiplier;
        insect.movementSpeedMultiplier -= moveSlow;   // slower, so it stays on the field longer
        insect.UpdateStats();
        insect.health = insect.maxHealth;   // top up to the boosted maximum

        insect.attackDamageType     = DamageType.Physical;
        insect.attackSourceOverride = plant;
        if (_reversesAtSpawn) insect.reversesAtSpawn = true;
    }

    // only runs if the marker is explicitly removed (it never expires on its own); undo the boosts,
    // then let the base revert the team back to enemy
    public override void OnExpire()
    {
        Insect insect = target as Insect;
        if (insect != null)
        {
            insect.maxHealthMultiplier     -= healthMultiplier;
            insect.attackDamageMultiplier  -= attackMultiplier;
            insect.movementSpeedMultiplier += moveSlow;
            insect.attackSourceOverride = null;
        }
        base.OnExpire();
    }

    public override HypnotizedEffect Clone(Entity newTarget) =>
        new FungalHypnosisEffect(newTarget, plant, healthMultiplier, attackMultiplier, moveSlow);

    public override string GetName() => "<color=#00FFFF>Fungal Hypnosis</color>";
    public override string GetDescription() => "Turned against its own with ghostly ice, fighting other insects.";
}
