using UnityEngine;
using System.Collections;

public class BogIris : Shooter
{
    [SerializeField] private GameObject geyserPrefab;
    [SerializeField] private SpriteRenderer closedVisual;
    [SerializeField] private SpriteRenderer openVisual;

    private SpriteRenderer _rootRenderer;
    private bool isOpen = false;
    private float cycleTimer = 0f;
    private float sunTickTimer = 0f;
    private float healTickTimer = 0f;
    private float TotalHeal => (BogData?.baseClosedHeal ?? 200f) + (BogData?.path2ClosedHealPerLevel ?? 20f) * effectivePath2Level;

    private BogIrisData BogData => data as BogIrisData;

    private float SunInterval => 2f * (1f + sunGenerationCooldown);
    private float OpenDuration => ((BogData?.baseOpenDuration ?? 0f) + (BogData?.path2OpenDurationPerLevel ?? 2f) * effectivePath2Level) * (1 + passiveDuration);
    private int SunGenerated => (BogData?.baseSunGenerated ?? 0) + (BogData?.path2SunPerLevel ?? 1) * effectivePath2Level;
    private float GeyserRadius => skillRadius + (BogData?.path3GeyserRadiusPerLevel ?? 0.15f) * effectivePath3Level;
    private float KnockUpHeight => ScaleCC(((BogData?.baseKnockUpHeight ?? 0f) + (BogData?.path3KnockUpPerLevel ?? 1f) * effectivePath3Level) * skillDuration);
    private float KnockUpForce => Mathf.Sqrt(2f * Insect.gravity * KnockUpHeight);
    private float GeyserDamage => (BogData?.baseGeyserDamage ?? 0f) + (BogData?.path3GeyserDamagePerLevel ?? 15f) * effectivePath3Level + skillDamageMultiplier * magicPower;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _rootRenderer = GetComponent<SpriteRenderer>();
        SetVisualState(false);
    }

    protected override void Update()
    {
        base.Update();
        UpdatePassive();
    }

    protected override bool GetPassiveBarVisible() => true;
    protected override float GetPassiveBarFill()
    {
        if (isOpen)
            return SunInterval > 0f ? Mathf.Clamp01(sunTickTimer / SunInterval) : 0f;
        return passiveCooldown > 0f ? Mathf.Clamp01(cycleTimer / passiveCooldown) : 0f;
    }

    private void UpdatePassive()
    {
        cycleTimer += Time.deltaTime;

        if (!isOpen)
        {
            healTickTimer += Time.deltaTime;
            if (healTickTimer >= 1f)
            {
                healTickTimer -= 1f;
                float healPerTick = TotalHeal / passiveCooldown;
                float scaledHeal = healPerTick * (1f + healingReceived) * (1f + healingBonus);
                float overflow = IsPath2Maxed ? Mathf.Max(0f, scaledHeal - (maxHealth - health)) : 0f;
                Heal(healPerTick);
                if (overflow > 0f && IsAlive)
                {
                    AquaticOvershieldEffect shield = GetEffect<AquaticOvershieldEffect>();
                    if (shield == null)
                    {
                        shield = new AquaticOvershieldEffect(this, this);
                        ApplyEffect(shield);
                    }
                    shield.AddShield(overflow);
                }
            }
            if (cycleTimer >= passiveCooldown)
            {
                isOpen = true;
                cycleTimer = 0f;
                sunTickTimer = 0f;
                SetVisualState(true);
            }
        }
        else
        {
            sunTickTimer += Time.deltaTime;
            if (sunTickTimer >= SunInterval)
            {
                sunTickTimer -= SunInterval;
                int granted = GenerateSun(SunGenerated);
                SunIndicator.Spawn(transform.position + new Vector3(0.25f, 0.5f, 0f), granted);
            }
            if (cycleTimer >= OpenDuration)
            {
                isOpen = false;
                cycleTimer = 0f;
                healTickTimer = 0f;
                SetVisualState(false);
            }
        }
    }

    protected override SpriteRenderer GetMainRenderer()
    {
        return isOpen ? openVisual : closedVisual;
    }

    private void SetVisualState(bool open)
    {
        ResetOutlineRenderers();
        if (_rootRenderer != null) _rootRenderer.enabled = !open;
        if (closedVisual != null) closedVisual.gameObject.SetActive(!open);
        if (openVisual != null) openVisual.gameObject.SetActive(open);
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        BogIrisProjectile bogProj = proj.GetComponent<BogIrisProjectile>();
        if (bogProj != null)
        {
            bogProj.SetTarget(FindTarget());
            bogProj.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(GeyserRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        skillCooldownTimer = skillCooldown;
        StartCoroutine(SpawnGeyser(position));
    }

    private IEnumerator SpawnGeyser(Vector3 position)
    {
        bool isRaining = WeatherManager.instance != null && WeatherManager.instance.HasWeather(WeatherType.Rain);
        yield return new WaitForSeconds(isRaining ? 0.5f : 1f);
        if (geyserPrefab == null) yield break;
        GameObject obj = Instantiate(geyserPrefab, position, Quaternion.identity);
        obj.GetComponent<Geyser>()?.Initialize(position, GeyserRadius, skillDuration, GeyserDamage, KnockUpForce, this);
        if (IsPath3Maxed)
        {
            foreach (Insect insect in new System.Collections.Generic.List<Insect>(Insect.allInsects))
            {
                if (insect == null || !insect.IsAlive) continue;
                if (Vector3.Distance(position, insect.transform.position) <= GeyserRadius)
                    insect.ApplyEffect(new GeyseredEffect(insect, 12f, 1, this));
            }
        }
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (BogData?.path1AttackDamagePerLevel ?? 8f) * level;
    }

    public override void UpdateStats()
    {
        base.UpdateStats();
        if (IsPath1Maxed)
        {
            attackDamage *= 1.33f;
            projectileSpeed *= 1.45f;
        }
    }

    public override void OnPath2Upgrade(int level) { }
    public override void OnPath3Upgrade(int level) { }

    public override string GetName() => "<b><color=#4FC3F7>Bog Iris</color></b>";
    public override string GetDescription() =>
        $"The {GetName()} is self-sufficient, providing herself with regeneration as well as generating sun for the garden.";

    public override string GetPath1Description(bool details = false)
    {
        float adpl = BogData?.path1AttackDamagePerLevel ?? 8f;
        string desc = details
            ? $"Fires a water bolt at a single target dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : $"Fires a water bolt at a single target dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=green><b>Attack Damage</b></color> by <color=green><b>33%</b></color> and <color=green><b>Projectile Speed</b></color> by <color=green><b>45%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float opendurpl = BogData?.path2OpenDurationPerLevel ?? 2f;
        int   sunpl     = BogData?.path2SunPerLevel          ?? 1;
        float healpl    = BogData?.path2ClosedHealPerLevel   ?? 20f;
        string desc = details
            ? $"Cycles between an <b><color=#4FC3F7>open</color></b> (<color=green><b>[({BogData?.baseOpenDuration:F0}) + ({opendurpl:F0}/Lvl.)]</b></color>) and <b><color=#4FC3F7>closed</color></b> (<color=green><b>{passiveCooldown:F1}</b></color> seconds) state.\n\n" +
              $"In <b><color=#4FC3F7>open</color></b> form, generates <color=green><b>[({BogData?.baseSunGenerated}) + ({sunpl}/Lvl.)]</b></color> Sun every <color=green><b>{SunInterval:F1}</b></color> seconds.\n\n" +
              $"In <b><color=#4FC3F7>closed</color></b> form, regenerates <color=green><b>[({BogData?.baseClosedHeal:F0}) + ({healpl:F0}/Lvl.)]</b></color> HP over <color=green><b>{passiveCooldown:F1}</b></color> seconds."
            : $"Cycles between an <b><color=#4FC3F7>open</color></b> (<color=green><b>{OpenDuration:F0}</b></color> seconds) and <b><color=#4FC3F7>closed</color></b> (<color=green><b>{passiveCooldown:F1}</b></color> seconds) state.\n\n" +
              $"In <b><color=#4FC3F7>open</color></b> form, generates <color=green><b>{SunGenerated}</b></color> Sun every <color=green><b>{SunInterval:F1}</b></color> seconds.\n\n" +
              $"In <b><color=#4FC3F7>closed</color></b> form, regenerates <color=green><b>{TotalHeal}</b></color> HP over <color=green><b>{passiveCooldown:F1}</b></color> seconds.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase the duration of the <b><color=#4FC3F7>open</color></b> state by <color=green><b>{opendurpl:F0}</b></color> seconds per level. [<color=green><b>+{opendurpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"Increase Sun generated per tick by <color=green><b>{sunpl}</b></color> per level. [<color=green><b>+{sunpl * effectivePath2Level}</b></color>]\n\n" +
               $"{Level5Section(path2Level, "Overhealing while in the <b><color=#4FC3F7>closed</color></b> state grants <color=#4FC3F7><b>Aquatic Overshield</b></color> up to <color=green><b>100</b></color>. Taking <color=#FF69B4><b>Physical Damage</b></color> while protected by the shield primes the attacker with <color=#1E90FF><b>Wet</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float dmgpl    = BogData?.path3GeyserDamagePerLevel  ?? 15f;
        float knockpl  = BogData?.path3KnockUpPerLevel        ?? 1f;
        float radiuspl = BogData?.path3GeyserRadiusPerLevel   ?? 0.15f;
        string desc = details
            ? $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>[({BogData?.baseGeyserDamage:F0}) + ({dmgpl:F0}/Lvl.) + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage and knocking all insects airborne by <color=green><b>[({BogData?.baseKnockUpHeight:F0}) + ({knockpl:F0}/Lvl.)]</b></color> units within radius <color=green><b>[({data.baseSkillRadius:F2}) + ({radiuspl:F2}/Lvl.)]</b></color>."
            : $"Target a location. After a brief delay, a geyser erupts, dealing <color=green><b>{(BogData?.baseGeyserDamage ?? 0f) + dmgpl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage and knocking all insects airborne by <color=green><b>{KnockUpHeight:F0}</b></color> units.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase the flat component of geyser damage by <color=green><b>{dmgpl:F0}</b></color> per level. [<color=green><b>+{dmgpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase the knock-up height by <color=green><b>{knockpl:F0}</b></color> per level. [<color=green><b>+{knockpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase the radius of the geyser by <color=green><b>{radiuspl:F2}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F2}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Successful <color=#4FC3F7><b>Geyser</b></color> hits inflict <color=#4FC3F7><b>Geysered</b></color> for <color=green><b>12</b></color> seconds, reducing <color=#00CED1><b>Armor</b></color> by <color=red><b>30</b></color> and <color=#A0522D><b>Fall Damage Resistance</b></color> by <color=red><b>33%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
