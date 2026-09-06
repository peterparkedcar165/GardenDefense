using UnityEngine;
using UnityEngine.InputSystem;

public class Dandelion : Shooter
{
    [SerializeField] private GameObject windGustPrefab;
    [SerializeField] private GameObject windGustIndicatorPrefab;
    private GameObject windGustIndicatorInstance;
    private GameObject _windGustInstance;
    private const int IndicatorSlices = 15;
    private int _obstacleMask;
    private float PushPower => ((DandelionData)data)?.basePushPower ?? 1.5f;

    // ignores this Shooter's own `piercing` stat entirely - the wind naturally pierces through
    // everything in its path, so upgrading piercing on Dandelion would otherwise do nothing
    private const int InfinitePiercing = int.MaxValue;

    private float WindGustDamage => data.baseSkillDamage + attackDamage + skillDamageMultiplier * magicPower;
    private float WindGustRange  => (DData?.baseWindGustRange ?? 10f) + (DData?.path3WindGustRangePerLevel ?? 0.5f) * effectivePath3Level;
    private const float GlobalGustRange = 30f;

    private float BlindingChance   => (DData?.baseBlindingChance ?? 0.5f) + (DData?.path2BlindingChancePerLevel ?? 0.05f) * effectivePath2Level;
    private float BlindingDuration => (DData?.baseBlindingDuration ?? 2f) + (DData?.path2BlindingDurationPerLevel ?? 1f) * effectivePath2Level;

