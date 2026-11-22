// Plant.mulchPrimerCooldownReduction: Entity.Damage takes it off an insect's element cooldown
// whenever this plant is the one landing the primer (MulchManager.PrimerCooldown)
public class PrimerCooldownMulchEffect : MulchEffect
{
    private readonly PrimerCooldownMulchData typedData;
    private float applied;
    // primers it shortened this level and the seconds it cut (MulchManager.PrimerCooldown counts them)
    private int primers;
    private float cut;

    public float Applied => applied;

    public PrimerCooldownMulchEffect(PrimerCooldownMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.cooldownReduction);
        Owner.mulchPrimerCooldownReduction += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.mulchPrimerCooldownReduction -= applied;
        applied = 0f;
    }

    public void CountPrimer(float seconds)
    {
        primers++;
        cut += seconds;
    }

    public override string GetStatus() => $"Primer cooldowns cut: {primers} ({cut:0.#}s in all)";
}
