using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3;
        baseMaxHealth = 20f;
        sunDrop = 15;
        base.Awake();
    }
}
