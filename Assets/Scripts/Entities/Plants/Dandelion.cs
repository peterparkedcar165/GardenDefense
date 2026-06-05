using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Dandelion : Shooter
{
    [SerializeField] private GameObject windGustPrefab;
    [SerializeField] private GameObject windGustIndicatorPrefab;
    private GameObject windGustIndicatorInstance;
    private GameObject _windGustInstance;
    private const int IndicatorSlices = 15;
    private int _obstacleMask;
    private float PushPower => ((DandelionData)data)?.basePushPower ?? 1.5f;

    private float WindGustDamage => data.baseSkillDamage + attackDamage + skillDamageMultiplier * magicPower;
    private float WindGustRange  => (DData?.baseWindGustRange ?? 10f) + (DData?.path3WindGustRangePerLevel ?? 0.5f) * effectivePath3Level;

    private DandelionData DData => data as DandelionData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _obstacleMask = LayerMask.GetMask("Obstacle");
    }

    protected override void Update()
    {
        base.Update();
        UpdateWindGustIndicator();
    }

    protected override void Shoot(Vector3 _)
    {
        int count = (DData?.baseSeedCount ?? 2) + effectivePath2Level;
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
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.GetAimPoint()) <= attackRange)
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
        baseelementalAffinity = level * (DData?.path1elementalAffinityPerLevel ?? 0.06f);
        baseAttackRange    = data.baseAttackRange + level * (DData?.path1AttackRangePerLevel ?? 0.25f);
    }

    public override void OnPath2Upgrade(int level) { }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + level * (DData?.path3SkillDurationPerLevel ?? 1f);
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
        BeginChannel();   // can't attack while the gust is blowing
        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        float beamWidth = (DData?.baseBeamWidth ?? 1f) + (DData?.path3BeamWidthPerLevel ?? 0.25f) * effectivePath3Level;
        if (windGustPrefab == null) return;
        _windGustInstance = Instantiate(windGustPrefab, transform.position, Quaternion.identity);
        _windGustInstance.GetComponent<WindGust>()?.Initialize(transform.position, direction, beamWidth, skillDuration, WindGustDamage, PushPower, this, WindGustRange);
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

        float beamWidth = (DData?.baseBeamWidth ?? 1f) + (DData?.path3BeamWidthPerLevel ?? 0.25f) * effectivePath3Level;
        Vector2 dir  = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        windGustIndicatorInstance.transform.SetPositionAndRotation(
            transform.position + (Vector3)(dir * WindGustRange * 0.5f),
            Quaternion.Euler(0f, 0f, angle));
        windGustIndicatorInstance.transform.localScale = new Vector3(WindGustRange, beamWidth, 1f);
        windGustIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;

        /* multi-slice obstacle clipping — re-enable when ready
        Vector2 perp = new Vector2(-dir.y, dir.x);
        EnsureSlices(windGustIndicatorInstance, IndicatorSlices);
        float sliceWidth = beamWidth / IndicatorSlices;
        for (int i = 0; i < IndicatorSlices; i++)
        {
            float offset = (i + 0.5f - IndicatorSlices * 0.5f) * sliceWidth;
            Vector2 origin = (Vector2)transform.position + perp * offset;
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, WindGustRange, _obstacleMask);
            float len = hit.collider != null ? hit.distance : WindGustRange;
            Transform slice = windGustIndicatorInstance.transform.GetChild(i);
            slice.position   = (Vector3)(origin + dir * len * 0.5f);
            slice.rotation   = Quaternion.Euler(0f, 0f, angle);
            slice.localScale = new Vector3(len, sliceWidth, 1f);
            slice.GetComponent<SpriteRenderer>().enabled = len > 0.05f;
        }
        */
    }

    private void EnsureSlices(GameObject indicator, int count)
    {
        if (indicator.transform.childCount == count) return;

        for (int i = indicator.transform.childCount - 1; i >= 0; i--)
            Destroy(indicator.transform.GetChild(i).gameObject);

        SpriteRenderer root = indicator.GetComponent<SpriteRenderer>();
        if (root != null) root.enabled = false;

        for (int i = 0; i < count; i++)
        {
            GameObject slice = new GameObject($"Slice_{i}");
            slice.transform.SetParent(indicator.transform);
            SpriteRenderer sr = slice.AddComponent<SpriteRenderer>();
            if (root != null)
            {
                sr.sprite         = root.sprite;
                sr.color          = root.color;
                sr.sortingLayerID = root.sortingLayerID;
                sr.sortingOrder   = root.sortingOrder;
            }
            sr.enabled = false;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_windGustInstance != null) Destroy(_windGustInstance);
    }

    public override string GetName() => $"<b><color=#B2EBF2>{(data != null ? data.displayName : "Dandelion")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} releases waves of seeds that ride the wind, striking multiple targets at once.";

    public override string GetPath1Description()
    {
        float eppl    = DData?.path1elementalAffinityPerLevel ?? 0.06f;
        float rangepl = DData?.path1AttackRangePerLevel    ?? 0.25f;
        return $"Attack:\n\n" +
               $"Each seed deals <color=green><b>{attackDamage}</b></color> <color=#B2EBF2>Wind</color> <color=#A0522D>Physical</color> damage.\n\n" +
               $"Increase Elemental Affinity by <color=green><b>{eppl * 100f:F0}%</b></color> per level. [<color=green><b>+{eppl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase Attack Range by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        int seeds = (DData?.baseSeedCount ?? 2) + effectivePath2Level;
        return $"Passive:\n\n" +
               $"Fires <color=green><b>{seeds}</b></color> seeds per attack, targeting the <color=green><b>{seeds}</b></color> highest-priority insects in range.\n\n" +
               $"Increase target count by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float beampl  = DData?.path3BeamWidthPerLevel     ?? 0.25f;
        float durpl   = DData?.path3SkillDurationPerLevel ?? 1f;
        float rangepl = DData?.path3WindGustRangePerLevel ?? 0.5f;
        float currentWidth = (DData?.baseBeamWidth ?? 1f) + beampl * effectivePath3Level;
        return $"Skill:\n\n" +
               $"Blows a powerful gust of pollen wind <color=green><b>{currentWidth:F2}</b></color> units wide towards the targeted direction, reaching <color=green><b>{WindGustRange:F1}</b></color> units, lasting <color=green><b>{skillDuration}</b></color> seconds. Insects caught in the gust take <color=green><b>{data.baseSkillDamage + attackDamage:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] <color=#B2EBF2>Wind</color> <color=#FFB6C1>Magic</color> damage per second, are pushed in the wind's direction, and are <color=#E0E0E0>Displaced</color>.\n\n" +
               $"Scaling: <color=green><b>100%</b></color> Attack Damage\n\n" +
               $"Scaling: <color=#FFB6C1><b>{skillDamageMultiplier * 100f:F0}%</b></color> Magic Power\n\n" +
               $"Increase skill duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"Increase gust width by <color=green><b>{beampl:F2}</b></color> per level. [<color=green><b>+{beampl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Increase gust range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
