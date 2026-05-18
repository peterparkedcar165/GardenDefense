public class Mosquito : FlyingInsect
{
    protected override void Awake()
    {
        base.Awake();
        baseMaxHealth      = 300f;
        baseAttackDamage   = 12f;
        baseAttackSpeed    = 1.5f;
        baseAttackRange    = 0.5f;
        baseMovementSpeed  = 1.4f;
        baseLifesteal      = 0.35f;
        sunDrop            = 7;
        baseFireResistance = -0.3f;
        aggressivity       = Aggressivity.High;
        targetingRange     = 2.5f;
    }

    public override string GetName() => "<b><color=#8B0000>Mosquito</color></b>";

    public override string GetDescription()
        => $"The {GetName()} relentlessly hunts down plants, latching on and draining their life force to sustain itself.";

    public override string GetPassiveDescription()
        => $"Aggressively targets the nearest plant. Heals for <color=green><b>{baseLifesteal * 100f:F0}%</b></color> of all damage dealt. Vulnerable to <color=orange>Fire</color>.";
}
