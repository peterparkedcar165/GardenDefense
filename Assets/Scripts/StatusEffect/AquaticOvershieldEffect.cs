public class AquaticOvershieldEffect : ShieldEffect
{
    private const float cap = 100f;

    public AquaticOvershieldEffect(Entity target, Entity source)
        : base(target, float.MaxValue, 1, source, 0f) { }

    public void AddShield(float heal)
    {
        float added = UnityEngine.Mathf.Min(heal, cap - amount);
        if (added <= 0f) return;
        amount += added;
        ShieldIndicator.Spawn(target.transform.position, added);
    }

    public override void OnAbsorbHit(Entity attacker, DamageType damageType)
    {
        if (damageType != DamageType.Physical || attacker == null || !(attacker is Insect)) return;
        attacker.ApplyEffect(new WetEffect(attacker, 3f, 1, target));
    }

    public override string GetName() => "<color=#4FC3F7>Aquatic Overshield</color>";
    public override string GetDescription() => $"Remaining Shield: [<color=grey><b>{amount:F0}/{cap}</b></color>]. Physical damage dealt to this shield primes the attacker with <color=#1E90FF>Wet</color>.";
}
