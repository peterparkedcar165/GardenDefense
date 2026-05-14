using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Dandelion : Shooter
{
    private float
    bAD = 16f,  // base attack damage
    bAS = 1f,   // base attack speed
    bAR = 2f,   // base attack range
    bPS = 5f,   // base projectile speed
    bMR = 20f;  // base max range
    private int bP = 0; // base piercing

    [SerializeField] private GameObject windGustPrefab;
    [SerializeField] private GameObject windGustIndicatorPrefab;
    private GameObject windGustIndicatorInstance;

    private const float indicatorLength = 30f;
    private const float pushForce = 1f;

    protected override void Awake()
    {
        base.Awake();
        baseAttackDamage = bAD;
        baseAttackSpeed = bAS;
        baseAttackRange = bAR;
        baseProjectileSpeed = bPS;
        baseMaxRange = bMR;
        basePiercing = bP;
        baseSkillCooldown = 1f;
        baseSkillDuration = 4f;
    }

    protected override void Update()
    {
        base.Update();
        UpdateWindGustIndicator();
    }

    protected override void Shoot(Vector3 _)
    {
        int count = 3 + effectivePath2Level;
        List<Insect> targets = FindMultipleTargets(count);
        if (targets.Count == 0) return;

        for (int i = 0; i < count; i++)
        {
            Insect target = targets[i % targets.Count];
            Vector3 predicted = PredictTargetPosition(target.gameObject);
            FireProjectile(predicted, target);
        }
    }

    private void FireProjectile(Vector3 targetPos, Insect target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        DandelionProjectile seed = proj.GetComponent<DandelionProjectile>();
        if (seed != null)
        {
            seed.SetTarget(target.gameObject);
            seed.Initialize(targetPos, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    private List<Insect> FindMultipleTargets(int count)
    {
        List<Insect> inRange = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (Vector3.Distance(transform.position, insect.transform.position) <= attackRange)
                inRange.Add(insect);
        }

        switch (targeting)
        {
            case TARGETING.First:
                inRange.Sort((a, b) =>
                {
                    if (a.currentWaypointIndex != b.currentWaypointIndex)
                        return b.currentWaypointIndex.CompareTo(a.currentWaypointIndex);
                    Transform wpA = a.GetCurrentWaypoint();
                    Transform wpB = b.GetCurrentWaypoint();
                    if (wpA == null || wpB == null) return 0;
                    return Vector3.Distance(a.transform.position, wpA.position)
                                  .CompareTo(Vector3.Distance(b.transform.position, wpB.position));
                });
                break;
            case TARGETING.Last:
                inRange.Sort((a, b) =>
                {
                    if (a.currentWaypointIndex != b.currentWaypointIndex)
                        return a.currentWaypointIndex.CompareTo(b.currentWaypointIndex);
                    Transform wpA = a.GetCurrentWaypoint();
                    Transform wpB = b.GetCurrentWaypoint();
                    if (wpA == null || wpB == null) return 0;
                    return Vector3.Distance(b.transform.position, wpB.position)
                                  .CompareTo(Vector3.Distance(a.transform.position, wpA.position));
                });
                break;
            case TARGETING.Nearest:
                inRange.Sort((a, b) =>
                    Vector3.Distance(transform.position, a.transform.position)
                           .CompareTo(Vector3.Distance(transform.position, b.transform.position)));
                break;
        }

        return inRange.Count <= count ? inRange : inRange.GetRange(0, count);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = bAD + (level * 2f);
        baseAttackSpeed = bAS + (level * 0.05f);
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = bAR + (level * 0.25f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = 4f + (level * 0.5f);
    }

    public override void ActivateSkill()
    {
        if (windGustIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
        if (windGustIndicatorPrefab != null)
        {
            windGustIndicatorInstance = Instantiate(windGustIndicatorPrefab, transform.position, Quaternion.identity);
            windGustIndicatorInstance.GetComponent<SpriteRenderer>().enabled = false;
        }
    }

    private void OnTargetConfirmed(Vector3 targetPosition)
    {
        skillCooldownTimer = skillCooldown;
        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        float beamWidth = 1.5f + 2.5f * effectivePath3Level;
        if (windGustPrefab == null) return;
        GameObject obj = Instantiate(windGustPrefab, transform.position, Quaternion.identity);
        obj.GetComponent<WindGust>()?.Initialize(transform.position, direction, beamWidth, skillDuration, attackDamage, pushForce, this);
    }

    private void UpdateWindGustIndicator()
    {
        if (windGustIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(windGustIndicatorInstance);
            windGustIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;

        float beamWidth = 1.5f + 2.5f * effectivePath3Level;
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        windGustIndicatorInstance.transform.SetPositionAndRotation(
            transform.position + (Vector3)(dir * indicatorLength * 0.5f),
            Quaternion.Euler(0f, 0f, angle));
        windGustIndicatorInstance.transform.localScale = new Vector3(indicatorLength, beamWidth, 1f);
        windGustIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;
    }


    // DESCRIPTION

    public override string GetName() => "<b><color=#B2EBF2>Dandelion</color></b>";

    public override string GetDescription()
        => $"The {GetName()} releases waves of seeds that ride the wind, striking multiple targets at once.";

    public override string GetAttackDescription()
        => $"Fires <color=green><b>{3 + effectivePath2Level}</b></color> seeds simultaneously, each dealing <color=green><b>{attackDamage}</b></color> <color=#B2EBF2>Wind</color> <color=#A0522D>Physical</color> damage and applying <color=#B2EBF2>Gust</color>.";

    public override string GetSkillDesription()
        => $"Blows a powerful gust of pollen wind <color=green><b>{1.5f + 2.5f * effectivePath3Level}</b></color> units wide towards the targeted direction, crossing the entire map. Insects caught in the gust take <color=#B2EBF2>Wind</color> <color=#FFB6C1>Magic</color> damage over time, are pushed in the wind's direction, and have <color=#B2EBF2>Gust</color> applied for <color=green><b>{skillDuration}</b></color> seconds.";

    public override string GetPassiveDescription()
        => $"Fires <color=green><b>{3 + effectivePath2Level}</b></color> seeds per attack, targeting the <color=green><b>{3 + effectivePath2Level}</b></color> highest-priority insects in range.";

    public override string GetPath1Description()
        => $"Attack:\n\n{GetAttackDescription()}\n\nIncrease Attack Damage by <color=green><b>2</b></color> per level. [<color=green><b>+{2 * effectivePath1Level}</b></color>]\n\n" +
           $"Increase Attack Speed by <color=green><b>0.05</b></color> per level. [<color=green><b>+{0.05 * effectivePath1Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";

    public override string GetPath2Description()
        => $"Passive:\n\n{GetPassiveDescription()}\n\nIncrease target count by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
           $"Increase Attack Range by <color=green><b>0.25</b></color> per level. [<color=green><b>+{0.25 * effectivePath2Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";

    public override string GetPath3Description()
        => $"Skill:\n\n{GetSkillDesription()}\n\nIncrease gust duration by <color=green><b>0.5</b></color> seconds per level. [<color=green><b>+{0.5 * effectivePath3Level}s</b></color>]\n\n" +
           $"Increase gust width by <color=green><b>2.5</b></color> per level. [<color=green><b>+{2.5 * effectivePath3Level}</b></color>]\n\n" +
           $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
}
