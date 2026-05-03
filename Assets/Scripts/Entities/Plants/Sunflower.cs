using UnityEngine;

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
    protected override void Awake()
    {
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        base.Awake();
        // sun cost is set in inspector!
    }

    protected override void Update()
    {
        base.Update();

        /* SPECIAL EFFECT */
        // checking the path 2 level

        generationInterval = (12 - 1 * (effectivePath2Level -1));
        sunGenerated = 15 + 5 * (effectivePath2Level -1);
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
            petal.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, DamageType.Magic, ElementalType.Fire, this); // change elemental everytime
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

    
    // DESCRIPTION

    public override string GetName()
    {
        return "<b><color=orange>Sunflower</color></b>"; // bold, then orange, name, uncolor, unbold
    }

    public override string GetDescription()
    {
        return $"A cheerful guardian of the garden. The {GetName()} draws power from sunlight, converting it into both blazing attacks and precious sun for the garden's growth.";
    }

    public override string GetAttackDescription()
    {
        return $"The {GetName()} briefly charges up a solar-powered energy orb then shoots it towards her target, dealing <color=orange>Fire</color> <color=pink>Magic </color>damage.";
    }

    public override string GetSkillDesription()
    {
        return $"The {GetName()} gathers a large burst of energy from the sun, calling down a blazing beam from above that deals massive <color=orange>Fire</color> <color=pink>Magic</color> damage to targets in its path.";
    }

    public override string GetPassiveDescription()
    {
        return $"The {GetName()} passively generates <color=yellow>Sun</color> for the garden. Each attack that hits an enemy reduces the generation cooldown.";
    }
}
