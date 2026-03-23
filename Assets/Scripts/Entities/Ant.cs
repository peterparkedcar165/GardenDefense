using UnityEngine;

public abstract class Ant : Insect
{

    protected override void Awake()
    {
        base.Awake();
        movementSpeed = 1f;
    }

    
}
