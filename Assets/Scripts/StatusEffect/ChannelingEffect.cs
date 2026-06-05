using UnityEngine;

// applied to a plant while it is casting/channeling a skill. for the duration the plant cannot
// perform its normal auto-attack (the skill drives its own effect instead)
public class ChannelingEffect : StatusEffect
{
    public ChannelingEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.neutral;
    }

    public override string GetName() => "<color=#C8A2C8>Channeling</color>";
    public override string GetDescription() => "Casting a spell; cannot attack.";

    public override void OnTick(float deltaTime) { }
}
