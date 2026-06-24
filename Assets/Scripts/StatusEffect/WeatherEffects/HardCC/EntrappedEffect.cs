public class EntrappedEffect : HardCrowdControl
{
    public EntrappedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        tags = new EffectTag[] { EffectTag.CrowdControl };
    }

    public override string GetName() => "<color=#9B59B6>Entrapped</color>";
    public override string GetDescription() => "Rooted in place by toxic thorns.";

    public override void OnApply() { }
    public override void OnExpire() { }
}
