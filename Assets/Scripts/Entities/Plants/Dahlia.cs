using UnityEngine;
using System.Collections.Generic;

public class Dahlia : Shooter
{
    private DahliaData DData => data as DahliaData;

    private Insect _currentTarget;

    private const float SeedDuration = 8f;
    private const float BurstRadius  = 2.5f;

    public float BurgeonHeal         => (DData?.baseBurgeonHeal     ?? 10f) + effectivePath2Level * (DData?.path2HealPerLevel    ?? 5f);
    public float BurgeonDuration     => DData?.baseBurgeonDuration  ?? 4f;
    public float BurgeonTickInterval => DData?.burgeonTickInterval   ?? 0.5f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
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
        _currentTarget.ApplyEffect(new RejuvenatingSeedEffect(_currentTarget, SeedDuration, this));
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed  = data.baseAttackSpeed  + level * (DData?.path1AttackSpeedPerLevel  ?? 0.08f);
        baseHealingBonus = data.baseHealingBonus + level * (DData?.path1HealingBonusPerLevel ?? 0.03f);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillCooldown = data.baseSkillCooldown - level * (DData?.path3CooldownReductionPerLevel ?? 5f);
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
        Plant revived = Plant.RevivePlant(tile, DData?.revivalHealthPercent ?? 0.2f);
        if (revived != null && IsPath3Maxed)
        {
            float shield   = DData?.verdantGuardianShield   ?? 200f;
            float regen    = DData?.verdantGuardianRegen    ?? 20f;
            float duration = DData?.verdantGuardianDuration ?? 8f;
            revived.ApplyEffect(new VerdantGuardianEffect(revived, duration, this, shield, regen));
        }
    }

    public override string GetName() => $"<b><color=green>Dahlia</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} breathes life back into fallen allies, seeding the enemy with restorative energy.";

    public override string GetAttackDescription() =>
        $"Instantly deals <color=green><b>{attackDamage:F0}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the target.";

    public override string GetPassiveDescription()
    {
        float burgeonHealPerTick = BurgeonHeal / Mathf.Max(1, Mathf.RoundToInt(BurgeonDuration / BurgeonTickInterval));
        return $"Attacks inflict <color=green><b>Rejuvenating Seed</b></color> on the target. " +
               $"When the target is attacked by a plant, that plant is granted <color=green><b>Rejuvenating Burgeon</b></color>, " +
               $"healing <color=green><b>{burgeonHealPerTick:F1}</b></color> health every <color=green><b>{BurgeonTickInterval}s</b></color> " +
               $"for <color=green><b>{BurgeonDuration:F0}s</b></color>.";
    }

    public override string GetSkillDesription() =>
        $"Target a tile where a plant has fallen to resurrect it. The plant is revived with <color=green><b>{(DData?.revivalHealthPercent ?? 0.2f) * 100f:F0}%</b></color> of its maximum health.";

    public override string GetPath1Name() => "Verdance";
    public override string GetPath2Name() => "Seed";
    public override string GetPath3Name() => "Revival";

    public override string GetPath1Description(bool details = false)
    {
        float aspl = DData?.path1AttackSpeedPerLevel  ?? 0.08f;
        float hbpl = DData?.path1HealingBonusPerLevel ?? 0.03f;
        string desc = details
            ? $"Instantly deals <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage to the target."
            : GetAttackDescription();
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Healing Bonus</b></color> by <color=green><b>{hbpl * 100f:F0}%</b></color> per level. [<color=green><b>+{hbpl * effectivePath1Level * 100f:F0}%</b></color>]\n\n" +
               $"{Level5Section(path1Level, $"When an insect with <color=green><b>Rejuvenating Seed</b></color> dies, it bursts, spreading the seed to nearby insects within a <color=green><b>{BurstRadius}</b></color>-radius.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float healpl  = DData?.path2HealPerLevel   ?? 5f;
        float baseHeal = DData?.baseBurgeonHeal    ?? 10f;
        float dur      = BurgeonDuration;
        float interval = BurgeonTickInterval;
        float hpt      = BurgeonHeal / Mathf.Max(1, Mathf.RoundToInt(dur / interval));
        string desc = details
            ? $"Attacks inflict <color=green><b>Rejuvenating Seed</b></color> on the target. " +
              $"When the target is attacked by a plant, that plant is granted <color=green><b>Rejuvenating Burgeon</b></color>, " +
              $"healing <color=green><b>[({baseHeal:F0}) + ({healpl:F0}/Lvl.)]</b></color> total health over <color=green><b>{dur:F0}s</b></color>."
            : GetPassiveDescription();
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Burgeon</b></color> total heal by <color=green><b>{healpl:F0}</b></color> per level. [<color=green><b>+{healpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, $"<color=green><b>Rejuvenating Seed</b></color> refreshes its duration when the target takes <color=#4FC3F7><b>Water Damage</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float cdrpl = DData?.path3CooldownReductionPerLevel ?? 5f;
        string desc = details ? GetSkillDesription() : GetSkillDesription();
        return $"Skill:\n\n{desc}\n\n" +
               $"Reduce <color=green><b>Skill Cooldown</b></color> by <color=green><b>{cdrpl:F0}s</b></color> per level. [<color=green><b>-{cdrpl * effectivePath3Level:F0}s</b></color>]\n\n" +
               $"{Level5Section(path3Level, $"Upon reviving a plant, grant it <color=green><b>Verdant Guardian</b></color>: a shield of <color=green><b>{DData?.verdantGuardianShield ?? 200f:F0}</b></color> health that regenerates <color=green><b>{DData?.verdantGuardianRegen ?? 20f:F0}</b></color> health per second while it lasts, for <color=green><b>{DData?.verdantGuardianDuration ?? 8f:F0}s</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
