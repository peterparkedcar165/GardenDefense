using UnityEngine;
using UnityEngine.InputSystem;

public class LeafRanger : Shooter
{
    private float 
    bAD = 122f, // base attack damage
    bAS = 0.25f, // base attack speed
    bAR = 99f, // base attack range
    bPS = 20f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 1; // base piercing
    public float activeRadius = 2f;

    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        activeDuration = 3f;
    }

    protected override void Update()
    {
        base.Update();
        basePiercing = bP + effectivePath2Level;
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        LeafRangerProjectile arrow = projectile.GetComponent<LeafRangerProjectile>();

        if (arrow != null)
        {
            arrow.SetTarget(FindTarget());
            arrow.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }
        public override void OnPath1Upgrade(int level)
    {
        baseCriticalChance = 0.05f + 0.05f*level;
        baseAttackSpeed = bAS + (0.05f*level);
    }

    public override void OnPath2Upgrade(int level)
    {
        // piercing taken care of in update
    }

    public override void OnPath3Upgrade(int level)
    {
        activeDuration = 3f + (level * 0.5f);
        activeRadius = 3f + (level * 0.3f);
    }


    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=green>Leaf Ranger</color></b>";
    }

    public override string GetDescription()
    {
        return $"The {GetName()} shoots slow but precise arrow shots from across the garden. His arrows can pierce through his target.";
    }

    public override string GetAttackDescription()
    {
        return $"Shoots slow but precise and fierce arrows at his target, dealing <color=green><b>{attackDamage}</b></color> <color=green><b>Nature</b></color> <color=#A0522D>Physical</color> damage.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} releases a trap that imprisons insects within its area, dealing <color=green><b>Nature</b></color> <color=#A0522D>Physical</color> damage upon impact.";
    }

    public override string GetPassiveDescription()
    {
        return $"Attacks can pierce <color=green><b>{piercing}</b></color> enemy.";
    }

    public override string GetPath1Name()
    {
        return "Attack";
    }

    public override string GetPath1Description()
    {
        return $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Critical Chance by <color=green><b>5%</b></color> per level. [<color=green><b>+" + (5*effectivePath1Level) + "%</b></color>]\n\n" +
        "Increase Attack Speed by <color=green><b>0.05</b></color> per level. [<color=green><b>+" + (0.05*effectivePath1Level) + "</b></color>]\n\n" +
        "Level: [<color=green><b>" + path1Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath1Level-path1Level) + ")</b></color>"; 
    }
    public override string GetPath2Name()
    {
        return "Passive";
    }

    public override string GetPath2Description()
    {
        return $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease Piercing by an additional <b><color=green>1</color></b> per level. [<b><color=green>+" + (1 * effectivePath2Level) + "</color></b>]\n\n" +
        "Level: [<color=green><b>" + path2Level + "/" + pathLevelCap + "</b></color>] <color=green><b>(+" + (effectivePath2Level-path2Level) + ")</b></color>";
    }
    public override string GetPath3Name()
    {
        return "Skill";
    }

    public override string GetPath3Description()
    {
        return "";
    }
}
