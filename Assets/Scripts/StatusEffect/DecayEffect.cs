using UnityEngine;

public class DecayEffect : StatusEffect
{
    public float attackSpeedReduction;
    public float attackDamageReduction;

    public DecayEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        attackSpeedReduction  = 0.22f * (1f + source.elementalPower);
        attackDamageReduction = 0.15f * (1f + source.elementalPower);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#9400D3>Decay</color>";
    public override string GetDescription() => $"Reduce Attack Speed by <color=green><b>{attackSpeedReduction * 100f:F0}%</b></color> and Attack Damage by <color=green><b>{attackDamageReduction * 100f:F0}%</b></color>. (22% / 15% × (1 + <color=#FFD700>{source.elementalPower * 100:F0}% EP</color>))";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Decay", new Color(0.6f, 0.1f, 0.8f));

        target.attackSpeedMultiplier  -= attackSpeedReduction;
        target.attackDamageMultiplier -= attackDamageReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier  += attackSpeedReduction;
        target.attackDamageMultiplier += attackDamageReduction;
    }
}
