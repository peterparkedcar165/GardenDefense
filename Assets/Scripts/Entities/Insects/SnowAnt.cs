using UnityEngine;

public class SnowAnt : Ant
{
    public float tempDecreasePerHit = 4f;

    private SnowAntData SAData => data as SnowAntData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity = Aggressivity.Medium;
        if (SAData != null)
            tempDecreasePerHit = SAData.tempDecreasePerHit;
    }

    public override void Attack()
    {
        IAttackable current = target;
        if (current == null) return;
        current.ReceiveAttack(attackDamage, this);
        Plant plant = current as Plant;
        if (plant != null && plant.IsAlive)
            plant.temperature = Mathf.Max(plant.temperature - tempDecreasePerHit, plant.temperatureMin);
    }
}
