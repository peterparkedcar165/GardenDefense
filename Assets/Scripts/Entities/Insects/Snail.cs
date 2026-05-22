using UnityEngine;

public class Snail : Insect
{
    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void OnShieldBreak(ShieldEffect shield)
    {
        if (shield is StartingShieldEffect)
            baseMovementSpeed += 0.2f;
    }
}
