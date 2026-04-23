using UnityEngine;
using System.Collections.Generic;

public class PoisonShroom : Shooter
{
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = 3f;
        baseAttackSpeed = 0.6f;
        baseAttackRange = 3f;
        baseProjectileSpeed = 3f;
        basePiercing = 0;
        baseMaxRange = 7f;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        PoisonShroomProjectile puff = projectile.GetComponent<PoisonShroomProjectile>();

        if (puff != null)
        {
            puff.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Magic, ElementalType.Poison, this);
        }
    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = 3f + (perLevel * 0.3f);
        baseAttackSpeed = 0.6f + (perLevel * 0.04f);
        baseAttackRange = 3f + (perLevel * 0.2f);
        // baseProjectileSpeed = 8f + (perLevel * 0.2f);
    }

    protected override GameObject FindTarget()
    {
        GameObject[] allInsects = GameObject.FindGameObjectsWithTag("Insect");
        List<GameObject> unpoisoned = new List<GameObject>();
        
        foreach (GameObject obj in allInsects)
        {
            float distance = Vector3.Distance(transform.position, obj.transform.position);
            if (distance <= attackRange && !obj.GetComponent<Insect>().HasEffect<PoisonEffect>())
            {
                unpoisoned.Add(obj);
            }
        }

        if (unpoisoned.Count > 0)
        {
            return FindFirst(unpoisoned.ToArray());
        }

        return base.FindTarget();

    }
}
