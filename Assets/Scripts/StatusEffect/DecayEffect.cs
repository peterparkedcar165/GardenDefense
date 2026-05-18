using UnityEngine;

public class DecayEffect : StatusEffect
{
    public float attackSpeedReduction = 0.33f;
    public float attackDamageReduction = 0.25f;

    public DecayEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#9400D3>Decay</color>";
    public override string GetDescription() => $"Reduces Attack Speed by <color=green>33%</color> and Attack Damage by <color=green>25%</color>.";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Decay", new Color(0.6f, 0.1f, 0.8f));

        target.attackSpeedMultiplier -= attackSpeedReduction;
        target.attackDamageMultiplier -= attackDamageReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier += attackSpeedReduction;
        target.attackDamageMultiplier += attackDamageReduction;
    }
}
