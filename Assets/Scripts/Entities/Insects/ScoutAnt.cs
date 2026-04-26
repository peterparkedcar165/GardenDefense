using UnityEngine;

public class ScoutAnt : Ant
{

    protected override void Awake() {
        baseAttackDamage = 3f;
        baseMaxHealth = 20f;
        sunDrop = 18;
        base.Awake();
        baseMovementSpeed = 2f;
    }

    }
