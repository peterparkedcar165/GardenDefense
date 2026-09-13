using UnityEngine;

public class FractureEffect : StatusEffect
{
    private float critDamageReceivedBonus;

    public FractureEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        critDamageReceivedBonus = 0.25f * (1f + source.elementalAffinity);
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Fracture</color>";
    public override string GetDescription() =>
        $"Increases <color=#FFD700><b>Critical Damage</b></color> taken by <color=red><b>{critDamageReceivedBonus * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Fracture", new Color(0f, 1f, 1f));
        target.bonusCritDamageReceivedAdder += critDamageReceivedBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.bonusCritDamageReceivedAdder -= critDamageReceivedBonus;
    }
}
