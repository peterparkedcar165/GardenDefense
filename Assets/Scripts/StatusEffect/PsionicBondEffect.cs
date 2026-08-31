// applied to the Shooter a Carrot is currently bonded to. no stat changes on its own - just lets
// the player see which plant is linked - except at Carrot's Path2 max, where it also grants this
// plant (and, separately, Carrot itself, handled in Carrot.UpdateStats) +15% Attack Speed and
// +15% Passive Cooldown Reduction. checked every tick rather than only on apply, since Carrot
// can reach Path2 max while already bonded and the bonus needs to switch on right then, not
// only the next time the bond is re-formed. removed by Carrot itself the instant the bond
// breaks (toggled off, or the bond resolves onto a different plant).
//
// source-stackable: several Carrots can all bond to the same Shooter (or to each other) at
// once, each carrying its own instance here rather than the newest bond overwriting the last -
// Carrot removes only its own instance via Entity.RemoveEffect<T>(source), never the others'
public class PsionicBondEffect : StatusEffect
{
    private readonly Carrot carrot;
    private bool _bonusActive;
    private const float BondBonus = 0.15f;

    public PsionicBondEffect(Entity target, Entity source, Carrot carrot)
        : base(target, float.MaxValue, 1, source)
    {
        this.carrot = carrot;
        effectType = Type.positive;
        elementalType = ElementalType.Ground;
        sourceStackable = true;
    }

    public override void OnApply() { }

    public override void OnTick(float deltaTime)
    {
        bool shouldBeActive = carrot != null && carrot.IsAlive && carrot.IsPath2Maxed;
        if (shouldBeActive == _bonusActive) return;

        _bonusActive = shouldBeActive;
        ApplyBonus(_bonusActive ? BondBonus : -BondBonus);
    }

    public override void OnExpire()
    {
        if (!_bonusActive) return;
        ApplyBonus(-BondBonus);
        _bonusActive = false;
    }

    private void ApplyBonus(float delta)
    {
        target.attackSpeedMultiplier += delta;
        if (target is Plant plant) plant.passiveCooldownReductionMultiplier += delta;
    }

    public override string GetName() => "<color=#B266FF><b>Psionic Bond</b></color>";
    public override string GetDescription()
    {
        string s = $"Linked to {(carrot != null ? carrot.GetName() : "a Carrot")}: every shot also triggers a Psionic Carrot.";
        if (_bonusActive)
            s += "\nGains <color=green><b>+15%</b></color> Attack Speed and <color=green><b>+15%</b></color> Passive Cooldown Reduction.";
        return s;
    }
}
