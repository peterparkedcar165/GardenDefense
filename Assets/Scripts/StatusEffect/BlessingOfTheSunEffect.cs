using UnityEngine;

// the Aeonium's skill buff: boosts the plant's sun yield (both its own generation
// and the sun its kills drop) for the duration
public class BlessingOfTheSunEffect : StatusEffect
{
    private readonly float bonus;

    public BlessingOfTheSunEffect(Entity target, float duration, int level, Entity source, float bonus)
        : base(target, duration, level, source)
    {
        this.bonus = bonus;
        effectType      = Type.positive;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Blessing of the Sun", new Color(0f, 0.5f, 0f));
        target.sunYieldBonus += bonus;
    }

    public override void OnExpire() => target.sunYieldBonus -= bonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=green>Blessing of the Sun</color>";
    public override string GetDescription() =>
        $"Sun yield increased by <color=green><b>{bonus * 100f:F0}%</b></color>.";
}
