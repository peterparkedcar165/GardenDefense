// Bog Iris's Guarded Bloom node: refreshed every regen tick (1s, see BogIris.UpdatePassive)
// while closed, so it's kept topped up for as long as she stays damaged. Duration is slightly
// longer than the regen tick interval so timing jitter can't let it lapse between refreshes
public class GuardedBloomEffect : ShieldEffect
{
    public GuardedBloomEffect(Entity target, Entity source, float amount)
        : base(target, 1.5f, 1, source, amount) { }

    public override string GetName() => "<color=#4FC3F7>Guarded Bloom</color>";
}
