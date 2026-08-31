using UnityEngine;
using UnityEngine.InputSystem;

public class Carrot : Shooter
{
    [SerializeField] private GameObject pillarVisualPrefab;   // optional per square visual left by the furrow
    [SerializeField] private GameObject skillIndicatorPrefab; // rectangle sprite shown while aiming the furrow
    [SerializeField] private GameObject psionicCarrotPrefab;  // Psionic Bond's own projectile

    private GameObject _skillIndicatorInstance;

    private CarrotData GData => data as CarrotData;

    public int   TargetSwitchBonusHits => GData?.path1TargetSwitchBonusHits ?? 1;

    public float VisualFadeIn         => GData?.visualFadeIn ?? 0.1f;
    public float VisualHold           => GData?.visualHold ?? 0.7f;
    public float VisualFadeOut        => GData?.visualFadeOut ?? 0.5f;
    public float VisualPositionJitter => GData?.visualPositionJitter ?? 0.05f;

    public float SquareRadius          => GData?.pillarRadius ?? 0.9f;
    public float SquareWidthMultiplier => 1f + (GData?.path3WidthPerLevel ?? 0.1f) * effectivePath3Level;
    public int   CarrotCount     => (GData?.carrotCountBase ?? 3) + (GData?.carrotsPerLevel ?? 1) * effectivePath3Level;
    public float SkillDamageFlat => (GData?.skillBaseDamage ?? 40f) + (GData?.path3SkillDamagePerLevel ?? 8f) * effectivePath3Level;
    public float SkillDamageMP   => skillDamageMultiplier * magicPower;
    public float SkillDamage     => SkillDamageFlat + SkillDamageMP;

    // Psionic Bond: a permanent link to a chosen Shooter plant, resolved live from the tile it
    // sits on (like Calendula's auto-cast target) so a bonded plant that dies and revives - a
    // brand new instance - gets automatically picked back up instead of leaving a stale link
    private Tile boundTile;
    private Shooter boundShooter;
    private float psionicCooldownTimer;
    private Plant _bondHighlighted;
    private static readonly Color BondHighlightColor = new Color(1f, 0.45f, 0.75f);

    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => boundTile != null;
    public override string AutoCastLabel => "Bond";

