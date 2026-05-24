using UnityEngine;

public class ChillEffect : StatusEffect
{
    private readonly float baseSlow;
    private readonly float slowPerLevel;

    private float TotalSlow => baseSlow + slowPerLevel * (level - 1);

    public ChillEffect(Entity target, float duration, int level, Entity source,
                       float baseSlow = 0.24f, float slowPerLevel = 0.06f)
        : base(target, duration, level, source)
    {
        this.baseSlow     = baseSlow;
        this.slowPerLevel = slowPerLevel;
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#00FFFF>Chill</color>";
    public override string GetDescription() => $"Reduce Movement Speed by <b>{TotalSlow * 100f:F0}%</b>.";

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= TotalSlow;
        Debug.Log("Chill applied at level " + level);
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Debug.Log("Chill expired");
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += TotalSlow;
    }
}
