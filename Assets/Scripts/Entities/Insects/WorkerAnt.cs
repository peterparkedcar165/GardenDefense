using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3;
        baseMaxHealth = 120f;
        sunDrop = 10;
        base.Awake();
    }

    public override string GetName() => "<b><color=#8B4513>Worker Ant</color></b>";

    public override string GetDescription() => $"The {GetName()} is trivial. He does nothing but eat.";

    public override string GetPassiveDescription() => $"The {GetName()} consumes food 1.5x faster than normal.";
}
