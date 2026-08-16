using UnityEngine;

// water elemental effect proc: reduces movement and attack speed, scaled by the source's elemental affinity
public class SoakedEffect : StatusEffect, IElementalAffinityEffect
{
    private float reduction;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    public SoakedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        reduction = 0.2f * (1f + source.elementalAffinity);
        effectType = Type.negative;
        elementalType = ElementalType.Water;
    }

    public override string GetName() => "<color=#4FC3F7>Soaked</color>";
    public override string GetDescription() =>
        $"Reduce <color=green><b>Movement Speed</b></color> and <color=green><b>Attack Speed</b></color> by <color=green><b>{reduction * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Soaked", new Color(0.31f, 0.76f, 0.97f));

        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= reduction;
        insect.attackSpeedMultiplier -= reduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += reduction;
        insect.attackSpeedMultiplier += reduction;
    }
}
