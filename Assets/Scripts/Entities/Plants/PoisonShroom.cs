using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PoisonShroom : Shooter
{
    private float 
    bAD = 8f, // base attack damage
    bAS = 0.8f, // base attack speed
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
            puff.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
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
        baseAttackSpeed = bAS + (level * 0.08f);
        baseAttackRange = bAR + (level * 0.1f);
    }

    public override void OnPath2Upgrade(int level)
    {
        poisonLevel = 1 + (level);
        poisonDuration = 6f + (level);
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
        return $"A fungi of few words. The {GetName()} puffs toxic spores at any insect foolish enough to wander close.";
    }

    public override string GetAttackDescription()
    {
        return $"Blows poisonous bubbles at his target, dealing <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage.";
    }

    public override string GetSkillDesription()
    {
        return $"Releases a lingering toxic cloud that poisons the area around himself, dealing <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage over time, and reducing any healing towards insects caught in the cloud.";
    }

    public override string GetPassiveDescription()
    {
        return $"The poisonous bubbles apply a <color=purple>Poison</color> effect on hit, dealing mild damage over time.";
    }
    public override string GetPath1Name()
    {
        return "";
    }

    public override string GetPath1Description()
    {
       return "Path 1:\n\nIncrease Attack Speed by <color=green><b>0.08</b></color> per level. [<color=green><b>+" + (0.08*effectivePath1Level) + "</b></color>]\n\n" +
        "Increase Attack Range by <color=green><b>0.1</b></color> per level. [<color=green><b>+" + (0.1*effectivePath1Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>";  
    }
    public override string GetPath2Name()
    {
        return "";
    }

    public override string GetPath2Description()
    {
        return "Path 2:\n\nIncrease <color=purple>Poison</color> level by <color=green><b>1</b></color> per level. [<color=green><b>+" + (1*effectivePath2Level) + "</b></color>]\n\n" +
        "Increase <color=purple>Poison</color> duration by <color=green><b>1</b></color> second per level. [<color=green><b>+" + (1*effectivePath2Level) + "</b></color>]\n\n" +
        "<color=purple>Poison</color>\nDamage: 6 + 3 per level per second [<b><color=green>" + (6 + 3 * poisonLevel) + "</color></b>]\nDuration: 6 + 1 per level [<b><color=green>" + poisonDuration + "</color></b>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";
    }
    public override string GetPath3Name()
    {
        return "";
    }

    public override string GetPath3Description()
    {
        return "";
    }
}
