using UnityEngine;

public class GerminateEffect : StatusEffect
{

    private float aoeRadius = 1.5f;
    private float cachedAttackDamage;
    private float cachedElementalPower;
    public GerminateEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#32CD32>Germinate</color>";

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

    DamageTag[] tags = new DamageTag[] { DamageTag.AoE, DamageTag.ElementalDebuff };
    public override void OnExpire()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(target.transform.position, aoeRadius);
        float damage = (cachedAttackDamage * 1.25f) + (20 * cachedElementalPower);
        
        foreach (Collider2D hit in hits)
        {
            Insect insect = hit.GetComponent<Insect>();
            if (insect != null)
            {
                if (source != null){ 
                insect.Damage(damage, DamageType.Physical, ElementalType.Nature, source, false, tags);
                }
                else
                {
                insect.Damage(damage, DamageType.Physical, ElementalType.Nature, tags);
                }
            }
        }
    }
}
