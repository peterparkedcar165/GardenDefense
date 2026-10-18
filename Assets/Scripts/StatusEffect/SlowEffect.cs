using UnityEngine;

public class SlowEffect : StatusEffect
{

    public const float baseSlowness = 0.14f;
    public const float slownessPerLevel = 0.07f;

    // 14% at level 1, +7% per level after that
    public float slowness;
    public SlowEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Water;
        slowness = baseSlowness + slownessPerLevel * (level - 1) + MulchManager.GlobalSlowBonus;
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
