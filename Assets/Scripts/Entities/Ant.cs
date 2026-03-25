using UnityEngine;

public abstract class Ant : Insect
{

    protected override void Awake()
    {
        base.Awake();
        baseMovementSpeed = 1f;
    }

    
}
