using UnityEngine;

public class BrittleEffect : StatusEffect
{
    public float bonusDamage;

    public BrittleEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        bonusDamage = 3f * (1f + source.elementalPower);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=green>Brittle</color>";
    public override string GetDescription() => $"Inflict <color=green><b>{bonusDamage:F0}</b></color> additional damage upon being hurt. Reduce Shield Toughness by <color=green>25%</color>. (3 × (1 + <color=#FFD700>{source.elementalPower * 100:F0}% EP</color>))";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Brittle", Color.green);
        target.shieldToughnessAdder -= 0.25f;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.shieldToughnessAdder += 0.25f;
    }
}
