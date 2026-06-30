using UnityEngine;

public class DecayEffect : StatusEffect
{
    public DecayEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
    }

    public override string GetName() => "<color=purple>Decay</color>";
    public override string GetDescription() => "Healing received is converted into <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Decay", new Color(0.6f, 0.1f, 0.8f));
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
