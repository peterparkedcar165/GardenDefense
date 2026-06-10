public class PuncturedEffect : StatusEffect
{
    public PuncturedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        target.armorAdder -= 0.5f * level;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.armorAdder += 0.5f * level;
    }

    public override string GetName() => "<color=#A0522D>Punctured</color>";
    public override string GetDescription() => $"Armor reduced by <color=green><b>{level * 0.5f:F1}</b></color>.";
}
