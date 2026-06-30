using UnityEngine;

public class FloralGlowEffect : StatusEffect
{
    private readonly Calendula calendula;
    private float HealingPerSecond => calendula?.FloralGlowHealPerSecond ?? 0f;
    private float healTickTimer = 0f;
    private float cachedLightRange;

    public FloralGlowEffect(Entity target, float duration, int level, Entity source, Calendula calendula)
        : base(target, duration, level, source)
    {
        this.calendula  = calendula;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    private float BestRangeExcluding(FloralGlowEffect exclude)
    {
        float best = 0f;
        foreach (var e in target.activeEffects)
            if (e is FloralGlowEffect fg && fg != exclude && fg.cachedLightRange > best)
                best = fg.cachedLightRange;
        return best;
    }

    public override void OnApply()
    {
        Plant plant = target as Plant;
        if (plant == null) return;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Floral Glow", new Color(1f, 0.6f, 0f));

        float prevBest = BestRangeExcluding(this);
        cachedLightRange = calendula?.baseLightEmissionRange ?? 0f;
        float delta = Mathf.Max(prevBest, cachedLightRange) - prevBest;
        if (delta > 0f)
        {
            plant.lightEmissionRangeAdder += delta;
            plant.UpdateStats();
        }

        Entity.OnPlantAttackHit += HandlePlantAttackHit;
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

        float currentBest = Mathf.Max(BestRangeExcluding(this), cachedLightRange);
        float nextBest = BestRangeExcluding(this);
        float delta = currentBest - nextBest;
        if (delta > 0f)
        {
            plant.lightEmissionRangeAdder -= delta;
            plant.UpdateStats();
        }

        Entity.OnPlantAttackHit -= HandlePlantAttackHit;
    }

    private void HandlePlantAttackHit(Plant plant, Insect insect, DamageTag[] tags)
    {
        if (plant != target) return;
        if (!System.Array.Exists(tags, t => t == DamageTag.Attack)) return;
        if (!System.Array.Exists(tags, t => t == DamageTag.Melee || t == DamageTag.Projectile)) return;
        if (calendula == null || insect == null || !insect.IsAlive) return;
        calendula.StartCoroutine(DelayedFireHit(insect));
    }

    private System.Collections.IEnumerator DelayedFireHit(Insect insect)
    {
        yield return new UnityEngine.WaitForSeconds(0.1f);
        if (calendula == null || insect == null || !insect.IsAlive) yield break;
        float hitDamage = calendula.attackDamage * 0.35f + calendula.skillDamageMultiplier * calendula.magicPower;
        insect.Damage(hitDamage, DamageType.Magic, ElementalType.Fire, calendula, false,
            new DamageTag[] { DamageTag.SkillDamage, DamageTag.Coordinated });
    }

    private float CoordinatedDamage =>
        ((calendula?.attackDamage ?? 0f) * 0.35f + (calendula?.skillDamageMultiplier ?? 0f) * (calendula?.magicPower ?? 0f))
        * (1f + (calendula?.coordinatedDamage ?? 0f));

    public override string GetName() => "<color=orange>Floral Glow</color>";
    public override string GetDescription() => $"Regenerates <color=green><b>{HealingPerSecond:F0}</b></color> health per second. Attacks inflict a <color=orange><b>Coordinated</b></color> <color=green><b>{CoordinatedDamage:F0}</b></color> <color=orange><b>Fire</b></color> <color=#FFB6C1><b>Magic</b></color> damage hit from the <color=orange><b>Calendula</b></color>.";
}
