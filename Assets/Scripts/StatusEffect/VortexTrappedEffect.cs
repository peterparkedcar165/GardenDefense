public class VortexTrappedEffect : SlowEffect
{
    public VortexTrappedEffect(Entity target, float duration, Entity source)
        : base(target, duration, 1, source)
    {
        slowness = 1f;
    }

    public override string GetName() => "<color=#E0E0E0>Vortex Trapped</color>";
    public override string GetDescription() => "Trapped inside a vortex. Movement Speed reduced by <color=red><b>100%</b></color>.";
}
