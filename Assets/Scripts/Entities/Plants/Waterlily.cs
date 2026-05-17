using UnityEngine;
using UnityEngine.InputSystem;

public class Waterlily : Shooter
{
    private float 
    bAD = 12f, // base attack damage
    bAS = 1f, // base attack speed
    bAR = 3f, // base attack range
    bPS = 2.6f, // base projectile speed
    bMR = 20f, // base max range
    bAoER = 0.75f; // base aoe range

    public float AoERange, baseAoERange, AoERangeMultiplier, AoERangeAdder;
    private int bP = 0; // base piercing

    public float skillAoERadius = 1.5f;
    public float bubbleDamage = 42f;
    [SerializeField] private GameObject bubbleTrapPrefab;

    protected override void Awake()
    {
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        baseAoERange = bAoER;
        baseSkillCooldown = 20f;
        baseSkillDuration = 5f;
        base.Awake();
        // sun cost is set in inspector!
    }

    protected override void Update()
    {
        base.Update();
        
        AoERange = baseAoERange + AoERangeAdder + (baseAoERange*AoERangeMultiplier);
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        WaterlilyProjectile bubble = projectile.GetComponent<WaterlilyProjectile>();

        if (bubble != null)
        {
            bubble.SetTarget(FindTarget()); // assign the target of this plant to the projectile
            bubble.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this); // change elemental everytime
        }
    }
    
    public override void OnPath1Upgrade(int level)
    {
        baseAttackRange = bAR + (level * 0.5f);
        baseAttackSpeed = bAS + (level * 0.3f);
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAoERange = bAoER + (level * 0.05f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = 5f + 1f * level;
        bubbleDamage = 42f + 12f * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(skillAoERadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (bubbleTrapPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        GameObject obj = Instantiate(bubbleTrapPrefab, transform.position, Quaternion.identity);
        BubblePrison bubble = obj.GetComponent<BubblePrison>();
        if (bubble != null)
            bubble.Initialize(position, skillAoERadius, skillDuration, bubbleDamage, this);
    }

    
    public override PlantBaseStats GetBaseStats() => new PlantBaseStats
    {
        attackDamage = bAD, attackSpeed = bAS, attackRange = bAR,
        skillCooldown = 20f, skillDuration = 5f,
        splashRadius = bAoER, bubbleDamage = 42f,
    };

    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=#3399FF>Waterlily</color></b>"; // bold, then orange, name, uncolor, unbold
    }

    public override string GetDescription()
    {
        return $"The {GetName()} shoots her targets with little bubbles that hurts surrounding insects. She can also imprison her foes with her larger bubble.";
    }

    public override string GetAttackDescription()
    {
        return $"Blow little bubbles towards her target, dealing <color=green><b>{attackDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic </color>damage.";
    }

    public override string GetSkillDesription()
    {
        return $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{bubbleDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic</color> damage upon impact, and keeping them airborne for <color=green><b>{skillDuration}</b></color> seconds.";
    }

    public override string GetPassiveDescription()
    {
        return $"Attacks deal <color=green><b>{attackDamage*(0.25 + 0.025*effectivePath2Level)}</b></color> <color=#3399FF>Water</color> damage to surrounding insects within a <color=green><b>{AoERange}</b></color> radius.";
    }


    public override string GetPath1Description()
    {
        return $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Speed by <color=green><b>0.06</b></color> per level. [<color=green><b>+" + (0.06*effectivePath1Level) + "</b></color>]\n\n" +
        "Increase Attack Range by <color=green><b>0.3</b></color> per level. [<color=green><b>+" + (0.3*effectivePath1Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>"; 
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease splash damage by <color=green><b>2.5%</b></color> per level. [<color=green><b>+" + (2.5*effectivePath2Level) + "%</b></color>]\n\n" +
        "Increase splash radius by <color=green><b>0.025</b></color> per level. [<color=green><b>" + (0.025*effectivePath2Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";  
    }

    public override string GetPath3Description()
    {
        return $"Skill:\n\n{GetSkillDesription()}\n\nIncrease impact damage by <color=green><b>12</b></color> per level. [<color=green><b>+" + (12*effectivePath3Level) + "</b></color>]\n\n" +
        $"Increase duration by <color=green><b>1</b></color> second per level. [<color=green><b>+{1 * effectivePath3Level}s</b></color>]\n\n" +
        "Level: [<color=green><b>" + path3Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath3Level-path3Level) + ")</b></color>";
    }
}
