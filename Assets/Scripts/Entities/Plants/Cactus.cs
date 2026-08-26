using UnityEngine;
using System.Collections.Generic;

public class Cactus : Shooter
{
    private float _tauntTickTimer;
    private const float TauntTickInterval  = 0.25f;
    private const float TauntEffectDuration = 0.35f;

    private readonly HashSet<Insect> _volleyHit = new HashSet<Insect>();

    private CactusData CactData => data as CactusData;
    private float ShieldAmount  => (CactData?.baseShieldAmount ?? 100f) + (CactData?.path3ShieldPerLevel ?? 10f) * effectivePath3Level;
    private float SkillDuration => data.baseSkillDuration + (CactData?.path3SkillDurationPerLevel ?? 2f) * effectivePath3Level;
    private float ShieldArmorBonus => (CactData?.baseShieldArmorBonus ?? 25f) + (CactData?.path3ShieldArmorPerLevel ?? 5f) * effectivePath3Level;

    // no targeting needed: the skill just shields Cactus herself
    private bool autoCastEnabled = false;
    public override bool UsesAutoCast => true;
    public override bool IsAutoCasting => autoCastEnabled;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        basePassiveDuration = 8f;
    }

    protected override void Update()
    {
        base.Update();

        if (autoCastEnabled && SkillReady)
            ActivateSkill();

        if (HasEffect<ShieldEffect>())
        {
            _tauntTickTimer += Time.deltaTime;
            if (_tauntTickTimer >= TauntTickInterval)
            {
                _tauntTickTimer -= TauntTickInterval;
                ApplyTauntInRange();
            }
        }
        else
        {
            _tauntTickTimer = 0f;
        }
    }

    // click Auto Cast to toggle it on, click again to turn it off — no target to pick
    public override void ToggleAutoCast() => autoCastEnabled = !autoCastEnabled;

    public override AutoCastState CaptureAutoCastState() =>
        new AutoCastState { enabled = autoCastEnabled };

    public override void RestoreAutoCastState(AutoCastState state)
    {
        if (!state.enabled) return;
        autoCastEnabled = true;
    }

    protected override void Shoot(Vector3 target)
    {
        if (projectilePrefab == null) return;
        _volleyHit.Clear();
        int needleBase  = CactData?.path1NeedlesBase      ?? 8;
        int needleLevel = CactData?.path1NeedlesPerLevel  ?? 3;
        int needleCount = needleBase + needleLevel * effectivePath1Level;
        float angleStep = 360f / needleCount;

        for (int i = 0; i < needleCount; i++)
        {
            float rad = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Vector3 targetPos = transform.position + dir * maxRange;

            GameObject proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
            CactusNeedleProjectile needle = proj.GetComponent<CactusNeedleProjectile>();
            needle?.Initialize(targetPos, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    protected override void OnHitByInsect(Insect attacker)
    {
        if (!IsAlive || !attacker.IsAlive) return;
        float armorBonus = IsPath2Maxed ? armor * 0.33f : 0f;
        attacker.Damage(attacker.attackDamage * 1.5f + armorBonus, damageType, elementalType, this, false, new DamageTag[] { DamageTag.PassiveDamage, DamageTag.Counter });
        ApplyPunctured(attacker, 1 + effectivePath2Level);
    }

    public void OnNeedleHit(Insect insect, float baseDamage)
    {
        if (!insect.IsAlive) return;
        float dmg = _volleyHit.Contains(insect) ? baseDamage * 0.5f : baseDamage;
        _volleyHit.Add(insect);
        insect.Damage(dmg, damageType, elementalType, this, true, new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
        ApplyPunctured(insect, 1 + effectivePath2Level);
    }

    private void ApplyPunctured(Insect insect, int stacks)
    {
        if (!insect.IsAlive) return;
        int current = insect.GetEffect<PuncturedEffect>()?.level ?? 0;
        int total = Mathf.Min(current + stacks, 100);
        if (total <= current) return;
        insect.ApplyEffect(new PuncturedEffect(insect, passiveDuration, total, this));
    }

    private void ApplyTauntInRange()
    {
        foreach (Insect insect in Insect.allInsects)
        {
            if (!insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= attackRange)
                insect.ApplyEffect(new TauntEffect(insect, TauntEffectDuration, 1, this, this));
        }
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        skillCooldownTimer = skillCooldown;
        ApplyEffect(new CactusShieldEffect(this, SkillDuration, 1, this, ShieldAmount));
    }

    public override void UpdateStats()
    {
        if (IsPath1Maxed) piercingAdder += 2;
        bool shielded = HasEffect<ShieldEffect>();
        float asBonus = IsPath3Maxed && shielded ? 0.35f : 0f;
        float armorBonus = shielded ? ShieldArmorBonus : 0f;
        attackSpeedTotalMultiplier += asBonus;
        armorAdder += armorBonus;
        base.UpdateStats();
        if (IsPath1Maxed) piercingAdder -= 2;
        attackSpeedTotalMultiplier -= asBonus;
        armorAdder -= armorBonus;
        temperatureMax = comfortMax;
    }

    public override void OnPath1Upgrade(int level) { }

    public override void OnPath2Upgrade(int level)
    {
        float hppl = CactData?.path2HealthPerLevel ?? 60f;
        baseMaxHealth = (data?.baseMaxHealth ?? 250f) + hppl * level;
        health += hppl;
        UpdateHealthBar();
    }

    public override string GetName() => "<b><color=green>Cactus</color></b>";

    public override string GetDescription() =>
        "A hardy desert plant that fires needles in all directions and retaliates against attackers.";

    public override string GetAttackDescription()
    {
        int needleBase  = CactData?.path1NeedlesBase     ?? 8;
        int needleLevel = CactData?.path1NeedlesPerLevel ?? 3;
        return $"Fires <color=green><b>{needleBase + needleLevel * effectivePath1Level}</b></color> needles in equal angles around itself, dealing " +
               $"<color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per needle. " +
               $"Each hit applies <color=#A0522D><b>Punctured</b></color>.";
    }

    public override string GetPassiveDescription() =>
        $"Insects that attack the {GetName()} take damage equal to <color=green><b>150%</b></color> of their own Attack Damage as " +
        $"{PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and receive " +
        $"<color=green><b>{1 + effectivePath2Level}</b></color> <color=#A0522D>Punctured</color> stack(s).";

    public override string GetSkillDesription() =>
        $"Grants the {GetName()} a <color=grey><b>{ShieldAmount:F0}</b></color> shield for <color=green><b>{SkillDuration:F0}s</b></color>. " +
        $"While the shield holds, nearby insects are forced to target the {GetName()}, and it gains <color=#00CED1><b>{ShieldArmorBonus:F0} Armor</b></color>.";

    public override string GetPath1Description(bool details = false)
    {
        int needleBase  = CactData?.path1NeedlesBase     ?? 8;
        int needleLevel = CactData?.path1NeedlesPerLevel ?? 3;
        string desc = details
            ? $"Fires <color=green><b>[({needleBase}) + ({needleLevel}/Lvl.)]</b></color> needles in equal angles around itself, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per needle. Each hit applies <color=#A0522D><b>Punctured</b></color>."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase needle count by <color=green><b>{needleLevel}</b></color> per level. [<color=green><b>+{needleLevel * effectivePath1Level}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=green><b>Piercing</b></color> by <color=green><b>2</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float hppl = CactData?.path2HealthPerLevel ?? 60f;
        string desc = details
            ? $"Insects that attack the {GetName()} take damage equal to <color=green><b>150%</b></color> of their own Attack Damage as {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage, and receive <color=green><b>[1 + (1/Lvl.)]</b></color> <color=#A0522D>Punctured</color> stack(s)."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"<color=#A0522D><b>Punctured</b></color>: reduces <color=#00CED1><b>Armor</b></color> by <color=red><b>1</b></color> per stack, lasts <color=green><b>{passiveDuration:F0}s</b></color>.\n\n" +
               $"Increase <color=#A0522D><b>Punctured</b></color> stacks per hit by <color=green><b>1</b></color> per level. [<color=green><b>+{effectivePath2Level}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Max Health</b></color> by <color=green><b>{hppl:F0}</b></color> per level. [<color=green><b>+{hppl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, details ? $"Retaliation damage is increased by <color=#00CED1><b>33%</b></color> of the {GetName()}'s <color=#00CED1><b>Armor</b></color>." : $"Retaliation deals <color=#00CED1><b>+{armor * 0.33f:F0}</b></color> bonus damage.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float shieldpl  = CactData?.path3ShieldPerLevel        ?? 10f;
        float durpl     = CactData?.path3SkillDurationPerLevel ?? 2f;
        float healpl    = CactData?.path3HealBonusPerLevel     ?? 0.04f;
        float shieldBase = CactData?.baseShieldAmount ?? 100f;
        float armorpl   = CactData?.path3ShieldArmorPerLevel  ?? 5f;
        float armorBase = CactData?.baseShieldArmorBonus      ?? 25f;
        string desc = details
            ? $"Grants the {GetName()} a <color=grey><b>[({shieldBase:F0}) + ({shieldpl:F0}/Lvl.)]</b></color> shield for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. While the shield holds, nearby insects are forced to target the {GetName()}."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase shield by <color=green><b>{shieldpl:F0}</b></color> per level. [<color=grey><b>+{shieldpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase healing received by <color=green><b>{healpl * 100f:F0}%</b></color> per level. [<color=green><b>+{healpl * effectivePath3Level * 100f:F0}%</b></color>]\n\n" +
               $"While <color=grey><b>Shielded</b></color> by any source, gain <color=#00CED1><b>[({armorBase:F0}) + ({armorpl:F0}/Lvl.)]</b></color> Armor. [<color=#00CED1><b>{ShieldArmorBonus:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"While <color=grey><b>Shielded</b></color>, <color=green><b>Total Attack Speed</b></color> is increased by <color=green><b>35%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
