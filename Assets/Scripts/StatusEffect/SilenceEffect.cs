// prevents a plant from casting its Skill while active.
// note: a plant under any HardCrowdControl is also treated as silenced (see Plant.IsSilenced)
public class SilenceEffect : StatusEffect
{
    public SilenceEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply() { }
    public override void OnExpire() { }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#B266FF>Silenced</color>";
    public override string GetDescription() => "Unable to cast Skill.";
}
