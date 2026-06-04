using UnityEngine;

// a ghostly summoner-shooter. it fires ice physical bolts, conjures Ghost Shroomlets that hold
// and engage enemies, and its skill turns an insect with Fungal Hypnosis. emits a faint light
public class GhostFungus : Shooter
{
    [SerializeField] private GameObject shroomletPrefab;     // a GhostShroomlet prefab
    [SerializeField] private GameObject hypnosisWavePrefab;  // a GhostHypnosisWave prefab
    [SerializeField] private Transform[] holdPoints;         // fixed posts for shroomlets (optional)
    [SerializeField] private float holdRadius = 0.8f;        // ring radius used when no holdPoints are set

    private GhostFungusData GData => data as GhostFungusData;
    private GhostShroomlet[] _slots = new GhostShroomlet[0];
    private float[] _slotTimer = new float[0];

    private int   ShroomletTarget => (GData?.baseShroomletCount ?? 1) + (GData?.path2ShroomletPerLevel ?? 1) * effectivePath2Level;
    private float FungalSlow       => (GData?.baseFungalSlowPercent ?? 0.2f) + (GData?.path3SlowPerLevel ?? 0.03f) * effectivePath3Level;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    protected override void Update()
    {
        base.Update();   // Shooter handles the ice bolt attack
        UpdateShroomlets();
    }

    // each hold-point slot keeps one shroomlet alive, respawning it at that post after a delay
    private void UpdateShroomlets()
    {
        int target = ShroomletTarget;
        if (holdPoints != null && holdPoints.Length > 0) target = Mathf.Min(target, holdPoints.Length);
        EnsureSlots(target);

        for (int i = 0; i < target; i++)
        {
            if (_slots[i] != null && _slots[i].IsAlive) continue;
            _slots[i] = null;
            _slotTimer[i] -= Time.deltaTime;
            if (_slotTimer[i] > 0f) continue;
            _slotTimer[i] = GData?.shroomletRespawnDelay ?? 5f;
            _slots[i] = SpawnShroomletAt(i, target);
        }
    }

    private void EnsureSlots(int n)
    {
        if (_slots.Length >= n) return;
        var newSlots = new GhostShroomlet[n];
        var newTimer = new float[n];
        _slots.CopyTo(newSlots, 0);
        _slotTimer.CopyTo(newTimer, 0);
        for (int i = _slotTimer.Length; i < n; i++) newTimer[i] = GData?.shroomletSpawnInterval ?? 2f; // first fill waits
        _slots = newSlots;
        _slotTimer = newTimer;
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
            attackDamage * (GData?.shroomletDamageFraction ?? 0.6f),
            maxHealth    * (GData?.shroomletHealthFraction ?? 0.5f),
            GData?.shroomletAttackSpeed ?? 1f,
            GData?.shroomletAttackRange ?? 0.5f);
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
        SkillTargetingManager.instance.BeginTargeting(0f, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        if (hypnosisWavePrefab == null) return;
        Vector2 dir = ((Vector2)position - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject obj = Instantiate(hypnosisWavePrefab, transform.position, Quaternion.identity);
        obj.GetComponent<GhostHypnosisWave>()?.Initialize(
            transform.position, dir,
            GData?.skillWaveSpeed ?? 14f, GData?.skillWaveWidth ?? 1.5f, GData?.skillThickness ?? 1f,
            GData?.skillTravelDistance ?? 12f, this,
            GData?.fungalHealthMultiplier ?? 1f, GData?.fungalAttackMultiplier ?? 1f,
            FungalSlow, GData?.fungalSlowDuration ?? 2f);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (GData?.path1AttackDamagePerLevel ?? 4f)  * level;
        baseAttackSpeed  = data.baseAttackSpeed  + (GData?.path1AttackSpeedPerLevel  ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        foreach (GhostShroomlet s in _slots)
            if (s != null && s.IsAlive) s.Kill();
    }

    public override string GetName() => $"<b><color=#B0E0E6>{(data != null ? data.displayName : "Ghost Fungus")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is a haunted summoner that fires ice bolts, raises Ghost Shroomlets to guard the lane, and can turn an insect against its own.";

    public override string GetAttackDescription() =>
        $"Fires a bolt of ice dealing <color=green><b>{attackDamage:F0}</b></color> <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        $"Conjures up to <color=green><b>{ShroomletTarget}</b></color> Ghost Shroomlet{(ShroomletTarget == 1 ? "" : "s")} that hold position until an enemy comes into sight, then engage with <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> attacks. They inherit her stats and respawn <color=green><b>{(GData?.shroomletRespawnDelay ?? 5f):F0}s</b></color> after dying.";

    public override string GetSkillDesription() =>
        $"Aim a direction to send a spectral wave that inflicts <color=#B266FF>Fungal Hypnosis</color> on the first insect hit, permanently turning it friendly. Its <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> strikes are credited to the {GetName()} and slow enemies' attack speed by <color=green><b>{FungalSlow * 100f:F0}%</b></color>.";

    public override string GetPath1Description()
    {
        float adpl = GData?.path1AttackDamagePerLevel ?? 4f;
        float aspl = GData?.path1AttackSpeedPerLevel  ?? 0.05f;
        return $"Attack:\n\n{GetAttackDescription()}\n\n" +
               $"Increase Attack Damage by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Attack Speed by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>";
    }

    public override string GetPath2Description()
    {
        int spl = GData?.path2ShroomletPerLevel ?? 1;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Conjure <color=green><b>{spl}</b></color> additional Ghost Shroomlet per level. [<color=green><b>+{spl * effectivePath2Level}</b></color>]\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>";
    }

    public override string GetPath3Description()
    {
        float spl = GData?.path3SlowPerLevel ?? 0.03f;
        return $"Skill:\n\n{GetSkillDesription()}\n\n" +
               $"Increase the attack speed slow by <color=green><b>{spl * 100f:F0}%</b></color> per level. [<color=green><b>+{spl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>";
    }
}
