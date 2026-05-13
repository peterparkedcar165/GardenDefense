using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PoisonShroom : Shooter
{
    private float 
    bAD = 24f, // base attack damage
    bAS = 0.8f, // base attack speed
    bAR = 3f, // base attack range
    bPS = 3f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 0; // base piercing
    public int poisonLevel = 1;
    public float poisonDuration = 6f, activeRadius = 0.75f;
    [SerializeField] private GameObject poisonBlobPrefab;
    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        baseSkillDuration = 5f;
        activeRadius = 1f;
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
        List<Insect> unpoisoned = new List<Insect>();

        foreach (Insect insect in Insect.allInsects)
        {
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance <= attackRange && !insect.HasEffect<PoisonEffect>())
            {
                unpoisoned.Add(insect);
            }
        }

        if (unpoisoned.Count > 0)
            return FindFirst(unpoisoned);

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
        baseSkillDuration = 5f + 1f * level;
        activeRadius = 1f + 0.2f * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(activeRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (poisonBlobPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        GameObject obj = Instantiate(poisonBlobPrefab, transform.position, Quaternion.identity);
        PoisonBlob blob = obj.GetComponent<PoisonBlob>();
        if (blob != null)
            blob.Initialize(position, activeRadius, skillDuration, this);
    }

    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=purple>Poison Shroom</color></b>";
    }

    public override string GetDescription()
    {
        return $"The {GetName()} shoots toxic spores dealing damage and applying <color=purple>Poison</color>.";
    }

    public override string GetAttackDescription()
    {
        return $"Blows poisonous bubbles at his target, dealing <color=green><b>{attackDamage}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage.";
    }

    public override string GetSkillDesription()
    {
        return $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{activeRadius}</b></color> radius that lasts <color=green><b>{skillDuration}</b></color> seconds. Insects standing in the field take <color=green><b>12</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second, and any debuffs on them are frozen in time.";
    }

    public override string GetPassiveDescription()
    {
        return $"Attacks apply a <color=purple>Poison</color> effect on hit for <color=green><b>{poisonDuration}</b></color> seconds, dealing <color=green><b>{12 + 4*(poisonLevel-1)}</b></color> <color=purple>Poison</color> damage per second.";
    }

    public override string GetPath1Description()
    {
        return $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Speed by <color=green><b>0.08</b></color> per level. [<color=green><b>+" + (0.08*effectivePath1Level) + "</b></color>]\n\n" +
        "Increase Attack Range by <color=green><b>0.1</b></color> per level. [<color=green><b>+" + (0.1*effectivePath1Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>";  
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease <color=purple>Poison</color> damage by <color=green><b>4</b></color> per level. [<color=green><b>+" + (4*effectivePath2Level) + "</b></color>]\n\n" +
        "Increase <color=purple>Poison</color> duration by <color=green><b>1</b></color> second per level. [<color=green><b>+" + (1*effectivePath2Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";
    }

    public override string GetPath3Description()
    {
        return $"Skill:\n\n{GetSkillDesription()}\n\nIncrease field duration by <color=green><b>1</b></color> second per level. [<color=green><b>+{1 * effectivePath3Level}s</b></color>]\n\n" +
        "Increase field radius by <color=green><b>0.2</b></color> per level. [<color=green><b>+" + (0.2 * effectivePath3Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path3Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath3Level - path3Level) + ")</b></color>";
    }
}
