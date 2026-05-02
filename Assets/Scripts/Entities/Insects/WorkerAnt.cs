using UnityEngine;

public class WorkerAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3;
        baseMaxHealth = 80f;
        sunDrop = 8;
        base.Awake();
    }
}
