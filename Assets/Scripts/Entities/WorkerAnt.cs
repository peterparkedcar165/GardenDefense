using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3;
        baseMaxHealth = 20f;
        sunDrop = 25;
        base.Awake();
    }
}
