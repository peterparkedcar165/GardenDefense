using UnityEngine;

public class DesertLocust : Insect
{
    private const float devourDuration = 12f;
    private DesertLocustData LData => data as DesertLocustData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void Attack()
    {
        // save target before base.Attack() resolves it (plant may die from the hit)
        IAttackable currentTarget = target;
        base.Attack();

        if (currentTarget is Plant plant && plant.IsAlive)
            ApplyDevour(plant);
    }

    private void ApplyDevour(Plant plant)
    {
        DevourEffect existing = plant.GetEffect<DevourEffect>();
        if (existing == null)
        {
            plant.ApplyEffect(new DevourEffect(plant, devourDuration, 1, this));
            existing = plant.GetEffect<DevourEffect>();
        }
        existing?.AddStack(attackDamage * (LData?.devourReductionPercent ?? 1f));
    }
}
