public class WindErosionEffect : StatusEffect
{
    private readonly float reductionPerStack;

    public WindErosionEffect(Entity target, float duration, int stacks, Entity source, float reductionPerStack)
        : base(target, duration, stacks, source)
    {
        this.reductionPerStack = reductionPerStack;
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
    }

    public override string GetName() => "<color=#B2EBF2>Wind Erosion</color>";

    public override string GetDescription() =>
        $"<color=#00CED1>Armor</color> and <color=#9370DB>Magic Resist</color> reduced by <color=red><b>{(int)(reductionPerStack * level)}</b></color>.";

    public override void OnApply()
    {
        target.armorAdder -= reductionPerStack * level;
        target.magicArmorAdder -= reductionPerStack * level;
    }

    public override void OnExpire()
    {
        target.armorAdder += reductionPerStack * level;
        target.magicArmorAdder += reductionPerStack * level;
    }

    public override void OnTick(float deltaTime) { }
}
