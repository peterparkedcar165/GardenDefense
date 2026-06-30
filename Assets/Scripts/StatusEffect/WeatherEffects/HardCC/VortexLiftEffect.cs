public class VortexLiftEffect : Airborne
{
    public VortexLiftEffect(Entity target, float duration, Entity source)
        : base(target, duration, 1, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
    }

    public override string GetName() => "<color=#B2EBF2>Airborne</color>";
    public override string GetDescription() => "Lifted into the air by the vortex. Cannot move.";
    public override void OnApply() { }
    public override void OnExpire() { }
    public override void OnTick(float deltaTime) { }
}
