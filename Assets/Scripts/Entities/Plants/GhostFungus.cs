using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

// a ghostly summoner-shooter. it fires ice physical bolts, conjures Ghost Shroomlets that hold
// and engage enemies, and its skill turns an insect with Fungal Hypnosis. emits a faint light
public class GhostFungus : Shooter
{
    [SerializeField] private GameObject shroomletPrefab;       // a GhostShroomlet prefab
    [SerializeField] private GameObject hypnosisWavePrefab;    // a GhostHypnosisWave prefab
    [SerializeField] private GameObject skillIndicatorPrefab;  // aim indicator for the skill direction
    [SerializeField] private Transform[] holdPoints;           // fixed posts for shroomlets (optional)
    [SerializeField] private Transform shroomletCenter;        // center of the shroomlet ring (defaults to the fungus)
    [SerializeField] private float holdRadius = 0.8f;          // ring radius used when no holdPoints are set

    private GhostFungusData GData => data as GhostFungusData;
    private GhostShroomlet[] _slots = new GhostShroomlet[0];
    private GameObject _skillIndicatorInstance;
    private Vector3 _formationCenter;        // runtime override set by the relocate button
    private bool _hasFormationOverride;

    private int   ShroomletTarget => (GData?.baseShroomletCount ?? 1) + (GData?.path2ShroomletPerLevel ?? 1) * effectivePath2Level;
    private float FungalHealthMultiplier => (GData?.baseFungalHealthMultiplier ?? 1f) + (GData?.path3HealthMultiplierPerLevel ?? 0.2f) * effectivePath3Level + (GData?.fungalHealthMP ?? 0f) * magicPower / 100f;
    private float FungalAttackMultiplier => (GData?.baseFungalAttackMultiplier ?? 1f) + (GData?.path3AttackMultiplierPerLevel ?? 0.2f) * effectivePath3Level + (GData?.fungalAttackMP ?? 0f) * magicPower / 100f;
    private float FungalMoveSlow         => (GData?.baseFungalMoveSlow ?? 0.1f) + (GData?.path3MoveSlowPerLevel ?? 0.05f) * effectivePath3Level;

