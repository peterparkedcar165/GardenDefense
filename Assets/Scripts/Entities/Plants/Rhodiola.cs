using UnityEngine;
using System.Collections.Generic;

public class Rhodiola : Shooter
{
    private RhodiolaData RData => data as RhodiolaData;

    private Insect _currentTarget;

    private const float SeedDurationBase = 8f;
    private const float BurstRadius  = 2.5f;

    public float BurgeonHealPerTick  => (RData?.baseBurgeonHealPerTick ?? 2f) + effectivePath2Level * (RData?.path2HealPerLevel ?? 1f);
    public float BurgeonDuration     => RData?.baseBurgeonDuration  ?? 4f;
    public float BurgeonTickInterval => RData?.burgeonTickInterval   ?? 0.5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        basePassiveDuration = SeedDurationBase;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override GameObject FindTarget()
    {
        List<Insect> unseeded = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float dist = Vector3.Distance(transform.position, insect.transform.position);
            if (dist > attackRange || !IsValidNightTarget(insect, dist)) continue;
            if (!insect.HasEffect<RejuvenatingSeedEffect>())
                unseeded.Add(insect);
        }
        GameObject target = unseeded.Count > 0 ? FindFirst(unseeded) : base.FindTarget();
        _currentTarget = target != null ? target.GetComponent<Insect>() : null;
        return target;
    }

    protected override void Shoot(Vector3 _)
    {
        if (_currentTarget == null || !_currentTarget.IsAlive) return;
        _currentTarget.Damage(attackDamage, damageType, elementalType, this, true,
            new DamageTag[] { DamageTag.Attack, DamageTag.SingleTarget });
        _currentTarget.ApplyEffect(new RejuvenatingSeedEffect(_currentTarget, passiveDuration, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed  = data.baseAttackSpeed  + level * (RData?.path1AttackSpeedPerLevel  ?? 0.08f);
        baseHealingBonus = data.baseHealingBonus + level * (RData?.path1HealingBonusPerLevel ?? 0.03f);
        baseAttackRange  = data.baseAttackRange  + level * (RData?.path1AttackRangePerLevel  ?? 0.2f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillCooldown = data.baseSkillCooldown - level * (RData?.path3CooldownReductionPerLevel ?? 0f);
    }

    public override void ActivateSkill()
    {
        if (!SkillReady) return;
        bool anyDead = false;
        foreach (var kvp in Tile.allTiles)
            if (kvp.Value.deadPlant != null) { anyDead = true; break; }
        if (!anyDead) return;
        SkillTargetingManager.instance.BeginDeadTileTargeting(OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Tile tile)
    {
        skillCooldownTimer = skillCooldown;
        Plant revived = Plant.RevivePlant(tile, RData?.revivalHealthPercent ?? 0.2f);
        if (revived != null && IsPath3Maxed)
        {
            float shield = RData?.verdantGuardianShield ?? 200f;
            float regen  = RData?.verdantGuardianRegen  ?? 20f;
            revived.ApplyEffect(new VerdantGuardianEffect(revived, skillDuration, this, shield, regen));
        }
    }

    public override string GetName() => $"<b><color=green>Rhodiola</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} breathes life back into fallen allies, seeding the enemy with restorative energy.";

    public override string GetAttackDescription() =>
        $"Instantly deals <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the target.";

    public override string GetPassiveDescription()
    {
        return $"Attacks inflict <color=green><b>Rejuvenating Seed</b></color> on the target for <color=green><b>{passiveDuration:F0}s</b></color>. " +
               $"When the target is attacked by a plant, that plant is granted <color=green><b>Rejuvenating Burgeon</b></color>, " +
               $"healing <color=green><b>{Mathf.RoundToInt(BurgeonHealPerTick)}</b></color> health every <color=green><b>{BurgeonTickInterval}s</b></color> " +
               $"for <color=green><b>{BurgeonDuration:F0}s</b></color>.";
    }

    public override string GetSkillDesription() =>
        $"Target a tile where a plant has fallen to resurrect it. The plant is revived with <color=green><b>{(RData?.revivalHealthPercent ?? 0.2f) * 100f:F0}%</b></color> of its maximum health.";

    public override string GetPath1Name() => "Verdance";
    public override string GetPath2Name() => "Seed";
    public override string GetPath3Name() => "Revival";

    public override string GetPath1Description(bool details = false)
    {
        float aspl  = RData?.path1AttackSpeedPerLevel  ?? 0.08f;
        float hbpl  = RData?.path1HealingBonusPerLevel ?? 0.03f;
        float rngpl = RData?.path1AttackRangePerLevel  ?? 0.2f;
        string desc = details
            ? $"Instantly deals <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the target."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rngpl:F2}</b></color> per level. [<color=green><b>+{rngpl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Healing Bonus</b></color> by <color=green><b>{hbpl * 100f:F0}%</b></color> per level. [<color=green><b>+{hbpl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"When an insect with <color=green><b>Rejuvenating Seed</b></color> dies, it bursts, spreading the seed to nearby insects within a <color=green><b>{BurstRadius}</b></color>-radius.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float healpl      = RData?.path2HealPerLevel      ?? 1f;
        float baseHealPT  = RData?.baseBurgeonHealPerTick ?? 2f;
        string desc = details
            ? $"Attacks inflict <color=green><b>Rejuvenating Seed</b></color> on the target. " +
              $"When the target is attacked by a plant, that plant is granted <color=green><b>Rejuvenating Burgeon</b></color>, " +
              $"healing <color=green><b>[({baseHealPT:F0}) + ({healpl:F0}/Lvl.)]</b></color> health every <color=green><b>{BurgeonTickInterval}s</b></color> for <color=green><b>{BurgeonDuration:F0}s</b></color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Burgeon</b></color> heal per tick by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=green><b>Rejuvenating Seed</b></color> refreshes its duration when the target takes <color=#4FC3F7><b>Water Damage</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float cdrpl = RData?.path3CooldownReductionPerLevel ?? 0.1f;
        string desc = GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Reduce <color=green><b>Base Skill Cooldown</b></color> by <color=green><b>{Mathf.RoundToInt(cdrpl)}s</b></color> per level. [<color=green><b>-{Mathf.RoundToInt(cdrpl * effectivePath3Level)}s</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, $"Upon reviving a plant, grant it <color=green><b>Verdant Guardian</b></color>, shielding it for <color=grey><b>{RData?.verdantGuardianShield ?? 200f:F0}</b></color> health and regenerating <color=green><b>{RData?.verdantGuardianRegen ?? 20f:F0}</b></color> health per second while the shield lasts, for <color=green><b>{skillDuration:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
