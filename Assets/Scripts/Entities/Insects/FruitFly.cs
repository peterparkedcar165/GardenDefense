using UnityEngine;

public class FruitFly : FlyingInsect
{

    protected override void Awake() {
        baseAttackDamage = 10;
        baseMaxHealth = 100f;
        baseMovementSpeed = 1.4f;
        sunDrop = 4;
        base.Awake();
    }

    public override string GetName() => "<b><color=#8B4513>Fruit Fly</color></b>";

    public override string GetDescription() => $"The {GetName()} is frail but flies at high velocity.";

    public override string GetPassiveDescription() => $"He's fast.";
}
