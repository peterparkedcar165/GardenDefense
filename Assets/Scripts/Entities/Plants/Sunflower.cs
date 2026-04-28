using UnityEngine;

public class Sunflower : Shooter
{
    private float 
    bAD = 30f, // base attack damage
    bAS = 0.6f, // base attack speed
    bAR = 3f, // base attack range
    bPS = 3f, // base projectile speed
    bMR = 20f; // base max range
    private int bP = 0; // base piercing

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
        // checking the passive level
        if (passiveLevel > 0)
        {
            generationInterval = (16 - 1 * (passiveLevel -1));
            sunGenerated = 10 + 2 * (passiveLevel -1);
            sunTimer -= Time.deltaTime;

            if (sunTimer <= 0)
            {
                GameManager.instance.AddSun(sunGenerated);
                sunTimer = generationInterval;
                Debug.Log(this + " has generated " + sunGenerated + " sun");
            }
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
        if (passiveLevel > 0)
        {
            sunTimer = Mathf.Max(0f, sunTimer - 1f);
            // Debug.Log("Reduced timer by 1");
        }
    }
    
    public override void LevelUp()
        {
            base.LevelUp();
            int perLevel = (level - 1);
            baseAttackDamage = bAD + (perLevel * 1f);
            baseAttackSpeed = bAS + (perLevel * 0.03f);
            baseAttackRange = bAR + (perLevel * 0.2f);
            baseProjectileSpeed = bPS + (perLevel * 0.3f);
        }
}
