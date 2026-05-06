using UnityEngine;

public class ScoutAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3f;
        baseMaxHealth = 120f;
        sunDrop = 14;
        base.Awake();
        baseMovementSpeed = 1.8f;
    }

    }
