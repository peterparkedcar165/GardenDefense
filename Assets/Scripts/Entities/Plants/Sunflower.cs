using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class Sunflower : Shooter
{
    private float 
    bAD = 35f, // base attack damage
    bAS = 0.6f, // base attack speed
    bAR = 3f, // base attack range
    bPS = 3f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 0; // base piercing
    public float activeDuration = 4;

    public float generationInterval, sunTimer = 15f;
    public int sunGenerated;
    public float skillAoERadius = 2.5f;
    public float channelDuration = 1.5f;
    [SerializeField] private GameObject sunrayPrefab;
    protected override void Awake()
    {
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        baseSkillCooldown = 3f;
        base.Awake();
        // sun cost is set in inspector!
    }

    protected override void Update()
    {
        base.Update();

        /* SPECIAL EFFECT */
        // checking the path 2 level

        generationInterval = (11 - 1 * (effectivePath2Level -1));
        sunGenerated = 10 + 2 * (effectivePath2Level);
        sunTimer -= Time.deltaTime;

        if (sunTimer <= 0)
        {
            GameManager.instance.AddSun(sunGenerated);
            sunTimer = generationInterval;
            Debug.Log(this + " has generated " + sunGenerated + " sun");
        }
        
        
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        SunflowerProjectile petal = projectile.GetComponent<SunflowerProjectile>();

        if (petal != null)
        {
            petal.SetTarget(FindTarget()); // assign the target of this plant to the projectile
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this); // change elemental everytime
        }
    }

    public void ReduceSunTimer()
    {
        sunTimer = Mathf.Max(0f, sunTimer - 1f);
        // Debug.Log("Reduced timer by 1");
    }
    
    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 5f);
        baseAttackSpeed = bAS + (level * 0.05f);
    }

    public override void OnPath2Upgrade(int level)
    {
        // can leave empty, because already taken care of on Update()
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 4 + 0.5f*(level -1);
    }

    
    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (sunrayPrefab == null) return;
        GameObject obj = Instantiate(sunrayPrefab, position, Quaternion.identity);
        Sunray sunray = obj.GetComponent<Sunray>();
        if (sunray != null)
            sunray.Initialize(attackDamage, skillAoERadius, activeDuration, channelDuration, this);
        skillCooldownTimer = skillCooldown;
    }

    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=orange>Sunflower</color></b>"; // bold, then orange, name, uncolor, unbold
    }

    public override string GetDescription()
    {
        return $"The {GetName()} shoots her targets with sun bolts and generate precious <color=yellow>Sun</color> for the garden.";
    }

    public override string GetAttackDescription()
    {
        return $"Briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color=green>{attackDamage}</color> <color=orange>Fire</color> <color=#FFB6C1>Magic </color>damage.";
    }

    public override string GetSkillDesription()
    {
        return $"Gathers a large burst of energy from the sun, calling down a blazing beam from above that deals massive <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage to targets in its path.";
    }

    public override string GetPassiveDescription()
    {
        return $"Passively generates <color=green>{sunGenerated}</color> <color=yellow>Sun</color> for the garden every <color=green>{generationInterval}</color> seconds. Attacks reduce the cooldown by <color=green>1</color> second.";
    }


    public override string GetPath1Description()
    {
        return $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <color=green><b>5</b></color> per level. [<color=green><b>+" + (5*effectivePath1Level) + "</b></color>]\n\n" +
        "Increase Attack Speed by <color=green><b>0.05</b></color> per level. [<color=green><b>+" + (0.05*effectivePath1Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>"; 
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease Sun Generated by <color=green><b>2</b></color> per level. [<color=green><b>+" + (2*effectivePath2Level) + "</b></color>]\n\n" +
        "Reduce Sun Generation Cooldown by <color=green><b>1</b></color> second per level. [<color=green><b>" + (1*effectivePath2Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";  
    }

    public override string GetPath3Description()
    {
        return $"Skill:\n\n{GetSkillDesription()}";
    }
}
