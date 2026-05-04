using UnityEngine;

public class ChillEffect : StatusEffect
{

    public float slowness = 0.24f;
    public ChillEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= (slowness + (0.06f * level-1));

        Debug.Log("Chill applied at level " + level);
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Chill expired");
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += (slowness + (0.06f * level-1));
    }
}
