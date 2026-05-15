using UnityEngine;

public class Wasp : FlyingInsect
{
    protected override void Awake()
    {
        baseAttackDamage = 20f;
        baseMaxHealth = 150f;
        baseMovementSpeed = 1.2f;
        baseAttackSpeed = 1f;
        baseAttackRange = 0.5f;
        aggressivity = Aggressivity.High;
        targetingRange = 2f;
        sunDrop = 10;
        base.Awake();
    }

    public override string GetName() => "<b><color=#DAA520>Wasp</color></b>";
    public override string GetDescription() => $"The {GetName()} flies through the garden, targeting and attacking plants directly.";
    public override string GetPassiveDescription() => $"Targets the nearest plant within range.";
}
