using UnityEngine;

public class FireAnt : Ant
{
    public float tempIncreasePerHit = 4f;

    private FireAntData FAData => data as FireAntData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity   = Aggressivity.Medium;
        targetingRange = 1.5f;
        if (FAData != null)
            tempIncreasePerHit = FAData.tempIncreasePerHit;
    }

    public override void Attack()
    {
        IAttackable current = target;
        if (current == null) return;
        current.ReceiveAttack(attackDamage, this);
        Plant plant = current as Plant;
        if (plant != null && plant.IsAlive)
            plant.temperature = Mathf.Min(plant.temperature + tempIncreasePerHit, plant.temperatureMax);
    }
}
