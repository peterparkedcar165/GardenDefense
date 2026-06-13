public class PuncturedEffect : StatusEffect
{
    public PuncturedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        target.armorAdder -= level;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.armorAdder += level;
    }

    public override string GetName() => "<color=#A0522D>Punctured</color>";
    public override string GetDescription() => $"<color=#00CED1><b>Armor</b></color> reduced by <color=red><b>{level}</b></color>.";
}
