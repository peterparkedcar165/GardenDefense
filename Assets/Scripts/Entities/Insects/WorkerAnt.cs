using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 20;
        baseMaxHealth = 220f;
        sunDrop = 6;
        base.Awake();
    }

    public override float eatMultiplier => 1.5f;

    public override string GetName() => "<b><color=#8B4513>Worker Ant</color></b>";

    public override string GetDescription() => $"The {GetName()} is trivial. He does nothing but eat.";

    public override string GetPassiveDescription() => $"The {GetName()} consumes food 1.5x faster than normal.";
}
