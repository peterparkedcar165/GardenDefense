using UnityEngine;

public class SlowEffect : StatusEffect
{

    private const float baseSlowness = 0.1f;
    private const float slownessPerLevel = 0.05f;

    // 10% at level 1, +5% per level after that
    public float slowness;
    public SlowEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        slowness = baseSlowness + slownessPerLevel * (level - 1);
    }

    public override string GetName() => "<color=#87CEEB>Slow</color>";
    public override string GetDescription() => $"Reduce <color=green><b>Movement Speed</b></color> by <color=green><b>{slowness*100:F0}%</b></color>.";

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= slowness;

        Debug.Log("Slow applied at level " + level);
    }

    public override void OnTick(float deltaTime)
    {

    }

    public override void OnExpire()
    {
        Debug.Log("Slow expired");
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += slowness;
    }
}
