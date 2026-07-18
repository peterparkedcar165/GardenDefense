using UnityEngine;

public class GroundThorn : Shooter
{
    [SerializeField] private GameObject pillarVisualPrefab;  // optional per pillar eruption visual

    private GroundThornData GData => data as GroundThornData;

    public float SplashRadius     => GData?.splashRadius ?? 1f;
    public float SplashMultiplier => GData?.splashMultiplier ?? 0.75f;
    public float GroundedDuration => GData?.groundedDuration ?? 3f;
    public float KnockbackDistance => GData?.knockbackDistance ?? 0.6f;

    public float GrassChance  => (GData?.grassChanceBase ?? 0.75f) + (GData?.grassChancePerLevel ?? 0.05f) * effectivePath2Level;
    public float SandChance   => (GData?.sandChanceBase ?? 0.5f) + (GData?.sandChancePerLevel ?? 0.1f) * effectivePath2Level;
    public float CaveChance   => (GData?.caveChanceBase ?? 0.5f) + (GData?.caveChancePerLevel ?? 0.05f) * effectivePath2Level;
    public float StunDuration => (GData?.stunDuration ?? 1f) + (GData?.stunDurationPerLevel ?? 0.2f) * effectivePath2Level;
    public float SnowSlow     => (GData?.snowSlowPercent ?? 0.15f) + (GData?.snowSlowPerLevel ?? 0.03f) * effectivePath2Level;
    public float DirtBonus    => (GData?.dirtDamageBonus ?? 0.25f) + (GData?.dirtBonusPerLevel ?? 0.05f) * effectivePath2Level;
    public float SunderPercent  => GData?.sunderPercent ?? 0.35f;
    public float SunderDuration => GData?.sunderDuration ?? 8f;

    public int   PillarCount     => (GData?.pillarCountBase ?? 3) + effectivePath3Level;
    public float SkillDamageFlat => (GData?.skillBaseDamage ?? 40f) + (GData?.path3SkillDamagePerLevel ?? 8f) * effectivePath3Level;
    public float SkillDamageMP   => skillDamageMultiplier * magicPower;
    public float SkillDamage     => SkillDamageFlat + SkillDamageMP;

    private TileType CurrentTile => occupiedTile != null ? occupiedTile.tileType : TileType.Path;

