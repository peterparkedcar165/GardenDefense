using UnityEngine;

public class Sunflower : Shooter
{
    public float generationInterval, sunTimer = 15f;
    public int sunGenerated;
    protected override void Awake()
    {
        baseAttackDamage = 10f;
        baseAttackSpeed = 0.5f;
        baseAttackRange = 3f;
        baseProjectileSpeed = 3f;
        basePiercing = 0;
        baseMaxRange = 6f;
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
            generationInterval = (15 - 1 * (passiveLevel -1));
            sunGenerated = 15 + 1 * (passiveLevel -1);
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
        sunTimer = Mathf.Max(0f, sunTimer - 1f);
        Debug.Log("Reduced timer by 1");
    }
    
    public override void LevelUp()
        {
            base.LevelUp();
            int perLevel = (level - 1);
            baseAttackDamage = 10f + (perLevel * 1f);
            baseAttackSpeed = 0.5f + (perLevel * 0.03f);
            baseAttackRange = 3f + (perLevel * 0.2f);
            baseProjectileSpeed = 3f + (perLevel * 0.3f);
        }
}
