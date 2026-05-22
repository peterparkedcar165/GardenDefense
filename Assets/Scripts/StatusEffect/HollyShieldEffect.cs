public class HollyShieldEffect : ShieldEffect
{
    public HollyShieldEffect(Entity target, float duration, int level, Entity source, float amount)
        : base(target, duration, level, source, amount) { }

    public override string GetName() => "<color=#00FFFF>Holly Shield</color>";
}