    public float PsionicCooldown    => Mathf.Max(0.5f, (GData?.psionicCooldownBase ?? 3f) - (GData?.psionicCooldownReductionPerLevel ?? 0.3f) * effectivePath2Level);
    public float PsionicDamageFlat  => (GData?.psionicDamageBase ?? 30f) + (GData?.psionicDamagePerLevel ?? 20f) * effectivePath2Level;
    public float PsionicDamageMP    => (GData?.psionicDamageMPScaling ?? 0.5f) * magicPower;
    public float PsionicDamage      => PsionicDamageFlat + PsionicDamageMP;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        // ground element perk, placeable on any non water non obstacle tile
        allowedTiles = new TileType[]
        {
            TileType.Grass, TileType.Dirt, TileType.Potted, TileType.Cave, TileType.Sand, TileType.Snow
        };
    }

    // Path2 max, mirrored on the bonded plant's side by PsionicBondEffect: while actively
    // bonded, both sides gain the same Attack Speed / Passive Cooldown Reduction bonus
    private const float BondMaxLevelBonus = 0.15f;

    public override void UpdateStats()
    {
        bool grantsBondBonus = IsPath2Maxed && boundShooter != null;
        float bonus = grantsBondBonus ? BondMaxLevelBonus : 0f;
        attackSpeedMultiplier += bonus;
        passiveCooldownReductionMultiplier += bonus;
        base.UpdateStats();
        attackSpeedMultiplier -= bonus;
        passiveCooldownReductionMultiplier -= bonus;
    }

    protected override void Update()
    {
        base.Update();
        UpdateSkillIndicator();

        if (psionicCooldownTimer > 0f)
            psionicCooldownTimer -= Time.deltaTime;

        ResyncBond();
        UpdateBondHighlight();
    }

    // while this Carrot is selected, outline its bonded target in pink so it's obvious at a
    // glance which plant is linked (mirrors Calendula's own auto-cast target highlight)
    private void UpdateBondHighlight()
    {
        Plant desired = IsSelected ? boundShooter : null;
        if (_bondHighlighted != null && _bondHighlighted != desired)
            _bondHighlighted.ClearHighlight();
        if (desired != null)
            desired.SetHighlight(BondHighlightColor);
        _bondHighlighted = desired;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Unbind();
        _bondHighlighted?.ClearHighlight();
    }

    // click Bond to pick a Shooter plant to link with, click again to break the bond.
    // clicking anything that isn't a Shooter (including Carrot itself) is silently rejected and
    // stays in targeting mode - spamming clicks on invalid plants never confirms anything
    public override void ToggleAutoCast()
    {
        if (boundTile != null)
        {
            boundTile = null;
            return;
        }
        SkillTargetingManager.instance.BeginPlantTargeting(OnBondTargetConfirmed, this);
    }

    private void OnBondTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return; // cancelled
        if (targetPlant is not Shooter || targetPlant == this)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnBondTargetConfirmed, this);
            return;
        }
        boundTile = targetPlant.occupiedTile;
    }

    // re-resolves whichever Shooter currently occupies boundTile every frame (cheap: a
    // reference compare, only doing real work when the occupant actually changed) and keeps
    // the OnFired subscription + visible Psionic Bond effect pointed at that live instance
    private void ResyncBond()
    {
        Shooter current = boundTile != null ? Plant.GetPlantOnTile(boundTile) as Shooter : null;

        bool boundShooterGone = boundShooter == null;
        if (!boundShooterGone && current == boundShooter) return;

        if (!boundShooterGone) Unbind();

        boundShooter = current;
        if (boundShooter != null)
        {
            boundShooter.OnFired += HandleBoundShooterFired;
            boundShooter.ApplyEffect(new PsionicBondEffect(boundShooter, this, this));
        }
    }

    private void Unbind()
    {
        if (boundShooter == null) return;
        boundShooter.OnFired -= HandleBoundShooterFired;
        // removes only this Carrot's own instance - PsionicBondEffect is source-stackable, so
        // other Carrots bonded to the same plant keep theirs untouched
        boundShooter.RemoveEffect<PsionicBondEffect>(this);
        boundShooter = null;
    }

    private void HandleBoundShooterFired(GameObject target)
    {
        if (psionicCooldownTimer > 0f) return;
        if (target == null || !IsAlive || IsStunned || IsChanneling) return;
        psionicCooldownTimer = PsionicCooldown;
        FirePsionicCarrot(target);
    }

    private void FirePsionicCarrot(GameObject target)
    {
        if (psionicCarrotPrefab == null || boundShooter == null) return;
        GameObject obj = Instantiate(psionicCarrotPrefab, transform.position, Quaternion.identity);
        PsionicCarrotProjectile proj = obj.GetComponent<PsionicCarrotProjectile>();
        if (proj == null)
        {
            Debug.LogWarning("Carrot: psionicCarrotPrefab has no PsionicCarrotProjectile component - check the prefab's script.");
            Destroy(obj);
            return;
        }
        proj.SetTarget(target);
        proj.Initialize(target.transform.position, PsionicDamage, projectileSpeed, maxRange,
            piercing, damageType, elementalType, this);
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = boundTile != null, targetTile = boundTile };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled || state.targetTile == null) return;
        boundTile = state.targetTile;
    }

    // Path1 max, called by both CarrotProjectile and PsionicCarrotProjectile so attack and
    // Psionic Carrot hits feed and benefit from the same stack. returns the multiplier the
    // caller should apply to the damage it's about to deal (based on stacks BEFORE this hit),
    // then adds/refreshes a stack for the next hit
    public float ApplyPsionicMark(Insect insect)
    {
        PsionicMarkEffect mark = insect.GetEffect<PsionicMarkEffect>(this);
        if (mark != null)
        {
            float multiplier = mark.DamageMultiplier;
            mark.AddStack();
            return multiplier;
        }
        insect.ApplyEffect(new PsionicMarkEffect(insect, this));
        return 1f;
    }

    // pushes an insect over a short time so the knockback reads as motion instead of a teleport.
    // runs on the plant because projectiles destroy themselves on impact
    public void PushInsect(Insect insect, Vector2 direction, float distance, float duration = 0.15f)
    {
        if (insect == null || !insect.IsAlive) return;
        StartCoroutine(PushRoutine(insect, direction.normalized, distance, duration));
    }

    private System.Collections.IEnumerator PushRoutine(Insect insect, Vector2 direction, float distance, float duration)
    {
        float moved = 0f;
        float speed = distance / duration;
        while (moved < distance)
        {
            if (insect == null || !insect.IsAlive) yield break;
            float step = Mathf.Min(speed * Time.deltaTime, distance - moved);
            Vector3 clamped = Insect.ClampStepAgainstObstacles(insect.transform.position, (Vector3)(direction * step));
            insect.transform.position += clamped;
            moved += step;
            yield return null;
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        CarrotProjectile proj = obj.GetComponent<CarrotProjectile>();
        if (proj == null) return;
        proj.SetTarget(FindTarget());
        proj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        if (_skillIndicatorInstance != null) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnSkillTargetConfirmed);
        if (skillIndicatorPrefab != null)
        {
            _skillIndicatorInstance = Instantiate(skillIndicatorPrefab, transform.position, Quaternion.identity);
            SpriteRenderer sr = _skillIndicatorInstance.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;   // hidden until the first aim update positions it
        }
    }

    // a rectangle pivoted at the plant, matching the exact footprint of the furrow,
    // aimed at the mouse while targeting. destroyed on confirm or cancel
    private void UpdateSkillIndicator()
    {
        if (_skillIndicatorInstance == null) return;

        if (!SkillTargetingManager.instance.IsTargeting)
        {
            Destroy(_skillIndicatorInstance);
            _skillIndicatorInstance = null;
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dir = ((Vector2)mouseWorld - (Vector2)transform.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float squareSize   = CarrotFurrow.VisualSquareSize(pillarVisualPrefab, SquareRadius * 2f) * SquareWidthMultiplier;
        float half         = squareSize * 0.5f;
        float startOffset  = GData?.pillarStartOffset ?? 1f;
        float frontEdge    = startOffset - half;
        float farEdge      = startOffset + squareSize * (CarrotCount - 1) + half;
        float length       = farEdge - frontEdge;

        _skillIndicatorInstance.transform.position = transform.position + (Vector3)(dir * (frontEdge + length * 0.5f));
        _skillIndicatorInstance.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        SpriteRenderer sr = _skillIndicatorInstance.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 spriteSize = sr.sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
                _skillIndicatorInstance.transform.localScale = new Vector3(length / spriteSize.x, squareSize / spriteSize.y, 1f);
            sr.enabled = true;
        }
    }

    private void OnSkillTargetConfirmed(Vector3 target)
    {
        skillCooldownTimer = skillCooldown;
        BeginChannel();

        Vector2 direction = ((Vector2)target - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        SpawnFurrow(direction);
    }

    private void SpawnFurrow(Vector2 direction)
    {
        GameObject waveObj = new GameObject("CarrotFurrow");
        waveObj.transform.position = transform.position;
        CarrotFurrow wave = waveObj.AddComponent<CarrotFurrow>();
        wave.Initialize(
            transform.position,
            direction,
            CarrotCount,
            GData?.pillarStartOffset ?? 1f,
            GData?.pillarInterval ?? 0.12f,
            SquareRadius,
            SquareWidthMultiplier,
            GData?.pillarHitboxMultiplier ?? 1.3f,
            SkillDamage,
            GData?.pillarKnockUpForce ?? 5f,
            this,
            pillarVisualPrefab);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (GData?.path1AttackDamagePerLevel ?? 23f) * level;
        baseAttackRange  = data.baseAttackRange  + (GData?.path1AttackRangePerLevel ?? 0.2f) * level;
        piercingAdder    = (GData?.path1PiercingPerLevel ?? 1) * level;
    }

    public override string GetName() => "<b><color=#ED9121>Carrot</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is an elderly root sage who commands the earth itself, hurling stone at its enemies and forging psionic links with its allies.";

    public override string GetAttackDescription() =>
        $"Hurls earth at the target, dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Forms a <color=#B266FF><b>Psionic Bond</b></color> with a chosen Shooter plant. Every time that plant fires, the {GetName()} also fires a " +
        $"<color=#B266FF><b>Psionic Carrot</b></color> at the same target, dealing <color=green><b>{PsionicDamageFlat:F0}</b></color> [<color=#FFB6C1><b>+{PsionicDamageMP:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage. " +
        $"Cooldown: <color=green><b>{PsionicCooldown:F1}s</b></color>.";

    public override string GetSkillDesription() =>
        $"Aim a direction. A furrow of churned earth plows from the {GetName()}, sprouting <color=green><b>{CarrotCount}</b></color> carrots in a line, each covering one square, " +
        $"striking each insect once for <color=green><b>{SkillDamageFlat:F0}</b></color> [<color=#FFB6C1><b>+{SkillDamageMP:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage. " +
        $"Insects struck are knocked up and pushed aside, away from the line of carrots.";

    public override string GetPath1Name() => "Upheaval";
    public override string GetPath2Name() => "Attunement";
    public override string GetPath3Name() => "Fault Line";

    public override string GetPath1Description(bool details = false)
    {
        float adpl  = GData?.path1AttackDamagePerLevel ?? 23f;
        float rngpl = GData?.path1AttackRangePerLevel ?? 0.2f;
        int   ppl   = GData?.path1PiercingPerLevel ?? 1;
        string desc = details
            ? $"Hurls earth at the target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rngpl:F2}</b></color> per level. [<color=green><b>+{rngpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Piercing</b></color> by <color=green><b>{ppl}</b></color> per level. [<color=green><b>+{ppl * effectivePath1Level}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Each hit against the same target increases damage by <color=green><b>{PsionicMarkEffect.DamagePerStack * 100f:F0}%</b></color>. Switching targets grants <color=green><b>+{TargetSwitchBonusHits}</b></color> additional hits.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float cdpl  = GData?.psionicCooldownReductionPerLevel ?? 0.3f;
        float dmgpl = GData?.psionicDamagePerLevel ?? 20f;
        string desc = details
            ? $"Forms a <color=#B266FF><b>Psionic Bond</b></color> with a chosen Shooter plant. Every time that plant fires, the {GetName()} also fires a <color=#B266FF><b>Psionic Carrot</b></color> at the same target, " +
              $"dealing <color=green><b>[({GData?.psionicDamageBase ?? 30f:F0}) + ({dmgpl:F0}/Lvl.) + <color=#FFB6C1>{(GData?.psionicDamageMPScaling ?? 0.5f) * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Decrease Psionic Carrot cooldown by <color=green><b>{cdpl:F1}s</b></color> per level. [<color=green><b>-{cdpl * effectivePath2Level:F1}s</b></color>]\n\n" +
               $"Increase Psionic Carrot damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"While bonded, both the {GetName()} and the bonded plant gain <color=green><b>{BondMaxLevelBonus * 100f:F0}%</b></color> Attack Speed and <color=green><b>{BondMaxLevelBonus * 100f:F0}%</b></color> Passive Cooldown Reduction.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        string desc = details
            ? $"Aim a direction. A furrow of churned earth plows from the {GetName()}, sprouting <color=green><b>[({GData?.carrotCountBase ?? 3}) + ({GData?.carrotsPerLevel ?? 1}/Lvl.)]</b></color> carrots in a line, each covering one square, " +
              $"striking each insect once for <color=green><b>[({GData?.skillBaseDamage ?? 40f:F0}) + ({GData?.path3SkillDamagePerLevel ?? 8f:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage. " +
              $"Insects struck are knocked up and pushed aside, away from the line of carrots."
            : GetSkillDesription();
        float wpl = GData?.path3WidthPerLevel ?? 0.1f;
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Carrots</b></color> by <color=green><b>{GData?.carrotsPerLevel ?? 1}</b></color> per level. [<color=green><b>+{(GData?.carrotsPerLevel ?? 1) * effectivePath3Level}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Damage</b></color> by <color=green><b>{GData?.path3SkillDamagePerLevel ?? 8f:F0}</b></color> per level. [<color=green><b>+{(GData?.path3SkillDamagePerLevel ?? 8f) * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase carrot width by <color=green><b>{wpl * 100f:F0}%</b></color> per level. [<color=green><b>+{wpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Each carrot further down the line deals <color=green><b>{CarrotFurrow.MaxLevelGrowthPerSegment * 100f:F0}%</b></color> more damage and grows <color=green><b>{CarrotFurrow.MaxLevelGrowthPerSegment * 100f:F0}%</b></color> larger than the last, calculated from the first carrot.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
