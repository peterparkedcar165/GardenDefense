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
    [SerializeField] private float holdRadius = 0.8f;          // ring radius used when no holdPoints are set

    private GhostFungusData GData => data as GhostFungusData;
    private GhostShroomlet[] _slots = new GhostShroomlet[0];
    private GameObject _skillIndicatorInstance;

    private int   ShroomletTarget => (GData?.baseShroomletCount ?? 1) + (GData?.path2ShroomletPerLevel ?? 1) * effectivePath2Level;
    private float FungalHealthMultiplier => (GData?.baseFungalHealthMultiplier ?? 1f) + (GData?.path3HealthMultiplierPerLevel ?? 0.2f) * effectivePath3Level;
    private float FungalAttackMultiplier => (GData?.baseFungalAttackMultiplier ?? 1f) + (GData?.path3AttackMultiplierPerLevel ?? 0.2f) * effectivePath3Level;
    private float FungalMoveSlow         => (GData?.baseFungalMoveSlow ?? 0.1f) + (GData?.path3MoveSlowPerLevel ?? 0.05f) * effectivePath3Level;

    // shroomlet core stats: base + per level (path 1 for attack, path 2 for health) + magic power.
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

    private Vector3 HoldPosition(int slot, int total)
    {
        if (holdPoints != null && holdPoints.Length > 0 && holdPoints[slot % holdPoints.Length] != null)
            return holdPoints[slot % holdPoints.Length].position;
        float angle = (total > 0 ? (360f / total) * slot : 0f) * Mathf.Deg2Rad;
        return transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * holdRadius;
    }

    private GhostShroomlet SpawnShroomletAt(int slot, int total)
    {
        if (shroomletPrefab == null) return null;
        Vector3 pos = HoldPosition(slot, total);
        GameObject go = Instantiate(shroomletPrefab, pos, Quaternion.identity);
        GhostShroomlet shroomlet = go.GetComponent<GhostShroomlet>();
        if (shroomlet == null) { Destroy(go); return null; }
        shroomlet.Configure(this, pos,
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
        // shave a flat amount off the shroomlet spawn cooldown per level
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

    public override string GetName() => $"<b><color=#B0E0E6>{(data != null ? data.displayName : "Ghost Fungus")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a haunted summoner that fires ice bolts, raises Ghost Shroomlets to guard the lane, and can turn an insect against its own.";

    public override string GetAttackDescription() =>
        $"Fires a bolt of ice dealing <color=green><b>{attackDamage:F0}</b></color> <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        $"Conjures a Ghost Shroomlet every <color=green><b>{passiveCooldown:F1}s</b></color> (up to <color=green><b>{ShroomletTarget}</b></color>) that holds position until an enemy comes into sight, then engages with <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> attacks dealing <color=green><b>{ShroomletAttackDamage:F0}</b></color> damage (<color=green><b>{ShroomletHealth:F0}</b></color> HP).";

    public override string GetSkillDesription() =>
        $"Send a spectral wave, inflicting <color=#00FFFF>Fungal Hypnosis</color> on the first insect hit, which permanently turns it friendly, and granting it:\n\n" +
        $"<color=green><b>+{FungalHealthMultiplier * 100f:F0}%</b></color> Max Health\n" +
        $"<color=green><b>+{FungalAttackMultiplier * 100f:F0}%</b></color> Attack Damage\n" +
        $"<color=green><b>-{FungalMoveSlow * 100f:F0}%</b></color> Movement Speed";

    public override string GetPath1Description()
    {
        float adpl  = GData?.path1AttackDamagePerLevel ?? 4f;
        float sadpl = GData?.shroomletAttackDamagePerLevel ?? 2f;
        float saspl = GData?.shroomletAttackSpeedPerLevel  ?? 0.05f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Shroomlet Attack Damage by <color=green><b>{sadpl:F0}</b></color> per level. [<color=green><b>+{sadpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Shroomlet Attack Speed by <color=green><b>{saspl:F2}</b></color> per level. [<color=green><b>+{saspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        int   spl  = GData?.path2ShroomletPerLevel ?? 1;
        float shpl = GData?.shroomletHealthPerLevel ?? 10f;
        float cdpl = GData?.path2SpawnCooldownReducerPerLevel ?? 0.5f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Conjure <color=green><b>{spl}</b></color> additional Ghost Shroomlet per level. [<color=green><b>+{spl * effectivePath2Level}</b></color>]\n\n" +
               $"Increase Shroomlet Health by <color=green><b>{shpl:F0}</b></color> per level. [<color=green><b>+{shpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Reduce spawn cooldown by <color=green><b>{cdpl:F1}s</b></color> per level. [<color=green><b>-{cdpl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float hpl  = GData?.path3HealthMultiplierPerLevel ?? 0.2f;
        float apl  = GData?.path3AttackMultiplierPerLevel ?? 0.2f;
        float mpl  = GData?.path3MoveSlowPerLevel ?? 0.05f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Increase <color=green>Max Health</color> bonus by <color=green><b>{hpl * 100f:F0}%</b></color> per level. [<color=green><b>+{hpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green>Attack Damage</color> bonus by <color=green><b>{apl * 100f:F0}%</b></color> per level. [<color=green><b>+{apl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase Movement Speed reduction by <color=green><b>{mpl * 100f:F0}%</b></color> per level. [<color=green><b>+{mpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
