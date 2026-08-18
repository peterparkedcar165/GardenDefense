public class GeyseredEffect : StatusEffect
{
    private readonly float armorShred;
    private readonly float fallDamageResistanceShred;

    public GeyseredEffect(Entity target, float duration, int level, Entity source, float armorShred, float fallDamageResistanceShred)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Water;
        this.armorShred = armorShred;
        this.fallDamageResistanceShred = fallDamageResistanceShred;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new UnityEngine.Vector3(0.4f, 0f, 0f), "Geysered", new UnityEngine.Color(0.3f, 0.7f, 1f));
        target.armorAdder -= armorShred;
        target.fallDamageResistanceAdder -= fallDamageResistanceShred;
    }

    public override void OnExpire()
    {
        target.armorAdder += armorShred;
        target.fallDamageResistanceAdder += fallDamageResistanceShred;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4FC3F7>Geysered</color>";
    public override string GetDescription() => $"<color=#00CED1><b>Armor</b></color> reduced by <color=red><b>{armorShred:F0}</b></color>. <color=#A0522D><b>Fall Damage Resistance</b></color> reduced by <color=red><b>{fallDamageResistanceShred * 100f:F0}%</b></color>.";
}
