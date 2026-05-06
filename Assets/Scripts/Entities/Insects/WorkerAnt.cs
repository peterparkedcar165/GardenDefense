using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3;
        baseMaxHealth = 120f;
        sunDrop = 10;
        base.Awake();
    }
}
