// applied by an Oleander Sprout's aura to insects within its radius while it stands.
// source-stackable: sprouts from different Nerium Oleanders each contribute their own reduction
public class OleandicCurseEffect : StatusEffect
{
    private readonly float reduction;

    public OleandicCurseEffect(Entity target, float duration, int level, Entity source, float reduction)
        : base(target, duration, level, source)
    {
        this.reduction = reduction;
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
        sourceStackable = true;
    }

    public override void OnApply()  => target.poisonResistanceAdder -= reduction;
    public override void OnExpire() => target.poisonResistanceAdder += reduction;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#9B59B6>Oleandic Curse</color>";
    public override string GetDescription() =>
        $"Reduce <color=purple><b>Poison Resistance</b></color> by <color=red><b>{reduction * 100f:F0}%</b></color>.";
}
