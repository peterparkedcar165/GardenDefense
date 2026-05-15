using UnityEngine;

public abstract class Ant : Insect
{

    protected override void Awake()
    {
        base.Awake();
        baseMovementSpeed = 1f;
        baseAttackSpeed = 1f;
        baseAttackRange = 0.5f;
    }

    
}
