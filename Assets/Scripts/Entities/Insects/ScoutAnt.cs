using UnityEngine;

public class ScoutAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 2f;
        baseMaxHealth = 200f;
        sunDrop = 14;
        base.Awake();
        baseMovementSpeed = 1.8f;
    }

    public override string GetName() => "<b><color=#8B4513>Scout Ant</color></b>";

    public override string GetDescription() => $"The {GetName()} is quick and evasive, hard to pin down.";

    public override string GetPassiveDescription() => "Moves faster than other ants.";
}
