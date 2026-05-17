public class FieryInfusionEffect : StatusEffect
{
    private readonly Calendula calendula;
    private float HealingPerSecond => 8f + 1f * (level - 1);
    private float healTickTimer = 0f;
    private float cachedLightRange;

    public FieryInfusionEffect(Entity target, float duration, int level, Entity source, Calendula calendula)
        : base(target, duration, level, source)
    {
        this.calendula = calendula;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        Plant plant = target as Plant;
        if (plant == null) return;
        cachedLightRange = calendula?.lightEmissionRange ?? 0f;
        plant.lightEmissionRangeAdder += cachedLightRange;
    }

    public override void OnTick(float deltaTime)
    {
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;
        healTickTimer += deltaTime;
        if (healTickTimer < 0.5f) return;
        healTickTimer -= 0.5f;
        float heal = HealingPerSecond * 0.5f * (1f + (calendula?.healingBonus ?? 0f));
        plant.Heal(heal);
    }

    public override void OnExpire()
    {
        Plant plant = target as Plant;
        if (plant == null) return;
        plant.lightEmissionRangeAdder -= cachedLightRange;
        plant.UpdateStats();
    }

    public void OnProjectileHit(Insect insect)
    {
        if (calendula == null || insect == null) return;
        calendula.StartCoroutine(DelayedFireHit(insect));
    }

    private System.Collections.IEnumerator DelayedFireHit(Insect insect)
    {
        yield return new UnityEngine.WaitForSeconds(0.1f);
        if (calendula == null || insect == null || !insect.IsAlive) yield break;
        insect.Damage(calendula.attackDamage, DamageType.Magic, ElementalType.Fire, calendula, false,
            new DamageTag[] { DamageTag.SkillDamage, DamageTag.Coordinated });
    }

    public override string GetName() => "<color=orange>Fiery Infusion</color>";
    public override string GetDescription() => $"Healing <color=green><b>{HealingPerSecond:F0}</b></color> health per second. Projectile attacks deal bonus fire magic damage.";
}
