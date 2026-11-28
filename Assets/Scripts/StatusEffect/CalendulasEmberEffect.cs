using UnityEngine;

public class CalendulasEmberEffect : PlantAuraBuffEffect
{
    private readonly float fireDamageBonus;

    public CalendulasEmberEffect(Entity target, int level, Plant source, float range, float fireDamageBonus, Transform centerOverride = null)
        : base(target, level, source, range, centerOverride)
    {
        this.fireDamageBonus = fireDamageBonus;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    public override void OnApply()  => target.fireDamageAdder += fireDamageBonus;
    public override void OnExpire() => target.fireDamageAdder -= fireDamageBonus;

    public override string GetName() => "<color=orange>Calendula's Ember</color>";
    public override string GetDescription() =>
        $"Increase <color=orange><b>Fire Damage</b></color> by <color=green><b>{fireDamageBonus * 100f:F0}%</b></color>.";
}
