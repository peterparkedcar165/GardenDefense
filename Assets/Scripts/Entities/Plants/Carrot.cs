using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Carrot : Shooter
{
    [SerializeField] private GameObject eruptionVisualPrefab; // spawned at the struck insect's position
    [SerializeField] private GameObject pillarVisualPrefab;   // optional per square visual left by the furrow (skill)
    [SerializeField] private GameObject skillIndicatorPrefab; // rectangle sprite shown while aiming the furrow

    private GameObject _skillIndicatorInstance;
    private readonly List<Insect> _scratch = new List<Insect>();
    private static readonly DamageTag[] eruptTags = { DamageTag.Attack, DamageTag.AoE, DamageTag.CanHitBurrowed };

    private CarrotData GData => data as CarrotData;

    public float EruptionRadius       => (GData?.baseEruptionRadius ?? 1f) + (GData?.path1RadiusPerLevel ?? 0.15f) * effectivePath1Level;
    public float EruptionKnockUpForce => GData?.eruptionKnockUpForce ?? 6f;
    public float CountdownBonus       => (GData?.bondCountdownBonus ?? 0.3f) + (GData?.path2CountdownBonusPerLevel ?? 0.05f) * effectivePath2Level;

    public float SquareRadius          => GData?.pillarRadius ?? 0.9f;
    public float SquareWidthMultiplier => 1f + (GData?.path3WidthPerLevel ?? 0.1f) * effectivePath3Level;
    public int   CarrotCount     => (GData?.carrotCountBase ?? 3) + (GData?.carrotsPerLevel ?? 1) * effectivePath3Level;
    public float SkillDamageFlat => (GData?.skillBaseDamage ?? 40f) + (GData?.path3SkillDamagePerLevel ?? 8f) * effectivePath3Level;
    public float SkillDamageMP   => skillDamageMultiplier * magicPower;
    public float SkillDamage     => SkillDamageFlat + SkillDamageMP;

    public float VisualFadeIn         => GData?.visualFadeIn ?? 0.1f;
    public float VisualHold           => GData?.visualHold ?? 0.7f;
    public float VisualFadeOut        => GData?.visualFadeOut ?? 0.5f;
    public float VisualPositionJitter => GData?.visualPositionJitter ?? 0.05f;

    // Soil Bond: a permanent link to a chosen Shooter plant, resolved live from the tile it sits
    // on (like Calendula's auto-cast target) so a bonded plant that dies and revives - a brand
    // new instance - gets automatically picked back up instead of leaving a stale link
    private Tile boundTile;
    private Shooter boundShooter;
    private Plant _bondHighlighted;
    private static readonly Color BondHighlightColor = new Color(1f, 0.55f, 0f);

    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => boundTile != null;
    public override string AutoCastLabel => "Bond";

    // hidden baseline: Carrot can always sense and strike burrowed insects, and its eruption
    // knocks them up on any hit (see Shoot). unrelated to Soil Bond's level
    public override bool DetectsBurrowed => true;

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

    // shows the attack cooldown bar (like Aura/Gloriosa) under the health bar, alongside the
    // skill bar - Carrot has no passive-cooldown bar of its own since Soil Bond isn't cooldown
    // based, so this is the only bar occupying that slot
    protected override bool GetAttackBarVisible() => attackCooldown > 0f;

    protected override void Update()
    {
        base.Update();
        UpdateSkillIndicator();
        ResyncBond();
        UpdateBondHighlight();
    }

    // while this Carrot is selected, outline its bonded target in orange and show its range
    // circle too, so it's obvious at a glance which plant is linked and how far it reaches
    // (mirrors Calendula's own auto-cast target highlight)
    private void UpdateBondHighlight()
    {
        Plant desired = IsSelected ? boundShooter : null;
        if (_bondHighlighted != null && _bondHighlighted != desired)
        {
            _bondHighlighted.ClearHighlight();
            _bondHighlighted.ShowExternalRangeCircle(false);
        }
        if (desired != null)
        {
            desired.SetHighlight(BondHighlightColor);
            desired.ShowExternalRangeCircle(true);
        }
        _bondHighlighted = desired;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Unbind();
        _bondHighlighted?.ClearHighlight();
        _bondHighlighted?.ShowExternalRangeCircle(false);
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
    // the Entity.OnHit subscription + visible Soil Bond effect pointed at that live instance
    private void ResyncBond()
    {
        Shooter current = boundTile != null ? Plant.GetPlantOnTile(boundTile) as Shooter : null;

        bool boundShooterGone = boundShooter == null;
        if (!boundShooterGone && current == boundShooter) return;

        if (!boundShooterGone) Unbind();

        boundShooter = current;
        if (boundShooter != null)
        {
            Entity.OnHit += HandleBoundShooterHit;
            boundShooter.ApplyEffect(new SoilBondEffect(boundShooter, this, this));
        }
    }

    private void Unbind()
    {
        if (boundShooter == null) return;
        Entity.OnHit -= HandleBoundShooterHit;
        // removes only this Carrot's own instance - SoilBondEffect is source-stackable, so
        // other Carrots bonded to the same plant keep theirs untouched
        boundShooter.RemoveEffect<SoilBondEffect>(this);
        boundShooter = null;
    }

    // every time the bonded plant's attack actually lands (not just when it fires), Carrot's own
    // attack countdown ticks forward - Entity.OnHit fires once per insect struck, so a piercing
    // shooter hitting several insects with one shot advances the countdown that many times.
    // requires the Attack tag specifically, so on-hit procs riding along on the same hit (Floral
    // Glow, Ablaze, Talon Focus, etc.) don't also advance the countdown - only the bonded plant's
    // actual attack damage counts
    private void HandleBoundShooterHit(EntityEventData data)
    {
        if (data.source != boundShooter) return;
        if (data.tags == null || !System.Array.Exists(data.tags, t => t == DamageTag.Attack)) return;
        attackCooldownTimer = Mathf.Min(attackCooldown, attackCooldownTimer + CountdownBonus);
    }

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = boundTile != null, targetTile = boundTile };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled || state.targetTile == null) return;
        boundTile = state.targetTile;
    }

    // the eruption strikes at the target's actual base (ground contact point), not its aim point
    // (which Shooter normally offsets upward for projectiles to visually land on the body) - and
    // skips lead prediction entirely, since the eruption is instant rather than something that
    // travels and needs to be aimed ahead of a moving target
    protected override Vector3 PredictTargetPosition(GameObject target)
    {
        Insect insect = target.GetComponent<Insect>();
        return insect != null ? insect.transform.position : target.transform.position;
    }

    // Soil Bond: while linked, an insect is a valid target if it's within Carrot's own circle OR
    // anything the bonded plant could itself target - deferred to the ally's own CanReachInsect
    // rather than re-deriving a plain circle from its attackRange, so a plant with exotic reach
    // (e.g. Nerium Oleander's sprout-chain targeting) is still seen correctly through the bond
    protected override bool IsWithinAttackRange(Insect insect, float distance)
    {
        if (distance <= attackRange) return true;
        return boundShooter != null && boundShooter.IsAlive && boundShooter.CanReachInsect(insect);
    }

    // pushes an insect over a short time so the knockback reads as motion instead of a teleport.
    // runs on the plant because projectiles/instant hits destroy themselves on impact
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

    // erupts a chunk of earth at the target's (predicted) position: strikes it and everything
    // else within EruptionRadius. the crit roll happens exactly once per attack and is shared by
    // every insect it hits - either all of them crit together or none do. burrowed insects
    // caught in the eruption are always knocked up (surfacing them - see Earthworm/DetectsBurrowed);
    // at Path1 max, a critical strike knocks up every insect it hits, burrowed or not
    protected override void Shoot(Vector3 target)
    {
        bool crit = Random.value < criticalChance;
        float radius = EruptionRadius;

        _scratch.Clear();
        _scratch.AddRange(Insect.allInsects);
        foreach (Insect insect in _scratch)
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) continue;
            if (Vector2.Distance(insect.transform.position, target) > radius) continue;

            bool wasBurrowed = insect.isBurrowed;
            insect.Damage(attackDamage, damageType, elementalType, this, false, eruptTags, crit);
            if (!insect.IsAlive) continue;

            if (wasBurrowed || (crit && IsPath1Maxed))
                insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, this, EruptionKnockUpForce));
        }

        SpawnEruptionVisual(target, radius);
    }

    // rendered at half the eruption's actual diameter - drawn smaller than the true hit area on
    // purpose, so it reads as "the visible burst at the center" rather than tracing the exact
    // (larger) hitbox edge to edge
    private const float EruptionVisualScale = 0.5f;

    private void SpawnEruptionVisual(Vector3 position, float radius)
    {
        if (eruptionVisualPrefab == null) return;
        float naturalSize = CarrotFurrow.VisualSquareSize(eruptionVisualPrefab, radius * 2f);
        GameObject visual = Instantiate(eruptionVisualPrefab, position, Quaternion.identity);
        visual.transform.localScale *= (radius * 2f * EruptionVisualScale) / naturalSize;
        visual.AddComponent<SpriteFadeInOut>().Play(VisualFadeIn, VisualHold, VisualFadeOut);
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
        baseAttackDamage   = data.baseAttackDamage   + (GData?.path1AttackDamagePerLevel ?? 8f) * level;
        baseCriticalChance = data.baseCriticalChance + (GData?.path1CritChancePerLevel ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + (GData?.path2AttackRangePerLevel ?? 0.15f) * level;
    }

    public override string GetName() => "<b><color=#ED9121>Carrot</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} is an elderly root sage who commands the earth itself, striking its enemies from below and forging bonds with its allies.";

    public override string GetAttackDescription() =>
        $"Erupts a chunk of earth beneath its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>{attackDamage:F0}</b></color> {PlantData.DamageTypeLabel(damageType)} to it and any insects within <color=green><b>{EruptionRadius:F1}</b></color> of it.";

    public override string GetPassiveDescription() =>
        $"Select a tile to form a <color=#B87333><b>Soil Bond</b></color> with the plant there. The bonded plant shares its targeting range with the {GetName()}. Every time the plant hits an attack, it reduces time before the {GetName()}'s next attack by <color=green><b>{CountdownBonus:F2}</b></color> seconds. In return, the bonded plant gains Attack Range equal to <color=green><b>{SoilBondEffect.RangeBonusFraction * 100f:F0}%</b></color> of the {GetName()}'s own.";

    public override string GetSkillDesription() =>
        $"Aim in a direction. A line of <color=green><b>{CarrotCount}</b></color> carrots erupt from the ground, dealing <color={PlantData.ElementalColor(elementalType)}><b>{SkillDamageFlat:F0}</b></color> [<color=#FFB6C1><b>+{SkillDamageMP:F0}</b></color>] {PlantData.DamageTypeLabel(damageType)} and knocking insects aside.";

    public override string GetPath1Name() => "Upheaval";
    public override string GetPath2Name() => "Soil Bond";
    public override string GetPath3Name() => "Fault Line";

    public override string GetPath1Description(bool details = false)
    {
        float adpl  = GData?.path1AttackDamagePerLevel ?? 8f;
        float ccpl  = GData?.path1CritChancePerLevel ?? 0.05f;
        float rpl   = GData?.path1RadiusPerLevel ?? 0.15f;
        string desc = details
            ? $"Erupts a chunk of earth beneath its target, dealing <color={PlantData.ElementalColor(elementalType)}><b>[100% Attack Damage]</b></color> {PlantData.DamageTypeLabel(damageType)} to it and any insects within <color=green><b>[({GData?.baseEruptionRadius ?? 1f:F1}) + ({rpl:F2}/Lvl.)]</b></color> of it."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Critical Chance</b></color> by <color=green><b>{ccpl * 100f:F0}%</b></color> per level. [<color=green><b>+{ccpl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"Increase <color=green><b>Eruption Radius</b></color> by <color=green><b>{rpl:F2}</b></color> per level. [<color=green><b>+{rpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"Critical strikes knock up every insect caught in the eruption.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float cbpl  = GData?.path2CountdownBonusPerLevel ?? 0.05f;
        float rgpl  = GData?.path2AttackRangePerLevel ?? 0.15f;
        string desc = details
            ? $"Select a tile to form a <color=#B87333><b>Soil Bond</b></color> with the plant there. The bonded plant shares its targeting range with the {GetName()}. Every time the plant hits an attack, it reduces time before the {GetName()}'s next attack by <color=green><b>[({GData?.bondCountdownBonus ?? 0.3f:F2}) + ({cbpl:F2}/Lvl.)]</b></color> seconds. In return, the bonded plant gains Attack Range equal to <color=green><b>{SoilBondEffect.RangeBonusFraction * 100f:F0}%</b></color> of the {GetName()}'s own."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase Attack Countdown bonus by <color=green><b>{cbpl:F2}</b></color> per level. [<color=green><b>+{cbpl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rgpl:F2}</b></color> per level. [<color=green><b>+{rgpl * effectivePath2Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"While bonded, the linked plant also gains <color=green><b>{SoilBondEffect.MaxLevelAttackSpeedBonus * 100f:F0}%</b></color> Attack Speed.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        string desc = details
            ? $"Aim in a direction. A line of <color=green><b>[({GData?.carrotCountBase ?? 3}) + ({GData?.carrotsPerLevel ?? 1}/Lvl.)]</b></color> carrots erupt from the ground, " +
              $"dealing <color=green><b>[({GData?.skillBaseDamage ?? 40f:F0}) + ({GData?.path3SkillDamagePerLevel ?? 8f:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.DamageTypeLabel(damageType)} and knocking insects aside."
            : GetSkillDesription();
        float wpl = GData?.path3WidthPerLevel ?? 0.1f;
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Carrots</b></color> by <color=green><b>{GData?.carrotsPerLevel ?? 1}</b></color> per level. [<color=green><b>+{(GData?.carrotsPerLevel ?? 1) * effectivePath3Level}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Damage</b></color> by <color=green><b>{GData?.path3SkillDamagePerLevel ?? 8f:F0}</b></color> per level. [<color=green><b>+{(GData?.path3SkillDamagePerLevel ?? 8f) * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase carrot width by <color=green><b>{wpl * 100f:F0}%</b></color> per level. [<color=green><b>+{wpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"The eruption deals an extra <color=green><b>{CarrotFurrow.MaxHealthDamagePercent * 100f:F0}%</b></color> Max Health {PlantData.DamageTypeLabel(damageType)} to insects.\n\nEach carrot further down the line deals <color=green><b>{CarrotFurrow.MaxLevelGrowthPerSegment * 100f:F0}%</b></color> more damage and grows <color=green><b>{CarrotFurrow.MaxLevelGrowthPerSegment * 100f:F0}%</b></color> larger than the one before it, compounding down the line.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
