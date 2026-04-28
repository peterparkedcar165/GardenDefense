using UnityEngine;
using System.Collections.Generic;

public class PoisonShroom : Shooter
{
    private float 
    bAD = 12f, // base attack damage
    bAS = 0.6f, // base attack speed
    bAR = 3f, // base attack range
    bPS = 3f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 0; // base piercing
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
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
            puff.SetTarget(FindTarget());
            puff.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Magic, ElementalType.Poison, this);
        }
    }

    public override void LevelUp()
    {
        base.LevelUp();
        int perLevel = (level - 1);
        baseAttackDamage = bAD + (perLevel * 0.3f);
        baseAttackSpeed = bAS + (perLevel * 0.04f);
        baseAttackRange = bAR + (perLevel * 0.1f);
        baseProjectileSpeed = bPS + (perLevel * 0.2f);
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
