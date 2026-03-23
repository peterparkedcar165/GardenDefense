using UnityEngine;

public class AcornSprout : Shooter
{
    protected override void Awake()
    {
        attackDamage = 2f;
        attackSpeed = 0.5f;
        attackRange = 5f;
    }

    protected override void Update()
    {
        base.Update();
    }
}
