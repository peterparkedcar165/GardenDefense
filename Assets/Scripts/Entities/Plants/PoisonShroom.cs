using UnityEngine;
using System.Collections.Generic;

public class Cordyceps : Shooter
{
    private PoisonShroomData PSData => data as PoisonShroomData;
    public float PoisonBaseDPS => PSData?.basePoisonDPS ?? 0f;

    public int invertLevel = 1;
    public float invertDuration;
    public float activeRadius;
    [SerializeField] private GameObject poisonBlobPrefab;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        activeRadius = data.baseSkillRadius;
    }

    protected override void Update()
    {
        base.Update();
    }

    protected override void Shoot(Vector3 target)
    {
        GameObject projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        CordycepsProjectile puff = projectile.GetComponent<CordycepsProjectile>();
        if (puff != null)
        {
            puff.SetTarget(FindTarget());
            puff.Initialize(target, attackDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, this);
        }
    }

    protected override GameObject FindTarget()
    {
        List<Insect> regenerating = new List<Insect>();
        List<Insect> fallback     = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            float distance = Vector3.Distance(transform.position, insect.transform.position);
            if (distance > attackRange || !IsValidNightTarget(insect, distance)) continue;
            if (insect.HasEffect<RegenerationEffect>())
                regenerating.Add(insect);
            else
                fallback.Add(insect);
        }
        if (regenerating.Count > 0) return FindFirst(regenerating);
        if (fallback.Count > 0)     return FindFirst(fallback);
        return base.FindTarget();
    }

    public override void OnPath1Upgrade(int level)
    {
        baseAttackSpeed = data.baseAttackSpeed + level * (PSData?.path1AttackSpeedPerLevel ?? 0.08f);
        baseAttackRange = data.baseAttackRange + level * (PSData?.path1AttackRangePerLevel ?? 0.1f);
    }

    public override void OnPath2Upgrade(int level) { }

    public override void UpdateStats()
    {
        base.UpdateStats();
        float durpl = PSData?.path2PoisonDurationPerLevel ?? 1f;
        invertLevel    = 1 + path2Level;
        invertDuration = ((PSData?.basePoisonDuration ?? 0f) + durpl * path2Level) * (1 + passiveDuration);
    }

    public override void OnPath3Upgrade(int level)
    {
        baseSkillDuration = data.baseSkillDuration + (PSData?.path3SkillDurationPerLevel ?? 1f) * level;
        activeRadius      = data.baseSkillRadius   + (PSData?.path3RadiusPerLevel        ?? 0.2f) * level;
    }

    public override void ActivateSkill()
    {
        SkillTargetingManager.instance.BeginTargeting(activeRadius, OnTargetConfirmed);
    }

    private void OnTargetConfirmed(Vector3 position)
    {
        if (poisonBlobPrefab == null) return;
        skillCooldownTimer = skillCooldown;
        float fieldDPS = PoisonBaseDPS + skillDamageMultiplier * magicPower;
        GameObject obj = Instantiate(poisonBlobPrefab, transform.position, Quaternion.identity);
        PoisonBlob blob = obj.GetComponent<PoisonBlob>();
        if (blob != null)
            blob.Initialize(position, activeRadius, skillDuration, this, fieldDPS);
    }

    public override string GetName() => $"<b><color=purple>{(data != null ? data.displayName : "Cordyceps")}</color></b>";

    public override string GetDescription() =>
        $"The {GetName()} shoots parasitic spores, dealing damage and inflicting <color=#9B30D0><b>Decay</b></color> — converting any healing the target receives into <color=purple>Poison</color> damage.";

    public override string GetPath1Description(bool details = false)
    {
        float aspl    = PSData?.path1AttackSpeedPerLevel ?? 0.08f;
        float rangepl = PSData?.path1AttackRangePerLevel ?? 0.1f;
        string desc = details
            ? $"Fires parasitic spores at the target, dealing <color=green><b>[100% Attack Damage]</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage."
            : $"Fires parasitic spores at the target, dealing <color=green><b>{attackDamage}</b></color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage.";
        return $"Attack:\n\n{desc}\n\n" +
               $"Increase <color=green><b>Base Attack Speed</b></color> by <color=green><b>{aspl:F2}</b></color> per level. [<color=green><b>+{aspl * effectivePath1Level:F2}</b></color>]\n\n" +
               $"Increase <color=green><b>Base Attack Range</b></color> by <color=green><b>{rangepl:F1}</b></color> per level. [<color=green><b>+{rangepl * effectivePath1Level:F1}</b></color>]\n\n" +
               $"{Level5Section(path1Level, "Attacks splash onto nearby insects within a <color=green><b>1.5</b></color> radius, dealing the same damage and applying <color=#9B30D0><b>Decay</b></color>.")}\n\n" +
               $"Level: [<color=green><b>{path1Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath1Level - path1Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath2Description(bool details = false)
    {
        float durpl = PSData?.path2PoisonDurationPerLevel ?? 1f;
        string desc = details
            ? $"Attacks apply <color=#9B30D0><b>Decay</b></color> for <color=green><b>[({PSData?.basePoisonDuration ?? 0f:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds."
            : $"Attacks apply <color=#9B30D0><b>Decay</b></color> for <color=green><b>{invertDuration:F0}</b></color> seconds.";
        return $"Passive:\n\n{desc}\n\n" +
               $"Increase <color=#9B30D0>Decay</color> duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath2Level:F0}</b></color>]\n\n" +
               $"{Level5Section(path2Level, "<color=#9B30D0><b>Decay</b></color> also applies to insects adjacent to the primary target.")}\n\n" +
               $"Level: [<color=green><b>{path2Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath2Level - path2Level})</b></color>\n\n" +
               ShiftHint(details);
    }

    public override string GetPath3Description(bool details = false)
    {
        float durpl    = PSData?.path3SkillDurationPerLevel ?? 1f;
        float radiuspl = PSData?.path3RadiusPerLevel        ?? 0.2f;
        string desc = details
            ? $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>[({data.baseSkillRadius:F1}) + ({radiuspl:F1}/Lvl.)]</b></color> radius that lasts <color=green><b>[({data.baseSkillDuration:F0}) + ({durpl:F0}/Lvl.)]</b></color> seconds. Insects inside take <color=green><b>{PoisonBaseDPS:F0}</b></color> <color=#FFB6C1>[+{skillDamageMultiplier * 100f:F0}% Magic Power]</color> {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second, and all debuffs on them are frozen in time."
            : $"Hurls a toxic blob towards a targeted area, creating a poison field with a <color=green><b>{activeRadius:F1}</b></color> radius that lasts <color=green><b>{skillDuration:F0}</b></color> seconds. Insects inside take <color=green><b>{PoisonBaseDPS:F0}</b></color> [<color=#FFB6C1><b>+{skillDamageMultiplier * magicPower:F0}</b></color>] {PlantData.ElementalTag(elementalType)} {PlantData.DamageTypeTag(damageType)} damage per second, and all debuffs on them are frozen in time.";
        return $"Skill:\n\n{desc}\n\n" +
               $"Increase field duration by <color=green><b>{durpl:F0}</b></color> second per level. [<color=green><b>+{durpl * effectivePath3Level:F0}</b></color>]\n\n" +
               $"Increase field radius by <color=green><b>{radiuspl:F1}</b></color> per level. [<color=green><b>+{radiuspl * effectivePath3Level:F1}</b></color>]\n\n" +
               $"{SkillCooldownLine()}\n\n" +
               $"{Level5Section(path3Level, "The <color=purple><b>Poison Field</b></color> applies <color=#9B30D0><b>Decay</b></color> to all insects on each tick.")}\n\n" +
               $"Level: [<color=green><b>{path3Level}/{pathLevelCap}</b></color>] <color=green><b>(+{effectivePath3Level - path3Level})</b></color>\n\n" +
               ShiftHint(details);
    }
}
