using UnityEngine;

public class ScoutAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3f;
        baseMaxHealth = 80f;
        sunDrop = 12;
        base.Awake();
        baseMovementSpeed = 2f;
    }

    }
