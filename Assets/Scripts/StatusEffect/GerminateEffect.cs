using UnityEngine;

public class GerminateEffect : StatusEffect
{

    private float aoeRadius = 1.5f;
    public float delay = 1f;
    private float cachedAttackDamage;
    private float cachedElementalPower;
    public GerminateEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#32CD32>Germinate</color>";
    public override string GetDescription() => $"Detonates <color=green>{delay}</color> second after acquiring. Dealing (<color=green>40</color> + <color=green>33%</color> Attack Damage) + (<color=green>2400%</color> Elemental Power) to nearby insects.";

    public override void OnApply()
    {

        cachedAttackDamage = source.attackDamage;
        cachedElementalPower = source.elementalPower;

        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Germinate", new Color(0.3f, 1f, 0.2f));
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.AoE, DamageTag.ElementalDebuff };
    public override void OnExpire()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Bloom", new Color(0.3f, 1f, 0.2f));
        float damage = 40 + (cachedAttackDamage * 0.33f) + (24 * cachedElementalPower);

        foreach (Insect insect in Insect.allInsects)
        {
            if (Vector3.Distance(target.transform.position, insect.transform.position) <= aoeRadius)
            {
                if (source != null)
                    insect.Damage(damage, DamageType.Physical, ElementalType.Nature, source, false, damageTags);
                else
                    insect.Damage(damage, DamageType.Physical, ElementalType.Nature, damageTags);
            }
        }
    }
}
