// Bog Iris's Bogged Barrier node: topped up whenever a regen tick (1s, see BogIris.UpdatePassive)
// overheals while closed. Duration is slightly longer than the regen tick interval so timing
// jitter can't let it lapse between refreshes
public class BoggedBarrierEffect : ShieldEffect
{
    private const float ResistanceBonus = 0.35f;

    public BoggedBarrierEffect(Entity target, Entity source, float amount)
        : base(target, 1.5f, 1, source, amount) { }

    public override string GetName() => "<color=#4FC3F7>Bogged Barrier</color>";

    public override void OnApply()
    {
        base.OnApply();
        target.fireResistanceAdder += ResistanceBonus;
        target.waterResistanceAdder += ResistanceBonus;
    }

    public override void OnExpire()
    {
        target.fireResistanceAdder -= ResistanceBonus;
        target.waterResistanceAdder -= ResistanceBonus;
        base.OnExpire();
    }
}
