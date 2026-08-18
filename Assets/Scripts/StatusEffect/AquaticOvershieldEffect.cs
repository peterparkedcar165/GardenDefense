public class AquaticOvershieldEffect : ShieldEffect
{
    private const float cap = 100f;

    public AquaticOvershieldEffect(Entity target, Entity source)
        : base(target, float.MaxValue, 1, source, 0f)
    {
        elementalType = ElementalType.Water;
    }

    public void AddShield(float heal)
    {
        float added = UnityEngine.Mathf.Min(heal, cap - amount);
        if (added <= 0f) return;
        amount += added;
        ShieldIndicator.Spawn(target.transform.position, added);
    }

    protected override float ShieldCap => cap;

    public override string GetName() => "<color=#4FC3F7>Aquatic Overshield</color>";
}
