public class GeyseredEffect : StatusEffect
{
    public GeyseredEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Water;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new UnityEngine.Vector3(0.4f, 0f, 0f), "Geysered", new UnityEngine.Color(0.3f, 0.7f, 1f));
        target.armorAdder -= 30f;
        target.fallDamageResistanceAdder -= 0.33f;
    }

    public override void OnExpire()
    {
        target.armorAdder += 30f;
        target.fallDamageResistanceAdder += 0.33f;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4FC3F7>Geysered</color>";
    public override string GetDescription() => "<color=#00CED1><b>Armor</b></color> reduced by <color=red><b>30</b></color>. <color=#A0522D><b>Fall Damage Resistance</b></color> reduced by <color=red><b>33%</b></color>.";
}