    private DandelionData DData => data as DandelionData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _obstacleMask = LayerMask.GetMask("Obstacle");
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        if (IsPath1Maxed)
        {
            bonusEffectChance += 0.35f;
            accuracyAdder += 1f; // +100% Accuracy - can no longer miss from a target's evasion
        }
    }

    protected override void Update()
    {
        base.Update();
        UpdateWindGustIndicator();
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject targetGO = FindTarget();
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        DandelionProjectile seed = proj.GetComponent<DandelionProjectile>();
        if (seed != null)
        {
            seed.SetTarget(targetGO);
            seed.Initialize(target, attackDamage, projectileSpeed, maxRange, InfinitePiercing, damageType, elementalType, this);
        }
    }

    // called by DandelionProjectile on every insect it hits along its path (piercing means this
    // can fire several times per shot, once independently per insect struck)
    public void TryApplyBlindingPollen(Insect insect)
    {
        if (!insect.IsAlive) return;
        if (Random.value >= BlindingChance * (1f + bonusEffectChance)) return;
        insect.ApplyEffect(new BlindingPollenEffect(insect, BlindingDuration, 1, this));

        // path2 max: Blinding Pollen also briefly stuns flying insects
        if (IsPath2Maxed && insect.isFlying)
            insect.ApplyEffect(new StunEffect(insect, DData?.minorStunDuration ?? 1f, 1, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + level * (DData?.path1AttackDamagePerLevel ?? 4f);
        baseAttackRange  = data.baseAttackRange  + level * (DData?.path1AttackRangePerLevel  ?? 0.25f);
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
        bool isGlobal = IsPath3Maxed;
        if (!isGlobal) BeginChannel();
        Vector2 direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
        float beamWidth = (DData?.baseBeamWidth ?? 1f) + (DData?.path3BeamWidthPerLevel ?? 0.25f) * effectivePath3Level;
        float gustRange = isGlobal ? GlobalGustRange : WindGustRange;
        if (windGustPrefab == null) return;
        _windGustInstance = Instantiate(windGustPrefab, transform.position, Quaternion.identity);
        _windGustInstance.GetComponent<WindGust>()?.Initialize(transform.position, direction, beamWidth, skillDuration, WindGustDamage, PushPower, this, gustRange, isGlobal, BlindingDuration);
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
        float indicatorRange = IsPath3Maxed ? GlobalGustRange : WindGustRange;
        Vector2 dir  = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        float   angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bool globalIndicator = IsPath3Maxed;
        Vector3 indicatorCenter = globalIndicator
            ? transform.position
            : transform.position + (Vector3)(dir * indicatorRange * 0.5f);
        float indicatorLength = globalIndicator ? indicatorRange * 2f : indicatorRange;
        windGustIndicatorInstance.transform.SetPositionAndRotation(indicatorCenter, Quaternion.Euler(0f, 0f, angle));
        windGustIndicatorInstance.transform.localScale = new Vector3(indicatorLength, beamWidth, 1f);
        windGustIndicatorInstance.GetComponent<SpriteRenderer>().enabled = true;

        /* multi-slice obstacle clipping , re-enable when ready
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
        $"The {GetName()} blows gusts of pollen-laden wind, blinding and disorienting its enemies.";

    public override string GetPath1Description(bool details = false)
    {
        float dmgpl   = DData?.path1AttackDamagePerLevel ?? 4f;
        float rangepl = DData?.path1AttackRangePerLevel  ?? 0.25f;
        string desc = details
            ? $"Blows a slow moving wind of pollen at a target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)}. The wind pierces through everything in its path."
            : $"Blows a slow moving wind of pollen at a target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)}. The wind pierces through everything in its path.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F2}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=green><b>Bonus Effect Chance</b></color> by <color=green><b>35%</b></color>, and <color=green><b>Accuracy</b></color> by <color=green><b>100%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float chancepl = DData?.path2BlindingChancePerLevel   ?? 0.05f;
        float durpl    = DData?.path2BlindingDurationPerLevel ?? 1f;
        string desc = details
            ? $"Attacks have a <color=green><b>[({(DData?.baseBlindingChance ?? 0.5f) * 100f:F0}%) + ({chancepl * 100f:F0}%/Lvl.)]</b></color> chance of applying <color=#B2EBF2><b>Blinding Pollen</b></color>, reducing the target's Accuracy by <color=red><b>{BlindingPollenEffect.DefaultReduction * 100f:F0}%</b></color> for <color=green><b>[({(DData?.baseBlindingDuration ?? 2f):F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds."
            : $"Attacks have a <color=green><b>{BlindingChance * 100f:F0}%</b></color> chance of applying <color=#B2EBF2><b>Blinding Pollen</b></color>, reducing the target's Accuracy by <color=red><b>{BlindingPollenEffect.DefaultReduction * 100f:F0}%</b></color> for <color=green><b>{BlindingDuration:F0}</b></color> seconds.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase chance by <color=green><b>{chancepl * 100f:F0}%</b></color> per level. [<color=green><b>+{chancepl * effectivePath2Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> second(s) per level. [<color=green><b>+{durpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=#B2EBF2><b>Blinding Pollen</b></color> inflicts a minor <color=#FFD700><b>Stun</b></color> on flying insects.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float beampl  = DData?.path3BeamWidthPerLevel     ?? 0.25f;
        float durpl   = DData?.path3SkillDurationPerLevel ?? 1f;
        float rangepl = DData?.path3WindGustRangePerLevel ?? 0.5f;
        float baseBeam = DData?.baseBeamWidth ?? 1f;
        float baseRange = DData?.baseWindGustRange ?? 10f;
        string desc = details
            ? $"Blows a powerful gust of pollen wind <color=green><b>[({baseBeam:F2}) + ({beampl:F2}/Lvl.)]</b></color> units wide towards the targeted direction, reaching <color=green><b>[({baseRange:F1}) + ({rangepl:F1}/Lvl.)]</b></color> units, lasting <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Insects caught in the gust take <color=green><b>[100% Attack Damage]</b></color> + <color=green><b>{data.baseSkillDamage:F0}</b></color> <color=#FFB6C1>[+{skillDamageMultiplier * 100f:F0}% Magic Power]</color> {PlantData.DamageTypeLabel(damageType)} per second, are pushed in the wind's direction, are <color=#E0E0E0>Displaced</color>, and every tick applies <color=#B2EBF2><b>Blinding Pollen</b></color> for the passive's current duration."
            : $"Blows a powerful gust of pollen wind <color=green><b>{(baseBeam + beampl * effectivePath3Level):F2}</b></color> units wide towards the targeted direction, reaching <color=green><b>{WindGustRange:F1}</b></color> units, lasting <color=green><b>{skillDuration}</b></color> seconds. Insects caught in the gust take <color={PlantData.ElementalColor(elementalType)}><b>{data.baseSkillDamage + attackDamage:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} per second, are pushed in the wind's direction, are <color=#E0E0E0>Displaced</color>, and every tick applies <color=#B2EBF2><b>Blinding Pollen</b></color> for <color=green><b>{BlindingDuration:F0}</b></color> seconds.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase skill duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase gust width by <color=green><b>{beampl:F2}</b></color> per level. [<color=green><b>+{beampl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"Increase gust range by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "No longer disarmed while the wind gust is active. The wind gust now extends <color=green><b>globally</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
