using UnityEngine;

public class GerminateEffect : StatusEffect
{
    private float aoeRadius = 1.5f;
    public float delay = 1f;
    private float cachedAttackDamage;
    private float cachedElementalPower;

    public GerminateEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedAttackDamage = source?.attackDamage ?? 0f;
        cachedElementalPower = source?.elementalPower ?? 0f;
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#32CD32>Germinate</color>";
    public override string GetDescription()
    {
        float ad = cachedAttackDamage;
        float ep = cachedElementalPower;
        float total = (42f + ad * 0.33f) * (1f + 1.43f * ep);
        return $"Detonates in <color=green><b>{delay:F0}s</b></color>. Deals <color=green><b>{total:F0}</b></color> <color=green>Nature</color> Physical damage to nearby insects. (42 + <color=green>33% Attack Damage</color>) × (1 + <color=#FFD700>143% Elemental Power</color>)";
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Germinate", new Color(0.3f, 1f, 0.2f));
    }

    public override void OnTick(float deltaTime) { }

    readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.AoE, DamageTag.ElementalDebuff };
    public override void OnExpire()
    {
        if (target == null) return;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Bloom", new Color(0.3f, 1f, 0.2f));

        float damage = (42f + cachedAttackDamage * 0.33f) * (1f + 1.43f * cachedElementalPower);

        Vector3 origin = target.transform.position;
        foreach (Insect insect in new System.Collections.Generic.List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(origin, insect.transform.position) <= aoeRadius)
            {
                if (source != null)
                    insect.Damage(damage, DamageType.Physical, ElementalType.Nature, source, false, damageTags);
                else
                    insect.Damage(damage, DamageType.Physical, ElementalType.Nature, damageTags);
            }
        }
    }
}
