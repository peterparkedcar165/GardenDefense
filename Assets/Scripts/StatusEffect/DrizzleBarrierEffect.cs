public class DrizzleBarrierEffect : ShieldEffect
{
    private const float Cap = 120f;
    private const float BaseDuration = 16f;

    public DrizzleBarrierEffect(Entity target, AloeVera source)
        : base(target, BaseDuration, 1, source, 0f) { }

    public void AddShield(float overflow)
    {
        float added = Mathf.Min(overflow, Cap - amount);
        if (added <= 0f) return;
        amount += added;
        duration = BaseDuration;
        ShieldIndicator.Spawn(target.transform.position, added);
    }

    public override string GetName() => "<color=#4FC3F7><b>Drizzle Barrier</b></color>";
    public override string GetDescription() => $"Shielded for <color=grey><b>{amount:F0}</b></color> damage.";
}