    // shroomlet core stats: base +/Lvl. (path 1 for attack, path 2 for health) + magic power.
    // public so live shroomlets can read them each frame and update as the fungus is upgraded
    public float ShroomletAttackDamage => (GData?.shroomletBaseAttackDamage ?? 8f)  + (GData?.shroomletAttackDamagePerLevel ?? 2f)   * effectivePath1Level + (GData?.shroomletAttackDamageMP ?? 0f) * magicPower;
    public float ShroomletAttackSpeed  => (GData?.shroomletBaseAttackSpeed ?? 1f)   + (GData?.shroomletAttackSpeedPerLevel ?? 0.05f) * effectivePath1Level + (GData?.shroomletAttackSpeedMP ?? 0f)  * magicPower;
    public float ShroomletHealth       => (GData?.shroomletBaseHealth ?? 50f)       + (GData?.shroomletHealthPerLevel ?? 10f)        * effectivePath2Level + (GData?.shroomletHealthMP ?? 0f)       * magicPower;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        passiveCooldownTimer = passiveCooldown;   // the spawn bar fills before the first shroomlet
    }

    private bool _lightColored = false;

    public override void UpdateStats()
    {
        // the faint light reaches exactly as far as her attack range
        baseLightEmissionRange = baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier);
        base.UpdateStats();

        if (!_lightColored)
        {
            Light2D light = GetComponentInChildren<Light2D>();
            if (light != null)
            {
                light.color = new Color(0.05f, 0.65f, 1f);   // same as the Glowshroom
                _lightColored = true;
            }
        }
    }

    // keep the spawn bar visible even while it sits full waiting for a shroomlet slot to open
    protected override bool GetPassiveBarVisible() => passiveCooldown > 0f;

    protected override void Update()
    {
        base.Update();   // Shooter handles the ice bolt attack
        UpdateShroomlets();
        UpdateSkillIndicator();
    }

    // the passive cooldown (and its bar) gates shroomlet spawns: one per fill. when the bar is full
    // it spawns into the first empty hold-point slot and refills. at the cap it just holds full,
    // waiting for a shroomlet to die before the next spawn
    private void UpdateShroomlets()
    {
        int target = ShroomletTarget;
        if (holdPoints != null && holdPoints.Length > 0) target = Mathf.Min(target, holdPoints.Length);
        EnsureSlots(target);

        int alive = 0;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null || !_slots[i].IsAlive) { _slots[i] = null; continue; }
            if (i < target) alive++;
        }

        if (alive >= target) return;            // cap full: the ready bar waits for a slot to open
        if (passiveCooldownTimer > 0f) return;  // bar still filling

        for (int i = 0; i < target; i++)
        {
            if (_slots[i] != null && _slots[i].IsAlive) continue;
            _slots[i] = SpawnShroomletAt(i, target);
            passiveCooldownTimer += passiveCooldown;   // consume the bar; the next spawn waits a full fill
            break;                                      // only one shroomlet per fill
        }
    }

    private void EnsureSlots(int n)
    {
        if (_slots.Length >= n) return;
        var newSlots = new GhostShroomlet[n];
        _slots.CopyTo(newSlots, 0);
        _slots = newSlots;
    }

    // Burgeon command: the info panel shows a relocate button for this plant
    public override bool CommandsMinions => true;

    // move the shroomlet ring to an aimed point (clamped to range inside HoldPosition) and tell every
    // live shroomlet to drop combat, walk to its new post, then resume hunting
    public override void RelocateMinionFormation(Vector3 worldPoint)
    {
        _formationCenter = worldPoint;
        _hasFormationOverride = true;

        int target = ShroomletTarget;
        if (holdPoints != null && holdPoints.Length > 0) target = Mathf.Min(target, holdPoints.Length);
        for (int i = 0; i < _slots.Length && i < target; i++)
            if (_slots[i] != null && _slots[i].IsAlive)
                _slots[i].RelocateTo(HoldPosition(i, target));
    }

    private Vector3 HoldPosition(int slot, int total)
    {
        if (holdPoints != null && holdPoints.Length > 0 && holdPoints[slot % holdPoints.Length] != null)
            return holdPoints[slot % holdPoints.Length].position;

        // the ring center is clamped to within the fungus' attack range; placed beyond, it snaps to the edge
        Vector3 center = _hasFormationOverride ? _formationCenter
                       : shroomletCenter != null ? shroomletCenter.position
                       : transform.position;
        Vector3 offset = center - transform.position;
        if (offset.magnitude > attackRange)
            center = transform.position + offset.normalized * attackRange;

        float angle = (total > 0 ? (360f / total) * slot : 0f) * Mathf.Deg2Rad;
        return center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * holdRadius;
    }

    private GhostShroomlet SpawnShroomletAt(int slot, int total)
    {
        if (shroomletPrefab == null) return null;
        Vector3 holdPos = HoldPosition(slot, total);
        // spawn on top of the fungus, then let it walk out to its hold point
        GameObject go = Instantiate(shroomletPrefab, transform.position, Quaternion.identity);
        GhostShroomlet shroomlet = go.GetComponent<GhostShroomlet>();
        if (shroomlet == null) { Destroy(go); return null; }
        shroomlet.Configure(this, holdPos,
            ShroomletAttackDamage,
            ShroomletHealth,
            ShroomletAttackSpeed,
            GData?.shroomletAttackRange ?? 0.5f,
            GData?.shroomletDetectionRange ?? 2.5f,
            GData?.shroomletMoveSpeed ?? 1.5f);
        return shroomlet;
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        GhostBoltProjectile bolt = proj.GetComponent<GhostBoltProjectile>();
        if (bolt != null)
        {
            bolt.SetTarget(FindTarget());
            bolt.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        if (_skillIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
        if (skillIndicatorPrefab != null)
        {
            _skillIndicatorInstance = Instantiate(skillIndicatorPrefab, transform.position, Quaternion.identity);
            SpriteRenderer sr = _skillIndicatorInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;   // hidden until the first UpdateSkillIndicator
        }
    }

    // a rectangle pivoted at the fungus, pointing at the mouse, spanning the wave's path
    private void UpdateSkillIndicator()
    {
        if (_skillIndicatorInstance == null) return;

        if (SkillTargetingManager.instance == null || !SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(_skillIndicatorInstance);
            _skillIndicatorInstance = null;
            return;
        }

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 mouseWorld  = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Camera.main.nearClipPlane));
        mouseWorld.z = 0f;
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        float length = GData?.skillTravelDistance ?? 12f;
        float width  = GData?.skillWaveWidth ?? 1.5f;
        Vector3 center = transform.position + (Vector3)(dir * length * 0.5f);   // wave travels forward from the fungus
        _skillIndicatorInstance.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
        _skillIndicatorInstance.transform.localScale = new Vector3(length, width, 1f);
        SpriteRenderer sr = _skillIndicatorInstance.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = true;
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (_skillIndicatorInstance != null) { Destroy(_skillIndicatorInstance); _skillIndicatorInstance = null; }
        skillCooldownTimer = skillCooldown;
        if (hypnosisWavePrefab == null) return;
        Vector2 dir = ((Vector2)position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject obj = Instantiate(hypnosisWavePrefab, transform.position, Quaternion.identity);
        obj.GetComponent<GhostHypnosisWave>()?.Initialize(
            transform.position, dir,
            GData?.skillWaveSpeed ?? 14f, GData?.skillWaveWidth ?? 1.5f, GData?.skillThickness ?? 1f,
            GData?.skillTravelDistance ?? 12f, this,
            FungalHealthMultiplier, FungalAttackMultiplier, FungalMoveSlow);
    }

    public override void OnPath1Upgrade(int level)
    {
        // path 1 raises the fungus' own bolt damage; shroomlet attack damage/speed scale off
        // effectivePath1Level automatically via ShroomletAttackDamage / ShroomletAttackSpeed
        baseAttackDamage = data.baseAttackDamage + (GData?.path1AttackDamagePerLevel ?? 4f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        // shave a flat amount off the shroomlet spawn cooldown/Lvl.
        passiveCooldownAdder = -(GData?.path2SpawnCooldownReducerPerLevel ?? 0.5f) * level;

        // force one immediate spawn through the normal pipeline, then resume the timer where it was
        float saved = passiveCooldownTimer;
        passiveCooldownTimer = 0f;     // ready: UpdateShroomlets fills the next free slot
        UpdateShroomlets();
        passiveCooldownTimer = saved;  // restore the bar's progress (UpdateShroomlets had refilled it)
    }
    public override void OnPath3Upgrade(int level) { }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_skillIndicatorInstance != null) Destroy(_skillIndicatorInstance);
        foreach (GhostShroomlet s in _slots)
            if (s != null && s.IsAlive) s.Kill();
    }

    public override string GetName() => $"<b><color=#00FFFF>{(data != null ? data.displayName : "Ghost Fungus")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a haunted summoner that fires ice bolts, raises Ghost Shroomlets to guard the lane, and can turn an insect against its own.";

    public override string GetAttackDescription() =>
        $"Fires a bolt of ice dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        $"Conjures a Ghost Shroomlet every <color=green><b>{passiveCooldown:F1}s</b></color> (up to <color=green><b>{ShroomletTarget}</b></color>) that holds position until an enemy comes into sight, then engages with {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} attacks dealing <color=green><b>{ShroomletAttackDamage:F0}</b></color> damage (<color=green><b>{ShroomletHealth:F0}</b></color> HP).";

    private static string SignedPct(float pct) =>
        pct >= 0f ? $"<color=green><b>+{pct:F0}%</b></color>" : $"<color=red><b>{pct:F0}%</b></color>";

    public override string GetSkillDesription() =>
        $"Send a spectral wave, inflicting <color=#00FFFF>Fungal Hypnosis</color> on the first insect hit, which permanently turns it friendly, and granting it:\n\n" +
        $"{SignedPct(FungalHealthMultiplier * 100f)} [<color=#FFB6C1><b>+{(GData?.fungalHealthMP ?? 0f) * magicPower:F0}%</b></color>] Max Health\n" +
        $"{SignedPct(FungalAttackMultiplier * 100f)} [<color=#FFB6C1><b>+{(GData?.fungalAttackMP ?? 0f) * magicPower:F0}%</b></color>] Attack Damage\n" +
        $"{SignedPct(-FungalMoveSlow * 100f)} Movement Speed";

    public override string GetPath1Description(bool details = false)
    {
        float adpl  = GData?.path1AttackDamagePerLevel      ?? 4f;
        float sadpl = GData?.shroomletAttackDamagePerLevel  ?? 2f;
        float saspl = GData?.shroomletAttackSpeedPerLevel   ?? 0.05f;
        string desc = details
            ? $"Fires a bolt of ice dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the first insect hit."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Shroomlet Attack Damage</b></color> by <color=green><b>{sadpl:F0}</b></color> per level. [<color=green><b>+{sadpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Shroomlet Attack Speed</b></color> by <color=green><b>{saspl:F2}</b></color> per level. [<color=green><b>+{saspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(effectivePath1Level)}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        int   spl  = GData?.path2ShroomletPerLevel              ?? 1;
        float shpl = GData?.shroomletHealthPerLevel              ?? 10f;
        float cdpl = GData?.path2SpawnCooldownReducerPerLevel    ?? 0.5f;
        float sadpl2 = GData?.shroomletAttackDamagePerLevel ?? 2f;
        string desc = details
            ? $"Conjures a Ghost Shroomlet every <color=green><b>[({data.basePassiveCooldown:F1}) - ({cdpl:F1}/Lvl.)]</b></color> seconds (up to <color=green><b>[({GData?.baseShroomletCount ?? 1}) + ({spl}/Lvl.)]</b></color>) that holds position until an enemy comes into sight, then engages with {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} attacks dealing <color=green><b>[({GData?.shroomletBaseAttackDamage ?? 8f:F0}) + ({sadpl2:F0}/Lvl.)]</b></color> damage (<color=green><b>[({GData?.shroomletBaseHealth ?? 50f:F0}) + ({shpl:F0}/Lvl.)]</b></color> HP)."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Conjure <color=green><b>{spl}</b></color> additional Ghost Shroomlet per level. [<color=green><b>+{spl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase Shroomlet Health by <color=green><b>{shpl:F0}</b></color> per level. [<color=green><b>+{shpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Reduce spawn cooldown by <color=green><b>{cdpl:F1}</b></color> seconds per level. [<color=green><b>-{cdpl * effectivePath2Level:F1}</b></color>]\n\n" +
               $"{Level5Section(effectivePath2Level)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float hpl = GData?.path3HealthMultiplierPerLevel ?? 0.2f;
        float apl = GData?.path3AttackMultiplierPerLevel ?? 0.2f;
        float mpl = GData?.path3MoveSlowPerLevel         ?? 0.05f;
        float hmp = GData?.fungalHealthMP                ?? 0f;
        float amp = GData?.fungalAttackMP                ?? 0f;
        float hBase = GData?.baseFungalHealthMultiplier  ?? 1f;
        float aBase = GData?.baseFungalAttackMultiplier  ?? 1f;
        float mBase = GData?.baseFungalMoveSlow          ?? 0.1f;
        string desc = details
            ? $"Send a spectral wave, inflicting <color=#00FFFF>Fungal Hypnosis</color> on the first insect hit, which permanently turns it friendly, and granting it:\n\n" +
              $"<color=green><b>[+{hBase * 100f:F0}% + ({hpl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{hmp:F0}% Magic Power</color>]</b></color> Max Health\n" +
              $"<color=green><b>[+{aBase * 100f:F0}% + ({apl * 100f:F0}%/Lvl.) + <color=#FFB6C1>{amp:F0}% Magic Power</color>]</b></color> Attack Damage\n" +
              $"<color=red><b>[-{mBase * 100f:F0}% - ({mpl * 100f:F0}%/Lvl.)]</b></color> Movement Speed"
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green>Max Health</color> bonus by <color=green><b>{hpl * 100f:F0}%</b></color> per level. [<color=green><b>+{hpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green>Attack Damage</color> bonus by <color=green><b>{apl * 100f:F0}%</b></color> per level. [<color=green><b>+{apl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase Movement Speed reduction by <color=green><b>{mpl * 100f:F0}%</b></color> per level. [<color=green><b>+{mpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(effectivePath3Level)}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
