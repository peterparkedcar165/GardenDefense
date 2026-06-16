using UnityEngine;
using System.Collections.Generic;

public class Calendula : Aura
{
    public float skillHealingMultiplier;
    private CalendulaData CData => data as CalendulaData;

    private float _illuminationHealTimer = 0f;
    private float _autoFloralGlowCooldownTimer = 0f;

    public float FloralGlowHealPerSecond =>
        (CData?.baseFloralGlowHeal ?? 8f) + (CData?.path3HealPerLevel ?? 1f) * effectivePath3Level
        + skillHealingMultiplier * magicPower;

    private string AutoCooldownText
    {
        get
        {
            if (_autoFloralGlowCooldownTimer <= 0f) return "Ready";
            int m = (int)(_autoFloralGlowCooldownTimer / 60f);
            int s = Mathf.CeilToInt(_autoFloralGlowCooldownTimer % 60f);
            return $"{m}:{s:D2}";
        }
    }

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        skillHealingMultiplier = CData?.baseSkillHealingMultiplier ?? 0f;
    }

    protected override bool ShowLight => DarknessManager.instance != null && (DarknessManager.instance.isDark || DarknessManager.instance.pitchBlack);
    protected override bool ShowDarkCircle => false;

    public override void UpdateStats()
    {
        baseLightEmissionRange = 2f * (baseAttackRange + attackRangeAdder + (baseAttackRange * attackRangeMultiplier));
        coordinatedDamageAdder = IsPath1Maxed ? 0.33f : 0f;
        base.UpdateStats();
    }

    protected override void Update()
    {
        base.Update();

        if (attackCooldownTimer < attackCooldown)
            attackCooldownTimer += Time.deltaTime;
        else if (!IsStunned && !IsChanneling && HasInsectsInRange())
            Attack();

        if (IsPath2Maxed && IsAlive)
        {
            _illuminationHealTimer += Time.deltaTime;
            if (_illuminationHealTimer >= 1f)
            {
                _illuminationHealTimer -= 1f;
                float healAmount = 3f + 0.06f * magicPower;
                foreach (Plant plant in Plant.allPlants)
                {
                    if (plant == null || !plant.IsAlive) continue;
                    if (Vector2.Distance(transform.position, plant.transform.position) <= lightEmissionRange)
                        plant.Heal(healAmount, this);
                }
            }
        }

        if (IsPath3Maxed && path3Unlocked && IsAlive)
        {
            if (_autoFloralGlowCooldownTimer > 0f)
                _autoFloralGlowCooldownTimer -= Time.deltaTime;
            else
                CheckAutoFloralGlow();
        }
    }

    private void CheckAutoFloralGlow()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (plant.maxHealth <= 0f || plant.health / plant.maxHealth >= 0.25f) continue;
            if (plant.GetEffect<FloralGlowEffect>() != null) continue;
            _autoFloralGlowCooldownTimer = skillCooldown;
            plant.ApplyEffect(new FloralGlowEffect(plant, skillDuration, effectivePath3Level + 1, this, this));
            return;
        }
    }

    protected override void Attack()
    {
        base.Attack();
        List<Insect> insects = GetInsectsInRange();
        foreach (Insect insect in insects)
            insect.Damage(attackDamage, damageType, elementalType, this, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.Attack });
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
    }

    private void OnTargetConfirmed(Plant targetPlant)
    {
        if (targetPlant == null) return;
        int myLevel = effectivePath3Level + 1;
        FloralGlowEffect existing = targetPlant.GetEffect<FloralGlowEffect>();
        if (existing != null && existing.level > myLevel)
        {
            SkillTargetingManager.instance.BeginPlantTargeting(OnTargetConfirmed, this);
            return;
        }
        skillCooldownTimer = skillCooldown;
        targetPlant.ApplyEffect(new FloralGlowEffect(targetPlant, skillDuration, myLevel, this, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackDamage = data.baseAttackDamage + (CData?.path1AttackDamagePerLevel ?? 5f)  * level;
        baseFireDamage   = (CData?.path1FireDamagePerLevel ?? 0.05f) * level;
    }

    public override void OnPath2Upgrade(int level)
    {
        baseAttackRange = data.baseAttackRange + (CData?.path2AttackRangePerLevel ?? 0.175f) * level;
    }

    public override void OnPath3Unlock()
    {
        skillCooldownTimer = 0f;
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (CData?.path3SkillDurationPerLevel ?? 2f) * level;
    }

    public override string GetName() => "<b><color=orange>Calendula</color></b>";
    public override string GetDescription() => $"The {GetName()} periodically releases waves of flaming petals and can infuse allies with fire energy.";
    public override string GetPath1Name() => "Petals";
    public override string GetPath2Name() => "Illuminate";
    public override string GetPath3Name() => "Floral Glow";

    public override string GetAttackDescription()
        => $"Releases flaming petals dealing <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range.";

    public override string GetPassiveDescription()
        => $"Illuminate the surrounding area with a radius of <color=green><b>{lightEmissionRange:F1}</b></color>.";

    public override string GetSkillDesription()
    {
        float healpl = CData?.path3HealPerLevel ?? 1f;
        float healBase = CData?.baseFloralGlowHeal ?? 8f;
        return $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>{skillDuration:F0}s</b></color>. The plant's attacks deal an additional <color=green><b>{attackDamage * 0.35f:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage on hit. Heals the plant for <color=green><b>{healBase + healpl * effectivePath3Level:F0}</b></color> [<color=#FFB6C1><b>+{skillHealingMultiplier * magicPower:F0}</b></color>] health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s Base Illumination range.";
    }

    public override string GetPath1Description(bool details = false)
    {
        float adpl   = CData?.path1AttackDamagePerLevel ?? 5f;
        float firepl = CData?.path1FireDamagePerLevel    ?? 0.05f;
        string desc = details
            ? $"Releases flaming petals dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to all insects within range."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Damage</b></color> by <color=green><b>{adpl:F0}</b></color> per level. [<color=green><b>+{adpl * effectivePath1Level:F0}</b></color>]\n\n" +
               $"Increase Fire Damage by <color=green><b>{firepl * 100f:F0}%</b></color> per level. [<color=green><b>+{firepl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Increase <color=#6495ED><b>Coordinated Damage</b></color> by <color=green><b>33%</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float rangepl = CData?.path2AttackRangePerLevel ?? 0.175f;
        string desc = details
            ? $"Illuminate the surrounding area with a radius equal to <color=green><b>2×</b></color> her Attack Range (<color=green><b>{lightEmissionRange:F1}</b></color>)."
            : GetPassiveDescription();
        string p2Bonus = details
            ? "Plants within illumination range heal <color=green><b>3</b></color> [<color=#FFB6C1><b>+6% Magic Power</b></color>] health per second."
            : "Plants within illumination range heal <color=green><b>3</b></color> health per second.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F3}</b></color> per level. [<color=green><b>+{rangepl * effectivePath2Level:F3}</b></color>]\n\n" +
               $"{Level5Section(path2Level, p2Bonus)}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl  = CData?.path3SkillDurationPerLevel ?? 2f;
        float healpl = CData?.path3HealPerLevel           ?? 1f;
        float healBase = CData?.baseFloralGlowHeal ?? 8f;
        string desc = details
            ? $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. The plant's attacks deal an additional <color=green><b>[35% Attack Damage + <color=#FFB6C1>{skillDamageMultiplier * 100f:F0}% Magic Power</color>]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage on hit. Heals the plant for <color=green><b>[({healBase:F0}) + ({healpl:F0}/Lvl.) + <color=#FFB6C1>{skillHealingMultiplier * 100f:F0}% Magic Power</color>]</b></color> health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s Base Illumination range."
            : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase duration by <color=green><b>{durpl:F0}</b></color> seconds per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase Healing per second by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "Whenever a plant on the field takes damage that would reduce their Health to under <color=green><b>25%</b></color>, if they do not already have the <color=orange><b>Floral Glow</b></color> effect, the <b><color=orange>Calendula</color></b> automatically blesses the plant with the effect. This instance has a separate cooldown, but share the same cooldown time." + (IsPath3Maxed ? $"\n\nCooldown: [<color=green><b>{AutoCooldownText}</b></color>]" : ""))}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
