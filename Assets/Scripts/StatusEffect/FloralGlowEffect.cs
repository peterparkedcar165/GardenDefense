public class FloralGlowEffect : StatusEffect
{
    private readonly Calendula calendula;
    private float HealingPerSecond => 8f + (level - 1) + (calendula?.skillHealingMultiplier ?? 0f) * (calendula?.magicPower ?? 0f);
    private float healTickTimer = 0f;
    private float cachedLightRange;

    public FloralGlowEffect(Entity target, float duration, int level, Entity source, Calendula calendula)
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
        plant.Heal(HealingPerSecond * 0.5f, calendula);
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
        float hitDamage = calendula.attackDamage + calendula.skillDamageMultiplier * calendula.magicPower;
        insect.Damage(hitDamage, DamageType.Magic, ElementalType.Fire, calendula, false,
            new DamageTag[] { DamageTag.SkillDamage, DamageTag.Coordinated });
    }

    private float CoordinatedDamage => (calendula?.attackDamage ?? 0f) + (calendula?.skillDamageMultiplier ?? 0f) * (calendula?.magicPower ?? 0f);

    public override string GetName() => "<color=orange>Floral Glow</color>";
    public override string GetDescription() => $"Regenerates <color=green><b>{HealingPerSecond:F0}</b></color> health per second. Projectile attacks inflict a Coordinated <color=green><b>{CoordinatedDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage from the Calendula.";
}
