using UnityEngine;

public class BurnEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public BurnEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        tickInterval = 0.25f;
    }

    public override string GetName() => "<color=#FF4500>Burn</color>";

    public override void OnApply()
    {
        base.OnApply();
        damagePerSecond = (0.06f*target.maxHealth) + (0.08f*source.attackDamage) + 4f;
        Debug.Log($"Burn applied by {source} to {target}");

        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Burn", new Color(1f, 0.4f, 0f));
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            if (source != null)
                target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Fire, source, false, tickTags);
            else
                target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Fire, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        Debug.Log("Burn expired");
    }
}
