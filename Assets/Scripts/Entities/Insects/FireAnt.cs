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

    // reads straight off the data asset (not the instance field, which is only synced by
    // Awake()) so this is accurate both in-battle and from the loadout screen's uninstantiated
    // prefab preview
    public override string GetDescription() =>
        $"Mildy aggressive insect. Attacks increase temperature of plants by <color=orange><b>{(FAData?.tempIncreasePerHit ?? tempIncreasePerHit):F0}</b></color>." + AggressivityLine();
}
