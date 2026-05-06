using UnityEngine;

public class SlowEffect : StatusEffect
{

    public float slowness = 0.24f;
    public SlowEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#87CEEB>Slow</color>";

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= slowness * level;

        Debug.Log("Slow applied at level " + level);
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Slow expired");
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += slowness * level;
    }
}