    // dirt grants flat bonus damage to attack and skill
    public float TileDamageMultiplier => CurrentTile == TileType.Dirt ? 1f + DirtBonus : 1f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
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
            insect.transform.position += (Vector3)(direction * step);
            moved += step;
            yield return null;
        }
    }

    // rolls the tile bonus for one damaged insect, called for attack hits, splash and pillars
    public void ApplyTileBonus(Insect insect)
    {
        if (insect == null || !insect.IsAlive) return;

        switch (CurrentTile)
        {
            case TileType.Grass:
                if (Random.value < GrassChance)
                    insect.ApplyEffect(new CharredEffect(insect, GData?.grassFireResDuration ?? 4f, 1, this, GData?.grassFireResReduction ?? 0.35f));
                break;

            case TileType.Sand:
                if (Random.value < SandChance)
                    insect.ApplyEffect(new BlindEffect(insect, GData?.blindDuration ?? 4f, 1, this, GData?.blindAccuracyPenalty ?? 0.5f));
                break;

            case TileType.Cave:
                if (Random.value < CaveChance)
                    insect.ApplyEffect(new StunEffect(insect, StunDuration, 1, this));
                break;

            case TileType.Snow:
                SlowEffect slow = new SlowEffect(insect, GData?.snowSlowDuration ?? 3f, 1, this);
                slow.slowness = SnowSlow;
                insect.ApplyEffect(slow);
                break;
        }
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject obj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        GroundThornProjectile proj = obj.GetComponent<GroundThornProjectile>();
        if (proj == null) return;
        proj.SetTarget(FindTarget());
        proj.Initialize(target, attackDamage * TileDamageMultiplier, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginTargeting(0f, OnSkillTargetConfirmed);
    }

    private void OnSkillTargetConfirmed(Vector3 target)
    {
        skillCooldownTimer = skillCooldown;
        BeginChannel();

        Vector2 direction = ((Vector2)target - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;

        GameObject waveObj = new GameObject("EarthPillars");
        waveObj.transform.position = transform.position;
        EarthPillars wave = waveObj.AddComponent<EarthPillars>();
        wave.Initialize(
            transform.position,
            direction,
            PillarCount,
            GData?.pillarStartOffset ?? 1f,
            GData?.pillarSpacing ?? 1f,
            GData?.pillarInterval ?? 0.12f,
            GData?.pillarRadius ?? 0.9f,
            GData?.pillarHitboxMultiplier ?? 1.2f,
            GData?.pillarDamageGrowth ?? 0.1f,
            SkillDamage * TileDamageMultiplier,
            GData?.pillarKnockUpForce ?? 5f,
            GData?.pillarKnockbackDistance ?? 0.9f,
            IsPath3Maxed,
            GData?.pillarStunHitThreshold ?? 3,
            GData?.pillarStunDuration ?? 1f,
            this,
            pillarVisualPrefab);
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (GData?.path1AttackDamagePerLevel ?? 23f) * level;
        baseAttackRange  = data.baseAttackRange  + (GData?.path1AttackRangePerLevel ?? 0.2f) * level;
    }

    public override string GetName() => "<b><color=#79391F>GroundThorn</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} commands the earth itself, hurling stone at its enemies and splitting the ground beneath them.";

    public override string GetAttackDescription() =>
        $"Hurls earth at the target, dealing <color=green><b>{attackDamage * TileDamageMultiplier:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the first insect hit " +
        $"and <color=green><b>{SplashMultiplier * 100f:F0}%</b></color> to other insects within a <color=green><b>{SplashRadius:F0}</b></color>-radius. Knocks down flying insects.";

    public override string GetPassiveDescription() =>
        $"When dealing damage with its attack or skill, gains a bonus effect depending on the tile it is planted on:\n" +
        $"<color=green><b>Grass</b></color>: <color=green><b>{GrassChance * 100f:F0}%</b></color> chance to reduce <color=orange><b>Fire Resistance</b></color> by <color=green><b>{(GData?.grassFireResReduction ?? 0.35f) * 100f:F0}%</b></color> for <color=green><b>{GData?.grassFireResDuration ?? 4f:F0}s</b></color>.\n" +
        $"<color=#79391F><b>Dirt</b></color>: <color=green><b>{DirtBonus * 100f:F0}%</b></color> increased damage.\n" +
        $"<color=#EDC9AF><b>Sand</b></color>: <color=green><b>{SandChance * 100f:F0}%</b></color> chance to <color=#DDDDDD><b>Blind</b></color> for <color=green><b>{GData?.blindDuration ?? 4f:F0}s</b></color>.\n" +
        $"<color=grey><b>Cave</b></color>: <color=green><b>{CaveChance * 100f:F0}%</b></color> chance to <color=#FFD700><b>Stun</b></color> for <color=green><b>{StunDuration:F1}s</b></color>.\n" +
        $"<color=#E0FFFF><b>Snow</b></color>: applies a <color=green><b>{SnowSlow * 100f:F0}%</b></color> <color=#87CEEB><b>Slow</b></color>.";

    public override string GetSkillDesription() =>
        $"Aim a direction. <color=green><b>{PillarCount}</b></color> pillars of earth erupt from the ground in a line, starting at the {GetName()} and emerging outwards. " +
        $"Each pillar deals <color=green><b>{SkillDamageFlat:F0}</b></color> [<color=#FFB6C1><b>+{SkillDamageMP:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, " +
        $"increased by <color=green><b>{(GData?.pillarDamageGrowth ?? 0.1f) * 100f:F0}%</b></color> for each pillar after the first, " +
        $"knocking insects up and pushing them along the line, carrying them into the next eruption.";

    public override string GetPath1Name() => "Upheaval";
    public override string GetPath2Name() => "Attunement";
    public override string GetPath3Name() => "Fault Line";

    public override string GetPath1Description(bool details = false)
    {
        float adpl  = GData?.path1AttackDamagePerLevel ?? 23f;
        float rngpl = GData?.path1AttackRangePerLevel ?? 0.2f;
        string desc = details
            ? $"Hurls earth at the target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the first insect hit " +
              $"and <color=green><b>{SplashMultiplier * 100f:F0}%</b></color> to other insects within a <color=green><b>{SplashRadius:F0}</b></color>-radius. Knocks down flying insects."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rngpl:F2}</b></color> per level. [<color=green><b>+{rngpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks knock insects back.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float gcpl = GData?.grassChancePerLevel ?? 0.05f;
        float dbpl = GData?.dirtBonusPerLevel ?? 0.05f;
        float scpl = GData?.sandChancePerLevel ?? 0.1f;
        float ccpl = GData?.caveChancePerLevel ?? 0.05f;
        float sdpl = GData?.stunDurationPerLevel ?? 0.2f;
        float sspl = GData?.snowSlowPerLevel ?? 0.03f;
        return $"Passive:\n\n{GetPassiveDescription()}\n\n" +
               $"Per level: <color=green><b>Grass</b></color> chance <color=green><b>+{gcpl * 100f:F0}%</b></color>, " +
               $"<color=#79391F><b>Dirt</b></color> damage <color=green><b>+{dbpl * 100f:F0}%</b></color>, " +
               $"<color=#EDC9AF><b>Sand</b></color> chance <color=green><b>+{scpl * 100f:F0}%</b></color>, " +
               $"<color=grey><b>Cave</b></color> chance <color=green><b>+{ccpl * 100f:F0}%</b></color> and duration <color=green><b>+{sdpl:F1}s</b></color>, " +
               $"<color=#E0FFFF><b>Snow</b></color> slow <color=green><b>+{sspl * 100f:F0}%</b></color>.\n\n" +
               $"{Level5Section(path2Level, $"Attacks also reduce the target's <color=#00CED1><b>Armor</b></color> by <color=green><b>{SunderPercent * 100f:F0}%</b></color> for <color=green><b>{SunderDuration:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        string desc = details
            ? $"Aim a direction. <color=green><b>[(3) + (1/Lvl.)]</b></color> pillars of earth erupt from the ground in a line, starting at the {GetName()} and emerging outwards. " +
              $"Each pillar deals <color=green><b>[({GData?.skillBaseDamage ?? 40f:F0}) + ({GData?.path3SkillDamagePerLevel ?? 8f:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, " +
              $"increased by <color=green><b>{(GData?.pillarDamageGrowth ?? 0.1f) * 100f:F0}%</b></color> for each pillar after the first, " +
              $"knocking insects up and pushing them along the line, carrying them into the next eruption."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Pillars</b></color> by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath3Level}</b></color>]\n\n" +
               $"Increase <color=green><b>Pillar Base Damage</b></color> by <color=green><b>{GData?.path3SkillDamagePerLevel ?? 8f:F0}</b></color> per level. [<color=green><b>+{(GData?.path3SkillDamagePerLevel ?? 8f) * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Insects struck by <color=green><b>{GData?.pillarStunHitThreshold ?? 3}</b></color> or more pillars are <color=#FFD700><b>Stunned</b></color> for <color=green><b>{GData?.pillarStunDuration ?? 1f:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
