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
    public int poisonLevel = 1;
    public float poisonDuration = 6f, activeDuration = 7f, activeRadius = 2f;
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

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = bAS + (level * 0.04f);
        baseAttackRange = bAR + (level * 0.1f);
    }

    public override void OnPath2Upgrade(int level)
    {
        poisonLevel = 1 + (level);
        poisonDuration = 6f + (level-1);
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 7f + 1*(level - 1);
        activeRadius = 2f + 0.5f*(level - 1);
    }

    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=purple>Poison Shroom</color></b>";
    }

    public override string GetDescription()
    {
        return $"A fungi of few words. The {GetName()} lurks in the shadows of the garden, puffing toxic spores at any insect foolish enough to wander close.";
    }

    public override string GetAttackDescription()
    {
        return $"The {GetName()} blows poisonous bubbles at his target, dealing <color=purple>Poison</color> <color=pink>Magic</color> damage, and applying a mild <color=purple>Poison</color> effect.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} releases a lingering toxic cloud that poisons the area around it, dealing <color=purple>Poison</color> <color=pink>Magic</color> damage over time, and reducing any healing towards insects caught in the cloud.";
    }

    public override string GetPassiveDescription()
    {
        return $"The {GetName()}'s toxin gets stronger, and spreads to nearby insects on hit.";
    }
}
