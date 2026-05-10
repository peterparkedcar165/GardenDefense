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

    public int sunGenerated;
    public float skillAoERadius, sunrayDamagePerSecond;
    public float channelDuration = 0.5f;
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
        basePassiveCooldown = 12f;
        skillAoERadius = 1.5f;
        activeDuration = 6;
        base.Awake();
        passiveCooldownTimer = passiveCooldown;
        // sun cost is set in inspector!
    }

    protected override void Update()
    {
        base.Update();

        sunGenerated = 10 + 2 * effectivePath2Level;

        if (passiveCooldownTimer <= 0)
        {
            GameManager.instance.AddSun(sunGenerated);
            passiveCooldownTimer = passiveCooldown;
            GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), transform.position + new Vector3(0.25f, 0.5f, 0f), Quaternion.identity);
            indicator.GetComponent<DamageIndicator>().Initialize($"+{sunGenerated} Sun", new Color(1f, 1f, 0f));
        }
        
        
    }

    protected override void UpdateStats()
    {
        base.UpdateStats();
        sunrayDamagePerSecond = (3f + 0.35f*effectivePath3Level) * attackDamage;
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
        passiveCooldownTimer = Mathf.Max(0f, passiveCooldownTimer - 1f);
    }
    
    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 5f);
        baseAttackSpeed = bAS + (level * 0.05f);
    }

    public override void OnPath2Upgrade(int level)
    {
        passiveCooldownAdder = -(float)level;
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 6 + 0.5f * (level - 1);
    }

    
    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (sunrayPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        StartCoroutine(ChannelAndSpawn(position));
    }

    private IEnumerator ChannelAndSpawn(Vector3 position)
    {
        yield return new WaitForSeconds(channelDuration);
        GameObject obj = Instantiate(sunrayPrefab, position, Quaternion.identity);
        Sunray sunray = obj.GetComponent<Sunray>();
        if (sunray != null)
            sunray.Initialize(sunrayDamagePerSecond, skillAoERadius, activeDuration, this);
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
        return $"Gathers a large burst of energy from the sun, calling down a scorching beam from above that deals <color=green>{sunrayDamagePerSecond}</color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second to insects within the designated area for <color=green>{activeDuration}</color> seconds.";
    }

    public override string GetPassiveDescription()
    {
        return $"Passively generates <color=green>{sunGenerated}</color> <color=yellow>Sun</color> for the garden every <color=green>{passiveCooldown}</color> seconds. Attacks reduce the cooldown by <color=green>1</color> second.";
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
        "Reduce Sun Generation Cooldown by <color=green><b>1</b></color> second per level. [<color=green><b>-" + effectivePath2Level + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";  
    }

    public override string GetPath3Description()
    {
        return $"Skill:\n\n{GetSkillDesription()}\n\nIncrease the Attack Damage multiplier of the Damage Per Second by <color=green><b>35%</b></color> per level. [<color=green><b>+" + (35*effectivePath3Level) + "%</b></color>]\n\n" +
        "Increase Sunray duration by <color=green><b>0.5</b></color> second per level. [<color=green><b>" + (0.5*effectivePath3Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path3Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath3Level-path3Level) + ")</b></color>";
    }
}
